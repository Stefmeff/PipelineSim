using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/**
 * @brief A placed custom chip in the Sandbox.
 *
 * @details Loads a ChipDefinition by chipName, dynamically creates external pins
 * matching the chip's interface, and builds a visual representation (body, labels, pins).
 *
 * The internal circuit exists as pure data objects (no GameObjects) — JSON deserialization
 * preserves all wire/pin references, and components subscribe to the Timer in their constructors.
 * Signal flow: external InputPin → ChipInputNode.InjectValue() → internal wire/pin chain
 * → ChipOutputNode.input.NewDataEvent → external OutputPin.SetValue().
 **/
public class CustomChip : CircuitComponent
{
    [JsonProperty] public string chipName;                                    /**< chipId used to load the ChipDefinition */
    [JsonProperty] public List<InputPin> inputs = new List<InputPin>();       /**< external input pins (wired in Sandbox) */
    [JsonProperty] public List<OutputPin> outputs = new List<OutputPin>();    /**< external output pins (wired in Sandbox) */

    [JsonIgnore] private ChipDefinition chipDef;  /**< loaded chip definition (.chip file) */
    [JsonIgnore] private TimeTick timer;

    /** @name Internal circuit — pure data, no GameObjects @{ */
    [JsonIgnore] private List<ChipInputNode> internalInputNodes = new List<ChipInputNode>();
    [JsonIgnore] private List<ChipOutputNode> internalOutputNodes = new List<ChipOutputNode>();
    [JsonIgnore] private List<CircuitComponent> internalComponents = new List<CircuitComponent>();
    /** @} */

    /** @name Delay visualization @{ */
    [JsonIgnore] private GameObject delayVisualizer;
    [JsonIgnore] private List<SignalEntry> signalQueue = new List<SignalEntry>();
    [JsonIgnore] private List<BitToken> lastInputs = new List<BitToken>();
    /** @} */

    [JsonIgnore] public static string chipNameToSpawn; /**< set before instantiation to specify which chip to create */

    /**
     * @brief Static stack tracking the ancestor chain of chips currently being loaded.
     *
     * @details Used as a guard against circular dependencies during InitInternalCircuit().
     * A chip name is pushed before recursing into its internal circuit and popped after.
     * If a chip name already exists in the stack, it means the chip is trying to load
     * itself through a chain of nested chips — a circular dependency.
     * Multiple sibling instances of the same chip are allowed (they don't overlap on the stack).
     **/
    [JsonIgnore] private static List<string> loadingAncestors = new List<string>();

    private static JsonSerializerSettings jsonSettings = new JsonSerializerSettings
    {
        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
        TypeNameHandling = TypeNameHandling.Auto,
        Formatting = Formatting.Indented
    };

    /**
     * @brief Constructor — loads the ChipDefinition and creates pins if chipName is set.
     *
     * @details chipName can be set either via the static chipNameToSpawn (for new placements)
     * or by JSON deserialization (for loading saved circuits). In the deserialization case,
     * chipName is set AFTER the constructor runs, so EnsureChipDefLoaded() handles deferred loading.
     **/
    public CustomChip()
    {
        if (!string.IsNullOrEmpty(chipNameToSpawn))
        {
            chipName = chipNameToSpawn;
            chipNameToSpawn = null;
        }

        if (!string.IsNullOrEmpty(chipName))
        {
            chipDef = ChipDefinition.Load(chipName);
            if (chipDef != null) CreatePins();
        }

        Subscribe();
    }

    /**
     * @brief Creates external input/output pins matching the ChipDefinition's interface.
     **/
    private void CreatePins()
    {
        inputs.Clear();
        outputs.Clear();

        foreach (InterfacePin pinDef in chipDef.inputs)
        {
            InputPin pin = new InputPin();
            pin.width = pinDef.width;
            inputs.Add(pin);
        }

        foreach (InterfacePin pinDef in chipDef.outputs)
        {
            OutputPin pin = new OutputPin();
            pin.width = pinDef.width;
            outputs.Add(pin);
        }
    }

    private void Subscribe()
    {
        GameObject o = GameObject.FindWithTag("Timer");
        timer = o.GetComponent<TimeTick>();
        timer.TimerTickEvent += OnTimerTick;
    }

    /**
     * @brief Ensures chipDef is loaded and pins are created.
     *
     * @details Needed because JSON deserialization sets chipName AFTER the constructor runs.
     * Called lazily before any operation that needs chipDef.
     **/
    private void EnsureChipDefLoaded()
    {
        if (chipDef == null && !string.IsNullOrEmpty(chipName))
        {
            chipDef = ChipDefinition.Load(chipName);
            if (chipDef == null)
            {
                InformationWindow.Show("Missing Chip Definition",
                    "The custom chip \"" + chipName + "\" could not be loaded. "
                    + "The .chip file may have been deleted or moved.\n\n"
                    + "Components referencing this chip will not simulate.");
                return;
            }
            if (inputs.Count == 0 && outputs.Count == 0)
            {
                CreatePins();
            }
        }
    }

    /**
     * @brief Loads the internal circuit as pure data objects (no GameObjects).
     *
     * @details Deserializes the internal circuit JSON from the ChipDefinition. All wire/pin
     * references are preserved by Newtonsoft's reference handling. Components subscribe to
     * the Timer in their constructors during deserialization. No Load() is called, so no
     * GameObjects are created and no ProjectManager registration occurs.
     *
     * After deserialization, external input pins are wired to internal ChipInputNodes,
     * and internal ChipOutputNodes are wired to external output pins via event subscriptions.
     * Nested custom chips are initialized recursively.
     *
     * A static HashSet tracks which chips are currently being loaded across the entire
     * recursion tree to detect circular references at load time (safety net for corrupt files).
     * Entries are added before recursing and removed after returning.
     **/
    public void InitInternalCircuit()
    {
        EnsureChipDefLoaded();
        if (chipDef == null || string.IsNullOrEmpty(chipDef.internalCircuit)) return;

        // Load-time circular dependency guard — checks ancestor chain, not siblings
        if (loadingAncestors.Contains(chipName))
        {
            string chain = string.Join(" -> ", loadingAncestors) + " -> " + chipName;
            Debug.LogWarning("Circular dependency detected at load time: " + chain);
            try { InformationWindow.Show("Circular Dependency",
                "A circular reference was detected. This chip will not simulate."); }
            catch { /* UI may not exist in pure-data context */ }
            return;
        }
        loadingAncestors.Add(chipName);

        try
        {
            InitInternalCircuitInner();
        }
        finally
        {
            // Always remove from ancestor stack, even if an exception occurs
            loadingAncestors.RemoveAt(loadingAncestors.Count - 1);
        }
    }

    /**
     * @brief Inner implementation of InitInternalCircuit, called after the circular dependency
     * guard has passed.
     *
     * @details Deserializes the internal circuit JSON, categorizes components, wires up
     * external pins to internal ChipInputNodes/ChipOutputNodes, and recursively initializes
     * nested custom chips.
     **/
    private void InitInternalCircuitInner()
    {
        List<CircuitComponent> elements;
        try
        {
            elements = JsonConvert.DeserializeObject<List<CircuitComponent>>(
                chipDef.internalCircuit, jsonSettings);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to load internal circuit of chip \"" + chipName + "\": " + e.Message);
            InformationWindow.Show("Failed to Load Chip Circuit",
                "The internal circuit of chip \"" + chipName + "\" could not be loaded. "
                + "It may be corrupted or from an incompatible version.\n\n" + e.Message);
            return;
        }

        if (elements == null) return;

        internalComponents = elements;
        internalInputNodes.Clear();
        internalOutputNodes.Clear();

        // Categorize and find nested custom chips
        foreach (CircuitComponent comp in elements)
        {
            if (comp is ChipInputNode cin)
                internalInputNodes.Add(cin);
            else if (comp is ChipOutputNode cout)
                internalOutputNodes.Add(cout);
            else if (comp is CustomChip nestedChip)
                nestedChip.InitInternalCircuit(); // recursive — guard is in the public method
        }

        // Sort by pin order (matching chipDef pin order)
        internalInputNodes.Sort((a, b) => {
            int idxA = chipDef.inputs.FindIndex(p => p.name == a.pinName);
            int idxB = chipDef.inputs.FindIndex(p => p.name == b.pinName);
            return idxA.CompareTo(idxB);
        });
        internalOutputNodes.Sort((a, b) => {
            int idxA = chipDef.outputs.FindIndex(p => p.name == a.pinName);
            int idxB = chipDef.outputs.FindIndex(p => p.name == b.pinName);
            return idxA.CompareTo(idxB);
        });

        // Wire external inputs → internal ChipInputNode outputs
        for (int i = 0; i < Mathf.Min(inputs.Count, internalInputNodes.Count); i++)
        {
            int idx = i;
            inputs[idx].NewDataEvent += () =>
            {
                if (internalInputNodes == null || inputs == null) return;
                if (idx >= internalInputNodes.Count || idx >= inputs.Count) return;
                internalInputNodes[idx].InjectValue(inputs[idx].data);
            };
        }

        // Wire internal ChipOutputNode → external outputs
        for (int i = 0; i < Mathf.Min(outputs.Count, internalOutputNodes.Count); i++)
        {
            int idx = i;
            internalOutputNodes[idx].input.NewDataEvent += () =>
            {
                if (internalOutputNodes == null || outputs == null) return;
                if (idx >= internalOutputNodes.Count || idx >= outputs.Count) return;
                outputs[idx].SetValue(internalOutputNodes[idx].input.data);
            };
        }

        // Init lastInputs for change detection
        lastInputs.Clear();
        for (int i = 0; i < inputs.Count; i++)
            lastInputs.Add(new BitToken());
    }

    /**
     * @brief Per-tick handler for delay visualization on the custom chip body.
     *
     * @details Detects input changes, creates visual squares in the delay visualizer,
     * and removes them once the signal has passed through the chip's propagation delay.
     *
     * @param[in] tick the current simulation tick
     **/
    private void OnTimerTick(int tick)
    {
        if (delayVisualizer == null) return;

        int delay = GetDelay();

        // Init signal queue with sentinel if empty
        if (signalQueue.Count == 0)
        {
            BitToken init = new BitToken();
            GameObject square = DelayHandler.NewSquare(100, init.ActiveColor(), delayVisualizer, 1);
            signalQueue.Add(new SignalEntry(init, square));
        }

        // Detect input changes → add visual square
        for (int i = 0; i < inputs.Count; i++)
        {
            if (i >= lastInputs.Count) break;
            BitToken current = inputs[i].data;
            if (!current.EqualsToken(lastInputs[i]))
            {
                lastInputs[i] = current;
                GameObject square = DelayHandler.NewSquare(0, current.ActiveColor(), delayVisualizer, tick);
                signalQueue.Add(new SignalEntry(current, square));
            }
        }

        // Check if next signal has passed through
        if (signalQueue.Count > 1)
        {
            BitToken nextOut = signalQueue[1].token;
            int arrivalTime = nextOut.GetTime();

            if (arrivalTime + delay <= tick)
            {
                DelayHandler.ReturnSquare(signalQueue[0].visual);
                signalQueue.RemoveAt(0);
            }
        }

        // Draw squares
        DelayHandler.DrawSquares(signalQueue, delay, tick);
    }

    public override void Reset()
    {
        // Reset all internal components first (clears their lastDataIn etc.)
        foreach (CircuitComponent comp in internalComponents)
        {
            comp.Reset();
        }

        // Clear visual squares
        foreach (SignalEntry t in signalQueue)
        {
            DelayHandler.ReturnSquare(t.visual);
        }
        signalQueue.Clear();

        // Reset last input tracking
        lastInputs.Clear();
        for (int i = 0; i < inputs.Count; i++)
            lastInputs.Add(new BitToken());

        foreach (OutputPin pin in outputs)
        {
            pin.SetValue(new BitToken(pin.width));
        }
    }

    /**
     * @brief Instantiates the chip prefab, builds the visual, and initializes the internal circuit.
     *
     * @return the ComponentMono attached to the instantiated GameObject
     **/
    public override Component Load()
    {
        // Force reload from disk (picks up edits made in ChipEditor)
        chipDef = ChipDefinition.Load(chipName);
        if (chipDef != null) ReconcilePins();

        GameObject prefab = Resources.Load("Prefabs/CustomChip") as GameObject;
        GameObject o = GameObject.Instantiate(prefab, pos, rot);
        ComponentMono c = o.GetComponent<ComponentMono>();
        c.Load(this); // calls LoadEditor, BuildVisual, InitInternalCircuit
        Subscribe();
        return c;
    }

    /// <summary>
    /// Reconciles deserialized (old) pins with the current chip definition.
    /// Added pins get created unconnected. Removed pins have their wires disconnected.
    /// Reordered pins keep existing wire connections as-is.
    /// </summary>
    private void ReconcilePins()
    {
        if (chipDef == null) return;
        if (inputs.Count == chipDef.inputs.Count && outputs.Count == chipDef.outputs.Count)
        {
            // Same count — just update widths
            for (int i = 0; i < inputs.Count; i++)
                inputs[i].width = chipDef.inputs[i].width;
            for (int i = 0; i < outputs.Count; i++)
                outputs[i].width = chipDef.outputs[i].width;
            return;
        }

        // Snapshot old pins
        List<InputPin> oldInputs = new List<InputPin>(inputs);
        List<OutputPin> oldOutputs = new List<OutputPin>(outputs);
        inputs.Clear();
        outputs.Clear();

        // Reconcile inputs: reuse existing, create new for added, disconnect removed
        for (int i = 0; i < chipDef.inputs.Count; i++)
        {
            if (i < oldInputs.Count)
            {
                oldInputs[i].width = chipDef.inputs[i].width;
                inputs.Add(oldInputs[i]);
            }
            else
            {
                InputPin pin = new InputPin();
                pin.width = chipDef.inputs[i].width;
                inputs.Add(pin);
            }
        }
        for (int i = chipDef.inputs.Count; i < oldInputs.Count; i++)
            oldInputs[i].disconnectWire();

        // Reconcile outputs: reuse existing, create new for added, disconnect removed
        for (int i = 0; i < chipDef.outputs.Count; i++)
        {
            if (i < oldOutputs.Count)
            {
                oldOutputs[i].width = chipDef.outputs[i].width;
                outputs.Add(oldOutputs[i]);
            }
            else
            {
                OutputPin pin = new OutputPin();
                pin.width = chipDef.outputs[i].width;
                outputs.Add(pin);
            }
        }
        for (int i = chipDef.outputs.Count; i < oldOutputs.Count; i++)
            oldOutputs[i].disconnectWire();
    }

    /**
     * @brief Builds the chip's visual representation: body sprite, border, pins, labels, delay visualizer.
     *
     * @details Dynamically sizes the chip body based on the number of pins. Input pins are placed
     * on the left, output pins on the right. A delay visualizer bar is added beneath the body
     * if the chip has a propagation delay > 0.
     *
     * @param[in] root the instantiated chip prefab GameObject to attach visuals to
     **/
    public void BuildVisual(GameObject root)
    {
        EnsureChipDefLoaded();
        if (chipDef == null) return;

        float grid = 5f;
        float pinSpacing = 10f;
        float topPadding = grid;
        float bottomPadding = grid;
        int maxPins = Mathf.Max(chipDef.inputs.Count, chipDef.outputs.Count, 1);
        float chipWidth = 7 * grid;
        float chipHeight = topPadding + bottomPadding + (maxPins - 1) * pinSpacing;

        // Create grey border sprite
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(root.transform, false);
        borderObj.transform.localPosition = Vector3.zero;
        borderObj.transform.localScale = new Vector3(chipWidth, chipHeight, 1);
        SpriteRenderer border = borderObj.AddComponent<SpriteRenderer>();
        border.sprite = CreateSquareSprite();
        border.color = new Color32(0x78, 0x85, 0x8D, 0xFF);
        border.sortingOrder = 1;

        // Create green inner surface
        float borderSize = 1f;
        GameObject innerObj = new GameObject("Inner");
        innerObj.transform.SetParent(root.transform, false);
        innerObj.transform.localPosition = Vector3.zero;
        innerObj.transform.localScale = new Vector3(chipWidth - borderSize * 2, chipHeight - borderSize * 2, 1);
        SpriteRenderer inner = innerObj.AddComponent<SpriteRenderer>();
        inner.sprite = CreateSquareSprite();
        inner.color = new Color32(0x2A, 0x3A, 0x2E, 0xFF);
        inner.sortingOrder = 2;

        // Resize collider
        BoxCollider2D col = root.GetComponent<BoxCollider2D>();
        if (col == null) col = root.AddComponent<BoxCollider2D>();
        col.size = new Vector2(chipWidth, chipHeight);

        // Description label for Draggable2D (shows chip name on hover)
        Draggable2D draggable = root.GetComponent<Draggable2D>();
        if (draggable != null)
        {
            GameObject descObj = new GameObject("Description");
            descObj.transform.SetParent(root.transform, false);
            descObj.transform.localPosition = new Vector3(0, chipHeight / 2f + grid, 0);
            TextMeshPro descTmp = descObj.AddComponent<TextMeshPro>();
            descTmp.text = chipDef.name;
            descTmp.fontSize = 36;
            descTmp.alignment = TextAlignmentOptions.Center;
            descTmp.color = new Color32(0xAF, 0xD6, 0x9E, 0xFF);
            descTmp.sortingOrder = 3;
            TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF")
                ?? TMP_Settings.defaultFontAsset;
            if (font != null) descTmp.font = font;
            RectTransform descRt = descObj.GetComponent<RectTransform>();
            descRt.sizeDelta = new Vector2(chipWidth * 2f, chipHeight);
            descObj.SetActive(false);
            draggable.description = descObj;
        }

        // Delay visualizer — beneath the chip body
        if (chipDef.delay > 0)
        {
            GameObject vizPrefab = Resources.Load("Prefabs/DelayVisualizer") as GameObject;
            if (vizPrefab != null)
            {
                GameObject vizObj = GameObject.Instantiate(vizPrefab, root.transform);
                vizObj.name = "DelayVisualizer";
                vizObj.transform.localPosition = new Vector3(0, -chipHeight / 2f - 2.5f, 0);
                vizObj.transform.localScale = new Vector3(chipWidth * 0.7f, 1.5f, 1);
                vizObj.SetActive(true);
                delayVisualizer = vizObj;
            }
        }

        // First pin Y
        float firstPinY = chipHeight / 2f - topPadding;

        // Spawn input pins
        GameObject inputPinPrefab = Resources.Load("Prefabs/InputPin") as GameObject;
        for (int i = 0; i < chipDef.inputs.Count; i++)
        {
            float pinY = firstPinY - i * pinSpacing;
            Vector3 pinPos = new Vector3(-chipWidth / 2f - grid * 0.25f, pinY, 0);

            GameObject pinObj = GameObject.Instantiate(inputPinPrefab, root.transform);
            pinObj.transform.localPosition = pinPos;
            pinObj.transform.localScale = new Vector3(4f, 4f, 1);

            InputPin_Mono pinMono = pinObj.GetComponentInChildren<InputPin_Mono>();
            if (pinMono != null) pinMono.Init(inputs[i]);

            float innerLeft = -chipWidth / 2f + borderSize;
            string inLabel = chipDef.inputs[i].width > 1
                ? chipDef.inputs[i].name + "[" + chipDef.inputs[i].width + "]"
                : chipDef.inputs[i].name;
            CreateLabel(root.transform, inLabel,
                new Vector3(innerLeft + (chipWidth - borderSize * 2) * 0.25f, pinY, 0),
                (chipWidth - borderSize * 2) * 0.45f, pinSpacing, TextAlignmentOptions.Left);
        }

        // Spawn output pins
        GameObject outputPinPrefab = Resources.Load("Prefabs/OutputPin") as GameObject;
        for (int i = 0; i < chipDef.outputs.Count; i++)
        {
            float pinY = firstPinY - i * pinSpacing;
            Vector3 pinPos = new Vector3(chipWidth / 2f + grid * 0.25f, pinY, 0);

            GameObject pinObj = GameObject.Instantiate(outputPinPrefab, root.transform);
            pinObj.transform.localPosition = pinPos;
            pinObj.transform.localScale = new Vector3(4f, 4f, 1);

            OutputPin_Mono pinMono = pinObj.GetComponentInChildren<OutputPin_Mono>();
            if (pinMono != null)
            {
                pinMono.Init(outputs[i]);
                if (pinMono.wirePrefab == null)
                    pinMono.wirePrefab = Resources.Load("Prefabs/Wire") as GameObject;
            }

            float innerRight = chipWidth / 2f - borderSize;
            string outLabel = chipDef.outputs[i].width > 1
                ? chipDef.outputs[i].name + "[" + chipDef.outputs[i].width + "]"
                : chipDef.outputs[i].name;
            CreateLabel(root.transform, outLabel,
                new Vector3(innerRight - (chipWidth - borderSize * 2) * 0.25f, pinY, 0),
                (chipWidth - borderSize * 2) * 0.45f, pinSpacing, TextAlignmentOptions.Right);
        }
    }

    /**
     * @brief Creates a TextMeshPro label as a child of the given parent.
     *
     * @param[in] parent transform to parent the label under
     * @param[in] text label text
     * @param[in] localPos local position relative to parent
     * @param[in] width label rect width
     * @param[in] height label rect height
     * @param[in] alignment text alignment
     **/
    private void CreateLabel(Transform parent, string text, Vector3 localPos, float width, float height, TextAlignmentOptions alignment)
    {
        GameObject labelObj = new GameObject("Label_" + text);
        labelObj.transform.SetParent(parent, false);
        labelObj.transform.localPosition = localPos;

        TextMeshPro tmp = labelObj.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = 24;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.sortingOrder = 3;

        RectTransform rt = labelObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);
    }

    /**
     * @brief Creates a 1x1 white sprite for use as chip body rectangles.
     *
     * @return a new white square Sprite
     **/
    private Sprite CreateSquareSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
    }

    public override void LoadPins(InputPin_Mono[] inPins, OutputPin_Mono[] outPins)
    {
        // Pins are created dynamically in BuildVisual
    }

    public override void CollectPins(List<InputPin> inputs, List<OutputPin> outputs)
    {
        inputs.AddRange(this.inputs);
        outputs.AddRange(this.outputs);
    }

    public override int GetDelay()
    {
        EnsureChipDefLoaded();
        return chipDef != null ? chipDef.delay : 0;
    }

    public int GetMinDelay()
    {
        EnsureChipDefLoaded();
        return chipDef != null ? chipDef.minDelay : 0;
    }

    public override void LoadDelay(GameObject delayVisualizer) { }

    [JsonIgnore] private GameObject editor;

    public void LoadEditor()
    {
        GameObject o = GameObject.FindWithTag("Editor");
        if (o != null) editor = o.transform.Find("FunctionBlockEditor")?.gameObject;
    }

    public override void OpenEditor()
    {
        if (editor == null) LoadEditor();
        if (editor == null) return;
        editor.SetActive(true);
        FunctionBlockEditor e = editor.GetComponent<FunctionBlockEditor>();
        if (e != null) e.init(this);
    }

    /**
     * @brief Cleans up timer subscription and disposes all internal circuit components.
     *
     * @details Unsubscribes internal components from the Timer to prevent memory leaks
     * when the chip is removed from the Sandbox.
     **/
    public override void Dispose()
    {
        if (timer != null) timer.TimerTickEvent -= OnTimerTick;

        foreach (CircuitComponent comp in internalComponents)
        {
            try { comp.Dispose(); } catch { /* internal components may lack GameObjects */ }
        }
        internalComponents.Clear();
        internalInputNodes.Clear();
        internalOutputNodes.Clear();
    }
}

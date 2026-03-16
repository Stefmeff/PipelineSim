using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// A placed custom chip in the Sandbox. Loads a ChipDefinition by name,
/// dynamically creates pins and visual layout.
/// Internal circuit exists as pure data objects (no GameObjects) — signals
/// propagate through deserialized pin/wire references.
/// </summary>
public class CustomChip : CircuitComponent
{
    [JsonProperty] public string chipName;
    [JsonProperty] public List<InputPin> inputs = new List<InputPin>();
    [JsonProperty] public List<OutputPin> outputs = new List<OutputPin>();

    [JsonIgnore] private ChipDefinition chipDef;
    [JsonIgnore] private TimeTick timer;

    // Internal circuit — pure data, no GameObjects
    [JsonIgnore] private List<ChipInputNode> internalInputNodes = new List<ChipInputNode>();
    [JsonIgnore] private List<ChipOutputNode> internalOutputNodes = new List<ChipOutputNode>();
    [JsonIgnore] private List<CircuitComponent> internalComponents = new List<CircuitComponent>();

    // Delay visualization
    [JsonIgnore] private GameObject delayVisualizer;
    [JsonIgnore] private List<Tuple<BitToken, GameObject>> signalQueue = new List<Tuple<BitToken, GameObject>>();
    [JsonIgnore] private List<BitToken> lastInputs = new List<BitToken>();

    [JsonIgnore] public static string chipNameToSpawn;

    private static JsonSerializerSettings jsonSettings = new JsonSerializerSettings
    {
        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
        TypeNameHandling = TypeNameHandling.Auto,
        Formatting = Formatting.Indented
    };

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

    /// <summary>
    /// Ensures chipDef is loaded and pins are created.
    /// Needed because JSON deserialization sets chipName AFTER constructor runs.
    /// </summary>
    private void EnsureChipDefLoaded()
    {
        if (chipDef == null && !string.IsNullOrEmpty(chipName))
        {
            chipDef = ChipDefinition.Load(chipName);
            if (chipDef != null && inputs.Count == 0 && outputs.Count == 0)
            {
                CreatePins();
            }
        }
    }

    /// <summary>
    /// Loads the internal circuit as pure data objects (no GameObjects).
    /// JSON deserialization preserves all wire connections between pins.
    /// Components subscribe to Timer in their constructors during deserialization.
    /// </summary>
    public void InitInternalCircuit()
    {
        EnsureChipDefLoaded();
        if (chipDef == null || string.IsNullOrEmpty(chipDef.internalCircuit)) return;

        // Deserialize internal components — constructors run, Timer subscriptions happen,
        // wire/pin references are preserved. No Load() = no GameObjects = no ProjectManager.
        List<CircuitComponent> elements = JsonConvert.DeserializeObject<List<CircuitComponent>>(
            chipDef.internalCircuit, jsonSettings);

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
                nestedChip.InitInternalCircuit(); // recursive — also calls EnsureChipDefLoaded
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

    private void OnTimerTick(int tick)
    {
        if (delayVisualizer == null) return;

        int delay = GetDelay();

        // Init signal queue with sentinel if empty
        if (signalQueue.Count == 0)
        {
            BitToken init = new BitToken();
            GameObject square = DelayHandler.NewSquare(100, init.ActiveColor(), delayVisualizer, 1);
            signalQueue.Add(Tuple.Create(init, square));
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
                signalQueue.Add(Tuple.Create(current, square));
            }
        }

        // Check if next signal has passed through
        if (signalQueue.Count > 1)
        {
            BitToken nextOut = signalQueue[1].Item1;
            int arrivalTime = nextOut.GetTime();

            if (arrivalTime + delay <= tick)
            {
                if (signalQueue[0].Item2 != null) GameObject.Destroy(signalQueue[0].Item2);
                signalQueue.RemoveAt(0);
            }
        }

        // Draw squares
        DelayHandler.DrawSquares(signalQueue, delay, tick);
    }

    public override void Reset()
    {
        // Clear visual squares
        foreach (Tuple<BitToken, GameObject> t in signalQueue)
        {
            if (t.Item2 != null) GameObject.Destroy(t.Item2);
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

    public override Component Load()
    {
        EnsureChipDefLoaded();

        GameObject prefab = Resources.Load("Prefabs/CustomChip") as GameObject;
        GameObject o = GameObject.Instantiate(prefab, pos, rot);
        ComponentMono c = o.GetComponent<ComponentMono>();
        c.Load(this);
        Subscribe();
        BuildVisual(o);
        InitInternalCircuit();
        return c;
    }

    /// <summary>
    /// Builds the chip's visual: body sprite as child, pins at world positions, labels.
    /// </summary>
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
            CreateLabel(root.transform, chipDef.inputs[i].name,
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
            CreateLabel(root.transform, chipDef.outputs[i].name,
                new Vector3(innerRight - (chipWidth - borderSize * 2) * 0.25f, pinY, 0),
                (chipWidth - borderSize * 2) * 0.45f, pinSpacing, TextAlignmentOptions.Right);
        }
    }

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

    public override void LoadDelay(GameObject delayVisualizer) { }

    public override void OpenEditor() { }

    public override void Dispose()
    {
        // Unsubscribe from timer
        if (timer != null) timer.TimerTickEvent -= OnTimerTick;

        // Unsubscribe internal components from Timer to prevent memory leaks
        foreach (CircuitComponent comp in internalComponents)
        {
            try { comp.Dispose(); } catch { /* internal components may lack GameObjects */ }
        }
        internalComponents.Clear();
        internalInputNodes.Clear();
        internalOutputNodes.Clear();
    }
}

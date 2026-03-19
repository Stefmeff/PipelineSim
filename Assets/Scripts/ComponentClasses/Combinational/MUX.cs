using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/**
 * @brief N-input multiplexer. Selects one of N data inputs based on a select bus.
 *
 * @details The select input is a bus of width ceil(log2(N)). The selected data input
 * is passed through to the output. Data inputs accept any bit width.
 * Default: 2-input MUX (1-bit select).
 *
 * Visual: classical trapezoid shape — wide on the input side, narrow on the output side,
 * select input at the bottom.
 **/
public class MUX : CircuitComponent
{
    [JsonProperty] private List<InputPin> dataIns = new List<InputPin>();
    [JsonProperty] private InputPin select;
    [JsonProperty] private OutputPin dataOut;
    [JsonProperty] public int nInputs = 2;

    [JsonIgnore] private TimeTick timer;
    [JsonIgnore] private List<BitToken> lastDataIns = new List<BitToken>();
    [JsonIgnore] private BitToken lastSelect = new BitToken();
    [JsonIgnore] private GameObject rootObject;
    [JsonIgnore] private List<GameObject> dynamicObjects = new List<GameObject>();

    [JsonIgnore] private GameObject editor;

    /**
     * @brief Constructor — creates a MUX with nInputs data inputs.
     **/
    public MUX()
    {
        int selectWidth = SelectWidth(nInputs);

        select = new InputPin();
        select.width = selectWidth;

        dataOut = new OutputPin();
        dataOut.width = 0;

        for (int i = 0; i < nInputs; i++)
        {
            InputPin pin = new InputPin();
            pin.width = 0; // accept any bit width
            dataIns.Add(pin);
            lastDataIns.Add(new BitToken());
        }

        Subscribe();
    }

    /**
     * @brief Calculates the required select bus width for N inputs.
     *
     * @param[in] n number of data inputs
     * @return ceil(log2(n)), minimum 1
     **/
    private static int SelectWidth(int n)
    {
        if (n <= 2) return 1;
        return Mathf.CeilToInt(Mathf.Log(n, 2));
    }

    private void Subscribe()
    {
        GameObject o = GameObject.FindWithTag("Timer");
        timer = o.GetComponent<TimeTick>();
        timer.TimerTickEvent += OnTimerTick;
    }

    /**
     * @brief On each tick, reads the select value and passes the selected input to the output.
     **/
    private void OnTimerTick(int tick)
    {
        BitToken sel = select.data;

        // Check if anything changed
        bool changed = !sel.EqualsToken(lastSelect);
        if (!changed)
        {
            for (int i = 0; i < nInputs && i < dataIns.Count; i++)
            {
                if (!dataIns[i].data.EqualsToken(lastDataIns[i]))
                {
                    changed = true;
                    break;
                }
            }
        }

        if (!changed) return;

        lastSelect = sel;
        for (int i = 0; i < nInputs && i < dataIns.Count; i++)
            lastDataIns[i] = dataIns[i].data;

        // Convert select bits to integer index
        int index = SelectToIndex(sel);
        if (index >= 0 && index < nInputs && index < dataIns.Count)
        {
            BitToken selected = dataIns[index].data;
            dataOut.SetValue(selected.NewToken(Math.Max(selected.GetTime(), sel.GetTime())));
        }
    }

    /**
     * @brief Converts a BitToken select value to an integer index.
     *
     * @param[in] sel the select bus token
     * @return integer index (0-based)
     **/
    private int SelectToIndex(BitToken sel)
    {
        int index = 0;
        for (int i = 0; i < sel.GetWidth(); i++)
        {
            if (sel.GetBit(i)) index |= (1 << i);
        }
        return index;
    }

    public override void Reset()
    {
        lastSelect = new BitToken();
        lastDataIns.Clear();
        for (int i = 0; i < nInputs; i++)
        {
            if (i < dataIns.Count) dataIns[i].SetValue(new BitToken());
            lastDataIns.Add(new BitToken());
        }
        select.SetValue(new BitToken(SelectWidth(nInputs)));
        dataOut.SetValue(new BitToken());
    }

    public override Component Load()
    {
        GameObject prefab = Resources.Load("Prefabs/MUX") as GameObject;
        GameObject o = GameObject.Instantiate(prefab, pos, rot);
        ComponentMono c = o.GetComponent<ComponentMono>();
        c.Load(this);
        Subscribe();
        BuildVisual(o);
        return c;
    }

    /**
     * @brief Builds the MUX visual: trapezoid body, data inputs on left, select at bottom, output on right.
     *
     * @param[in] root the instantiated prefab GameObject
     **/
    public void BuildVisual(GameObject root)
    {
        this.rootObject = root;

        // Clear previous
        foreach (GameObject obj in dynamicObjects)
        {
            if (obj != null) GameObject.Destroy(obj);
        }
        dynamicObjects.Clear();

        float grid = 5f;
        float pinSpacing = 10f;
        float bodyWidth = 4 * grid;
        float inputHeight = (nInputs - 1) * pinSpacing + 2 * grid;
        float outputHeight = inputHeight * 0.5f;

        // Trapezoid body (border)
        GameObject borderObj = CreateTrapezoid("Border", root.transform,
            bodyWidth, inputHeight, outputHeight,
            new Color32(0x78, 0x85, 0x8D, 0xFF), 1);
        dynamicObjects.Add(borderObj);

        // Trapezoid body (inner fill)
        float borderSize = 1f;
        GameObject innerObj = CreateTrapezoid("Inner", root.transform,
            bodyWidth - borderSize * 2, inputHeight - borderSize * 2, outputHeight - borderSize * 2,
            new Color32(0x2A, 0x3A, 0x2E, 0xFF), 2);
        dynamicObjects.Add(innerObj);

        // Collider
        BoxCollider2D col = root.GetComponent<BoxCollider2D>();
        if (col == null) col = root.AddComponent<BoxCollider2D>();
        col.size = new Vector2(bodyWidth, inputHeight);

        // Description label
        Draggable2D draggable = root.GetComponent<Draggable2D>();
        if (draggable != null)
        {
            GameObject descObj = new GameObject("Description");
            descObj.transform.SetParent(root.transform, false);
            descObj.transform.localPosition = new Vector3(0, inputHeight / 2f + grid, 0);
            TextMeshPro descTmp = descObj.AddComponent<TextMeshPro>();
            descTmp.text = "MUX (" + nInputs + ")";
            descTmp.fontSize = 24;
            descTmp.alignment = TextAlignmentOptions.Center;
            descTmp.color = new Color32(0xAF, 0xD6, 0x9E, 0xFF);
            descTmp.sortingOrder = 3;
            RectTransform descRt = descObj.GetComponent<RectTransform>();
            descRt.sizeDelta = new Vector2(bodyWidth * 2f, grid * 2);
            descObj.SetActive(false);
            draggable.description = descObj;
            dynamicObjects.Add(descObj);
        }

        // "MUX" label on the body
        GameObject muxLabel = new GameObject("MuxLabel");
        muxLabel.transform.SetParent(root.transform, false);
        muxLabel.transform.localPosition = new Vector3(0, 0, 0);
        TextMeshPro muxTmp = muxLabel.AddComponent<TextMeshPro>();
        muxTmp.text = "MUX";
        muxTmp.fontSize = 20;
        muxTmp.alignment = TextAlignmentOptions.Center;
        muxTmp.color = Color.white;
        muxTmp.sortingOrder = 3;
        RectTransform muxRt = muxLabel.GetComponent<RectTransform>();
        muxRt.sizeDelta = new Vector2(bodyWidth, grid * 2);
        dynamicObjects.Add(muxLabel);

        float firstPinY = (nInputs - 1) * pinSpacing / 2f;

        // Data input pins — left side
        GameObject inputPinPrefab = Resources.Load("Prefabs/InputPin") as GameObject;
        for (int i = 0; i < nInputs; i++)
        {
            float pinY = firstPinY - i * pinSpacing;
            GameObject pinObj = GameObject.Instantiate(inputPinPrefab, root.transform);
            pinObj.transform.localPosition = new Vector3(-bodyWidth / 2f - grid * 0.25f, pinY, 0);
            pinObj.transform.localScale = new Vector3(4f, 4f, 1);

            InputPin_Mono pinMono = pinObj.GetComponentInChildren<InputPin_Mono>();
            if (pinMono != null) pinMono.Init(dataIns[i]);
            dynamicObjects.Add(pinObj);

            // Pin index label
            CreatePinLabel(root.transform, i.ToString(),
                new Vector3(-bodyWidth / 2f + 3f, pinY, 0),
                TextAlignmentOptions.Left);
        }

        // Select pin — bottom
        GameObject selPinObj = GameObject.Instantiate(inputPinPrefab, root.transform);
        selPinObj.transform.localPosition = new Vector3(0, -inputHeight / 2f - grid * 0.25f, 0);
        selPinObj.transform.localScale = new Vector3(4f, 4f, 1);
        InputPin_Mono selMono = selPinObj.GetComponentInChildren<InputPin_Mono>();
        if (selMono != null) selMono.Init(select);
        dynamicObjects.Add(selPinObj);

        // Select label
        CreatePinLabel(root.transform, "S",
            new Vector3(0, -inputHeight / 2f + 3f, 0),
            TextAlignmentOptions.Center);

        // Output pin — right side
        GameObject outputPinPrefab = Resources.Load("Prefabs/OutputPin") as GameObject;
        GameObject outPinObj = GameObject.Instantiate(outputPinPrefab, root.transform);
        outPinObj.transform.localPosition = new Vector3(bodyWidth / 2f + grid * 0.25f, 0, 0);
        outPinObj.transform.localScale = new Vector3(4f, 4f, 1);
        OutputPin_Mono outMono = outPinObj.GetComponentInChildren<OutputPin_Mono>();
        if (outMono != null)
        {
            outMono.Init(dataOut);
            if (outMono.wirePrefab == null)
                outMono.wirePrefab = Resources.Load("Prefabs/Wire") as GameObject;
        }
        dynamicObjects.Add(outPinObj);
    }

    /**
     * @brief Creates a trapezoid mesh programmatically.
     *
     * @details The trapezoid is wider on the left (input side) and narrower on the right (output side).
     * Built using a Mesh with 4 vertices and 2 triangles.
     *
     * @param[in] name GameObject name
     * @param[in] parent parent transform
     * @param[in] width horizontal width of the trapezoid
     * @param[in] leftHeight height on the left (input) side
     * @param[in] rightHeight height on the right (output) side
     * @param[in] color fill color
     * @param[in] sortingOrder render order
     * @return the created trapezoid GameObject
     **/
    private GameObject CreateTrapezoid(string name, Transform parent,
        float width, float leftHeight, float rightHeight, Color color, int sortingOrder)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = Vector3.zero;

        MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();

        // 4 vertices: top-left, bottom-left, top-right, bottom-right
        float halfWidth = width / 2f;
        float halfLeftH = leftHeight / 2f;
        float halfRightH = rightHeight / 2f;

        mesh.vertices = new Vector3[]
        {
            new Vector3(-halfWidth, halfLeftH, 0),    // top-left
            new Vector3(-halfWidth, -halfLeftH, 0),   // bottom-left
            new Vector3(halfWidth, halfRightH, 0),    // top-right
            new Vector3(halfWidth, -halfRightH, 0)    // bottom-right
        };

        mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;

        // Use a simple unlit material
        meshRenderer.material = new Material(Shader.Find("Sprites/Default"));
        meshRenderer.material.color = color;
        meshRenderer.sortingOrder = sortingOrder;

        return obj;
    }

    private void CreatePinLabel(Transform parent, string text, Vector3 localPos, TextAlignmentOptions alignment)
    {
        GameObject labelObj = new GameObject("Label_" + text);
        labelObj.transform.SetParent(parent, false);
        labelObj.transform.localPosition = localPos;

        TextMeshPro tmp = labelObj.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.sortingOrder = 3;

        RectTransform rt = labelObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(8f, 8f);
    }

    /**
     * @brief Changes the number of inputs. Recreates pins and visual.
     *
     * @param[in] text string representation of the new input count
     * @return the actual input count after parsing
     **/
    public int ParseInputCount(string text)
    {
        if (text.Length > 0)
        {
            try
            {
                int value = int.Parse(text);
                if (value >= 2 && value <= 16)
                {
                    nInputs = value;
                    RecreatePins();
                    if (rootObject != null) BuildVisual(rootObject);
                }
            }
            catch { }
        }
        return nInputs;
    }

    private void RecreatePins()
    {
        int selectWidth = SelectWidth(nInputs);

        select = new InputPin();
        select.width = selectWidth;

        dataOut = new OutputPin();
        dataOut.width = 0;

        dataIns.Clear();
        lastDataIns.Clear();
        for (int i = 0; i < nInputs; i++)
        {
            InputPin pin = new InputPin();
            pin.width = 0;
            dataIns.Add(pin);
            lastDataIns.Add(new BitToken());
        }
    }

    public override void LoadPins(InputPin_Mono[] inPins, OutputPin_Mono[] outPins)
    {
        // Pins are created dynamically in BuildVisual
    }

    public override void LoadDelay(GameObject delayVisualizer) { }

    public override void CollectPins(List<InputPin> inputs, List<OutputPin> outputs)
    {
        inputs.AddRange(dataIns);
        inputs.Add(select);
        outputs.Add(dataOut);
    }

    public override void Dispose()
    {
        timer.TimerTickEvent -= OnTimerTick;
    }

    public override void OpenEditor()
    {
        // TODO: MUX editor for changing input count
    }

    public override int GetDelay() { return 0; }
}

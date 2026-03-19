using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Splits an N-bit bus input into N individual 1-bit outputs.
/// Default: 2-bit. Configurable via editor.
/// </summary>
public class Splitter : CircuitComponent
{
    [JsonProperty] private InputPin dataIn;
    [JsonProperty] private List<OutputPin> dataOuts = new List<OutputPin>();
    [JsonProperty] public int nBits = 2;

    [JsonIgnore] private TimeTick timer;
    [JsonIgnore] private BitToken lastDataIn;
    [JsonIgnore] private GameObject rootObject;
    [JsonIgnore] private List<GameObject> dynamicPinObjects = new List<GameObject>();

    [JsonIgnore] private GameObject editor;

    public Splitter()
    {
        dataIn = new InputPin();
        dataIn.width = nBits;
        lastDataIn = new BitToken(nBits);

        for (int i = 0; i < nBits; i++)
        {
            OutputPin pin = new OutputPin();
            pin.width = 1;
            dataOuts.Add(pin);
        }

        Subscribe();
    }

    private void Subscribe()
    {
        GameObject o = GameObject.FindWithTag("Timer");
        timer = o.GetComponent<TimeTick>();
        timer.TimerTickEvent += OnTimerTick;
    }

    private void OnTimerTick(int tick)
    {
        BitToken d = dataIn.data;
        if (!d.EqualsToken(lastDataIn))
        {
            lastDataIn = d;
            for (int i = 0; i < nBits && i < dataOuts.Count; i++)
            {
                bool bitValue = d.GetBit(i);
                dataOuts[i].SetValue(new BitToken(bitValue, d.GetTime(), d.TokenColor()));
            }
        }
    }

    public override void Reset()
    {
        lastDataIn = new BitToken(nBits);
        dataIn.SetValue(new BitToken(nBits));
        foreach (OutputPin pin in dataOuts)
            pin.SetValue(new BitToken());
    }

    public override Component Load()
    {
        GameObject prefab = Resources.Load("Prefabs/Splitter") as GameObject;
        GameObject o = GameObject.Instantiate(prefab, pos, rot);
        ComponentMono c = o.GetComponent<ComponentMono>();
        c.Load(this);
        Subscribe();
        BuildVisual(o);
        return c;
    }

    /// <summary>
    /// Builds the visual: input pin on left, slim vertical bar, output pins on right.
    /// </summary>
    public void BuildVisual(GameObject root)
    {
        this.rootObject = root;

        // Clear any previous dynamic objects
        foreach (GameObject obj in dynamicPinObjects)
        {
            if (obj != null) GameObject.Destroy(obj);
        }
        dynamicPinObjects.Clear();

        float grid = 5f;
        float pinSpacing = 10f;
        float barWidth = 1f;
        float barHeight = (nBits - 1) * pinSpacing;
        float totalHeight = barHeight + grid; // padding
        float topY = barHeight / 2f;

        // Vertical bar
        GameObject barObj = new GameObject("Bar");
        barObj.transform.SetParent(root.transform, false);
        barObj.transform.localPosition = Vector3.zero;
        barObj.transform.localScale = new Vector3(barWidth, barHeight, 1);
        SpriteRenderer barRend = barObj.AddComponent<SpriteRenderer>();
        barRend.sprite = CreateSquareSprite();
        barRend.color = new Color32(0x55, 0x5F, 0x66, 0xFF);
        barRend.sortingOrder = 1;
        dynamicPinObjects.Add(barObj);

        // Resize collider — only covers the bar
        BoxCollider2D col = root.GetComponent<BoxCollider2D>();
        if (col == null) col = root.AddComponent<BoxCollider2D>();
        col.size = new Vector2(barWidth * 2f, barHeight + barWidth);

        // Small horizontal lines from bar to each output pin
        for (int i = 0; i < nBits; i++)
        {
            float pinY = topY - i * pinSpacing;
            GameObject line = new GameObject("Line_" + i);
            line.transform.SetParent(root.transform, false);
            line.transform.localPosition = new Vector3(grid * 0.5f, pinY, 0);
            line.transform.localScale = new Vector3(grid, barWidth, 1);
            SpriteRenderer lineRend = line.AddComponent<SpriteRenderer>();
            lineRend.sprite = CreateSquareSprite();
            lineRend.color = new Color32(0x55, 0x5F, 0x66, 0xFF);
            lineRend.sortingOrder = 1;
            dynamicPinObjects.Add(line);
        }

        // Horizontal line from input pin to bar
        {
            GameObject line = new GameObject("Line_In");
            line.transform.SetParent(root.transform, false);
            line.transform.localPosition = new Vector3(-grid * 0.5f, 0, 0);
            line.transform.localScale = new Vector3(grid, barWidth, 1);
            SpriteRenderer lineRend = line.AddComponent<SpriteRenderer>();
            lineRend.sprite = CreateSquareSprite();
            lineRend.color = new Color32(0x55, 0x5F, 0x66, 0xFF);
            lineRend.sortingOrder = 1;
            dynamicPinObjects.Add(line);
        }

        // Description for Draggable2D
        Draggable2D draggable = root.GetComponent<Draggable2D>();
        if (draggable != null)
        {
            GameObject descObj = new GameObject("Description");
            descObj.transform.SetParent(root.transform, false);
            descObj.transform.localPosition = new Vector3(0, totalHeight / 2f + grid, 0);
            TMPro.TextMeshPro descTmp = descObj.AddComponent<TMPro.TextMeshPro>();
            descTmp.text = "Splitter (" + nBits + "-bit)";
            descTmp.fontSize = 24;
            descTmp.alignment = TMPro.TextAlignmentOptions.Center;
            descTmp.color = new Color32(0xAF, 0xD6, 0x9E, 0xFF);
            descTmp.sortingOrder = 3;
            RectTransform descRt = descObj.GetComponent<RectTransform>();
            descRt.sizeDelta = new Vector2(grid * 6, grid * 2);
            descObj.SetActive(false);
            draggable.description = descObj;
            dynamicPinObjects.Add(descObj);
        }

        // First output Y = top of bar

        // Input pin — centered on left side of bar
        GameObject inputPinPrefab = Resources.Load("Prefabs/InputPin") as GameObject;
        GameObject inPinObj = GameObject.Instantiate(inputPinPrefab, root.transform);
        inPinObj.transform.localPosition = new Vector3(-grid, 0, 0);
        inPinObj.transform.localScale = new Vector3(4f, 4f, 1);
        InputPin_Mono inPinMono = inPinObj.GetComponentInChildren<InputPin_Mono>();
        if (inPinMono != null) inPinMono.Init(dataIn);
        dynamicPinObjects.Add(inPinObj);

        // Output pins — right side, spaced every grid section
        GameObject outputPinPrefab = Resources.Load("Prefabs/OutputPin") as GameObject;
        for (int i = 0; i < nBits; i++)
        {
            float pinY = topY - i * pinSpacing;
            GameObject outPinObj = GameObject.Instantiate(outputPinPrefab, root.transform);
            outPinObj.transform.localPosition = new Vector3(grid, pinY, 0);
            outPinObj.transform.localScale = new Vector3(4f, 4f, 1);

            OutputPin_Mono outPinMono = outPinObj.GetComponentInChildren<OutputPin_Mono>();
            if (outPinMono != null)
            {
                outPinMono.Init(dataOuts[i]);
                if (outPinMono.wirePrefab == null)
                    outPinMono.wirePrefab = Resources.Load("Prefabs/Wire") as GameObject;
            }
            dynamicPinObjects.Add(outPinObj);
        }
    }

    /// <summary>
    /// Change bit width. Recreates pins and visual.
    /// </summary>
    public int ParseBitSize(string text)
    {
        if (text.Length > 0)
        {
            try
            {
                int value = int.Parse(text);
                if (value >= 2 && value <= 16)
                {
                    nBits = value;
                    RecreatePins();
                    if (rootObject != null) BuildVisual(rootObject);
                }
            }
            catch { }
        }
        return nBits;
    }

    private void RecreatePins()
    {
        dataIn = new InputPin();
        dataIn.width = nBits;
        lastDataIn = new BitToken(nBits);

        dataOuts.Clear();
        for (int i = 0; i < nBits; i++)
        {
            OutputPin pin = new OutputPin();
            pin.width = 1;
            dataOuts.Add(pin);
        }
    }

    public override void LoadPins(InputPin_Mono[] inPins, OutputPin_Mono[] outPins)
    {
        // Pins are created dynamically in BuildVisual
    }

    public override void LoadDelay(GameObject delayVisualizer) { }

    public override void CollectPins(List<InputPin> inputs, List<OutputPin> outputs)
    {
        inputs.Add(dataIn);
        outputs.AddRange(dataOuts);
    }

    public override void Dispose()
    {
        timer.TimerTickEvent -= OnTimerTick;
    }

    public override void OpenEditor()
    {
        if (editor != null) editor.SetActive(true);
        editor?.GetComponent<SplitterEditor>()?.Init(this);
    }

    public void LoadEditor()
    {
        GameObject o = GameObject.FindWithTag("Editor");
        editor = o?.transform.Find("SplitterEditor")?.gameObject;
    }

    private Sprite CreateSquareSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
    }
}

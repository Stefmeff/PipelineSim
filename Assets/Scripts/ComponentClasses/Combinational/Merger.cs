using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Merges N individual 1-bit inputs into a single N-bit bus output.
/// Default: 2-bit. Configurable via editor.
/// </summary>
public class Merger : CircuitComponent
{
    [JsonProperty] private List<InputPin> dataIns = new List<InputPin>();
    [JsonProperty] private OutputPin dataOut;
    [JsonProperty] public int nBits = 2;

    [JsonIgnore] private TimeTick timer;
    [JsonIgnore] private List<BitToken> lastDataIns = new List<BitToken>();
    [JsonIgnore] private GameObject rootObject;
    [JsonIgnore] private List<GameObject> dynamicPinObjects = new List<GameObject>();

    [JsonIgnore] private GameObject editor;

    public Merger()
    {
        dataOut = new OutputPin();
        dataOut.width = nBits;

        for (int i = 0; i < nBits; i++)
        {
            InputPin pin = new InputPin();
            pin.width = 1;
            dataIns.Add(pin);
            lastDataIns.Add(new BitToken());
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
        bool changed = false;
        for (int i = 0; i < nBits && i < dataIns.Count; i++)
        {
            if (!dataIns[i].data.EqualsToken(lastDataIns[i]))
            {
                lastDataIns[i] = dataIns[i].data;
                changed = true;
            }
        }

        if (changed)
        {
            bool[] values = new bool[nBits];
            Color tokenColor = new Color32(0xc4, 0x0c, 0x0c, 0xff); // default red
            int maxTime = 0;

            for (int i = 0; i < nBits && i < dataIns.Count; i++)
            {
                values[i] = dataIns[i].data.GetValue();
                if (dataIns[i].data.GetTime() > maxTime)
                {
                    maxTime = dataIns[i].data.GetTime();
                    tokenColor = dataIns[i].data.TokenColor();
                }
            }

            dataOut.SetValue(new BitToken(values, maxTime, tokenColor));
        }
    }

    public override void Reset()
    {
        lastDataIns.Clear();
        for (int i = 0; i < nBits; i++)
        {
            if (i < dataIns.Count) dataIns[i].SetValue(new BitToken());
            lastDataIns.Add(new BitToken());
        }
        dataOut.SetValue(new BitToken(nBits));
    }

    public override Component Load()
    {
        GameObject prefab = Resources.Load("Prefabs/Merger") as GameObject;
        GameObject o = GameObject.Instantiate(prefab, pos, rot);
        ComponentMono c = o.GetComponent<ComponentMono>();
        c.Load(this);
        Subscribe();
        BuildVisual(o);
        return c;
    }

    /// <summary>
    /// Builds the visual: input pins on left, slim vertical bar, output pin on right.
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
        float totalHeight = barHeight + grid;
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

        // Small horizontal lines from each input pin to bar
        for (int i = 0; i < nBits; i++)
        {
            float pinY = topY - i * pinSpacing;
            GameObject line = new GameObject("Line_" + i);
            line.transform.SetParent(root.transform, false);
            line.transform.localPosition = new Vector3(-grid * 0.5f, pinY, 0);
            line.transform.localScale = new Vector3(grid, barWidth, 1);
            SpriteRenderer lineRend = line.AddComponent<SpriteRenderer>();
            lineRend.sprite = CreateSquareSprite();
            lineRend.color = new Color32(0x55, 0x5F, 0x66, 0xFF);
            lineRend.sortingOrder = 1;
            dynamicPinObjects.Add(line);
        }

        // Horizontal line from bar to output pin
        {
            GameObject line = new GameObject("Line_Out");
            line.transform.SetParent(root.transform, false);
            line.transform.localPosition = new Vector3(grid * 0.5f, 0, 0);
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
            descTmp.text = "Merger (" + nBits + "-bit)";
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

        // Input pins — left side, spaced every grid section
        GameObject inputPinPrefab = Resources.Load("Prefabs/InputPin") as GameObject;
        for (int i = 0; i < nBits; i++)
        {
            float pinY = topY - i * pinSpacing;
            GameObject inPinObj = GameObject.Instantiate(inputPinPrefab, root.transform);
            inPinObj.transform.localPosition = new Vector3(-grid, pinY, 0);
            inPinObj.transform.localScale = new Vector3(4f, 4f, 1);

            InputPin_Mono inPinMono = inPinObj.GetComponentInChildren<InputPin_Mono>();
            if (inPinMono != null) inPinMono.Init(dataIns[i]);
            dynamicPinObjects.Add(inPinObj);
        }

        // Output pin — centered on right side of bar
        GameObject outputPinPrefab = Resources.Load("Prefabs/OutputPin") as GameObject;
        GameObject outPinObj = GameObject.Instantiate(outputPinPrefab, root.transform);
        outPinObj.transform.localPosition = new Vector3(grid, 0, 0);
        outPinObj.transform.localScale = new Vector3(4f, 4f, 1);

        OutputPin_Mono outPinMono = outPinObj.GetComponentInChildren<OutputPin_Mono>();
        if (outPinMono != null)
        {
            outPinMono.Init(dataOut);
            if (outPinMono.wirePrefab == null)
                outPinMono.wirePrefab = Resources.Load("Prefabs/Wire") as GameObject;
        }
        dynamicPinObjects.Add(outPinObj);
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
        dataOut = new OutputPin();
        dataOut.width = nBits;

        dataIns.Clear();
        lastDataIns.Clear();
        for (int i = 0; i < nBits; i++)
        {
            InputPin pin = new InputPin();
            pin.width = 1;
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
        outputs.Add(dataOut);
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

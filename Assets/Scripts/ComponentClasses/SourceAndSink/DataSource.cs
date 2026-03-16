using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/**
 * @author: Stefan Moser
 *
 * @details: DataSource is used for generating generic data tokens at each rising clock edge.
 * Outputs an n-bit token on a single bus wire. Width is configurable (1-16 bits).
 * */
public class DataSource : CircuitComponent
{
    [JsonProperty] private InputPin clkIn;               //clock input pin
    [JsonProperty] private OutputPin dataOut;             //single n-bit output pin

    [JsonProperty] private int nBits = 1;

    // Token data — owned by DataSource, editor is just a view
    [JsonProperty] private List<string> tokenBinaries = new List<string>();

    [JsonIgnore] public int tokenCount = 0;

    [JsonIgnore] public static readonly List<Color> colorScheme = new List<Color>
    {
        SetTransp(Color.green),
        SetTransp(Color.blue),
        SetTransp(Color.yellow),
        SetTransp(Color.cyan),
        SetTransp(Color.magenta),
        new Color32(0xff, 0x88, 0x00, 0xFF)
    };

    [JsonIgnore] private TimeTick timer;                //global simulation timer
    [JsonIgnore] private BitToken lastClkIn;

    [JsonIgnore] private GameObject editor;
    [JsonIgnore] private DataSourceEditor sourceEditor;

    /**
     * Constructor of DataSource object
     * */
    public DataSource()
    {
        //init pins:
        this.clkIn = new InputPin();
        this.dataOut = new OutputPin();
        this.dataOut.width = 1;

        lastClkIn = new BitToken();

        // Init default tokens if empty (one per color)
        if (tokenBinaries.Count == 0)
        {
            for (int i = 0; i < colorScheme.Count; i++)
            {
                tokenBinaries.Add(new string('1', nBits));
            }
        }

        Subscribe();
    }

    /// <summary>
    /// Gets the next token from the stored data (works with or without editor).
    /// </summary>
    public Token GetNextToken()
    {
        if (tokenBinaries.Count == 0) return null;
        if (tokenCount >= tokenBinaries.Count) tokenCount = 0;

        int colorIndex = tokenCount % colorScheme.Count;
        Token token = new Token(tokenBinaries[tokenCount], colorScheme[colorIndex]);
        tokenCount++;
        return token;
    }

    public List<string> GetTokenBinaries() { return tokenBinaries; }

    public void SetTokenBinary(int index, string binary)
    {
        if (index >= 0 && index < tokenBinaries.Count)
            tokenBinaries[index] = binary;
    }

    public void AddToken()
    {
        tokenBinaries.Add(new string('1', nBits));
    }

    public void RemoveToken()
    {
        if (tokenBinaries.Count > 0)
            tokenBinaries.RemoveAt(tokenBinaries.Count - 1);
    }

    //Event: new clock input data
    private void OnTimerTick(int tick)
    {
        BitToken c = clkIn.data;

        //rising clock edge
        if(!c.EqualsToken(lastClkIn))
        {
            lastClkIn = c;
            if(lastClkIn.GetValue() == true){

                Token token = GetNextToken();
                if(token != null){
                    // Pack token bits into a single n-bit BitToken
                    bool[] values = new bool[nBits];
                    for(int i = 0; i < nBits; i++){
                        BitToken bit = token.GetBitAt((nBits - 1) - i);
                        values[i] = bit != null ? bit.GetValue() : false;
                    }
                    Color tokenColor = token.GetBitAt(0).TokenColor();
                    BitToken busToken = new BitToken(values, clkIn.data.GetTime(), tokenColor);
                    dataOut.SetValue(busToken);
                }
            }
        }
    }


    //Event: simulation restart
    public override void Reset()
    {
        tokenCount = 0;
        clkIn.SetValue(new BitToken());
        dataOut.SetValue(new BitToken(nBits));
    }


    public override Component Load()
    {
        GameObject prefab = Resources.Load("Prefabs/DataSource") as GameObject;
        GameObject o = GameObject.Instantiate(prefab, pos, rot);
        ComponentMono c = o.GetComponent<ComponentMono>();
        c.Load(this);
        Subscribe();
        return c;
    }

    private void Subscribe()
    {
        //subscribe to Timer events:
        GameObject o = GameObject.FindWithTag("Timer");
        timer = o.GetComponent<TimeTick>();
        timer.TimerTickEvent += OnTimerTick;
    }

    //unsubscribe from all event so garbage collector can delete object
    public override void Dispose()
    {
        timer.TimerTickEvent -= OnTimerTick;
    }

    public override void LoadPins(InputPin_Mono[] inPins, OutputPin_Mono[] outPins)
    {
        inPins[0].Init(clkIn);
        outPins[0].Init(dataOut);
    }

    public int parseBitSize(string text){
        if (text.Length > 0)
        {
            try{
                int value = int.Parse(text);
                if (value > 0 && value <= 16)
                {
                    nBits = value;
                    dataOut.width = nBits;

                    // Resize all token binaries to match new bit width
                    for (int i = 0; i < tokenBinaries.Count; i++)
                    {
                        tokenBinaries[i] = new string('1', nBits);
                    }
                }
            }catch{

            }
        }
        return nBits;
    }

    public override void CollectPins(List<InputPin> inputs, List<OutputPin> outputs)
    {
        inputs.Add(clkIn); outputs.Add(dataOut);
    }

    public override void LoadDelay(GameObject delayVisualizer)
    {
    }

    public void LoadEditor(){
        if(editor)GameObject.Destroy(editor);
        GameObject o = GameObject.FindWithTag("Editor");
        GameObject prefab = Resources.Load("Prefabs/DataSourceEditor") as GameObject;
        editor = GameObject.Instantiate(prefab);

        editor.transform.SetParent(o.transform,false);
        editor.SetActive(true);
        sourceEditor = editor.GetComponent<DataSourceEditor>();
        sourceEditor.init(this);
        editor.SetActive(false);

    }

    public override void OpenEditor()
    {
        editor.SetActive(true);
    }

    private static Color32 SetTransp(Color c)
    {
        Color32 b = new Color(c.r, c.g, c.b);
        b.a = 0xD4;
        return b;
    }
}

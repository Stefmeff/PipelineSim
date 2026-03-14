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

        Subscribe();
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

                Token token = sourceEditor.GetNextToken();
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
        sourceEditor.tokenCount = 0;
        clkIn.SetValue(new BitToken());
        dataOut.SetValue(new BitToken());
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
                }
            }catch{

            }
        }
        return nBits;
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
}

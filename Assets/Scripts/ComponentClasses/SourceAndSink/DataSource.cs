using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEditor;
using UnityEngine;

/**
 * @author: Stefan Moser
 * 
 * @details: DataSource is used for generating generic data tokens at each rising clock edge.
 * The output signals of DataSource are not meant to be used for implementing any logic, but rather for the
 * visualisation of different signals/tokens progressing through the circuit.
 * */
public class DataSource : Loadable
{
    [JsonProperty] private InputPin clkIn;               //clock input pin
    [JsonProperty] private OutputPin[] dataOutPins;

    [JsonProperty] private int nBits = 1;

    [JsonIgnore] private TimeTick timer;                //global simulation timer
    [JsonIgnore] private BitToken lastClkIn;

    [JsonIgnore] private OutputPin_Mono[] pinGameObjects;

    [JsonIgnore] private GameObject editor;

    [JsonIgnore] private DataSourceEditor sourceEditor;

    /**
     * Constructor of DataSource object
     * */
    public DataSource()
    {
        //init pins:
        this.clkIn = new InputPin();
        this.dataOutPins = new OutputPin[8];

        for(int i = 0; i < dataOutPins.Length; i++){
            dataOutPins[i] = new OutputPin();
            dataOutPins[i].SetValue(new BitToken());
        }
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
                    for(int i = 0; i < nBits; i++){
                        BitToken t = token.GetBitAt((nBits-1)-i).NewToken(clkIn.data.GetTime());
                        dataOutPins[i].SetValue(t);
                    }
                }
            }
        }
    }


    //Event: simulation restart
    public override void Reset()
    {
        sourceEditor.tokenCount = 0;
        clkIn.SetValue(new BitToken());
        foreach(OutputPin p in dataOutPins){
            p.SetValue(new BitToken());
        }
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
        
        for(int i = 0; i < dataOutPins.Length; i++){
            outPins[i].Init(dataOutPins[i]);
        }
        pinGameObjects = outPins;
        ActivatePins();
    }

    public int parseBitSize(string text){
        if (text.Length > 0)
        {
            try{
                int value = int.Parse(text);
                if (value > 0 && value <= 8)
                {
                    nBits = value;
                    ActivatePins();
                }
            }catch{

            }
        }
        return nBits;
    }

    private void ActivatePins(){
        for(int i = 0; i < dataOutPins.Length; i++){
            if(i<nBits){
                pinGameObjects[i].gameObject.SetActive(true);
            }else{
                dataOutPins[i].disconnectWire();
                pinGameObjects[i].gameObject.SetActive(false);
            }
        }
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

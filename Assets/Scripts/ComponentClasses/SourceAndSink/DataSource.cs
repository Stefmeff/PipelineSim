using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using static Pin;


/**
 * @author: Stefan Moser
 * 
 * @details: DataSource is used for generating generic data tokens at each rising clock edge.
 * The output signals of DataSource are not meant to be used for implementing any logic, but rather for the
 * visualisation of different signals/tokens progressing through the circuit.
 * */
public class DataSource : ILoadable
{
    [JsonProperty] private InputPin clkIn;               //clock input pin
    [JsonProperty] private OutputPin[] dataOutPins;

    [JsonProperty] private int nBits = 1;

    [JsonIgnore] private TimeTick timer;                //global simulation timer
    [JsonIgnore] private BitToken lastClkIn;

    [JsonIgnore] private OutputPin_Mono[] pinGameObjects;
    
    [JsonIgnore] private List<TokenInputField> tokenFields;
    [JsonIgnore] private int tokenCount = 0;




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

        tokenFields = new List<TokenInputField>();
        
        //dataOut.SetValue(new BitToken());

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

                if(tokenFields.Count != 0){
                    if(tokenCount >= tokenFields.Count)tokenCount=0;
                    TokenInputField token = tokenFields[tokenCount];

                    for(int i = 0; i < nBits; i++){
                        BitToken t = token.GetBitTokenAt((nBits-1)-i,clkIn.data.GetTime());
                        dataOutPins[i].SetValue(t);
                    }
                    tokenCount++;
                }
            }
        }
    }


    //Event: simulation restart
    public override void Reset()
    {
        tokenCount = 0;
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
                    UpdateTokenSizes();
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

    public GameObject LoadEditor(){
        GameObject o = GameObject.FindWithTag("Editor");
        GameObject prefab = Resources.Load("Prefabs/DataSourceEditor") as GameObject;
        GameObject editor = GameObject.Instantiate(prefab);

        editor.transform.SetParent(o.transform,false);
        editor.SetActive(true);
        DataSourceEditor d = editor.GetComponent<DataSourceEditor>();
        d.init(this);
        editor.SetActive(false);
        return editor;
    }

    public void AddToken(TokenInputField t){
        tokenFields.Add(t);
        Debug.Log("ADD TOKEN: " +tokenFields.Count);
    }

    public void RemoveToken(){
        int lastIndex = tokenFields.Count - 1;
        TokenInputField last = tokenFields[lastIndex];
        tokenFields.RemoveAt(lastIndex);
        GameObject.Destroy(last.gameObject);
    }

    private void UpdateTokenSizes(){
        foreach(TokenInputField tF in tokenFields){
            tF.UpdateInputSize(nBits);
        }
    }
}

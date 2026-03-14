using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;

/**
 * @author: Stefan Moser
 * 
 * @brief: This class describes the behaviour and state of a capture-pass latch.
 * 
 * @details: 
 * */
public class CPLatch : CircuitComponent, IDelay
{
    //PINS:
    [JsonProperty] private InputPin capture;     //capture input pin
    [JsonProperty] private InputPin pass;        //pass input pin
    [JsonProperty] private InputPin dataIn;     //data input pin
    [JsonProperty] private OutputPin dataOut;    //data output pin

    //TIMING PARAMETERS:
    [JsonProperty] private int delay = 0;
    [JsonProperty] private int setup = 0;
    [JsonProperty] private int hold = 0;

    //DELAY VISUALIZATION:
    [JsonIgnore] private List<Tuple<BitToken, GameObject>> signalQueue;        //all the signals currently traveling through this delay element
    [JsonIgnore] public GameObject delayVisualizer;                         //used for visualizing the delay and the signal transitions on the output
    [JsonProperty] public bool visualizerOn = false;
    [JsonIgnore] private TimeTick timer;                                    //global simulation timer

    //last inputs:
    [JsonIgnore] private BitToken lastCapture = new BitToken();
    [JsonIgnore] private BitToken lastPass = new BitToken();
    [JsonIgnore] private BitToken lastData = new BitToken();

    //parameters for animation:
    [JsonIgnore] public SpriteRenderer sRend;
    [JsonIgnore] public TextMeshPro errorMessage;
    [JsonIgnore] private Color defaultColor = new Color32(0x78,0x85, 0x8D, 0xFF);
    [JsonIgnore] private Color errorColor = new Color32(0xFF, 0x13, 0x00, 0xFF);

    [JsonIgnore] private GameObject editor;

    public CPLatch(){

        //init the capture-pass latch pins:
        this.capture = new InputPin();
        this.pass = new InputPin();
        this.dataIn = new InputPin();
        this.dataIn.width = 0; // accept any bit width
        this.dataOut = new OutputPin();

        signalQueue = new List<Tuple<BitToken,GameObject>>();
        GameObject o = GameObject.FindWithTag("Timer");
        timer = o.GetComponent<TimeTick>();
        Subscribe();

        o = GameObject.FindWithTag("Editor");
        editor = o.transform.GetChild(5).gameObject;
    }

    private void OnTickNoDelay(int tick){
        BitToken c = capture.data;
        BitToken p = pass.data;
        BitToken d = dataIn.data;

        if(!c.EqualsToken(lastCapture)||!p.EqualsToken(lastPass)||!d.EqualsToken(lastData)){
            lastCapture = c;
            lastPass = p;
            lastData = d;

            if ((c.GetValue() == false && p.GetValue() == false) || (c.GetValue() == true && p.GetValue() == true))
            {
                //PASS MODE:
                int time = Math.Max(d.GetTime(),Math.Max(c.GetTime(), p.GetTime()));
                dataOut.SetValue(d.NewToken(time));
            }else if((c.GetValue() == true && p.GetValue() == false) || (c.GetValue() == false && p.GetValue() == true)){
                //CAPTURE MODE:
                int time = Math.Max(c.GetTime(), p.GetTime());
                if(d.GetTime()<= time){
                    if(CheckSetup(time)){
                        dataOut.SetValue(d.NewToken(time));
                    }
                }else{
                    CheckHold(time);
                }
            }
        }
    }

    private void OnTickDelay(int tick){
        BitToken c = capture.data;
        BitToken p = pass.data;
        BitToken d = dataIn.data;

        if(!c.EqualsToken(lastCapture)||!p.EqualsToken(lastPass)||!d.EqualsToken(lastData)){
            lastCapture = c;
            lastPass = p;
            lastData = d;

            if ((c.GetValue() == false && p.GetValue() == false) || (c.GetValue() == true && p.GetValue() == true))
            {
                //PASS MODE:
                int time = Math.Max(d.GetTime(),Math.Max(c.GetTime(), p.GetTime()));
                signalQueue.Add(Tuple.Create(d.NewToken(time),(GameObject)null));
                
            }else if((c.GetValue() == true && p.GetValue() == false) || (c.GetValue() == false && p.GetValue() == true)){
                //CAPTURE MODE:
                int time = Math.Max(c.GetTime(), p.GetTime());
                if(d.GetTime()<= time){
                    if(CheckSetup(time)){
                        signalQueue.Add(Tuple.Create(d.NewToken(time),(GameObject)null));
                    }
                }else{
                    CheckHold(time);
                }
            }
        }

        if (signalQueue.Count > 1)
        {

            BitToken nextOut = signalQueue[1].Item1;
            int arrivalTime = nextOut.GetTime();

            //check if next output signal is ready
            if (arrivalTime + delay <= tick)
            {
                //set output and remove from signal queue
                if(signalQueue[0].Item2 != null) GameObject.Destroy(signalQueue[0].Item2);
                signalQueue.RemoveAt(0);
                dataOut.SetValue(nextOut.NewToken(arrivalTime + delay));  
            }
        } 
    }

    private void OnTickVisualizeDelay(int tick){
        BitToken c = capture.data;
        BitToken p = pass.data;
        BitToken d = dataIn.data;

        if(!c.EqualsToken(lastCapture)||!p.EqualsToken(lastPass)||!d.EqualsToken(lastData)){
            lastCapture = c;
            lastPass = p;
            lastData = d;

            if ((c.GetValue() == false && p.GetValue() == false) || (c.GetValue() == true && p.GetValue() == true))
            {
                //PASS MODE:
                int time = Math.Max(d.GetTime(),Math.Max(c.GetTime(), p.GetTime()));
                GameObject square = DelayHandler.NewSquare(0,d.ActiveColor(),delayVisualizer,tick);
                signalQueue.Add(Tuple.Create(d.NewToken(time),square));
                
            }else if((c.GetValue() == true && p.GetValue() == false) || (c.GetValue() == false && p.GetValue() == true)){
                //CAPTURE MODE:
                int time = Math.Max(c.GetTime(), p.GetTime());
                if(d.GetTime()<= time){
                    if(CheckSetup(time)){
                        GameObject square = DelayHandler.NewSquare(0,d.ActiveColor(),delayVisualizer,tick);
                        signalQueue.Add(Tuple.Create(d.NewToken(time),square));
                    }
                }else{
                    CheckHold(time);
                }
            }
        }

        if (signalQueue.Count > 1)
        {

            BitToken nextOut = signalQueue[1].Item1;
            int arrivalTime = nextOut.GetTime();

            //check if next output signal is ready
            if (arrivalTime + delay <= tick)
            {
                //set output and remove from signal queue
                if(signalQueue[0].Item2 != null) GameObject.Destroy(signalQueue[0].Item2);
                signalQueue.RemoveAt(0);
                dataOut.SetValue(nextOut.NewToken(arrivalTime + delay));  
            }
        } 

        //Draw each signal delay squares:
        DelayHandler.DrawSquares(signalQueue,delay,tick); 
    }

    public override void Reset()
    {
        sRend.color = defaultColor;
        lastData = new BitToken();
        errorMessage.text = "";

        foreach(Tuple<BitToken,GameObject> t in signalQueue){
            if(t.Item2 != null) GameObject.Destroy(t.Item2);
        }
        signalQueue.Clear();

        capture.SetValue(new BitToken());
        pass.SetValue(new BitToken());
        dataIn.SetValue(new BitToken());
        dataOut.SetValue(new BitToken());

        DelayInit();
    }

    private bool CheckSetup(int sampleTime){
        if(lastData.GetTime() + setup <= sampleTime){
            //correct setup
            return true;
        }else{
            //error setup
            sRend.color = errorColor;
            errorMessage.text = "SETUP!";
            timer.pause(true);
            return false;
        }
    }

    private bool CheckHold(int sampleTime){
        if(lastData.GetTime() >= sampleTime + hold){
            //correct hold
            return true;
        }
        else{
            //error hold
            sRend.color = errorColor;
            errorMessage.text = "HOLD!";
            timer.pause(true);
            return false;
        }
    }
    
    public override Component Load()
    {
        GameObject prefab = Resources.Load("Prefabs/CPLatch") as GameObject;
        GameObject o = GameObject.Instantiate(prefab, pos, rot);
        ComponentMono c = o.GetComponent<ComponentMono>();
        c.Load(this);

        Subscribe();
        return c;
    }

    public override void LoadPins(InputPin_Mono[] inPins, OutputPin_Mono[] outPins)
    {
        inPins[0].Init(capture);
        inPins[1].Init(pass);
        inPins[2].Init(dataIn);
        outPins[0].Init(dataOut);
    }

    public override void LoadDelay(GameObject delayVisualizer)
    {
        this.delayVisualizer = delayVisualizer;
        DelayInit();
    }

    
    public void DelayInit(){
        lastData = new BitToken();
        GameObject square = DelayHandler.NewSquare(100,lastData.ActiveColor(),delayVisualizer,1);
        signalQueue.Add(Tuple.Create(lastData,square));
        delayVisualizer.SetActive(visualizerOn);
    }

    private void Subscribe()
    {
        Dispose();
        
        if(visualizerOn){
            timer.TimerTickEvent += OnTickVisualizeDelay;
        }else if(delay > 0){
            timer.TimerTickEvent += OnTickDelay;  
        }else{
            timer.TimerTickEvent += OnTickNoDelay;
        }
    }

    public override void Dispose()
    {
        timer.TimerTickEvent -= OnTickNoDelay;
        timer.TimerTickEvent -= OnTickDelay;
        timer.TimerTickEvent -= OnTickVisualizeDelay;
    }

    public void VisualizeDelay(bool on)
    {
        visualizerOn = on;
        delayVisualizer.SetActive(visualizerOn);
        Subscribe();
    }

    public bool IsVisualizerActive(){
        return visualizerOn;
    }

    public int parseDelay(string inputDelay){
        if (inputDelay.Length > 0)
        {   
            try{
                this.delay = int.Parse(inputDelay);
            }
            catch{
                
            }
        }
        return this.delay;
    }

    public int parseSetup(string inputSetup){
        if (inputSetup.Length > 0)
        {   
            try{
                this.setup = int.Parse(inputSetup);
            }
            catch{
                
            }
        }
        return this.setup;
    }

    public int parseHold(string inputHold){
        if (inputHold.Length > 0)
        {   
            try{
                this.hold = int.Parse(inputHold);
            }
            catch{
                
            }
        }
        return this.hold;
    }

    public override void OpenEditor()
    {
        editor.SetActive(true);
        CPLatchEditor e = editor.GetComponent<CPLatchEditor>();
        e.init(this);
    }
}



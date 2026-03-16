using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/**
 * @author: Stefan Moser
 * 
 * @brief: This class describes the behaviour of a latch
 * 
 * @details: A Latch has two inputs (data, clock) and one data output. If the clock input is high, all inputs
 * get transferred to the output. If the clock input is low, the last sampled input is hold at the output.
 * 
 * 
 * */
public class Latch : CircuitComponent , IDelay
{
    //PINS:
    [JsonProperty] private InputPin clk;         //clock input pin
    [JsonProperty] private InputPin dataIn;      //data input pin
    [JsonProperty] private OutputPin dataOut;    //data output pin

    //TIMING PARAMETERS:
    [JsonProperty] private int setup = 0;        //setup time
    [JsonProperty] private int hold = 0;         //hold time
    [JsonProperty] private int delay = 0;        //delay

    //DELAY VISUALIZATION:
    [JsonIgnore] private List<Tuple<BitToken, GameObject>> signalQueue;        //all the signals currently traveling through this delay element
    [JsonIgnore] public GameObject delayVisualizer;                         //used for visualizing the delay and the signal transitions on the output
    [JsonProperty] public bool visualizerOn = false;    
    [JsonIgnore] private TimeTick timer;                                    //global simulation timer

    //last inputs:
    [JsonIgnore] private BitToken lastClk = new BitToken();
    [JsonIgnore] private BitToken lastDataIn = new BitToken();
    [JsonIgnore] private BitToken storedData = new BitToken();

    //parameters for animation:
    [JsonIgnore] public SpriteRenderer sRend;
    [JsonIgnore] public TextMeshPro errorMessage;
    [JsonIgnore] private Color defaultColor = new Color32(0x78,0x85, 0x8D, 0xFF);
    [JsonIgnore] private Color errorColor = new Color32(0xFF, 0x13, 0x00, 0xFF);

    [JsonIgnore] private GameObject editor;

    /**
     * Constructor of Latch object
     * */
    public Latch()
    {
        //init the latch pins:
        this.clk = new InputPin();
        this.dataIn = new InputPin();
        this.dataIn.width = 0; // accept any bit width
        this.dataOut = new OutputPin();

        signalQueue = new List<Tuple<BitToken,GameObject>>();

        //subscribe to Timer events:
        GameObject o = GameObject.FindWithTag("Timer");
        timer = o.GetComponent<TimeTick>();
        Subscribe();

        o = GameObject.FindWithTag("Editor");
        editor = o.transform.Find("LatchEditor")?.gameObject;
    }

    //Tick Event: Behaviour when gate does not have a delay
    private void OnTickNoDelay(int tick){
        BitToken c = clk.data;
        BitToken d = dataIn.data;

        if(!c.EqualsToken(lastClk) || !d.EqualsToken(lastDataIn)){
            lastClk = c;
            lastDataIn = d;

            if(c.GetValue() == true){
                int newTime = Math.Max(c.GetTime(), d.GetTime());
                dataOut.SetValue(d.NewToken(newTime));
            }else{
                if(d.GetTime() <= c.GetTime()){
                    //sample:
                    if(CheckSetup()){
                        storedData = d.NewToken(d.GetTime());
                        dataOut.SetValue(storedData);
                    }
                }else{
                    //no sampling => check hold time
                    CheckHold();
                }
            }
        }
    }

    //Tick Event: Behaviour when gate has a delay > 0
    private void OnTickDelay(int tick){
        if (signalQueue.Count == 0) DelayInit();
        BitToken c = clk.data;
        BitToken d = dataIn.data;

        if(!c.EqualsToken(lastClk) || !d.EqualsToken(lastDataIn)){
            lastClk = c;
            lastDataIn = d;

            if(c.GetValue() == true){
                int newTime = Math.Max(c.GetTime(), d.GetTime());
                signalQueue.Add(Tuple.Create(d.NewToken(newTime),(GameObject)null));
            }else{
                if(d.GetTime() <= c.GetTime()){
                    //sample:
                    if(CheckSetup()){
                        storedData = d.NewToken(d.GetTime());
                        signalQueue.Add(Tuple.Create(storedData,(GameObject)null));
                    }
                }else{
                    //no sampling => check hold time
                    CheckHold();
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

     //Tick Event: Behaviour with delay visualization
    private void OnTickVisualizeDelay(int tick){
        if (signalQueue.Count == 0) DelayInit();
        BitToken c = clk.data;
        BitToken d = dataIn.data;

        if(!c.EqualsToken(lastClk) || !d.EqualsToken(lastDataIn)){
            lastClk = c;
            lastDataIn = d;

            if(c.GetValue() == true){
                int newTime = Math.Max(c.GetTime(), d.GetTime());
                BitToken result = d.NewToken(newTime);
                GameObject square = DelayHandler.NewSquare(0,result.ActiveColor(),delayVisualizer,tick);
                signalQueue.Add(Tuple.Create(result,square));
            }else{
                if(d.GetTime() <= c.GetTime()){
                    //sample:
                    if(c.GetTime() >= setup)CheckSetup();
                }else{
                    //no sampling => check hold time
                    CheckHold();
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


    //Event: Restart simulation
    public override void Reset()
    {
        if (sRend != null) sRend.color = defaultColor;
        storedData = new BitToken();
        if (errorMessage != null) errorMessage.text = "";

        foreach(Tuple<BitToken,GameObject> t in signalQueue){
            if(t.Item2 != null) GameObject.Destroy(t.Item2);
        }
        signalQueue.Clear();

        clk.SetValue(new BitToken());
        dataIn.SetValue(new BitToken());
        dataOut.SetValue(new BitToken(dataOut.width));

        DelayInit();
    }


    //Check setup time
    private bool CheckSetup(){

        if(lastDataIn.GetTime() + setup <= lastClk.GetTime()){
            //correct setup
            return true;
        }else{
            //error setup
            if (sRend != null) sRend.color = errorColor;
            if (errorMessage != null) errorMessage.text = "SETUP!";
            InformationWindow.Show("Setup Time Violation",
                "Data arrived " + (lastDataIn.GetTime() - (lastClk.GetTime() - setup))
                + " ticks too late (setup=" + setup + ").\n\n"
                + "The data input must be stable at least " + setup + " ticks before the clock edge.");
            return false;
        }

    }

    private bool CheckHold(){
        if(lastDataIn.GetTime() >= lastClk.GetTime() + hold){
            //correct hold
            return true;
        }
        else{
            //error hold
            if (sRend != null) sRend.color = errorColor;
            if (errorMessage != null) errorMessage.text = "HOLD!";
            InformationWindow.Show("Hold Time Violation",
                "Data changed " + ((lastClk.GetTime() + hold) - lastDataIn.GetTime())
                + " ticks too early (hold=" + hold + ").\n\n"
                + "The data input must remain stable for at least " + hold + " ticks after the clock edge.");
            return false;
        }
    }

    public override Component Load()
    {
        GameObject prefab = Resources.Load("Prefabs/Latch") as GameObject;
        GameObject o = GameObject.Instantiate(prefab, pos, rot);
        ComponentMono c = o.GetComponent<ComponentMono>();
        c.Load(this);
        Subscribe();
        return c;
    }

    public override void LoadPins(InputPin_Mono[] inPins, OutputPin_Mono[] outPins)
    {
        inPins[0].Init(clk);
        inPins[1].Init(dataIn);
        outPins[0].Init(dataOut);
    }

    public override void LoadDelay(GameObject delayVisualizer)
    {
        this.delayVisualizer = delayVisualizer;
        DelayInit();
    }

    public void DelayInit(){
        lastDataIn = new BitToken();
        GameObject square = delayVisualizer != null ? DelayHandler.NewSquare(100,lastDataIn.ActiveColor(),delayVisualizer,1) : null;
        signalQueue.Add(Tuple.Create(lastDataIn,square));
        if (delayVisualizer != null) delayVisualizer.SetActive(visualizerOn);
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

    [System.Runtime.Serialization.OnDeserialized]
    internal void OnDeserialized(System.Runtime.Serialization.StreamingContext context)
    {
        Subscribe();
    }


    //unsubscribe from all event so garbage collector can delete object
    public override void Dispose()
    {
        timer.TimerTickEvent -= OnTickNoDelay;
        timer.TimerTickEvent -= OnTickDelay;
        timer.TimerTickEvent -= OnTickVisualizeDelay;
    }

    public void SetDelay(int delay)
    {
        this.delay = delay;
        Subscribe();
    }

    public void VisualizeDelay(bool on)
    {
        visualizerOn = on;
        if (delayVisualizer != null) delayVisualizer.SetActive(visualizerOn);
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

    public override int GetDelay() { return delay; }
    public override void CollectPins(List<InputPin> inputs, List<OutputPin> outputs)
    {
        inputs.Add(clk); inputs.Add(dataIn); outputs.Add(dataOut);
    }

    public override void OpenEditor()
    {
        editor.SetActive(true);
        editor.GetComponent<LatchEditor>().init(this);
    }
}

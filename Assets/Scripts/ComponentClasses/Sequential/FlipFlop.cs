using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using TMPro;
using Unity.Properties;

/**
 * @author: Stefan Moser
 * 
 * @brief: this class describes the logical behaviour and state of a flip-flop
 * 
 * @details: A flip-flop has 2 input pins (clk and dataIn) and one output pin.
 * The input data is sampled and transferred to the output only on the rising clock edge, 
 * meaning when the clk input changes from low to high.
 * */
public class FlipFlop : CircuitComponent, IDelay
{
    //PINS:
    [JsonProperty] private InputPin clk;         //clock input pin
    [JsonProperty] private InputPin dataIn;      //data input pin
    [JsonProperty] private OutputPin dataOut;    //data output pin

    //TIMING PARAMETERS:
    [JsonProperty] private int delay = 0;        //Clock-to-Q Delay
    [JsonProperty] private int setup = 0;
    [JsonProperty] private int hold = 0;


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
     * Constructor of FlipFlop object
     * */
    public FlipFlop()
    {
        //init the flip-flop pins:
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
        editor = o.transform.Find("FlopEditor")?.gameObject;
    }

    //Tick Event: Behaviour when gate does not have a delay
    private void OnTickNoDelay(int tick){
        BitToken c = clk.data;
        BitToken d = dataIn.data;

        if(!c.EqualsToken(lastClk) || !d.EqualsToken(lastDataIn)){
            lastClk = c;
            lastDataIn = d;

            if(lastClk.GetValue() == true){
                if(lastDataIn.GetTime() <= lastClk.GetTime()){
                    //sample:
                    if(CheckSetup()){
                        storedData = dataIn.data.NewToken(lastClk.GetTime());
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
        BitToken c = clk.data;
        BitToken d = dataIn.data;

        if(!c.EqualsToken(lastClk) || !d.EqualsToken(lastDataIn)){
            lastClk = c;
            lastDataIn = d;

            if(lastClk.GetValue() == true){
                if(lastDataIn.GetTime() <= lastClk.GetTime()){
                    //sample:
                    if(CheckSetup()){
                        storedData = dataIn.data.NewToken(lastClk.GetTime());
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
        BitToken c = clk.data;
        BitToken d = dataIn.data;

        if(!c.EqualsToken(lastClk) || !d.EqualsToken(lastDataIn)){
            lastClk = c;
            lastDataIn = d;

            if(lastClk.GetValue() == true){
                if(lastDataIn.GetTime() <= lastClk.GetTime()){
                    //sample:
                    if(CheckSetup()){
                        storedData = dataIn.data.NewToken(lastClk.GetTime());
                        GameObject square = DelayHandler.NewSquare(0,storedData.ActiveColor(),delayVisualizer,tick);
                        signalQueue.Add(Tuple.Create(storedData,square));
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
        //Draw each signal delay squares:
        DelayHandler.DrawSquares(signalQueue,delay,tick);
    }

    //Event: Restart simulation
    public override void Reset()
    {
        sRend.color = defaultColor;
        storedData = new BitToken();
        errorMessage.text = "";

        foreach(Tuple<BitToken,GameObject> t in signalQueue){
            if(t.Item2 != null) GameObject.Destroy(t.Item2);
        }
        signalQueue.Clear();

        clk.SetValue(new BitToken());
        dataIn.SetValue(new BitToken());
        dataOut.SetValue(new BitToken());

        DelayInit();

    }

    //Check setup time
    private bool CheckSetup(){
        if(lastDataIn.GetTime() + setup <= lastClk.GetTime()){
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

    private bool CheckHold(){
        if(lastDataIn.GetTime() >= lastClk.GetTime() + hold){
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
        GameObject prefab = Resources.Load("Prefabs/FlipFlop") as GameObject;
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
        GameObject square = DelayHandler.NewSquare(100,lastDataIn.ActiveColor(),delayVisualizer,1);
        signalQueue.Add(Tuple.Create(lastDataIn,square));
        delayVisualizer.SetActive(visualizerOn);
    }

    public void SetDelay(int delay)
    {
        this.delay = delay;
        Subscribe();
    }

    public int GetDelay()
    {
        return this.delay;
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


    //Subscribes to all the important events
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

    //unsubscribe from all event so garbage collector can delete object
    public override void Dispose()
    {
        timer.TimerTickEvent -= OnTickNoDelay;
        timer.TimerTickEvent -= OnTickDelay;
        timer.TimerTickEvent -= OnTickVisualizeDelay;
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
        editor.GetComponent<FlopEditor>().init(this);
    }
}

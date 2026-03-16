using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class XOR : CircuitComponent, IDelay
{
    //PINS:
    [JsonProperty] private InputPin dataA;       //input pin a
    [JsonProperty] private InputPin dataB;       //input pin b
    [JsonProperty] private OutputPin dataOut;    //output pin 


    //DELAY:
    [JsonProperty] private int delay = 0;                                   //number of timer-ticks till input gets transferred to output 
    [JsonIgnore] private List<Tuple<BitToken, GameObject>> signalQueue;        //all the signals currently traveling through this delay element
    [JsonIgnore] public GameObject delayVisualizer;                         //used for visualizing the delay and the signal transitions on the output
    [JsonProperty] public bool visualizerOn = false;


    [JsonIgnore] private TimeTick timer;            //global simulation timer
    [JsonIgnore] private BitToken lastA = new BitToken();       //last input on Pin A
    [JsonIgnore] private BitToken lastB = new BitToken();       //last input on Pin B

    [JsonIgnore] private GameObject editor;

    /**
     * Constructor of a XOR Object
     * */
    public XOR()
    {
        //init the pins of the MullerC-element:
        this.dataA = new InputPin();
        this.dataB = new InputPin();
        this.dataOut = new OutputPin();

        signalQueue = new List<Tuple<BitToken,GameObject>>();

        //subscribe to Timer events:
        GameObject o = GameObject.FindWithTag("Timer");
        timer = o.GetComponent<TimeTick>();
        Subscribe();
        
        o = GameObject.FindWithTag("Editor");
        editor = o.transform.Find("DelayEditor")?.gameObject;
    }

    //Tick Event: Behaviour when gate does not have a delay
    private void OnTickNoDelay(int tick){
        //new inputs:
        BitToken A = dataA.data;
        BitToken B = dataB.data;

        if(!A.EqualsToken(lastA) || !B.EqualsToken(lastB)){
            lastA = A;
            lastB = B;
            BitToken last = (A.GetTime() >= B.GetTime()) ? A : B;
            BitToken xor = new BitToken(A.GetValue()^B.GetValue(),last.GetTime(),last.TokenColor());


            //set ouput to computed result:
            dataOut.SetValue(xor); 
        }
    }

    //Tick Event: Behaviour when gate has a delay > 0
    private void OnTickDelay(int tick){
        if (signalQueue.Count == 0) DelayInit();
        //new inputs:
        BitToken A = dataA.data;
        BitToken B = dataB.data;

        if(!A.EqualsToken(lastA) || !B.EqualsToken(lastB)){
            lastA = A;
            lastB = B;
            BitToken last = (A.GetTime() >= B.GetTime()) ? A : B;
            BitToken xor = new BitToken(A.GetValue()^B.GetValue(),last.GetTime(),last.TokenColor());


            //add computed result to delay queue:
            signalQueue.Add(Tuple.Create(xor,(GameObject)null));
        }

        //check signal queue
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
        BitToken A = dataA.data;
        BitToken B = dataB.data;

        if(!A.EqualsToken(lastA) || !B.EqualsToken(lastB)){
            lastA = A;
            lastB = B;
            BitToken last = (A.GetTime() >= B.GetTime()) ? A : B;
            BitToken xor = new BitToken(A.GetValue()^B.GetValue(),last.GetTime(),last.TokenColor());

            //add result to queue: 
            GameObject square = DelayHandler.NewSquare(0,xor.ActiveColor(),delayVisualizer,tick);
            signalQueue.Add(Tuple.Create(xor,square));
        }

        //check signal queue
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

        DelayHandler.DrawSquares(signalQueue,delay,tick);
    }

    public override Component Load()
    {
        GameObject prefab = Resources.Load("Prefabs/XOR") as GameObject;
        GameObject o = GameObject.Instantiate(prefab, pos, rot);
        ComponentMono c = o.GetComponent<ComponentMono>();
        c.Load(this);
        Subscribe();
        return c;
    }

    public override void LoadPins(InputPin_Mono[] inPins, OutputPin_Mono[] outPins)
    {
        inPins[0].Init(dataA);
        inPins[1].Init(dataB);
        outPins[0].Init(dataOut);
    }

    public override void LoadDelay(GameObject delayVisualizer)
    {
        this.delayVisualizer = delayVisualizer;
        DelayInit();
    }

    public override void Reset(){
        //clear delay queue and destroy the drawn squares
        foreach(Tuple<BitToken,GameObject> t in signalQueue){
            if(t.Item2 != null) GameObject.Destroy(t.Item2);
        }
        signalQueue.Clear();

        dataA.SetValue(new BitToken());
        dataB.SetValue(new BitToken());
        dataOut.SetValue(new BitToken());

        DelayInit();
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

    public void DelayInit(){
        BitToken initSignal = new BitToken();
        GameObject square = delayVisualizer != null ? DelayHandler.NewSquare(100,initSignal.ActiveColor(),delayVisualizer,1) : null;
        signalQueue.Add(Tuple.Create(initSignal,square));
        if (delayVisualizer != null) delayVisualizer.SetActive(visualizerOn);
    }

    public override void Dispose()
    {
        //unsubscribe from input events
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

    public void VisualizeDelay(bool on)
    {
        visualizerOn = on;
        if (delayVisualizer != null) delayVisualizer.SetActive(visualizerOn);
        Subscribe();
    }

    public bool IsVisualizerActive(){
        return visualizerOn;
    }

    public override int GetDelay() { return delay; }
    public override void CollectPins(List<InputPin> inputs, List<OutputPin> outputs)
    {
        inputs.Add(dataA); inputs.Add(dataB); outputs.Add(dataOut);
    }

    public override void OpenEditor()
    {
        editor.SetActive(true);
        DelayEditor e = editor.GetComponent<DelayEditor>();
        e.init(this);
        e.SetTitle("XOR-Gate");
        e.SetDescription("The XOR gate outputs 1 if one, and only one, of the inputs is 1, otherwise it outputs 0.");
    }
}

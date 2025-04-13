using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

/**
 * @author: Stefan Moser
 * 
 * @brief: This class describes the behaviour of AND gate.
 * 
 * @details: The AND Gate outputs 1, only and only if both inputs are 1, otherwise it outputs 0. The behaviour of 
 * the AND gate is implemented in the "OnTick..." methods, while there are difference if the component has a propagation delay
 * or also visualizes the delay. 
 * */
public class AND : Loadable, IDelay
{
    //PINS:
    [JsonProperty] private InputPin dataA;       //input pin a
    [JsonProperty] private InputPin dataB;       //input pin b
    [JsonProperty] private OutputPin dataOut;    //output pin 

    //DELAY:
    [JsonProperty] private int delay = 0;                                    //number of timer-ticks till input gets transferred to output 
    [JsonIgnore] private List<Tuple<BitToken, GameObject>> signalQueue;        //all the signals currently traveling through this delay element
    [JsonIgnore] public GameObject delayVisualizer;                         //used for visualizing the delay and the signal transitions on the output
    [JsonProperty] public bool visualizerOn = false;


    [JsonIgnore] private TimeTick timer;            //global simulation timer
    [JsonIgnore] private BitToken lastA = new BitToken();
    [JsonIgnore] private BitToken lastB = new BitToken();

    /**
     * Constructor of a AND Object
     * */
    public AND()
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
            BitToken and = new BitToken(A.GetValue() & B.GetValue(),last.GetTime(),last.TokenColor());
            dataOut.SetValue(and); 
        }
    }

    
    //Tick Event: Behaviour when gate has a delay > 0
    private void OnTickDelay(int tick){
        //new inputs:
        BitToken A = dataA.data;
        BitToken B = dataB.data;

        //calculate new output if input has changed:
        if(!A.EqualsToken(lastA) || !B.EqualsToken(lastB)){
            lastA = A;
            lastB = B;
            BitToken last = (A.GetTime() >= B.GetTime()) ? A : B;
            BitToken and = new BitToken(A.GetValue() & B.GetValue(),last.GetTime(),last.TokenColor());

            signalQueue.Add(Tuple.Create(and,new GameObject()));
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
                GameObject.Destroy(signalQueue[0].Item2);
                signalQueue.RemoveAt(0);
                dataOut.SetValue(nextOut.NewToken(arrivalTime + delay));  
            }
        }
    }

    //Tick Event: Behaviour with delay visualization
    private void OnTickVisualizeDelay(int tick){
        //new inputs:
        BitToken A = dataA.data;
        BitToken B = dataB.data;

        //calculate new output if input has changed:
        if(!A.EqualsToken(lastA) || !B.EqualsToken(lastB)){
            lastA = A;
            lastB = B;
            BitToken last = (A.GetTime() >= B.GetTime()) ? A : B;
            BitToken and = new BitToken(A.GetValue() & B.GetValue(),last.GetTime(),last.TokenColor());
            
            //add result to queue: 
            GameObject square = DelayHandler.NewSquare(0,and.ActiveColor(),delayVisualizer,tick);
            signalQueue.Add(Tuple.Create(and,square));
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
                GameObject.Destroy(signalQueue[0].Item2);
                signalQueue.RemoveAt(0);
                dataOut.SetValue(nextOut.NewToken(arrivalTime + delay));  
            }
        }

        DelayHandler.DrawSquares(signalQueue,delay,tick);
    }

    public override Component Load()
    {
        GameObject prefab = Resources.Load("Prefabs/AND") as GameObject;
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

    public override void Reset(){
        //clear delay queue and destroy the drawn squares
        foreach(Tuple<BitToken,GameObject> t in signalQueue){
            GameObject.Destroy(t.Item2);
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

    public override void Dispose()
    {
        //unsubscribe from timer events
        timer.TimerTickEvent -= OnTickNoDelay;
        timer.TimerTickEvent -= OnTickDelay;
        timer.TimerTickEvent -= OnTickVisualizeDelay;
    }

    public void DelayInit(){
        BitToken initSignal = new BitToken();
        GameObject square = DelayHandler.NewSquare(100,initSignal.ActiveColor(),delayVisualizer,1);
        signalQueue.Add(Tuple.Create(initSignal,square));
        delayVisualizer.SetActive(visualizerOn);
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
        delayVisualizer.SetActive(visualizerOn);
        Subscribe();
    }

    public bool IsVisualizerActive()
    {
        return visualizerOn;
    }

    public override void LoadDelay(GameObject delayVisualizer)
    {
        this.delayVisualizer = delayVisualizer;
        DelayInit();
    }
}

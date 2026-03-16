using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

/**
 * @author: Stefan Moser
 * 
 * @brief: This class describes the behaviour of a Muller-C element.
 * 
 * @details: The Muller C element has two inputs (a,b) and one output. The output is set to 0 when both inputs are 0, and
 * to 1 when both inputs are 1. For any other input combinations the value does not change.
 * */
public class MullerC : CircuitComponent, IDelay
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

    [JsonIgnore] private TimeTick timer;        //global simulation timer

    
    [JsonIgnore] private BitToken lastA = new BitToken();     //last input on Pin A
    [JsonIgnore] private BitToken lastB = new BitToken();  //last input on PIn B
    [JsonIgnore] private BitToken lastResult = new BitToken();   //last computed result

    [JsonIgnore] private GameObject editor;

    /**
     * Constructor of a MullerC Object
     * */
    public MullerC()
    {
        //init the pins of the MullerC-element:
        this.dataA = new InputPin();
        this.dataB = new InputPin();
        this.dataOut = new OutputPin();


        lastResult = new BitToken();
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

        BitToken A = dataA.data;
        BitToken B = dataB.data;

        //new inputs?
        if(!A.EqualsToken(lastA) || !B.EqualsToken(lastB)){
            
            lastA = A;
            lastB = B;

            //compute new output according to MUllerC:
            if (A.GetValue().Equals(B.GetValue()))
            {
                BitToken last = (A.GetTime() >= B.GetTime()) ? A : B;
                BitToken lastResult = new BitToken(last.GetValue(),last.GetTime(),last.TokenColor());

                dataOut.SetValue(lastResult);                
            }
        }
    }

    //Tick Event: Behaviour when gate has a delay > 0
    private void OnTickDelay(int tick){

        BitToken A = dataA.data;
        BitToken B = dataB.data;

        //new inputs?
        if(!A.EqualsToken(lastA) || !B.EqualsToken(lastB)){
            
            lastA = A;
            lastB = B;

            //compute new output according to MUllerC:
            if (A.GetValue().Equals(B.GetValue()))
            {
                BitToken last = (A.GetTime() >= B.GetTime()) ? A : B;
                BitToken lastResult = new BitToken(last.GetValue(),last.GetTime(),last.TokenColor());

                signalQueue.Add(Tuple.Create(lastResult,(GameObject)null));
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
    private void OnTickVisualizeDelay(int tick)
    {
        
        BitToken A = dataA.data;
        BitToken B = dataB.data;

        //new inputs?
        if(!A.EqualsToken(lastA) || !B.EqualsToken(lastB)){
            
            lastA = A;
            lastB = B;

            //compute new output according to MUllerC:
            if (A.GetValue().Equals(B.GetValue()))
            {
                BitToken last = (A.GetTime() >= B.GetTime()) ? A : B;
                BitToken lastResult = new BitToken(last.GetValue(),last.GetTime(),last.TokenColor());

                //add result to queue: 
                GameObject square = DelayHandler.NewSquare(0,lastResult.ActiveColor(),delayVisualizer,tick);
                signalQueue.Add(Tuple.Create(lastResult,square));
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

    //reset all internal values
    public override void Reset(){
        lastResult = new BitToken();
        //delete delay visualization
        foreach(Tuple<BitToken,GameObject> t in signalQueue){
            if(t.Item2 != null) GameObject.Destroy(t.Item2);
        }
        signalQueue.Clear();

        //reset all the values:
        dataA.SetValue(lastResult);
        dataB.SetValue(lastResult);
        dataOut.SetValue(lastResult);

        DelayInit();
    }

    /**
    * Instantiate a MullerC prefab and initialize with the contents of this class.
    * This method is used for deserelization when loading a saved project.
    **/
    public override Component Load()
    {
        GameObject prefab = Resources.Load("Prefabs/MullerC") as GameObject;
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

    /**
    * Subscribe to all the necessary events
    **/
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

    public void DelayInit(){
        lastResult = new BitToken(); 
        GameObject square = DelayHandler.NewSquare(100,lastResult.ActiveColor(),delayVisualizer,1);
        signalQueue.Add(Tuple.Create(lastResult,square));
        delayVisualizer.SetActive(visualizerOn);
    }


    public override void Dispose()
    {
        //unsubscribe from timer events
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
        delayVisualizer.SetActive(visualizerOn);
        Subscribe();
    }

    public bool IsVisualizerActive(){
        return visualizerOn;
    }

    public override void OpenEditor()
    {
        editor.SetActive(true);
        DelayEditor e = editor.GetComponent<DelayEditor>();
        e.init(this);
        e.SetTitle("Muller C-Element");
        e.SetDescription("The C-Elemet outputs 1, when all inputs are 1 and 0, when all inputs are 0.\n\nFor all other input combinations, the C-gate holds it’s current state.");
    }
}

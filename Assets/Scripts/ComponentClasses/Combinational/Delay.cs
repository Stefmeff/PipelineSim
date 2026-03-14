using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/**
 * @author: Stefan Moser
 * 
 * @brief: This class describes a delay element
 * 
 * @details: The delay element acts similar to a FIFO. The input signals get added to a queue, and after the delay
 * has passed the signals get transferred to the output and removed from the queue.
 * 
 * The timing is realized by listening to a global timer that synchronizes all the events happening in the simulation.
 * 
 * For further details, please read the documentation and code below!
 * */
public class Delay : CircuitComponent, IDelay
{
    //PINS:
    [JsonProperty] private InputPin dataIn;              //delay's input pin
    [JsonProperty] private OutputPin dataOut;            //delay's output pin

    //DELAY:
    [JsonProperty] private int delay = 0;                    //number of timer-ticks till input gets transferred to output
    [JsonIgnore] private List<Tuple<BitToken, GameObject>> signalQueue;     //all the signals currently traveling through this delay element
    [JsonIgnore] public GameObject delayVisualizer;                         //used for visualizing the delay and the signal transitions on the output
    [JsonProperty] public bool visualizerOn = false;
    [JsonIgnore] private BitToken lastDataIn;

    [JsonIgnore] private TimeTick timer;                //global simulation timer
    
    [JsonIgnore] private GameObject editor;


    /**
     * Constructor of Delay Object
     * 
     * @param delay for signal traveling through this element
     * */
    public Delay()
    {
        //init pins:
        this.dataIn = new InputPin();
        this.dataIn.width = 0; // accept any bit width
        this.dataOut = new OutputPin();
        lastDataIn = new BitToken();

        signalQueue = new List<Tuple<BitToken,GameObject>>();
        dataOut.SetValue(new BitToken());

        //Get timer reference
        GameObject o = GameObject.FindWithTag("Timer");
        timer = o.GetComponent<TimeTick>();
        Subscribe();

        o = GameObject.FindWithTag("Editor");
        editor = o.transform.GetChild(1).gameObject;
    }


       //Tick Event: Behaviour when gate does not have a delay
    private void OnTickNoDelay(int tick){
        BitToken d = dataIn.data;
        if(!d.EqualsToken(lastDataIn)){
            lastDataIn = d.NewToken(d.GetTime());
            dataOut.SetValue(lastDataIn);
        }
    }

    //Tick Event: Behaviour when gate has a delay > 0
    private void OnTickDelay(int tick){
        BitToken d = dataIn.data;
        if(!d.EqualsToken(lastDataIn)){
            lastDataIn = d.NewToken(d.GetTime());
            signalQueue.Add(Tuple.Create(lastDataIn,(GameObject)null));
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
        BitToken d = dataIn.data;
        if(!d.EqualsToken(lastDataIn)){
            lastDataIn = d.NewToken(d.GetTime());

            //add result to queue: 
            GameObject square = DelayHandler.NewSquare(0,lastDataIn.ActiveColor(),delayVisualizer,tick);
            signalQueue.Add(Tuple.Create(lastDataIn,square));        }

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



    //Event: Restart of the simulation => reset delay element
    public override void Reset()
    {
        foreach(Tuple<BitToken,GameObject> t in signalQueue){
            if(t.Item2 != null) GameObject.Destroy(t.Item2);
        }
        signalQueue.Clear();

        dataIn.SetValue(new BitToken());
        dataOut.SetValue(new BitToken());

        DelayInit();
    }


    //Load delay prefab and init with this delay object
    public override Component Load()
    {
        GameObject prefab = Resources.Load("Prefabs/Delay") as GameObject;
        GameObject o = GameObject.Instantiate(prefab, pos, rot);
        ComponentMono c = o.GetComponent<ComponentMono>();
        c.Load(this);
        Subscribe();
        return c;
    }

    public override void LoadPins(InputPin_Mono[] inPins, OutputPin_Mono[] outPins)
    {
        inPins[0].Init(dataIn);
        outPins[0].Init(dataOut);
    }


    public override void LoadDelay(GameObject delayVisualizer)
    {
        this.delayVisualizer = delayVisualizer;
        DelayInit();
    }

    //Subscribe to all the important events
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

    private void DelayInit(){
        lastDataIn = new BitToken();
        GameObject square = DelayHandler.NewSquare(100,lastDataIn.ActiveColor(),delayVisualizer,1);
        signalQueue.Add(Tuple.Create(lastDataIn,square));
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

    public bool IsVisualizerActive(){
        return visualizerOn;
    }

    public override void OpenEditor()
    {
        editor.SetActive(true);
        DelayEditor e = editor.GetComponent<DelayEditor>();
        e.init(this);
        e.SetTitle("Propagation Delay");
        e.SetDescription("The propagation delay delays incoming signals by the given time.");
    }
}

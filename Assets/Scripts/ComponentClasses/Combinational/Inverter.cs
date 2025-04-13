using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * @author: Stefan Moser
 * 
 * @details: This class describes the behaviour of an Inverter. It takes bit input signals and negates
 * them at the output.
 * */
public class Inverter : Loadable, IDelay
{ 
    //PINS:
    [JsonProperty] private InputPin dataIn;          //data input pin
    [JsonProperty] private OutputPin dataOut;        //data output pin

    //DELAY:
    [JsonProperty] private int delay = 0;                                    //number of timer-ticks till input gets transferred to output 
    [JsonIgnore] private List<Tuple<BitToken, GameObject>> signalQueue;        //all the signals currently traveling through this delay element
    [JsonIgnore] public GameObject delayVisualizer;                         //used for visualizing the delay and the signal transitions on the output
    [JsonProperty] public bool visualizerOn = false;

    [JsonIgnore] private TimeTick timer;            //global simulation timer
    [JsonIgnore] private BitToken lastDataIn;

    //Constructor of an inverter:
    public Inverter()
    {
        //init pins:
        this.dataIn = new InputPin();
        this.dataOut = new OutputPin();

        lastDataIn = new BitToken(true, 0);

        signalQueue = new List<Tuple<BitToken,GameObject>>();

        //Get timer reference
        GameObject o = GameObject.FindWithTag("Timer");
        timer = o.GetComponent<TimeTick>();

        Subscribe();
    }

    //Tick Event: Behaviour when gate does not have a delay
    private void OnTickNoDelay(int tick){
        //if new input data => new output calculated with no delay
        if(!dataIn.data.EqualsToken(lastDataIn)){
            lastDataIn = dataIn.data;
            BitToken inverted = new BitToken(!lastDataIn.GetValue(), lastDataIn.GetTime(),lastDataIn.TokenColor());

            dataOut.SetValue(inverted);
        }
    }

    //Tick Event: Behaviour when gate has a delay > 0
    private void OnTickDelay(int tick){
        //if new input data => new output with delay
        if(!dataIn.data.EqualsToken(lastDataIn)){
            lastDataIn = dataIn.data;
            BitToken inverted = new BitToken(!lastDataIn.GetValue(), lastDataIn.GetTime(),lastDataIn.TokenColor());

            signalQueue.Add(Tuple.Create(inverted,new GameObject()));
        }

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

        //if new input data => new output with delay visulization 
        if(!dataIn.data.EqualsToken(lastDataIn)){
            lastDataIn = dataIn.data;
            BitToken inverted = new BitToken(!lastDataIn.GetValue(), lastDataIn.GetTime(),lastDataIn.TokenColor());

            //add result to queue: 
            GameObject square = DelayHandler.NewSquare(0,inverted.ActiveColor(),delayVisualizer,tick);
            signalQueue.Add(Tuple.Create(inverted,square));
        }

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
        //Draw each signal delay squares:
        DelayHandler.DrawSquares(signalQueue,delay,tick);
    }


    //Event: Restart of the simulation => reset delay element
    public override void Reset()
    {
        foreach(Tuple<BitToken,GameObject> t in signalQueue){
            GameObject.Destroy(t.Item2);
        }
        signalQueue.Clear();

        dataIn.SetValue(new BitToken());
        dataOut.SetValue(new BitToken());

        DelayInit();
    }


    public override Component Load()
    {
        GameObject prefab = Resources.Load("Prefabs/Inverter") as GameObject;
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

    private void DelayInit(){
        lastDataIn = new BitToken(true,0);
        GameObject square = DelayHandler.NewSquare(100,lastDataIn.ActiveColor(),delayVisualizer,1);
        signalQueue.Add(Tuple.Create(lastDataIn,square));
        delayVisualizer.SetActive(visualizerOn);
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

    public void VisualizeDelay(bool on)
    {
        visualizerOn = on;
        delayVisualizer.SetActive(visualizerOn);
        Subscribe();
    }

    public bool IsVisualizerActive(){
        return visualizerOn;
    }
}

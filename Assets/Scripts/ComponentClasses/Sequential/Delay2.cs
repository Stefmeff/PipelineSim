using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
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
public class Delay2 : Loadable
{
    //PINS:
    [JsonProperty] private InputPin dataIn;              //delay's input pin
    [JsonProperty] private OutputPin dataOut;            //delay's output pin

    //DELAY:
    [JsonProperty] public int delay = 0;                    //number of timer-ticks till input gets transferred to output
    [JsonIgnore] private List<Tuple<BitToken, GameObject>> signalQueue;     //all the signals currently traveling through this delay element   
    [JsonIgnore] public GameObject delayVisualizer;


    [JsonIgnore] private TimeTick timer;                //global simulation timer
    [JsonIgnore] private BitToken lastDataIn;



    /**
     * Constructor of Delay Object
     * 
     * @param delay for signal traveling through this element
     * */
    public Delay2()
    {
        //init pins:
        this.dataIn = new InputPin();
        this.dataOut = new OutputPin();

        signalQueue = new List<Tuple<BitToken,GameObject>>();

        lastDataIn = new BitToken();


        //subscribe to Timer events:
        GameObject o = GameObject.FindWithTag("Timer");
        timer = o.GetComponent<TimeTick>();

        Subscribe();
    }


    //Event: Time tick => sample new input signals and output signal if delay achieved
    private void OnTimerTick(int tick)
    {
        if (!dataIn.data.EqualsToken(lastDataIn))
        {
            lastDataIn = dataIn.data;

            //new input signal => add to queue (signal + square)
            GameObject square = DelayHandler.NewSquare(0,lastDataIn.ActiveColor(),delayVisualizer,tick);
            signalQueue.Add(Tuple.Create(lastDataIn,square));
        }
        
        if (signalQueue.Count > 1)
        {
            BitToken nextOut = signalQueue[1].Item1;
            int arrivalTime = nextOut.GetTime();

            //check if next output signal is ready
            if (arrivalTime + delay <= tick)
            {
                dataOut.SetValue(nextOut.NewToken(arrivalTime + delay));  
                //set output and remove from signal queue
                GameObject.Destroy(signalQueue[0].Item2);
                signalQueue.RemoveAt(0);

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


    //Load delay prefab and init with this delay object
    public override Component Load()
    {
        GameObject prefab = Resources.Load("Prefabs/Delay2") as GameObject;
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
        timer.TimerTickEvent += OnTimerTick;
    }

    //unsubscribe from timing events
    public override void Dispose()
    {
        timer.TimerTickEvent -= OnTimerTick;
    }

    public void DelayInit(){
        lastDataIn = new BitToken();
        GameObject square = DelayHandler.NewSquare(100,lastDataIn.ActiveColor(),delayVisualizer,1);
        signalQueue.Add(Tuple.Create(lastDataIn,square));
    }



}
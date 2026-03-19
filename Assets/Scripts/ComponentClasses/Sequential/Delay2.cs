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
public class Delay2 : CircuitComponent
{
    //PINS:
    [JsonProperty] private InputPin dataIn;              //delay's input pin
    [JsonProperty] private OutputPin dataOut;            //delay's output pin

    //DELAY:
    [JsonProperty] public int delay = 0;                    //number of timer-ticks till input gets transferred to output
    [JsonIgnore] private List<SignalEntry> signalQueue;     //all the signals currently traveling through this delay element   
    [JsonIgnore] public GameObject delayVisualizer;


    [JsonIgnore] private TimeTick timer;                //global simulation timer
    [JsonIgnore] private BitToken lastDataIn;

    [JsonIgnore] private GameObject editor;

    /**
     * Constructor of Delay Object
     * 
     * @param delay for signal traveling through this element
     * */
    public Delay2()
    {
        //init pins:
        this.dataIn = new InputPin();
        this.dataIn.width = 0; // accept any bit width
        this.dataOut = new OutputPin();

        signalQueue = new List<SignalEntry>();

        lastDataIn = new BitToken();


        //subscribe to Timer events:
        GameObject o = GameObject.FindWithTag("Timer");
        timer = o.GetComponent<TimeTick>();
        Subscribe();
                
        o = GameObject.FindWithTag("Editor");
        editor = o.transform.Find("Delay2Editor")?.gameObject;
    }


    //Event: Time tick => sample new input signals and output signal if delay achieved
    private void OnTimerTick(int tick)
    {
        // Self-initialize if DelayInit was never called (pure-data mode)
        if (signalQueue.Count == 0) DelayInit();

        if (!dataIn.data.EqualsToken(lastDataIn))
        {
            lastDataIn = dataIn.data;

            //new input signal => add to queue (signal + square)
            GameObject square = delayVisualizer != null ? DelayHandler.NewSquare(0,lastDataIn.ActiveColor(),delayVisualizer,tick) : null;
            signalQueue.Add(new SignalEntry(lastDataIn,square));
        }

        if (signalQueue.Count > 1)
        {
            BitToken nextOut = signalQueue[1].token;
            int arrivalTime = nextOut.GetTime();

            //check if next output signal is ready
            if (arrivalTime + delay <= tick)
            {
                dataOut.SetValue(nextOut.NewToken(arrivalTime + delay));
                //set output and remove from signal queue
                DelayHandler.ReturnSquare(signalQueue[0].visual);
                signalQueue.RemoveAt(0);

            }
        }


        //Draw each signal delay squares:
        if (delayVisualizer != null) DelayHandler.DrawSquares(signalQueue,delay,tick);
    }

    //Event: Restart of the simulation => reset delay element
    public override void Reset()
    {
        foreach(SignalEntry t in signalQueue){
            DelayHandler.ReturnSquare(t.visual);
        }
        signalQueue.Clear();

        dataIn.SetValue(new BitToken());
        dataOut.SetValue(new BitToken(dataOut.width));
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
        GameObject square = delayVisualizer != null ? DelayHandler.NewSquare(100,lastDataIn.ActiveColor(),delayVisualizer,1) : null;
        signalQueue.Add(new SignalEntry(lastDataIn,square));
    }

    public override int GetDelay() { return delay; }
    public override void CollectPins(List<InputPin> inputs, List<OutputPin> outputs)
    {
        inputs.Add(dataIn); outputs.Add(dataOut);
    }

    public override void OpenEditor()
    {
        editor.SetActive(true);
        editor.GetComponent<Delay2Editor>().init(this);
    }
}
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * @author: Stefan Moser
 * 
 * @brief: This class describes the state and behaviour of a clock
 * 
 * @details: The clock generates a periodic signal that oscillates between high and low and is used
 * to synchronize the circuit.
 * 
 * The user can set the period and duty cycle of the clock via the parameters high and low, 
 * where high/low denotes the number of simulation ticks the clock outputs 1/0 respectively. 
 * 
 * For further details on the implementation please read the code and documentation below.
 * */
public class Clock : CircuitComponent
{
    [JsonProperty] private OutputPin clkOut;     //the clock's output pin  

    [JsonProperty] public int high = 100;       //how many time ticks the clock pulse is high
    [JsonProperty] public int low  = 100;       //how many time ticks the clock pulse is low


    [JsonIgnore] private TimeTick timer;        //global simulation timer
    [JsonIgnore] private int tick = 0;
    [JsonIgnore] private bool value = false;

    [JsonIgnore] private GameObject editor;


    /**
    * Constructor of Clock object
    * */
    public Clock()
    {
        //init the clock pins:
        this.clkOut = new OutputPin();

        //subscribe to Timer events:
        GameObject o = GameObject.FindWithTag("Timer");
        timer = o.GetComponent<TimeTick>();
        Subscribe();

        o = GameObject.FindWithTag("Editor");
        editor = o.transform.GetChild(0).gameObject;
    }


    /**
     * Constructor of Clock object
     * 
     * @param high: number of ticks clock is high
     * @param low: number of ticks clock is low
     * */
    public Clock(int high, int low)
    {
        //set clock period:
        this.high = high;
        this.low = low;

        //init Output pin
        this.clkOut = new OutputPin();

        //subscribe to Timer events:
        GameObject o = GameObject.FindWithTag("Timer");
        timer = o.GetComponent<TimeTick>();
        Subscribe();

        o = GameObject.FindWithTag("Editor");
        editor = o.transform.GetChild(0).gameObject;
    }


    /**
     * On timer tick, check simulation time and adjust clock output
     * 
     * @param timerTick global simulation time
     * */
    private void OnTimerTick(int timerTick)
    {
    	tick++;

        if (value == true && tick == high)
        {
            value = !value;
            tick = 0;
            clkOut.SetValue(new BitToken(value, timerTick));
        }
        else if (value == false && tick == low)
        {
            value = !value;
            tick = 0;
            clkOut.SetValue(new BitToken(value, timerTick));
        }
    }

    //Event: simulation restart
    public override void Reset()
    {
        tick = 0;
        value = false;
        clkOut.SetValue(new BitToken(value, tick));
    }

    //Load clock prefab and init with this clock object
    public override Component Load()
    {
        GameObject prefab = Resources.Load("Prefabs/Clock") as GameObject;
        GameObject o = GameObject.Instantiate(prefab, pos, rot);
        ComponentMono c = o.GetComponent<ComponentMono>();
        c.Load(this);
        return c;
    }

    public override void LoadPins(InputPin_Mono[] inPins, OutputPin_Mono[] outPins)
    {
        outPins[0].Init(clkOut);
    }

    public override void LoadDelay(GameObject delayVisualizer)
    {
    }

    private void Subscribe()
    {
        timer.TimerTickEvent += OnTimerTick;
    }

    //unsubscribe from all event so garbage collector can delete object
    public override void Dispose()
    {
        timer.TimerTickEvent -= OnTimerTick;
    }

    public override void OpenEditor()
    {
        editor.SetActive(true);
        editor.GetComponent<ClockEditor>().init(this);
    }
}

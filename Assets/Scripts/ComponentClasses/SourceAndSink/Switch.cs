using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/**
 * @author: Stefan Moser
 * 
 * @brief: This class implements a switch.
 * */
public class Switch : Loadable
{

    [JsonProperty] public OutputPin output;         //output pin
    [JsonIgnore] private bool value;              //current value (pressed or not)

    [JsonIgnore] private TimeTick timer;          //global simulation timer
    [JsonIgnore] private int tick = 0;                  //current tick
    [JsonIgnore] private bool paused = true;        //simulation paused flag

    [JsonIgnore] public Color color = new BitToken().ActiveColor();

    //Event: update the color of this led
    public delegate void UpdateAnimation(bool b);
    public event UpdateAnimation UpdateAnimationEvent;

    public Switch()
    {
        output = new OutputPin();
        value = false;

        Subscribe();
    }

    /**
     * Upon press: negate current value and send
     * out new signal with current simulation tick
     * */
    public void press()
    {
        if (!timer.paused || timer.stepModeOn)
        {
            this.value = !this.value;
            UpdateAnimationEvent?.Invoke(value);
  
            BitToken bit = new BitToken(value, tick);
            output.SetValue(bit);
        }
    }

    //Event: new simulation tick
    private void OnTimerTick(int tick)
    {
        this.tick = tick;
    }

    //Event: Simulation restarted
    public override void Reset()
    {
        tick = 0;
        output.SetValue(new BitToken());
        value = false;
        UpdateAnimationEvent?.Invoke(value);
    }


    public override Component Load()
    {
        GameObject prefab = Resources.Load("Prefabs/Switch") as GameObject;
        GameObject o = GameObject.Instantiate(prefab, pos, rot);
        Switch_Mono c = o.GetComponent<Switch_Mono>();
        c.Init(this);
        return c;
    }

    private void Subscribe()
    {
        //subscribe to Timer events:
        GameObject o = GameObject.FindWithTag("Timer");
        timer = o.GetComponent<TimeTick>();
        timer.TimerTickEvent += OnTimerTick;
    }

    //unsubscribe from all event so garbage collector can delete object
    public override void Dispose()
    {
        timer.TimerTickEvent -= OnTimerTick;
    }

    public override void LoadPins(InputPin_Mono[] inPins, OutputPin_Mono[] outPins)
    {
        throw new System.NotImplementedException();
    }

    public override void LoadDelay(GameObject delayVisualizer)
    {
    }
}

using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LED : CircuitComponent
{
    [JsonProperty] public InputPin input;
    [JsonProperty] private BitToken data;

    [JsonIgnore] private Color color = new BitToken().ActiveColor();

    [JsonIgnore] private TimeTick timer;            //global simulation timer

    //Event: update the color of this led
    public delegate void UpdateColor(Color c);
    public event UpdateColor UpdateColorEvent;    
    public LED()
    {
        this.input = new InputPin();
        this.data = new BitToken();

        Subscribe();
    }

    private void NewInputData()
    {
        this.data = input.data;
        this.color = data.ActiveColor();
        UpdateColorEvent?.Invoke(color);
    }

    public override void Reset()
    {
        this.data = new BitToken();
        this.color = data.ActiveColor();
        UpdateColorEvent?.Invoke(color);
    }

    private void Subscribe()
    {
         //subscribe to Timer events:
        GameObject o = GameObject.FindWithTag("Timer");
        timer = o.GetComponent<TimeTick>();

        input.NewDataEvent += NewInputData;
    }

    public override void Dispose()
    {
        input.NewDataEvent -= NewInputData;
    }

    public override Component Load()
    {
        GameObject prefab = Resources.Load("Prefabs/LED") as GameObject;
        GameObject o = GameObject.Instantiate(prefab, pos, rot);
        LED_Mono c = o.GetComponent<LED_Mono>();
        c.Init(this);
        Subscribe();
        return c;
    }

    public override void LoadPins(InputPin_Mono[] inPins, OutputPin_Mono[] outPins)
    {
        inPins[0].Init(input);
    }

    public override void LoadDelay(GameObject delayVisualizer)
    {
    }

    public override void OpenEditor()
    {
        
    }
}

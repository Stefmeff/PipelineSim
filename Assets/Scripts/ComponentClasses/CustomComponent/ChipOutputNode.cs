using Newtonsoft.Json;
using System;
using UnityEngine;

/// <summary>
/// Interface output pin node for the chip editor workspace.
/// Sinks data from the sub-circuit (has one InputPin).
/// Follows the LED pattern.
/// </summary>
public class ChipOutputNode : CircuitComponent
{
    [JsonProperty] public InputPin input;
    [JsonProperty] public string pinId;
    [JsonProperty] public string pinName;
    [JsonProperty] public int pinWidth;

    [JsonIgnore] private BitToken data;
    [JsonIgnore] private TimeTick timer;

    public delegate void UpdateLabel(string name);
    public event UpdateLabel UpdateLabelEvent;

    public delegate void UpdateColor(Color c);
    public event UpdateColor UpdateColorEvent;

    public ChipOutputNode()
    {
        input = new InputPin();
        input.width = 0; // accept any bit width
        data = new BitToken();
        pinId = Guid.NewGuid().ToString();
        pinName = "Out";
        pinWidth = 1;

        Subscribe();
    }

    public void SetPinName(string name)
    {
        pinName = name;
        UpdateLabelEvent?.Invoke(name);
    }

    public void SetPinWidth(int width)
    {
        if (width >= 1 && width <= 16)
        {
            pinWidth = width;
            input.width = width;
        }
    }

    /// <summary>
    /// Returns the current output value (used during simulation when chip is placed).
    /// </summary>
    public BitToken GetValue()
    {
        return data;
    }

    private void NewInputData()
    {
        data = input.data;
        Color color = data.ActiveColor();
        UpdateColorEvent?.Invoke(color);
    }

    public override void Reset()
    {
        data = new BitToken();
        UpdateColorEvent?.Invoke(data.ActiveColor());
    }

    private void Subscribe()
    {
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
        GameObject prefab = Resources.Load("Prefabs/ChipOutput") as GameObject;
        GameObject o = GameObject.Instantiate(prefab, pos, rot);
        ComponentMono c = o.GetComponent<ComponentMono>();
        c.Load(this);
        Subscribe();
        return c;
    }

    public override void LoadPins(InputPin_Mono[] inPins, OutputPin_Mono[] outPins)
    {
        inPins[0].Init(input);
    }

    public override void LoadDelay(GameObject delayVisualizer) { }

    public override void OpenEditor() { }
}

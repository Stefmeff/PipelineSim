using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interface input pin node for the chip editor workspace.
/// Sources data into the sub-circuit (has one OutputPin).
/// Follows the Switch pattern.
/// </summary>
public class ChipInputNode : CircuitComponent
{
    [JsonProperty] public OutputPin output;
    [JsonProperty] public string pinId;
    [JsonProperty] public string pinName;
    [JsonProperty] public int pinWidth;

    [JsonIgnore] private TimeTick timer;
    [JsonIgnore] private int tick = 0;

    [JsonIgnore] private GameObject editorObj;
    [JsonIgnore] private ChipPinEditor pinEditor;

    public delegate void UpdateLabel(string name);
    public event UpdateLabel UpdateLabelEvent;

    public ChipInputNode()
    {
        output = new OutputPin();
        pinId = Guid.NewGuid().ToString();
        pinName = "In";
        pinWidth = 1;
        output.width = 1;

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
            output.width = width;
        }
    }

    /// <summary>
    /// Injects a value into the sub-circuit (used during simulation when chip is placed).
    /// </summary>
    public void InjectValue(BitToken value)
    {
        output.SetValue(value);
    }

    private void OnTimerTick(int tick)
    {
        this.tick = tick;
    }

    public override void Reset()
    {
        tick = 0;
        output.SetValue(new BitToken(output.width));
    }

    public override Component Load()
    {
        GameObject prefab = Resources.Load("Prefabs/ChipInput") as GameObject;
        GameObject o = GameObject.Instantiate(prefab, pos, rot);
        ComponentMono c = o.GetComponent<ComponentMono>();
        c.Load(this);
        Subscribe();
        return c;
    }

    private void Subscribe()
    {
        GameObject o = GameObject.FindWithTag("Timer");
        timer = o.GetComponent<TimeTick>();
        timer.TimerTickEvent += OnTimerTick;
    }

    public override void Dispose()
    {
        timer.TimerTickEvent -= OnTimerTick;
    }

    public override void LoadPins(InputPin_Mono[] inPins, OutputPin_Mono[] outPins)
    {
        outPins[0].Init(output);
    }

    public override void CollectPins(List<InputPin> inputs, List<OutputPin> outputs)
    {
        outputs.Add(output);
    }

    public override void LoadDelay(GameObject delayVisualizer) { }

    public void LoadEditor()
    {
        GameObject o = GameObject.FindWithTag("Editor");
        // Find ChipPinEditor child by name
        Transform t = o.transform.Find("ChipPinEditor");
        if (t != null)
        {
            editorObj = t.gameObject;
            pinEditor = editorObj.GetComponent<ChipPinEditor>();
        }
    }

    public override void OpenEditor()
    {
        if (pinEditor != null)
        {
            pinEditor.Init(this);
            editorObj.SetActive(true);
        }
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

/**
 * @brief Interface input pin node for the chip editor workspace.
 *
 * @details Sources data into the sub-circuit via a single OutputPin.
 * In the chip editor, this appears as a draggable node on the left pin bar.
 * When the chip is placed in the Sandbox, CustomChip.InitInternalCircuit()
 * calls InjectValue() to forward external input signals into the internal circuit.
 * Follows the Switch pattern (output-only component).
 **/
public class ChipInputNode : CircuitComponent
{
    [JsonProperty] public OutputPin output;  /**< the single output pin feeding into the sub-circuit */
    [JsonProperty] public string pinId;      /**< unique identifier for this interface pin */
    [JsonProperty] public string pinName;    /**< user-editable display name */
    [JsonProperty] public int pinWidth;      /**< bit width of the pin */

    [JsonIgnore] private TimeTick timer;
    [JsonIgnore] private int tick = 0;

    [JsonIgnore] private GameObject editorObj;
    [JsonIgnore] private ChipPinEditor pinEditor;

    public delegate void UpdateLabel(string name);
    public event UpdateLabel UpdateLabelEvent;  /**< fired when pinName changes, updates the visual label */

    /**
     * @brief Default constructor — creates a 1-bit input node named "In".
     **/
    public ChipInputNode()
    {
        output = new OutputPin();
        pinId = Guid.NewGuid().ToString();
        pinName = "In";
        pinWidth = 1;
        output.width = 1;

        Subscribe();
    }

    /**
     * @brief Sets the pin's display name and notifies listeners to update the visual label.
     *
     * @param[in] name new pin name
     **/
    public void SetPinName(string name)
    {
        pinName = name;
        UpdateLabelEvent?.Invoke(name);
    }

    /**
     * @brief Sets the pin's bit width (clamped to 1..16).
     *
     * @param[in] width new bit width
     **/
    public void SetPinWidth(int width)
    {
        if (width >= 1 && width <= 16)
        {
            pinWidth = width;
            output.width = width;
        }
    }

    /**
     * @brief Injects a value into the sub-circuit from an external input.
     *
     * @details Called by CustomChip.InitInternalCircuit() during simulation when the
     * chip's external input pin receives new data. Propagates the value through the
     * internal circuit via the output pin's connected wires.
     *
     * @param[in] value the BitToken to inject
     **/
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

    /**
     * @brief Loads the ChipPinEditor reference for editing pin name and width in the editor scene.
     **/
    public void LoadEditor()
    {
        GameObject o = GameObject.FindWithTag("Editor");
        Transform t = o.transform.Find("ChipPinEditor");
        if (t != null)
        {
            editorObj = t.gameObject;
            pinEditor = editorObj.GetComponent<ChipPinEditor>();
        }
    }

    /**
     * @brief Opens the ChipPinEditor UI for this input node.
     **/
    public override void OpenEditor()
    {
        if (pinEditor != null)
        {
            pinEditor.Init(this);
            editorObj.SetActive(true);
        }
    }
}

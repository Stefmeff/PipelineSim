using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

/**
 * @brief Interface output pin node for the chip editor workspace.
 *
 * @details Sinks data from the sub-circuit via a single InputPin.
 * In the chip editor, this appears as a draggable node on the right pin bar.
 * When the chip is placed in the Sandbox, CustomChip.InitInternalCircuit()
 * subscribes to this node's input.NewDataEvent to forward internal output signals
 * to the chip's external output pins. Follows the LED pattern (input-only component).
 **/
public class ChipOutputNode : CircuitComponent
{
    [JsonProperty] public InputPin input;    /**< the single input pin receiving data from the sub-circuit */
    [JsonProperty] public string pinId;      /**< unique identifier for this interface pin */
    [JsonProperty] public string pinName;    /**< user-editable display name */
    [JsonProperty] public int pinWidth;      /**< bit width of the pin */

    [JsonIgnore] private BitToken data;      /**< current output value */
    [JsonIgnore] private TimeTick timer;

    [JsonIgnore] private GameObject editorObj;
    [JsonIgnore] private ChipPinEditor pinEditor;

    public delegate void UpdateLabel(string name);
    public event UpdateLabel UpdateLabelEvent;   /**< fired when pinName changes, updates the visual label */

    public delegate void UpdateColor(Color c);
    public event UpdateColor UpdateColorEvent;   /**< fired when input data changes, updates the visual indicator */

    /**
     * @brief Default constructor — creates a 1-bit output node named "Out".
     **/
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
            input.width = width;
        }
    }

    /**
     * @brief Returns the current output value.
     *
     * @return the latest BitToken received from the sub-circuit
     **/
    public BitToken GetValue()
    {
        return data;
    }

    /**
     * @brief Called when the input pin receives new data from the internal circuit.
     *
     * @details Updates the stored value and fires UpdateColorEvent so the visual
     * indicator (LED-style color) reflects the current signal.
     **/
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

    public override void CollectPins(List<InputPin> inputs, List<OutputPin> outputs)
    {
        inputs.Add(input);
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
     * @brief Opens the ChipPinEditor UI for this output node.
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

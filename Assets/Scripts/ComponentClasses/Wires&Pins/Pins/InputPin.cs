using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/**
 * @author: Stefan Moser
 * 
 * @brief: This class describes an InputPin. 
 * */
[JsonObject(IsReference = true)]
public class InputPin
{
    [JsonIgnore] public BitToken data {get;private set;}                //data that is set on this input pin
    [JsonProperty] private Wire wire;                   //connected input wire
    [JsonProperty] public int width = 1;                //bit width of this pin (1 = single-bit, 0 = accept any width)
    [JsonIgnore] public string parentName = "";         //name of parent component for error messages
    [JsonIgnore] public Transform transform;            //the transform of this pin

    //Event: new data on this pin
    public delegate void NewData();
    public event NewData NewDataEvent;                  //Event that is sent out on new inputs (parent component can listen to this input event)

    public InputPin()
    {
        this.data = new BitToken();
        this.wire = null;
    }

    public void SetValue(BitToken data)
    {
        // Width validation at simulation time:
        // - width=0 on pin means accept any width
        if (width > 0 && data.GetWidth() != width)
        {
            InformationWindow.Show(
                "Bus Width Mismatch",
                "A " + data.GetWidth() + "-bit signal was connected to a " + width + "-bit input pin. "
                + "Make sure all connected wires carry the same bit width."
            );
            return;
        }
        this.data = data;
        NewDataEvent?.Invoke();
    }

    public Wire connectWire(Wire wire){
        if(this.wire == null){
            this.wire = wire;
        }
        return wire;
    }

    public bool disconnectWire()
    {
        if (wire != null)
        {
            this.wire.dataIn.disconnectPin(this);
            this.wire.Dispose();
            this.wire = null;
            return true;
        }
        return false;
    }
}
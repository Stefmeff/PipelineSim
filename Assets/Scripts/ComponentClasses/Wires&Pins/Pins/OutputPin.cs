using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity;
using Newtonsoft.Json;

/**
 * @author: Stefan Moser
 * 
 * @brief: This class describes an Output Pin. It contains the wire, all the connect pins and
 * also the type of data this pin writes.
 * */
public class OutputPin
{
    [JsonIgnore] public BitToken data;                      //data that this pin writes
    [JsonProperty] private List<InputPin> connectedPins;    //pins this pin writes to
    [JsonProperty] private Wire wire;
    [JsonIgnore] public Transform transform;                //the transform of this pin

    public OutputPin()
    {
        this.data = new BitToken();

        this.wire = null;
        connectedPins = new List<InputPin>();
    }

    public OutputPin(BitToken data)
    {
        this.data = data;

        this.wire = null;
        connectedPins = new List<InputPin>();
    }

    /**
     * Sets the value of this pin and all the connected elements (wires,pins)
     * 
     * @param data that is set
     * */
    public virtual void SetValue(BitToken data)
    {
        this.data = data;

        if (wire != null)
        {
            wire.SetValue(data);
            foreach (InputPin p in connectedPins)
            {
                p.SetValue(data);
            }
        }
    }

    /**
     * Connects an InputPin to this OutputPin
     * 
     * @param InputPin to be connected
     * */
    public virtual void connectPin(InputPin pin)
    {
        connectedPins.Add(pin);
    }

    public void disconnectPin(InputPin pin)
    {
        connectedPins.Remove(pin);
    }

    public virtual Wire connectWire()
    {
        this.wire = new Wire(this);
        return this.wire;
    }


    /**
     * If a wire is connected, it will be destroyed and all
     * the connected pins will be disconnected.
     * */
    public virtual bool disconnectWire()
    {
        if (wire != null)
        {
            this.wire.Dispose();
            this.wire = null;
            foreach(InputPin p in connectedPins){
                p.SetValue(new BitToken());
            }
            connectedPins.Clear();
            return true;
        }
        return false;
    }
}

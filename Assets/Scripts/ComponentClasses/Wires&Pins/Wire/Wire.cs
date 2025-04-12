using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * @author: Stefan
 * 
 * @details: This class describes the main properties of a wire. Each wire has a source OutputPin that writes to the wire,
 * and a sink InputPin. Each wire can also has intermediate wire knots, from which branching wires can be created.
 * */
public class Wire : ILoadable
{
    [JsonProperty] public OutputPin dataIn;         //OutputPin that writes to this wire
    [JsonProperty] public InputPin dataOut;         //InputPin this wire writes to
    [JsonProperty] public List<OutputPin> knots;    //Additional Knots on this wire

    [JsonProperty] public int knotCount = 0;        //total knot count (OutputPin + InputPin + knots)

    [JsonProperty] [JsonConverter(typeof(ColorHandler))] public Color coloring;      //the color this wire has, depending on the data transported

    //Event: destruction of this wire
    public delegate void Destruct();
    public event Destruct DestructEvent;

    public Wire()
    {
        //empty constructor for json
    }


    /**
     * Constructor of Wire Object
     * 
     * @param dataIn Pin that send signals into this wire
     * */
    public Wire(OutputPin dataIn)
    {
        knotCount++;
        this.dataIn = dataIn;
        this.knots = new List<OutputPin>();
        this.coloring = dataIn.data.ActiveColor();
    }


    /**
     * Sets the data that is sent through this wire
     * */
    public void SetValue(BitToken data)
    {
        //set colour of wire according to input data:
        this.coloring = data.ActiveColor();

        //set value at all the knots so it is propagated to potentail branching wires
        foreach (Knot k in knots)
        {
            k.SetValue(data);
        }
    }


    public void setOutput(InputPin dataOut)
    {
        //add an InputPin that this wire writes on
        knotCount++;
        this.dataOut = dataOut;
        dataIn.connectPin(dataOut);
    }


    /**
     * Adds a knot to this wire
     * 
     * @param knot
     * */
    public void addKnot(Knot knot)
    {
        knotCount++;
        knots.Add(knot);
    }

    public override void Reset()
    {
        
    }

    public override Component Load()
    {
        GameObject prefab = Resources.Load("Prefabs/Wire") as GameObject;
        GameObject o = GameObject.Instantiate(prefab, pos, rot);
        Wire_Mono w = o.GetComponent<Wire_Mono>();
        w.Init(this);

        return w;
    }

    public override void Dispose()
    {
        DestructEvent?.Invoke();
    }

    public override void LoadPins(InputPin_Mono[] inPins, OutputPin_Mono[] outPins)
    {
        
    }

    public override void LoadDelay(GameObject delayVisualizer)
    {
        //no delay on wire component
    }
}

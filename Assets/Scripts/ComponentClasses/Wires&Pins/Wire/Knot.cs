using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
* @author: Stefan Moser
*
* @details: This class describes intermediate wire knots. From these knots, the user
* can draw branching wires to other components
**/
public class Knot : OutputPin
{
    [JsonProperty] private OutputPin dataIn;    //source pin that writes to this wire
    [JsonProperty] private Wire wire;               //wire that branches from this knot


    //position and rotation of knot used for serialization:
    [JsonProperty] public SerializableVector3 pos = new SerializableVector3(0, 0, 0);
    [JsonProperty] public SerializableQuaternion rot = new SerializableQuaternion(0, 0, 0, 1);


    public Knot()
    {

    }

    /**
     * Constructor of Knot Object
     * 
     * @param dataIn Pin that writes to this knots wire
     * */
    public Knot(OutputPin dataIn)
    {
        this.dataIn = dataIn;
    }


    /**
    * Connect a new branching wire to this knot
    **/
    public override Wire connectWire()
    {
        this.wire= new Wire(this);
        return this.wire;
    }

    /**
    * Disconnect the branching wire
    **/
    public override bool disconnectWire()
    {
        if (wire != null)
        {
            dataIn.disconnectPin(wire.dataOut);
            this.wire.Dispose();
            this.wire = null;
            return true;
        }
        return false;
    }

    /**
    * Connect new input pin to the source output pin of the knots wire
    **/
    public override void connectPin(InputPin pin)
    {
        dataIn.connectPin(pin);
    }


    /**
    * Set a token value on this knot and its branching wire
    **/
    public override void SetValue(BitToken data)
    {
        this.data = data;
        if (wire != null)
        {
            this.wire.SetValue(data);
        }
    }

    public Component Load()
    {
        GameObject prefab = Resources.Load("Prefabs/Knot") as GameObject;
        GameObject o= GameObject.Instantiate(prefab, pos, rot);
        Knot_Mono m = o.GetComponent<Knot_Mono>();
        m.Init(this);

        return m;
    }

    public void SaveTransform(Transform t)
    {
        //save the position and rotation of this object so it can be serialized
        Vector3 p = t.position;
        Quaternion r = t.rotation;
        this.pos = new SerializableVector3(p.x, p.y, p.z);
        this.rot = new SerializableQuaternion(r.x, r.y, r.z, r.w);
    }
}

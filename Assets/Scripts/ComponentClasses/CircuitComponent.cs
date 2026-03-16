using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * @author: Stefan Moser
 *
 * @brief: describes classes that can be saved as json files and reloaded into a respective gameObject
 * */
public abstract class CircuitComponent
{

    //Position and Rotation for saving object to json file
    [JsonProperty] public SerializableVector3 pos = new SerializableVector3(0, 0, 0);
    [JsonProperty] public SerializableQuaternion rot = new SerializableQuaternion(0, 0, 0, 1);


    /**
    * @brief: Loads this components prefab and couples its monobehaviour with the components class.
    **/
    public abstract Component Load();

    //Load and couple the pin classes of the component with the pin gameObjects
    public abstract void LoadPins(InputPin_Mono[] inPins, OutputPin_Mono[] outPins);

    //Load and couple gameObject that visualizes the delay
    public abstract void LoadDelay(GameObject delayVisualizer);

    /**
     * Save the position and rotation of a game element for serialization
     * */
    public void SaveTransform(Transform t)
    {
        //save the position and rotation of this object so it can be serialized
        Vector3 p = t.position;
        Quaternion r = t.rotation;
        this.pos = new SerializableVector3(p.x, p.y, p.z);
        this.rot = new SerializableQuaternion(r.x, r.y, r.z, r.w);
    }

    //public abstract Component GetMainComponent();
    public abstract void OpenEditor();
    public abstract void Reset();

    public abstract void Dispose();

    /// <summary>
    /// Returns the propagation delay of this component (0 if none).
    /// </summary>
    public virtual int GetDelay() { return 0; }

    /// <summary>
    /// Collects all input and output pins of this component into the provided lists.
    /// Used for graph traversal (e.g., delay path calculation).
    /// </summary>
    public virtual void CollectPins(List<InputPin> inputs, List<OutputPin> outputs) { }
}

using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

/**
 * @brief Data class for a text annotation in the sandbox.
 *
 * @details A text annotation is a resizable text box placed in the workspace for
 * documenting circuits, labeling components, or adding notes. It has no electrical
 * behavior — it's purely visual. Serialized with the project like any other component.
 **/
public class TextAnnotation : CircuitComponent
{
    [JsonProperty] public string text = "Text...";
    [JsonProperty] public float width = 30f;
    [JsonProperty] public float height = 15f;

    public override Component Load()
    {
        GameObject prefab = Resources.Load("Prefabs/TextAnnotation") as GameObject;
        GameObject o = GameObject.Instantiate(prefab, pos, rot);
        TextAnnotation_Mono mono = o.GetComponent<TextAnnotation_Mono>();
        mono.Init(this);
        return mono;
    }

    public override void LoadPins(InputPin_Mono[] inPins, OutputPin_Mono[] outPins) { }
    public override void LoadDelay(GameObject delayVisualizer) { }
    public override void Reset() { }
    public override void Dispose() { }
    public override void OpenEditor() { }
    public override void CollectPins(List<InputPin> inputs, List<OutputPin> outputs) { }
}

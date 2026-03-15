using Newtonsoft.Json;
using System;

/// <summary>
/// Defines a single interface pin on a custom chip (input or output).
/// Stored as part of a ChipDefinition.
/// </summary>
[Serializable]
public class InterfacePin
{
    [JsonProperty] public string id;
    [JsonProperty] public string name;
    [JsonProperty] public int width;
    [JsonProperty] public int order;

    public InterfacePin()
    {
        id = Guid.NewGuid().ToString();
        name = "Pin";
        width = 1;
        order = 0;
    }

    public InterfacePin(string name, int width, int order)
    {
        id = Guid.NewGuid().ToString();
        this.name = name;
        this.width = width;
        this.order = order;
    }
}

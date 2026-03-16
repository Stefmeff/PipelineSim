using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Schema for a .chip file. Contains chip metadata, interface pin definitions,
/// and the internal circuit serialized from ProjectManager.SaveWorld().
/// </summary>
[Serializable]
public class ChipDefinition
{
    [JsonProperty] public string chipId;
    [JsonIgnore] public string name;
    [JsonProperty] public string color;
    [JsonProperty] public List<InterfacePin> inputs;
    [JsonProperty] public List<InterfacePin> outputs;
    [JsonProperty] public string internalCircuit;

    private static JsonSerializerSettings settings = new JsonSerializerSettings
    {
        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
        TypeNameHandling = TypeNameHandling.Auto,
        Formatting = Formatting.Indented
    };

    public ChipDefinition()
    {
        chipId = Guid.NewGuid().ToString();
        name = "New Chip";
        color = "#4A90D9";
        inputs = new List<InterfacePin>();
        outputs = new List<InterfacePin>();
        internalCircuit = "";
    }

    /// <summary>
    /// Returns the directory where .chip files are stored.
    /// </summary>
    public static string GetChipDirectory()
    {
        string dir = Path.Combine(Application.persistentDataPath, "CustomComponents");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return dir;
    }

    /// <summary>
    /// Saves this chip definition to a .chip file.
    /// </summary>
    public void Save()
    {
        string dir = GetChipDirectory();
        string path = Path.Combine(dir, chipId + ".chip");
        string json = JsonConvert.SerializeObject(this, settings);
        File.WriteAllText(path, json);
        Debug.Log("Chip saved to: " + path);
    }

    /// <summary>
    /// Loads a chip definition from a .chip file by chipId.
    /// </summary>
    public static ChipDefinition Load(string chipId)
    {
        string path = Path.Combine(GetChipDirectory(), chipId + ".chip");
        if (!File.Exists(path))
        {
            Debug.LogError("Chip file not found: " + path);
            return null;
        }
        string json = File.ReadAllText(path);
        ChipDefinition chip = JsonConvert.DeserializeObject<ChipDefinition>(json, settings);
        if (chip != null) chip.name = chipId;
        return chip;
    }

    /// <summary>
    /// Returns all saved chip definitions.
    /// </summary>
    public static List<ChipDefinition> LoadAll()
    {
        List<ChipDefinition> chips = new List<ChipDefinition>();
        string dir = GetChipDirectory();
        foreach (string file in Directory.GetFiles(dir, "*.chip"))
        {
            string json = File.ReadAllText(file);
            ChipDefinition chip = JsonConvert.DeserializeObject<ChipDefinition>(json, settings);
            if (chip != null)
            {
                chip.name = Path.GetFileNameWithoutExtension(file);
                chips.Add(chip);
            }
        }
        return chips;
    }
}

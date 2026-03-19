using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

/**
 * @brief Schema for a .chip file — the persistent representation of a custom chip.
 *
 * @details Contains chip metadata (ID, name, color), interface pin definitions (inputs/outputs),
 * and the internal circuit serialized as a JSON string from ProjectManager.SaveWorld().
 * Chip files are stored in Application.persistentDataPath/CustomComponents/.
 **/
[Serializable]
public class ChipDefinition
{
    [JsonProperty] public string chipId;              /**< unique identifier, used as filename */
    [JsonIgnore] public string name;                  /**< display name (derived from filename on load) */
    [JsonProperty] public string color;               /**< hex color string for chip appearance */
    [JsonProperty] public List<InterfacePin> inputs;  /**< input pin definitions */
    [JsonProperty] public List<InterfacePin> outputs; /**< output pin definitions */
    [JsonProperty] public string internalCircuit;     /**< serialized internal circuit JSON */
    [JsonProperty] public int delay;                  /**< longest-path propagation delay in ticks */

    private static JsonSerializerSettings settings = new JsonSerializerSettings
    {
        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
        TypeNameHandling = TypeNameHandling.Auto,
        Formatting = Formatting.Indented
    };

    /**
     * @brief Default constructor — creates a new chip with a unique ID and empty circuit.
     **/
    public ChipDefinition()
    {
        chipId = Guid.NewGuid().ToString();
        name = "New Chip";
        color = "#4A90D9";
        inputs = new List<InterfacePin>();
        outputs = new List<InterfacePin>();
        internalCircuit = "";
    }

    /**
     * @brief Returns the directory where .chip files are stored.
     * Creates the directory if it does not exist.
     *
     * @return absolute path to the CustomComponents directory
     **/
    public static string GetChipDirectory()
    {
        string dir = Path.Combine(Application.persistentDataPath, "CustomComponents");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return dir;
    }

    /**
     * @brief Saves this chip definition to a .chip file.
     *
     * @details The file is written to GetChipDirectory()/chipId.chip as JSON.
     **/
    public void Save()
    {
        string dir = GetChipDirectory();
        string path = Path.Combine(dir, chipId + ".chip");
        string json = JsonConvert.SerializeObject(this, settings);
        File.WriteAllText(path, json);
        Debug.Log("Chip saved to: " + path);
    }

    /**
     * @brief Loads a chip definition from a .chip file by chipId.
     *
     * @param[in] chipId the chip's unique identifier (filename without extension)
     * @return the loaded ChipDefinition, or null if the file is missing or corrupt
     **/
    public static ChipDefinition Load(string chipId)
    {
        string path = Path.Combine(GetChipDirectory(), chipId + ".chip");
        if (!File.Exists(path))
        {
            Debug.LogError("Chip file not found: " + path);
            return null;
        }
        try
        {
            string json = File.ReadAllText(path);
            ChipDefinition chip = JsonConvert.DeserializeObject<ChipDefinition>(json, settings);
            if (chip != null) chip.name = chipId;
            return chip;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to load chip \"" + chipId + "\": " + e.Message);
            return null;
        }
    }

    /**
     * @brief Checks whether saving this chip would create a circular dependency.
     *
     * @details Scans the internal circuit JSON for CustomChip references, then recursively
     * checks each nested chip's dependencies. If this chip's own name appears anywhere
     * in the dependency tree, a circular reference exists.
     *
     * @param[in] chipName the name this chip will be saved as (filename without extension)
     * @return null if no cycle detected, or a string describing the circular dependency chain
     **/
    public string CheckCircularDependency(string chipName)
    {
        HashSet<string> visited = new HashSet<string>();
        List<string> chain = new List<string> { chipName };
        return CheckCircularDependencyRecursive(chipName, internalCircuit, visited, chain);
    }

    /**
     * @brief Recursively walks the dependency tree looking for a cycle back to targetName.
     *
     * @param[in] targetName the chip name we're checking for cycles against
     * @param[in] circuitJson the internal circuit JSON to scan for CustomChip references
     * @param[in,out] visited set of chip names already checked (prevents redundant work)
     * @param[in,out] chain the current dependency path (for error message)
     * @return null if no cycle, or a description string if a cycle is found
     **/
    private static string CheckCircularDependencyRecursive(string targetName, string circuitJson,
        HashSet<string> visited, List<string> chain)
    {
        if (string.IsNullOrEmpty(circuitJson)) return null;

        // Extract all chipName values from CustomChip entries in the JSON
        HashSet<string> nestedChips = ExtractNestedChipNames(circuitJson);

        foreach (string nested in nestedChips)
        {
            if (nested == targetName)
            {
                chain.Add(nested);
                return "Circular dependency: " + string.Join(" → ", chain);
            }

            if (visited.Contains(nested)) continue;
            visited.Add(nested);

            // Load the nested chip's definition to check its dependencies
            ChipDefinition nestedDef = Load(nested);
            if (nestedDef == null) continue;

            chain.Add(nested);
            string result = CheckCircularDependencyRecursive(targetName, nestedDef.internalCircuit, visited, chain);
            if (result != null) return result;
            chain.RemoveAt(chain.Count - 1);
        }

        return null;
    }

    /**
     * @brief Extracts all CustomChip chipName values from a serialized circuit JSON string.
     *
     * @details Uses regex to find "chipName" properties in the JSON that belong to CustomChip
     * type entries. This avoids full deserialization which would trigger constructors and
     * Timer subscriptions.
     *
     * @param[in] circuitJson the serialized internal circuit JSON
     * @return set of chip names referenced in the circuit
     **/
    public static HashSet<string> ExtractNestedChipNames(string circuitJson)
    {
        HashSet<string> names = new HashSet<string>();
        if (string.IsNullOrEmpty(circuitJson)) return names;

        // Match "chipName": "value" in the JSON
        MatchCollection matches = Regex.Matches(circuitJson, "\"chipName\"\\s*:\\s*\"([^\"]+)\"");
        foreach (Match m in matches)
        {
            if (m.Groups.Count > 1)
                names.Add(m.Groups[1].Value);
        }
        return names;
    }

    /**
     * @brief Returns all saved chip definitions from the CustomComponents directory.
     *
     * @return list of all loadable ChipDefinitions
     **/
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

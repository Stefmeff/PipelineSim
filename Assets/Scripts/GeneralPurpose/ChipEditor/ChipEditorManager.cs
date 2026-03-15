using System.IO;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main controller for the ChipEditor scene.
/// Manages chip metadata, save/load .chip files, and back button.
/// </summary>
public class ChipEditorManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField chipNameField;
    [SerializeField] private TMP_InputField colorField;
    [SerializeField] private Button backButton;

    [Header("Pin Bars")]
    [SerializeField] private PinBarClickHandler inputPinBar;
    [SerializeField] private PinBarClickHandler outputPinBar;

    private ChipDefinition chipDefinition;
    private ProjectManager projectManager;

    private void Awake()
    {
        GameObject pmObj = GameObject.FindGameObjectWithTag("ProjectManager");
        projectManager = pmObj.GetComponent<ProjectManager>();

        if (backButton != null) backButton.onClick.AddListener(OnBack);

        // Load existing chip or create new
        if (!string.IsNullOrEmpty(SceneTransition.chipIdToEdit))
        {
            chipDefinition = ChipDefinition.Load(SceneTransition.chipIdToEdit);
            if (chipDefinition != null)
            {
                LoadChipIntoEditor();
            }
            else
            {
                chipDefinition = new ChipDefinition();
            }
        }
        else
        {
            chipDefinition = new ChipDefinition();
        }

        if (chipNameField != null) chipNameField.text = chipDefinition.name;
        if (colorField != null) colorField.text = chipDefinition.color;
    }

    private void LoadChipIntoEditor()
    {
        if (chipNameField != null) chipNameField.text = chipDefinition.name;
        if (colorField != null) colorField.text = chipDefinition.color;

        // Load internal circuit
        if (!string.IsNullOrEmpty(chipDefinition.internalCircuit))
        {
            projectManager.LoadWorld(chipDefinition.internalCircuit);
            needsPinRegistration = true;
        }
    }

    private bool needsPinRegistration = false;

    private void Start()
    {
        if (needsPinRegistration)
        {
            RegisterLoadedPins();
            needsPinRegistration = false;
        }
    }

    private void RegisterLoadedPins()
    {
        ComponentMono[] allComps = Object.FindObjectsOfType<ComponentMono>();
        Debug.Log("RegisterLoadedPins: found " + allComps.Length + " ComponentMono objects");

        foreach (ComponentMono comp in allComps)
        {
            Debug.Log("Component: " + comp.gameObject.name + " type: " + (comp.component != null ? comp.component.GetType().Name : "null"));
            if (comp.component is ChipInputNode && inputPinBar != null)
            {
                Debug.Log("Registering input pin: " + comp.gameObject.name);
                inputPinBar.RegisterExistingPin(comp.gameObject);
            }
            else if (comp.component is ChipOutputNode && outputPinBar != null)
            {
                Debug.Log("Registering output pin: " + comp.gameObject.name);
                outputPinBar.RegisterExistingPin(comp.gameObject);
            }
        }
    }

    /// <summary>
    /// Called by SaveButton when in ChipEditor scene.
    /// </summary>
    public void SaveChip()
    {
        if (chipNameField != null) chipDefinition.name = chipNameField.text;
        if (colorField != null) chipDefinition.color = colorField.text;

        // Collect interface pins from world-space pin objects
        if (inputPinBar != null)
        {
            chipDefinition.inputs.Clear();
            var inputPins = inputPinBar.GetPinsOrdered();
            for (int i = 0; i < inputPins.Count; i++)
            {
                chipDefinition.inputs.Add(new InterfacePin("In" + i, 1, i));
            }
        }

        if (outputPinBar != null)
        {
            chipDefinition.outputs.Clear();
            var outputPins = outputPinBar.GetPinsOrdered();
            for (int i = 0; i < outputPins.Count; i++)
            {
                chipDefinition.outputs.Add(new InterfacePin("Out" + i, 1, i));
            }
        }

        chipDefinition.internalCircuit = projectManager.SaveWorld();

#if UNITY_WEBGL && !UNITY_EDITOR
        // TODO: WebGL chip saving
        chipDefinition.Save();
#else
        string defaultDir = ChipDefinition.GetChipDirectory();
        string path = SFB.StandaloneFileBrowser.SaveFilePanel("Save Chip", defaultDir, chipDefinition.name, "chip");
        if (!string.IsNullOrEmpty(path))
        {
            // Use the filename (without extension) as the chip name
            chipDefinition.name = System.IO.Path.GetFileNameWithoutExtension(path);
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(chipDefinition, new Newtonsoft.Json.JsonSerializerSettings
            {
                PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects,
                TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
                Formatting = Newtonsoft.Json.Formatting.Indented
            });
            System.IO.File.WriteAllText(path, json);
            Debug.Log("Chip saved: " + chipDefinition.name + " at " + path);
        }
#endif
    }

    /// <summary>
    /// Called by LoadButton when in ChipEditor scene. Opens file browser for .chip files.
    /// </summary>
    public void LoadChip()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // TODO: WebGL chip loading
#else
        string defaultDir = ChipDefinition.GetChipDirectory();
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Load Chip", defaultDir, "chip", false);
        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            string json = File.ReadAllText(paths[0]);
            var settings = new Newtonsoft.Json.JsonSerializerSettings
            {
                PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects,
                TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
                Formatting = Newtonsoft.Json.Formatting.Indented
            };
            ChipDefinition loaded = Newtonsoft.Json.JsonConvert.DeserializeObject<ChipDefinition>(json, settings);
            if (loaded != null)
            {
                projectManager.ClearWorld();
                chipDefinition = loaded;
                LoadChipIntoEditor();
                RegisterLoadedPins();
            }
        }
#endif
    }

    private void OnBack()
    {
        SceneTransition.ReturnToSandbox();
    }

    private void OnDestroy()
    {
        if (backButton != null) backButton.onClick.RemoveListener(OnBack);
    }
}

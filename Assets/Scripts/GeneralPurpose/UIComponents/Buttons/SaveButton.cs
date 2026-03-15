using System.Collections;
using System.Collections.Generic;
using System.IO;
using SFB;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using System.Text;
using System.Runtime.InteropServices;
using TMPro;

/**
* using UnityStandalonFileBrowser from https://github.com/gkngkc/UnityStandaloneFileBrowser
**/
public class SaveGame : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    private ProjectManager projectManager;
    private Image i;
    private Color32 defaultColor = new Color32(0x42,0x42,0x42,0xFF);
    private Color32 overColor = new Color32(0x32,0x32,0x32,0xFF);
    private TMP_Text toolTip;

    private void Awake()
    {
        GameObject o = GameObject.FindGameObjectWithTag("ProjectManager");
        projectManager = o.GetComponent<ProjectManager>();

        i = gameObject.GetComponent<Image>();
        o = GameObject.FindWithTag("ToolTip");
        toolTip = o.GetComponent<TMP_Text>();
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        // In ChipEditor scene, delegate to ChipEditorManager
        ChipEditorManager chipEditor = Object.FindObjectOfType<ChipEditorManager>();
        if (chipEditor != null)
        {
            chipEditor.SaveChip();
            return;
        }

        string jsonString = projectManager.SaveWorld();
        SaveAsFile(jsonString);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        i.color = overColor;
        ChipEditorManager chipEditor = Object.FindObjectOfType<ChipEditorManager>();
        toolTip.text = chipEditor != null ? "  save chip" : "  save to file";
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        i.color = defaultColor;
        toolTip.text = "  tool tip:";
    }

#if UNITY_WEBGL && !UNITY_EDITOR

    // WebGL
    [DllImport("__Internal")]
    private static extern void DownloadFile(string gameObjectName, string methodName, string filename, byte[] byteArray, int byteArraySize);

    public void SaveAsFile(string jsonString) {
        var bytes = Encoding.UTF8.GetBytes(jsonString);
        DownloadFile(gameObject.name, "OnFileDownload", "project.pip", bytes, bytes.Length);
    }

    // Called from browser
    public void OnFileDownload() { }

#else
    private void SaveAsFile(string jsonString)
    {
        string defaultDirectory = Application.persistentDataPath;

        string path = StandaloneFileBrowser.SaveFilePanel("Save File", defaultDirectory, "untitled","pip");
        if(!string.IsNullOrEmpty(path)){
            File.WriteAllText(path,jsonString);
        }
    }

#endif

}

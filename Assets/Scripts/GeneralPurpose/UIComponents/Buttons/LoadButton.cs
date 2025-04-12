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

public class LoadGame : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    private ProjectManager projectManager;
    private TimeTick timer;        //global simulation timer
    private Image i;
    private Color32 defaultColor = new Color32(0x42,0x42,0x42,0xFF);
    private Color32 overColor = new Color32(0x32,0x32,0x32,0xFF);
    private TMP_Text toolTip;

    
    private void Awake()
    {
        GameObject o = GameObject.FindGameObjectWithTag("ProjectManager");
        projectManager = o.GetComponent<ProjectManager>();

        //subscribe to Timer events:
        o = GameObject.FindWithTag("Timer");
        timer = o.GetComponent<TimeTick>();

        i = gameObject.GetComponent<Image>();

        
        o = GameObject.FindWithTag("ToolTip");
        toolTip = o.GetComponent<TMP_Text>();
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        //pause simulation before loading file:
        timer.pause(true);
        timer.restart();
        OpenProject();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        i.color = overColor;
        toolTip.text = "  load file";
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        i.color = defaultColor;
        toolTip.text = "  tool tip:";
    }


#if UNITY_WEBGL && !UNITY_EDITOR
    //
    // WebGL
    //
    [DllImport("__Internal")]
    private static extern void UploadFile(string gameObjectName, string methodName, string filter, bool multiple);

    public void OpenProject() {
        projectManager.ClearWorld();

        UploadFile(gameObject.name, "OnFileUpload", ".pip", false);
    }

    // Called from browser
    public void OnFileUpload(string url) {
        StartCoroutine(OutputRoutine(url));
    }

#else
    private void OpenProject(){
        
        projectManager.ClearWorld();

        string defaultDirectory = Application.persistentDataPath;


        string[] paths = StandaloneFileBrowser.OpenFilePanel("Open Project",defaultDirectory,"pip",false);  //parameters: title, path, allowed extensions, multiple files?
        if(paths.Length > 0){
           //Debug.Log(paths[0]);
           //string jsonString = File.ReadAllText(paths[0]);
           //projectManager.LoadWorld(jsonString);
           StartCoroutine(OutputRoutine(new System.Uri(paths[0]).AbsoluteUri));
        }
    }

#endif
    private IEnumerator OutputRoutine(string url) {
        UnityWebRequest www = UnityWebRequest.Get(url);
        
        yield return www.SendWebRequest();

        if(www.result!=UnityWebRequest.Result.Success){
            Debug.Log("WWW Error: " + www.error);
        }
        else
        {
           string jsonString = www.downloadHandler.text;
           projectManager.LoadWorld(jsonString);
        }
    }

}

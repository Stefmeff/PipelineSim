using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using System.Text;
using System.Runtime.InteropServices;

/**
 * @author: Stefan Moser
 * 
 * @details: The project manager keeps track of all game elements in a project and is used 
 * for saving or loading a users project to or from file. When saving it serializes all the game elements
 * to a json file. Loading happens again via deserialization. For this, we use the Newtonsoft json package.
 * 
 * */
public class ProjectManager : MonoBehaviour
{
    //list of the projects game elements:
    private List<IObjectMono> gameElements = new List<IObjectMono>();

    public bool zoomActive = true;
    public bool dragActive = true;


    private void Awake()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR

            //Web: Open File from URL parameter:
            string fileUrl = GetUrlParameter("file");
            Debug.Log("File URL: " + fileUrl);

            if(!string.IsNullOrEmpty(fileUrl))
            {
                //Open File
                Debug.Log("Loading JSON from: " + fileUrl);
                StartCoroutine(DownloadAndLoadJson(fileUrl));
            }
            else
            {
                Debug.LogWarning("File cannot be opened");
            }
            

        #else
            // Check if returning from ChipEditor with auto-saved state
            if (!SceneTransition.TryRestoreTempSave(this))
            {
                //if project has command line arguments => open project
                string[] args = System.Environment.GetCommandLineArgs();
                if(args.Length > 1 && !args[1].StartsWith("-")){
                    try
                    {
                            string path = args[1];
                            string jsonString = File.ReadAllText(path);
                            LoadWorld(jsonString);
                    }
                    catch(FileNotFoundException e){
                        //file not found
                        Debug.Log(e);
                    }
                }
            }
        #endif
    }

    IEnumerator GetText(string url) {
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();
 
        if (www.result != UnityWebRequest.Result.Success) {
            Debug.Log(www.error);
        }
        else {
           string jsonString = www.downloadHandler.text;
           LoadWorld(jsonString);
        }
    }

    //settings of the json serializer => most importantly preserve the references of the objects!
    private static JsonSerializerSettings settings = new JsonSerializerSettings
    {
        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
        TypeNameHandling = TypeNameHandling.Auto,
        Formatting = Formatting.Indented
    };

    //add object to projects list
    public void addToProject(IObjectMono o)
    {
        gameElements.Add(o);
    }

    //remove object from projects list
    public void removeFromProject(IObjectMono o)
    {
        gameElements.Remove(o);
    }

    //clear the project list
    public void ClearWorld()
    {
        foreach(IObjectMono o in gameElements)
        {
            o.Clear();
        }

        gameElements.Clear();
    }

    /**
    * @brief: loads project from json file into gameElements
    * 
    * @param jsonString path name
    * */
    public void LoadWorld(string jsonString)
    {

        List<CircuitComponent> elements = JsonConvert.DeserializeObject<List<CircuitComponent>>(jsonString, settings);

        foreach (CircuitComponent l in elements)
        {
            l.Load();
        }
    }

    //save project to json file
    public string SaveWorld()
    {
        string fileName = Application.persistentDataPath;
        List<CircuitComponent> elements = new List<CircuitComponent>();

        foreach(IObjectMono o in gameElements)
        {
            o.SaveTransform();
            CircuitComponent l = o.GetMain();
            elements.Add(l);
        }

        string jsonString = JsonConvert.SerializeObject(elements, settings);
        return jsonString;
    }

    //Reset all the game elements a defaul state
    public void ResetComponents(){
        foreach(IObjectMono o in gameElements){
            o.GetMain().Reset();
        }
    }

    //Finds the coordinate center of all the game elements
    public void FindCenter(){
        float xSum = 0;
        float ySum = 0;
        float dotCount = 0;

        foreach(IObjectMono o in gameElements)
        {
            dotCount ++;
            xSum += ((MonoBehaviour)o).gameObject.transform.position.x;
            ySum += ((MonoBehaviour)o).gameObject.transform.position.y;
        }

        if(dotCount > 0){
            xSum = xSum / dotCount;
            ySum = ySum / dotCount;
        }


        Camera.main.transform.position = new Vector3(xSum,ySum,-10);
    }


    // Extracts query parameter from URL (for WebGL only)
    private string GetUrlParameter(string key)
    {
        string url = Application.absoluteURL;

        if (string.IsNullOrEmpty(url)) return null;


        string[] parameters = url.Split('?');
        if (parameters.Length < 2) return null;

        string[] queryParams = parameters[1].Split('&');
        foreach (var param in queryParams)
        {
            string[] pair = param.Split('=');
            if (pair.Length == 2 && pair[0] == key)
            {
                return System.Uri.UnescapeDataString(pair[1]);
            }
        }
        return null;
    }


    IEnumerator DownloadAndLoadJson(string url)
    {
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        if (www.result != UnityWebRequest.Result.Success)
#else
        if (www.isNetworkError || www.isHttpError)
#endif
        {
            Debug.LogError("Failed to load JSON: " + www.error);
        }
        else
        {
            string jsonString = www.downloadHandler.text;

            // Call your world-loading function
            LoadWorld(jsonString);
        }
    }
}

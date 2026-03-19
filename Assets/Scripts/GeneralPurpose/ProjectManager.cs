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
        List<CircuitComponent> elements;
        try
        {
            elements = JsonConvert.DeserializeObject<List<CircuitComponent>>(jsonString, settings);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to deserialize circuit: " + e.Message);
            InformationWindow.Show("Failed to Load Circuit",
                "The circuit file could not be read. It may be corrupted or from an incompatible version.\n\n"
                + e.Message);
            return;
        }

        if (elements == null)
        {
            InformationWindow.Show("Failed to Load Circuit",
                "The circuit file is empty or could not be parsed.");
            return;
        }

        foreach (CircuitComponent l in elements)
        {
            try
            {
                l.Load();
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to load component: " + l.GetType().Name + " — " + e.Message);
                InformationWindow.Show("Failed to Load Component",
                    "A component of type \"" + l.GetType().Name + "\" could not be loaded.\n\n"
                    + e.Message);
            }
        }

        FitCameraToCircuit();
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

    //Centers camera on all game elements and adjusts zoom so everything is visible
    public void FitCameraToCircuit(){
        if (gameElements.Count == 0) return;

        //compute bounding box of all elements
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach(IObjectMono o in gameElements)
        {
            Vector3 p = ((MonoBehaviour)o).gameObject.transform.position;
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }

        //compute zoom to fit all elements with padding
        float padding = 40f;
        float width = maxX - minX + padding;
        float height = maxY - minY + padding;

        //the sandbox area is bounded by UI panels:
        // - left sidebar (ScrollBackground): 0 to 16.25% of screen width
        // - top menu bar (MenuArea): 91.5% to 100% of screen height
        // so the visible sandbox occupies the remaining fraction of the full camera view
        float visibleFractionX = 1f - 0.1625f;  // 83.75% of screen width
        float visibleFractionY = 0.915f;         // 91.5% of screen height

        //orthographicSize controls half the vertical world extent across the FULL screen,
        //but only visibleFraction of that is actually visible in the sandbox area
        float zoomForHeight = height / (2f * visibleFractionY);
        float zoomForWidth = width / (2f * visibleFractionX * Camera.main.aspect);

        //offset the camera center so the circuit is centered in the sandbox area, not the full screen
        //the sandbox midpoint in normalized screen coords:
        //  X: (0.1625 + 1.0) / 2 = 0.58125  (shifted right from 0.5)
        //  Y: (0.0 + 0.915) / 2 = 0.4575    (shifted down from 0.5)
        float cx = (minX + maxX) / 2f;
        float cy = (minY + maxY) / 2f;
        float targetZoom = Mathf.Max(zoomForHeight, zoomForWidth);

        //convert the screen-center offset to world units
        //full camera view spans orthographicSize*2 vertically and orthographicSize*2*aspect horizontally
        float offsetX = (0.58125f - 0.5f) * targetZoom * 2f * Camera.main.aspect;
        float offsetY = (0.4575f - 0.5f) * targetZoom * 2f;
        Camera.main.transform.position = new Vector3(cx - offsetX, cy - offsetY, -10);

        CameraZoom cameraZoom = Camera.main.GetComponent<CameraZoom>();
        if (cameraZoom != null)
        {
            cameraZoom.SetZoom(targetZoom);
        }
        else
        {
            Camera.main.orthographicSize = targetZoom;
        }
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

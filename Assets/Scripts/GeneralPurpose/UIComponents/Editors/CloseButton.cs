using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

//Closes the parent editor
public class CloseButton : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Image cross;
    private Image outline;
    private ProjectManager projectManager;
    private GameObject editor;
    void Start()
    {
        GameObject o = gameObject.transform.GetChild(0).gameObject;
        cross = o.GetComponent<Image>();

        o = gameObject.transform.GetChild(1).gameObject;
        outline = o.GetComponent<Image>();

        
        o = GameObject.FindGameObjectWithTag("ProjectManager");
        projectManager = o.GetComponent<ProjectManager>();

        editor = GameObject.FindWithTag("Editor");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        cross.color = (Color)new Color32(182, 182, 182, 255);
        outline.color = (Color)new Color32(182, 182, 182, 255);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        cross.color = (Color)new Color32(147, 247, 219, 255);
        outline.color = (Color)new Color32(147, 247, 219, 255);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        cross.color = (Color)new Color32(182, 182, 182, 255);
        outline.color = (Color)new Color32(182, 182, 182, 255);

        for( int i = 0; i < editor.transform.childCount; ++i )
        {
            editor.transform.GetChild(i).gameObject.SetActive(false);
        } 

        projectManager.dragActive = true;
        projectManager.zoomActive = true;

    }
}

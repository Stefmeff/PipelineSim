using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonYes : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    private GameObject clearConfirmWindow;
    private ProjectManager projectManager;  //project manager used to delet game objects


    void Awake()
    {
        GameObject o = GameObject.FindGameObjectWithTag("ProjectManager");
        projectManager = o.GetComponent<ProjectManager>();
        clearConfirmWindow = gameObject.transform.parent.gameObject;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        gameObject.GetComponent<Image>().color = (Color)new Color32(0x7D,0x83,0x88,0xFF);
        projectManager.ClearWorld();
        clearConfirmWindow.SetActive(false);
    }

public void OnPointerEnter(PointerEventData eventData)
    {
        gameObject.GetComponent<Image>().color = (Color)new Color32(0x67,0x7C,0x8E,0xFF);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        gameObject.GetComponent<Image>().color = (Color)new Color32(0x7D,0x83,0x88,0xFF);
    }
}

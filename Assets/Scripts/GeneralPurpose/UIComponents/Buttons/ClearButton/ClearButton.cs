using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ClearGame : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    private GameObject clearConfirmWindow;
    private ProjectManager projectManager;  //project manager used to delet game objects
    private GameObject outline;             //outline visualizes hover over the ClearGame Button 
    private TMP_Text toolTip;

    private Image i;
    private void Awake()
    {
        GameObject o = GameObject.FindGameObjectWithTag("ProjectManager");
        projectManager = o.GetComponent<ProjectManager>();

        o = GameObject.FindWithTag("DialogBoxes");
        clearConfirmWindow = o.transform.GetChild(0).gameObject;

        outline = gameObject.transform.GetChild(0).gameObject;
        o = GameObject.FindWithTag("ToolTip");
        toolTip = o.GetComponent<TMP_Text>();

        i =  gameObject.GetComponent<Image>();
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        clearConfirmWindow.SetActive(true);
        //projectManager.ClearWorld();
        //Debug.Log("Clear world...");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        i.color =  new Color32(0x64,0x7B,0xCA,0x96);
        toolTip.text = "  clear project";
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        i.color = new Color32(0x2B,0x2B,0x2B,0xFF);
        toolTip.text = "  tool tip:";
    }
}

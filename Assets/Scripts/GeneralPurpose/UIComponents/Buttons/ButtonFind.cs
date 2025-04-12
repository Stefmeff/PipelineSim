using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;


public class ButtonFind  : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    private ProjectManager projectManager;
    private TMP_Text toolTip;
    void Awake()
    {
        GameObject o = GameObject.FindGameObjectWithTag("ProjectManager");
        projectManager = o.GetComponent<ProjectManager>();

        o = GameObject.FindWithTag("ToolTip");
        toolTip = o.GetComponent<TMP_Text>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        projectManager.FindCenter();
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        Image i = gameObject.GetComponent<Image>();
        i.color =  new Color32(0x64,0x7B,0xCA,0x96);
        toolTip.text = "  find center";
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Image i = gameObject.GetComponent<Image>();
        i.color = new Color32(0x2B,0x2B,0x2B,0xFF);
        toolTip.text = "  tool tip:";

    }
}

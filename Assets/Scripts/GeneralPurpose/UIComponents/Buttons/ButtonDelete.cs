using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonDelete : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    public delegate void DeleteObject();
    public event DeleteObject DeleteObjectEvent;
    private TMP_Text toolTip;

    void Awake()
    {

        GameObject o = GameObject.FindWithTag("ToolTip");
        toolTip = o.GetComponent<TMP_Text>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        DeleteObjectEvent?.Invoke();
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        Image i = gameObject.GetComponent<Image>();
        i.color =  new Color32(0x64,0x7B,0xCA,0x96);
        toolTip.text = "  delete element";
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Image i = gameObject.GetComponent<Image>();
        i.color = new Color32(0x2B,0x2B,0x2B,0xFF);
        toolTip.text = "  tool tip:";

    } 
}

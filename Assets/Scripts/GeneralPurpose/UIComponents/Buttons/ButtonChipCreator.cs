using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonChipCreator : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    private TMP_Text toolTip;

    void Awake()
    {
        GameObject o = GameObject.FindWithTag("ToolTip");
        toolTip = o.GetComponent<TMP_Text>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("Chip Creator: opening editor (not yet implemented)");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Image i = gameObject.GetComponent<Image>();
        i.color = new Color32(0x64,0x7B,0xCA,0x96);
        toolTip.text = "  chip creator";
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Image i = gameObject.GetComponent<Image>();
        i.color = new Color32(0x2B,0x2B,0x2B,0xFF);
        toolTip.text = "  tool tip:";
    }
}

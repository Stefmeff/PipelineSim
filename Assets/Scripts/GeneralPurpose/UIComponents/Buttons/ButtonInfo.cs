using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonInfo : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    private GameObject infoBar;

    private bool infoBarOn = false;
    private Image i;
    private Color32 defaultColor = new Color32(0x42,0x42,0x42,0xFF);
    private Color32 overColor = new Color32(0x32,0x32,0x32,0xFF);

    private TMP_Text toolTip;
    void Awake()
    {
        GameObject o = GameObject.FindGameObjectWithTag("InfoBar");
        infoBar = o.transform.GetChild(0).gameObject;

        i = gameObject.GetComponent<Image>();
        o = GameObject.FindWithTag("ToolTip");
        toolTip = o.GetComponent<TMP_Text>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        infoBarOn = !infoBarOn;
        infoBar.SetActive(infoBarOn);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        i.color = overColor;
        toolTip.text = "  help";
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        i.color = defaultColor;
        toolTip.text = "  tool tip:";
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonRestart : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    private TimeTick timer;
    private TMP_Text toolTip;

    // Start is called before the first frame update
    void Start()
    {
        GameObject o = GameObject.FindWithTag("Timer");
        timer = o.GetComponent<TimeTick>();

        o = GameObject.FindWithTag("ToolTip");
        toolTip = o.GetComponent<TMP_Text>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        timer.restart();

    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        Image i = gameObject.GetComponent<Image>();
        i.color = new Color32(0x64,0x7B,0xCA,0x96);
        toolTip.text = "  reset";
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Image i = gameObject.GetComponent<Image>();
        i.color = new Color32(0x2B,0x2B,0x2B,0xFF);
        toolTip.text = "  tool tip:";
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonInfo : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    private const string helpUrl = "https://stefmeff.github.io/PipelineSim/intro.html";

    private Image i;
    private Color32 defaultColor = new Color32(0x42,0x42,0x42,0xFF);
    private Color32 overColor = new Color32(0x32,0x32,0x32,0xFF);

    private TMP_Text toolTip;
    void Awake()
    {
        i = gameObject.GetComponent<Image>();
        GameObject o = GameObject.FindWithTag("ToolTip");
        toolTip = o.GetComponent<TMP_Text>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Application.OpenURL(helpUrl);
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

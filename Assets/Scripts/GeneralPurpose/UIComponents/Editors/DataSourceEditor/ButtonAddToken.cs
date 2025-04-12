using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class ButtonAddToken : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    private DataSourceEditor dataSourceEditor;
    private TMP_Text toolTip;

    private Image outline;

    private void Awake()
    {
        GameObject o = GameObject.FindWithTag("ToolTip");
        toolTip = o.GetComponent<TMP_Text>();

        dataSourceEditor = gameObject.transform.parent.parent.gameObject.GetComponent<DataSourceEditor>();

        o = gameObject.transform.GetChild(0).gameObject;
        outline = o.GetComponent<Image>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        dataSourceEditor.AddToken();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        outline.color = Color.white;
        toolTip.text = "  add token";
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        outline.color = new Color32(0xB3,0xB3,0xB3,0xFF);
        toolTip.text = "  tool tip:";
    }
}

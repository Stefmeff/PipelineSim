using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

//Opens the ChipEditor for the selected custom chip
public class EditButton : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector] public FunctionBlockEditor editor;

    private Image image;
    private TextMeshProUGUI label;
    private Color32 defaultColor = new Color32(182, 182, 182, 255);
    private Color32 hoverColor = new Color32(147, 247, 219, 255);

    void Awake()
    {
        image = GetComponent<Image>();

        //get text label: ButtonEdit/Image(1)/Text(TMP)
        Transform imgChild = transform.Find("Image (1)");
        if (imgChild != null)
        {
            Transform textChild = imgChild.Find("Text (TMP)");
            if (textChild != null) label = textChild.GetComponent<TextMeshProUGUI>();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (image != null) image.color = (Color)hoverColor;
        if (label != null) label.color = (Color)hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (image != null) image.color = (Color)defaultColor;
        if (label != null) label.color = (Color)defaultColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (image != null) image.color = (Color)defaultColor;
        if (label != null) label.color = (Color)defaultColor;
        if (editor != null) editor.OnEditClicked();
    }
}

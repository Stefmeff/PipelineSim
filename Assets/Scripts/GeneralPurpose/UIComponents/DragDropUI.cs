using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

/**
 * @Description:
 * 
 */
public class DragDropUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject dropPrefab;                   //prefab that should be initialized upon drag & drop

    [SerializeField] private Canvas canvas;     //Canvas of UI element
    private RectTransform rectTransform;        //transfrom component of UI element
    private Image image;

    Color origColor;
    Color dragColor = (Color) new Color32(89, 98, 103,255);

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        origColor = image.color;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {

    }

    public void OnDrag(PointerEventData eventData)
    {
        //TODO: display item at mouse position
        image.color = dragColor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        image.color = origColor;


        //TODO: instantiate icon prefab at mouse location
        Vector3 mousePos;
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        mousePos = GridSnap.Snap(new Vector3(mousePos.x, mousePos.y, 0));
        Instantiate(dropPrefab, mousePos, dropPrefab.transform.rotation);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        image.color = dragColor;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        image.color = origColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        image.color = origColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        image.color = dragColor;
    }
}

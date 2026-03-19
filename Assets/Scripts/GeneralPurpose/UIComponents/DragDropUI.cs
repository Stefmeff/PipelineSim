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

    private static Texture2D dragCursor;
    private static Vector2 dragHotspot;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        origColor = image.color;

        if (dragCursor == null)
        {
            dragCursor = Resources.Load<Texture2D>("Art/Cursors/drag");
            if (dragCursor != null)
                dragHotspot = new Vector2(dragCursor.width / 2f, dragCursor.height / 2f);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {

    }

    public void OnDrag(PointerEventData eventData)
    {
        //TODO: display item at mouse position
        image.color = dragColor;
        Cursor.SetCursor(dragCursor, dragHotspot, CursorMode.Auto);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        image.color = origColor;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);


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

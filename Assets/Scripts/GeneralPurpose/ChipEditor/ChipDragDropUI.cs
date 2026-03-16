using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Drag-drop handler for custom chip library entries.
/// Sets CustomChip.chipNameToSpawn before instantiating the prefab.
/// </summary>
public class ChipDragDropUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    public string chipName;
    public GameObject dropPrefab;

    private Image image;
    private Color origColor;
    private Color dragColor = (Color)new Color32(89, 98, 103, 255);

    void Awake()
    {
        image = GetComponent<Image>();
        if (image != null) origColor = image.color;
    }

    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        if (image != null) image.color = dragColor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (image != null) image.color = origColor;

        // Set which chip to spawn
        CustomChip.chipNameToSpawn = chipName;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos = GridSnap.Snap(new Vector3(mousePos.x, mousePos.y, 0));
        Instantiate(dropPrefab, mousePos, Quaternion.identity);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (image != null) image.color = dragColor;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (image != null) image.color = origColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (image != null) image.color = origColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (image != null) image.color = dragColor;
    }
}

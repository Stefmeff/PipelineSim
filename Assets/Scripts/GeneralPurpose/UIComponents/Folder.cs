using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class Folder : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [SerializeField] private List<GameObject> items;
    private bool opened = true;

    //variables for animation:
    private Image image;
    Color origColor;
    Color overColor = (Color)new Color32(89, 98, 103, 255);

    // Start is called before the first frame update
    void Start()
    {
        image = GetComponent<Image>();
        origColor = image.color;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        opened = !opened;
        foreach(GameObject item in items)
        {
            item.SetActive(opened);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        image.color = origColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        image.color = overColor;
    }
}

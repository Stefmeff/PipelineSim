using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

//Button that activates/deactivates scrollbar
public class LibraryButton : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    private GameObject scrollBar;
    private GameObject outline;
    bool on = true;

    // Start is called before the first frame update
    void Awake()
    {
        scrollBar = GameObject.FindGameObjectWithTag("ScrollBar");
        
        outline = gameObject.transform.GetChild(0).gameObject;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        on = !on;
        scrollBar.SetActive(on);
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        outline.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        outline.SetActive(false);
    }
}


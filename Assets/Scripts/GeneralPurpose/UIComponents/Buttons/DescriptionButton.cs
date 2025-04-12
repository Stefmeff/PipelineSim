using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

//Button that activates/deactivates scrollbar
public class DescriptionButton : MonoBehaviour, IPointerDownHandler
{
    GameObject description;
    bool on = true;

    // Start is called before the first frame update
    void Awake()
    {
        description = GameObject.FindGameObjectWithTag("Description");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        on = !on;
        description.SetActive(on);
    }
}

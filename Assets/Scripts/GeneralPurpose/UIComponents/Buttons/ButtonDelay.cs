using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonDelay : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    //global boolean for displaying delay
    public bool showDelay = false;

    //Delay Editor
    private DelayEditor dEdit;

    //check mark icon for animation
    private GameObject checkMarkIcon;

    //button for animation
    private Vector3 originalScale;
    private Vector3 animateScale;


    // Start is called before the first frame update
    void Awake()
    {
        originalScale = gameObject.transform.localScale;
        animateScale = originalScale * 1.1f;
        GameObject o = gameObject.transform.GetChild(0).gameObject;
        checkMarkIcon = o.transform.GetChild(0).gameObject;

        o = gameObject.transform.parent.parent.parent.parent.gameObject;
        dEdit = o.GetComponent<DelayEditor>();

    }

    public void OnPointerDown(PointerEventData eventData)
    {
        showDelay=!showDelay;
        UpdateButton();
    }

    public void UpdateButton(){
        checkMarkIcon.SetActive(showDelay);
        dEdit.VisualizeDelay(showDelay);    
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        this.transform.localScale = animateScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        this.transform.localScale = originalScale;
    }
}

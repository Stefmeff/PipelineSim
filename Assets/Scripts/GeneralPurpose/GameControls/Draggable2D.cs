using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]  //draggable components need a collider

/**
 * @author: Stefan Moser
 * 
 * @brief: This classed is used for dragging/moving 2D game components with the mouse
 * */
public class Draggable2D : MonoBehaviour
{
    public Component main;  //main component implementing game logic of this 2D-Element

    //variables used for animation
    private new Transform transform;
    private Vector3 originalScale;
    private Vector3 animateScale;
    public GameObject description;

    //variable for ctrl
    private CameraMouseDrag camDrag;
    private SelectCtrl selector;
    private Editor editor;  //opens component editor on left click

    // Start is called before the first frame update
    void Start()
    {
        //init variable for animation:
        transform = this.GetComponent<Transform>();
        originalScale = transform.localScale;
        animateScale = originalScale * 1.1f;

        //init background canvas to disable "world drag" when "object drag"
        GameObject o = GameObject.FindWithTag("BackgroundCanvas");
        o = o.transform.GetChild(0).gameObject;
        camDrag = o.GetComponent<CameraMouseDrag>();

        //init SelectCtrl to steer object => Delete, Rotate,...
        o = GameObject.FindWithTag("Selector");
        selector = o.GetComponent<SelectCtrl>();

        //init SelectCtrl to steer object => Delete, Rotate,...
        o = GameObject.FindWithTag("Editor");
        editor = o.GetComponent<Editor>();
    }


    public void OnMouseDrag()
    {
        camDrag.camDragOn = false;
        if(!editor.active){
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            transform.position = new Vector3(mousePos.x, mousePos.y, 0);
        }
    }

    public void OnMouseUp()
    {
        camDrag.camDragOn = true;
    }

    public void OnMouseOver()
    {
        //animate:
        transform.localScale = animateScale;
        if(description)description.SetActive(true);
        selector.over = this.gameObject;
        selector.overMono = (IObjectMono)this.main;
    }

    public void OnMouseExit()
    {
        transform.localScale = originalScale;
        if(description)description.SetActive(false);
        selector.over = null;
    }
}

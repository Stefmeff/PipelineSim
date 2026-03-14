using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Collider2D))] 
public class Switch_Mono : MonoBehaviour, IObjectMono
{
    public Switch s;
    public OutputPin_Mono output;


    private ProjectManager projectManager;

    private float dragBound = 4.0f;


    //variables used for animation
    private new Transform transform;
    private Vector3 originalScale;
    private Vector3 animateScale;
    private bool drag = false;
    public GameObject description;
    private GameObject animationOn;
    private GameObject animationOff;


    //variable for ctrl
    private CameraMouseDrag camDrag;
    private SelectCtrl selector;


    // Start is called before the first frame update
    void Awake()
    {

        //add to project:
        GameObject o = GameObject.FindGameObjectWithTag("ProjectManager");
        projectManager = o.GetComponent<ProjectManager>();
        projectManager.addToProject(this);

        //init variable for animation:
        transform = this.GetComponent<Transform>();
        originalScale = transform.localScale;
        animateScale = originalScale * 1.1f;
        animationOn = this.transform.GetChild(3).gameObject;
        animationOff = this.transform.GetChild(4).gameObject;

        //init background canvas to disable "world drag" when "object drag"
        o = GameObject.FindWithTag("BackgroundCanvas");
        o = o.transform.GetChild(0).gameObject;
        camDrag = o.GetComponent<CameraMouseDrag>();

        //init SelectCtrl to steer object => Delete, Rotate,...
        o = GameObject.FindWithTag("Selector");
        selector = o.GetComponent<SelectCtrl>();

        this.s = new Switch();
        s.UpdateAnimationEvent += UpdateAnimation;
        InitPins();
    }


    public void Init(Switch s)
    {
        if(this.s != null)this.s.Dispose();
        this.s = s;
        s.UpdateAnimationEvent += UpdateAnimation;
        InitPins();
    }

    private void InitPins()
    {
        output.Init(s.output);
    }

    private void UpdateAnimation(bool b)
    {
       animationOn.SetActive(b);
       animationOff.SetActive(!b);
    }


    public CircuitComponent GetMain()
    {
        return this.s;
    }

    public void SaveTransform()
    {
        this.s.SaveTransform(this.transform);
    }

    public void Clear()
    {
        Destroy(this.gameObject);
    }


    private void OnDestroy()
    {
        projectManager.removeFromProject(this);
        s.UpdateAnimationEvent -= UpdateAnimation;
        s.Dispose();
        s = null;
    }


    //Mouse Input Events:
    private void OnMouseDown()
    {
        camDrag.camDragOn = false;
    }

    private void OnMouseUp()
    {
        if (!drag)
        {
            s.press();
        }
        drag = false;
        camDrag.camDragOn = true;
        transform.position = GridSnap.Snap(transform.position);
    }


    private void OnMouseDrag()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (projectManager.dragActive && drag)
        {
            transform.position = new Vector3(mousePos.x, mousePos.y, 0);
        }
        else if (Mathf.Abs(transform.position.x - mousePos.x) > dragBound || Mathf.Abs(transform.position.y - mousePos.y) > dragBound)
        {
            drag = true;
        }
    }


    private void OnMouseOver()
    {
        //animate:
        transform.localScale = animateScale;
        description.SetActive(true);
        selector.over = this.gameObject;
    }

    private void OnMouseExit()
    {
        transform.localScale = originalScale;
        description.SetActive(false);
        selector.over = null;
    }


}

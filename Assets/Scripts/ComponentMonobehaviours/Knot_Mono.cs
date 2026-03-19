using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
* @author: Stefan Moser
*
* @details: this class is used for coupling the "Knot" class with the respective gameObject and
* also handles the user interaction.
**/
public class Knot_Mono : MonoBehaviour
{
    private Knot knot;
    [SerializeField] private GameObject wirePrefab;   //prefab used for initializing wire
    private Wire_Mono wire_mono = null;

    //variables used for animation
    private new Transform transform;
    private Vector3 originalScale;
    private Vector3 animateScale;
    private CameraMouseDrag camDrag;

    private ProjectManager projectManager;
    private Camera cam;

    private bool drag = false;

    // Start is called before the first frame update
    void Awake()
    {
           

        //init variable for animation:
        transform = this.GetComponent<Transform>();
        originalScale = transform.localScale;
        animateScale = originalScale * 1.2f;

        //control of camera drag:
        GameObject o = GameObject.FindWithTag("BackgroundCanvas");
        o = o.transform.GetChild(0).gameObject;
        camDrag = o.GetComponent<CameraMouseDrag>();

        o = GameObject.FindGameObjectWithTag("ProjectManager");
        projectManager = o.GetComponent<ProjectManager>();

        cam = Camera.main;

        if (knot != null)
        {
            knot.transform = this.transform;
        }
    }

    public void Init(Knot knot)
    {
        this.knot = knot;
        knot.transform = this.transform;
    }

    public void SaveTransform()
    {
        this.knot.SaveTransform(this.gameObject.transform);
    }

    public void Clear()
    {
        if(this!=null)Destroy(this.gameObject);
    }

    private void OnDestroy()
    {
        knot.disconnectWire();
    }

    private void OnMouseOver()
    {
        //animate:
        transform.localScale = animateScale;
    }

    private void OnMouseExit()
    {
        transform.localScale = originalScale;
    }

    private void OnMouseDown()
    {
        camDrag.camDragOn = false;
    }


    /**
    * Drag wire knot
    **/
    private void OnMouseDrag()
    {

        Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        if (projectManager.dragActive && drag)
        {
            transform.position = new Vector3(mousePos.x, mousePos.y, 0);
        }
        else if (Mathf.Abs(transform.position.x - mousePos.x) > 1.5 || Mathf.Abs(transform.position.y - mousePos.y) > 1.5)
        {
            drag = true;
        }
    }

    /**
    * Create new branching wire after knot has been clicked
    **/
    private void OnMouseUp()
    {
        if (!drag)
        {
            //Destroy existing wire:
            if (!knot.disconnectWire())
            {
                //create wire and add this pin as input + knot
                GameObject o = Instantiate(wirePrefab, new Vector3(0, 0, 0), Quaternion.identity);
                wire_mono = o.GetComponent<Wire_Mono>();
                Wire wire = knot.connectWire();
                wire_mono.Init(wire);
                wire_mono.DrawModeOn();
            }
        }

        camDrag.camDragOn = true;
        drag = false;
        transform.position = GridSnap.Snap(transform.position);
    }





}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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
    private SelectCtrl selector; //opens component editor on left click
    private ProjectManager projectManager;
    private Camera cam;
    private static Texture2D dragCursor;
    private static Vector2 dragHotspot;

    // Start is called before the first frame update
    void Start()
    {
        //init variable for animation:
        transform = this.GetComponent<Transform>();
        originalScale = transform.localScale;
        animateScale = originalScale * 1.03f;

        //init background canvas to disable "world drag" when "object drag"
        GameObject o = GameObject.FindWithTag("BackgroundCanvas");
        o = o.transform.GetChild(0).gameObject;
        camDrag = o.GetComponent<CameraMouseDrag>();

        //init SelectCtrl to steer object => Delete, Rotate,...
        o = GameObject.FindWithTag("Selector");
        selector = o.GetComponent<SelectCtrl>();

        o = GameObject.FindGameObjectWithTag("ProjectManager");
        projectManager = o.GetComponent<ProjectManager>();

        cam = Camera.main;

        if (dragCursor == null)
        {
            dragCursor = Resources.Load<Texture2D>("Art/Cursors/drag");
            if (dragCursor != null)
                dragHotspot = new Vector2(dragCursor.width / 2f, dragCursor.height / 2f);
        }
    }


    private bool dragBlocked = false;

    private void OnMouseDown()
    {
        // Block drag if the click started over the sidebar or menu bar
        Vector3 mousePos = Input.mousePosition;
        dragBlocked = mousePos.x < Screen.width * 0.1625f || mousePos.y > Screen.height * 0.915f;
    }

    public void OnMouseDrag()
    {
        if (dragBlocked) return;
        camDrag.camDragOn = false;
        Cursor.SetCursor(dragCursor, dragHotspot, CursorMode.Auto);
        if(projectManager.dragActive){
            Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);

            // Interface pins: only update Y via pinLock
            InterfacePinLock pinLock = GetComponent<InterfacePinLock>();
            if (pinLock != null)
            {
                pinLock.UpdateDragY(mousePos.y);
            }
            else
            {
                transform.position = new Vector3(mousePos.x, mousePos.y, 0);
            }
        }
    }

    public void OnMouseUp()
    {
        camDrag.camDragOn = true;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        InterfacePinLock pinLock = GetComponent<InterfacePinLock>();
        if (pinLock != null)
        {
            pinLock.FinishDrag();
        }
        else
        {
            // Snap to grid on release
            transform.position = GridSnap.Snap(transform.position);
        }
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

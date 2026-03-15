using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/**
 * This class is used for moving the main camera via mouse drag.
 */
public class CameraMouseDrag : MonoBehaviour, IPointerDownHandler, IEndDragHandler, IDragHandler
{
    private Vector3 origin;
    private Vector3 difference;

    private Camera cam;
    public Texture2D cursorTexture;
    public bool camDragOn = true;

    void Start()
    {
        //select main camera
        cam = Camera.main;
        //cursorTexture = Resources.Load("SmoothHand") as Texture2D;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        //get original mouse position
        origin = GetMousePos;

        //Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
    }

    public void OnDrag(PointerEventData eventData)
    {

        if(!camDragOn) return;


        //calculate the difference of the mouse drag and reposition camera
        difference = GetMousePos - cam.transform.position;
        cam.transform.position = origin - difference;

    }

    public void OnEndDrag(PointerEventData eventData)
    {


        //Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    private Vector3 GetMousePos => cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());

}

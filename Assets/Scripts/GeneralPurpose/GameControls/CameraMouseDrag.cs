using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/**
 * This class is used for moving the main camera via mouse drag.
 */
public class CameraMouseDrag : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IEndDragHandler, IDragHandler
{
    private Vector3 origin;
    private Vector3 difference;

    private Camera cam;
    private Texture2D moveCursor;
    private Vector2 cursorHotspot;
    public bool camDragOn = true;
    private PinConnectionHandler connectionHandler;

    void Start()
    {
        cam = Camera.main;
        moveCursor = Resources.Load<Texture2D>("Art/Cursors/move");
        if (moveCursor != null)
            cursorHotspot = new Vector2(moveCursor.width / 2f, moveCursor.height / 2f);
        connectionHandler = FindObjectOfType<PinConnectionHandler>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        origin = GetMousePos;

        // Only show move cursor if no component is under the mouse and not drawing a wire
        bool drawingWire = connectionHandler != null && connectionHandler.searchesConnection != null;
        if (camDragOn && !drawingWire && !Physics2D.OverlapPoint(cam.ScreenToWorldPoint(Mouse.current.position.ReadValue())))
            Cursor.SetCursor(moveCursor, cursorHotspot, CursorMode.Auto);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if(!camDragOn) return;
        difference = GetMousePos - cam.transform.position;
        cam.transform.position = origin - difference;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    private Vector3 GetMousePos => cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());

}

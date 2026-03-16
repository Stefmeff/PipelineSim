using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Locks a world-space interface pin to the bar edge on the X axis.
/// Y is only modified when user drags the pin.
/// </summary>
public class InterfacePinLock : MonoBehaviour
{
    private PinBarClickHandler parentBar;
    private RectTransform barRect;
    private bool isInputSide;
    private Canvas canvas;
    private Camera canvasCam;

    private float savedY;

    public void Init(PinBarClickHandler bar, float initialX)
    {
        parentBar = bar;
        barRect = bar.GetComponent<RectTransform>();
        isInputSide = bar.IsInputSide();
        canvas = barRect.GetComponentInParent<Canvas>();
        canvasCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        Vector3 pos = transform.position;
        pos.x = initialX;
        transform.position = pos;
        savedY = pos.y;
    }

    private void LateUpdate()
    {
        if (barRect == null) return;

        // Lock X to bar edge
        Vector3[] corners = new Vector3[4];
        barRect.GetWorldCorners(corners);

        Vector2 screenPoint;
        if (isInputSide)
            screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCam, corners[2]);
        else
            screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCam, corners[0]);

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        screenPos.x = screenPoint.x;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);

        // Always lock X to bar, always keep savedY
        transform.position = new Vector3(worldPos.x, savedY, 0f);
    }

    /// <summary>
    /// Called by Draggable2D during drag. Updates Y with neighbor clamping.
    /// </summary>
    public void UpdateDragY(float newY)
    {
        savedY = ClampYBetweenNeighbors(newY);
    }

    /// <summary>
    /// Called on drag end. Snaps Y to grid.
    /// </summary>
    public void FinishDrag()
    {
        savedY = GridSnap.Snap(new Vector3(0, savedY, 0)).y;
        savedY = ClampYBetweenNeighbors(savedY);
        if (parentBar != null) parentBar.RenumberPins();
    }

    private float ClampYBetweenNeighbors(float y)
    {
        if (parentBar == null) return y;

        float grid = GridSnap.gridSize;
        List<GameObject> pins = parentBar.GetPins();

        float upperLimit = float.MaxValue;
        float lowerLimit = float.MinValue;

        foreach (GameObject other in pins)
        {
            if (other == null || other == gameObject) continue;

            InterfacePinLock otherLock = other.GetComponent<InterfacePinLock>();
            float otherY = otherLock != null ? otherLock.savedY : other.transform.position.y;

            if (otherY > y)
            {
                float ceiling = otherY - grid;
                if (ceiling < upperLimit) upperLimit = ceiling;
            }
            else if (otherY < y)
            {
                float floor = otherY + grid;
                if (floor > lowerLimit) lowerLimit = floor;
            }
        }

        return Mathf.Clamp(y, lowerLimit, upperLimit);
    }
}

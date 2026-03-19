using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

/**
 * @brief Locks a world-space interface pin to the bar edge on the X axis.
 * Y is only modified when user drags the pin.
 *
 * @details Also creates a UI text label on the bar's Canvas that follows the pin's
 * screen position. The label stays on the bar regardless of zoom because it's a
 * UI element on the same Canvas.
 **/
public class InterfacePinLock : MonoBehaviour
{
    private PinBarClickHandler parentBar;
    private RectTransform barRect;
    private bool isInputSide;
    private Canvas canvas;
    private Camera canvasCam;
    private Camera cam;

    private float savedY;

    private TextMeshProUGUI pinLabel;  /**< UI label showing the pin name on the bar */
    private RectTransform labelRect;
    private Outline labelOutline;     /**< border outline, shown on hover */

    public void Init(PinBarClickHandler bar, float initialX)
    {
        parentBar = bar;
        barRect = bar.GetComponent<RectTransform>();
        isInputSide = bar.IsInputSide();
        canvas = barRect.GetComponentInParent<Canvas>();
        canvasCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        cam = Camera.main;

        Vector3 pos = transform.position;
        pos.x = initialX;
        transform.position = pos;
        savedY = pos.y;

        CreateLabel();
        SubscribeToNameChanges();
    }

    /**
     * @brief Creates a UI label on the bar's Canvas that displays the pin name.
     **/
    private void CreateLabel()
    {
        // Border rectangle
        GameObject borderObj = new GameObject("PinLabelBorder");
        borderObj.transform.SetParent(barRect, false);
        Image border = borderObj.AddComponent<Image>();
        border.color = new Color32(0x4B, 0x5C, 0x96, 0xFF);

        labelRect = borderObj.GetComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(barRect.rect.width, 20f);

        // Text inside the border
        GameObject labelObj = new GameObject("PinLabel");
        labelObj.transform.SetParent(borderObj.transform, false);

        pinLabel = labelObj.AddComponent<TextMeshProUGUI>();
        pinLabel.fontSize = 14;
        pinLabel.alignment = TextAlignmentOptions.Center;
        pinLabel.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        pinLabel.enableWordWrapping = false;
        pinLabel.overflowMode = TextOverflowModes.Truncate;

        RectTransform textRect = labelObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        // Hover outline — hidden by default
        labelOutline = borderObj.AddComponent<Outline>();
        labelOutline.effectColor = new Color(1f, 1f, 1f, 0.15f);
        labelOutline.effectDistance = new Vector2(1f, 1f);
        labelOutline.enabled = false;

        // Click handler + hover handler
        PinLabelUI handler = borderObj.AddComponent<PinLabelUI>();
        handler.Init(this);

        // Get initial pin name
        ComponentMono comp = GetComponent<ComponentMono>();
        if (comp != null)
        {
            if (comp.component is ChipInputNode cin) pinLabel.text = TruncateName(cin.pinName);
            else if (comp.component is ChipOutputNode cout) pinLabel.text = TruncateName(cout.pinName);
        }
    }

    /**
     * @brief Called when the label is clicked — opens the pin editor.
     **/
    public void OnLabelClicked()
    {
        ComponentMono comp = GetComponent<ComponentMono>();
        if (comp != null && comp.component != null)
            comp.component.OpenEditor();
    }

    /**
     * @brief Called when the mouse enters/exits the label — toggles the outline.
     **/
    public void OnLabelHover(bool hovering)
    {
        if (labelOutline != null) labelOutline.enabled = hovering;
    }

    private void OnMouseOver()
    {
        OnLabelHover(true);
    }

    private void OnMouseExit()
    {
        OnLabelHover(false);
    }

    private void OnMouseDrag()
    {
        OnLabelHover(true);
    }

    private void OnMouseUp()
    {
        OnLabelHover(false);
    }

    /**
     * @brief Subscribes to the pin's UpdateLabelEvent so the UI label stays in sync.
     **/
    private void SubscribeToNameChanges()
    {
        ComponentMono comp = GetComponent<ComponentMono>();
        if (comp == null) return;

        if (comp.component is ChipInputNode cin)
            cin.UpdateLabelEvent += (name) => { if (pinLabel != null) pinLabel.text = TruncateName(name); };
        else if (comp.component is ChipOutputNode cout)
            cout.UpdateLabelEvent += (name) => { if (pinLabel != null) pinLabel.text = TruncateName(name); };
    }

    private string TruncateName(string name)
    {
        return name.Length > 6 ? name.Substring(0, 6) : name;
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

        Vector3 screenPos = cam.WorldToScreenPoint(transform.position);
        screenPos.x = screenPoint.x;
        Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);

        // Always lock X to bar, always keep savedY
        transform.position = new Vector3(worldPos.x, savedY, 0f);

        // Update UI label position to follow the pin's Y on screen
        if (labelRect != null)
        {
            Vector2 pinScreen = cam.WorldToScreenPoint(transform.position);
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                barRect, pinScreen, canvasCam, out localPoint);
            labelRect.anchoredPosition = new Vector2(0, localPoint.y);
        }
    }

    private void OnDestroy()
    {
        if (labelRect != null) Destroy(labelRect.gameObject);
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

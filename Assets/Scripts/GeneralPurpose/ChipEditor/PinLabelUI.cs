using UnityEngine;
using UnityEngine.EventSystems;

/**
 * @brief Handles right-click and hover events on the pin label UI element.
 *
 * @details Attached to the label border GameObject. Right-click opens the pin editor.
 * Hover toggles a white outline around the label. All other interaction (drag, select)
 * is handled by the pin's world-space BoxCollider2D via Draggable2D.
 **/
public class PinLabelUI : MonoBehaviour, IPointerClickHandler
{
    private InterfacePinLock pinLock;

    public void Init(InterfacePinLock pinLock)
    {
        this.pinLock = pinLock;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && pinLock != null)
            pinLock.OnLabelClicked();
    }
}

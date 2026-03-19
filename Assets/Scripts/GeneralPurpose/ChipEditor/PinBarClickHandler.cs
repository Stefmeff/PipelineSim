using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

/**
 * @brief Attached to the UI pin bar image. On left-click spawns a world-space pin prefab
 * at the clicked Y position, locked to the bar edge's X coordinate.
 **/
public class PinBarClickHandler : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private int maxPins = 8;
    [SerializeField] private bool isInputSide = true;

    private RectTransform barRect;
    private List<GameObject> pins = new List<GameObject>();

    private void Awake()
    {
        barRect = GetComponent<RectTransform>();
    }

    /// <summary>
    /// Calculates the world X where pins should be placed.
    /// Input side: right edge of bar. Output side: left edge of bar.
    /// </summary>
    private float GetFixedWorldX()
    {
        Vector3[] corners = new Vector3[4];
        barRect.GetWorldCorners(corners);
        // corners: 0=bottom-left, 1=top-left, 2=top-right, 3=bottom-right

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera canvasCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (isInputSide)
        {
            // Right edge of input bar
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCam, corners[2]);
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, 0));
            return GridSnap.Snap(worldPoint).x;
        }
        else
        {
            // Left edge of output bar
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCam, corners[0]);
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, 0));
            return GridSnap.Snap(worldPoint).x;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (pins.Count >= maxPins) return;

        string prefabName = isInputSide ? "ChipInput" : "ChipOutput";
        GameObject prefab = Resources.Load("Prefabs/" + prefabName) as GameObject;
        if (prefab == null)
        {
            Debug.LogError(prefabName + " prefab not found in Resources/Prefabs/");
            return;
        }

        float fixedX = GetFixedWorldX();

        // Convert click position to world space
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(eventData.position);
        worldPos.z = 0f;
        worldPos.x = fixedX;

        // Snap Y to grid
        worldPos = GridSnap.Snap(worldPos);
        worldPos.x = fixedX;

        // Check if grid position is already occupied
        if (IsPositionOccupied(worldPos))
        {
            worldPos = FindNearestFreePosition(worldPos, fixedX);
            if (worldPos == Vector3.zero) return;
        }

        GameObject pin = Instantiate(prefab, worldPos, Quaternion.identity);

        // Add the pin lock script
        InterfacePinLock pinLock = pin.GetComponent<InterfacePinLock>();
        if (pinLock == null) pinLock = pin.AddComponent<InterfacePinLock>();
        pinLock.Init(this, fixedX);

        pins.Add(pin);
        RenumberPins();
    }

    private bool IsPositionOccupied(Vector3 pos)
    {
        float threshold = GridSnap.gridSize * 0.4f;
        foreach (GameObject pin in pins)
        {
            if (pin == null) continue;
            if (Mathf.Abs(pin.transform.position.y - pos.y) < threshold) return true;
        }
        return false;
    }

    private Vector3 FindNearestFreePosition(Vector3 target, float fixedX)
    {
        float grid = GridSnap.gridSize;
        for (int i = 1; i <= maxPins; i++)
        {
            Vector3 up = new Vector3(fixedX, target.y + grid * i, 0);
            if (!IsPositionOccupied(up)) return up;

            Vector3 down = new Vector3(fixedX, target.y - grid * i, 0);
            if (!IsPositionOccupied(down)) return down;
        }
        return Vector3.zero;
    }

    /// <summary>
    /// Registers an existing pin GameObject (e.g. loaded from file) with this bar.
    /// Adds InterfacePinLock and snaps X to bar edge.
    /// </summary>
    public void RegisterExistingPin(GameObject pin)
    {
        float fixedX = GetFixedWorldX();

        InterfacePinLock pinLock = pin.GetComponent<InterfacePinLock>();
        if (pinLock == null) pinLock = pin.AddComponent<InterfacePinLock>();
        pinLock.Init(this, fixedX);

        pins.Add(pin);
    }

    public void RemovePin(GameObject pin)
    {
        pins.Remove(pin);
        Destroy(pin);
    }

    public List<GameObject> GetPinsOrdered()
    {
        pins.RemoveAll(p => p == null);
        return pins.OrderByDescending(p => p.transform.position.y).ToList();
    }

    public List<GameObject> GetPins()
    {
        pins.RemoveAll(p => p == null);
        return pins;
    }

    public bool IsInputSide()
    {
        return isInputSide;
    }

    /// <summary>
    /// Renumbers auto-named pins based on visual order (top = 1).
    /// Pins with custom names are left unchanged.
    /// </summary>
    public void RenumberPins()
    {
        string prefix = isInputSide ? "IN" : "OUT";
        var ordered = GetPinsOrdered();

        for (int i = 0; i < ordered.Count; i++)
        {
            ComponentMono comp = ordered[i].GetComponent<ComponentMono>();
            if (comp == null) continue;

            string currentName = null;
            if (comp.component is ChipInputNode cin) currentName = cin.pinName;
            else if (comp.component is ChipOutputNode cout) currentName = cout.pinName;

            if (currentName == null) continue;

            // Check if name is auto-generated (prefix + number, or default "In"/"Out")
            if (IsAutoName(currentName, prefix))
            {
                string newName = prefix + (i + 1);
                if (comp.component is ChipInputNode cin2) cin2.SetPinName(newName);
                else if (comp.component is ChipOutputNode cout2) cout2.SetPinName(newName);
            }
        }
    }

    private bool IsAutoName(string name, string prefix)
    {
        if (name == "In" || name == "Out") return true;
        if (!name.StartsWith(prefix)) return false;
        string suffix = name.Substring(prefix.Length);
        return int.TryParse(suffix, out _);
    }
}

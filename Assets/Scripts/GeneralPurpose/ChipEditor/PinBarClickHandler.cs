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
    private ProjectManager projectManager;
    private Canvas canvas;
    private Camera canvasCam;
    private Dictionary<GameObject, bool> pinHiddenState = new Dictionary<GameObject, bool>();

    private void Awake()
    {
        barRect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        GameObject pmObj = GameObject.FindGameObjectWithTag("ProjectManager");
        if (pmObj != null) projectManager = pmObj.GetComponent<ProjectManager>();
    }

    private void LateUpdate()
    {
        if (pins.Count == 0) return;

        float barEdgeWorldX = GetBarEdgeWorldX();

        foreach (GameObject pin in pins)
        {
            if (pin == null) continue;

            bool shouldHide = ShouldHidePin(pin, barEdgeWorldX);

            // Only update when state changes (SetVisible uses FindObjectsOfType)
            bool wasHidden;
            pinHiddenState.TryGetValue(pin, out wasHidden);
            if (shouldHide != wasHidden)
            {
                pinHiddenState[pin] = shouldHide;
                InterfacePinLock pinLock = pin.GetComponent<InterfacePinLock>();
                if (pinLock != null) pinLock.SetVisible(!shouldHide);
            }
        }
    }

    /// <summary>
    /// Checks if a pin's wire points in the wrong direction (backwards).
    /// For input pins: hide if the connected component is to the LEFT of the bar edge.
    /// For output pins: hide if the connected component is to the RIGHT of the bar edge.
    /// </summary>
    private bool ShouldHidePin(GameObject pin, float barEdgeWorldX)
    {
        ComponentMono comp = pin.GetComponent<ComponentMono>();
        if (comp == null) return false;

        if (isInputSide)
        {
            // ChipInput has an OutputPin — check where its connected InputPins are
            OutputPin_Mono outMono = pin.GetComponentInChildren<OutputPin_Mono>();
            if (outMono != null && outMono.pin != null)
            {
                foreach (InputPin connected in outMono.pin.GetConnectedPins())
                {
                    if (connected.transform != null && connected.transform.position.x < barEdgeWorldX)
                        return true;
                }
            }
        }
        else
        {
            // ChipOutput has an InputPin — check where the source OutputPin is
            InputPin_Mono inMono = pin.GetComponentInChildren<InputPin_Mono>();
            if (inMono != null && inMono.pin != null)
            {
                Transform source = inMono.pin.GetSourceTransform();
                if (source != null && source.position.x > barEdgeWorldX)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns the bar edge world X without grid snapping (for visibility checks).
    /// </summary>
    private float GetBarEdgeWorldX()
    {
        Vector3[] corners = new Vector3[4];
        barRect.GetWorldCorners(corners);

        Vector2 screenPoint;
        if (isInputSide)
            screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCam, corners[2]); // right edge
        else
            screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCam, corners[0]); // left edge

        return Camera.main.ScreenToWorldPoint(new Vector3(screenPoint.x, 0, 0)).x;
    }

    /// <summary>
    /// Calculates the world X where pins should be placed.
    /// Input side: right edge of bar. Output side: left edge of bar.
    /// </summary>
    private float GetFixedWorldX()
    {
        return GridSnap.Snap(new Vector3(GetBarEdgeWorldX(), 0, 0)).x;
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
        pinHiddenState.Remove(pin);
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

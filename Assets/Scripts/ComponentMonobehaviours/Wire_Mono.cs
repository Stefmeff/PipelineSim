using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(EdgeCollider2D))]

/**
 * @brief MonoBehaviour responsible for drawing and updating wires in the circuit.
 *
 * @details Wires are rendered as Bezier curves between a source pin, optional knots,
 * and a sink pin using a LineRenderer. A dirty flag system ensures that the expensive
 * Bezier geometry rebuild only runs when a pin or knot has actually moved, rather than
 * every frame unconditionally.
 *
 * Multi-bit (bus) wires display a slash notation with bit width label at the midpoint
 * of the first segment. Bus label properties are cached to avoid redundant TMP updates.
 **/
public class Wire_Mono : MonoBehaviour, IObjectMono
{
    private Wire wire;   /**< main Wire data object that stores the state of this GameObject */

    private Transform sourcePin;   /**< transform of the wire's source (OutputPin) */
    private Transform sinkPin;     /**< transform of the wire's sink (InputPin) */
    private List<Knot_Mono> knots; /**< visual knot MonoBehaviours along the wire */
    private PinConnectionHandler connectionHandler;

    private bool draw = false;       /**< true while the user is actively drawing/creating this wire */
    private Transform mouseTransform;

    public GameObject knotPrefab;

    private UnityEngine.LineRenderer lineRend; /**< LineRenderer component for drawing the wire curve */
    private EdgeCollider2D edgeColl;           /**< EdgeCollider2D for mouse interaction (hover, click) */

    private Vector2 lastMousePos;

    private ProjectManager projectManager;
    private Camera cam;

    /**
     * @name Dirty flag system
     * @brief Prevents the expensive Bezier rebuild from running every frame.
     *
     * @details Each frame, the cached positions of source, sink and knots are compared
     * against their current transforms. If nothing moved, UpdateLineRenderer() is skipped entirely.
     * During wire creation (draw mode), the wire is always dirty since the mouse moves every frame.
     * @{
     **/
    private bool dirty = true;                                        /**< if true, geometry needs rebuild */
    private Vector3 cachedSourcePos;                                  /**< last known source pin position */
    private Vector3 cachedSinkPos;                                    /**< last known sink pin position */
    private List<Vector3> cachedKnotPositions = new List<Vector3>();   /**< last known knot positions */
    /** @} */

    /**
     * @name Bus label cache
     * @brief Avoids redundant TMP property assignments that trigger internal layout work.
     * @{
     **/
    private Color cachedWireColor;   /**< last wire color applied to the LineRenderer */
    private string cachedBusText;    /**< last bit width string written to the TMP label */
    private Color cachedBusColor;    /**< last color applied to the bus slash and text */
    /** @} */

    // Bus notation GameObjects
    private GameObject busLabel;
    private TextMeshPro busText;
    private LineRenderer slashLine;

    private void Awake()
    {
        //add to project:
        GameObject o = GameObject.FindGameObjectWithTag("ProjectManager");
        projectManager = o.GetComponent<ProjectManager>();
        projectManager.addToProject(this);
        cam = Camera.main;

        //init connection handler:
        o = GameObject.FindGameObjectWithTag("PinConnectionHandler");
        connectionHandler = o.GetComponent<PinConnectionHandler>();

        //init mouse transform:
        o = GameObject.FindGameObjectWithTag("MousePointer");
        mouseTransform = o.transform;

        lineRend = this.GetComponent<LineRenderer>();
        edgeColl = this.GetComponent<EdgeCollider2D>();
        knots = new List<Knot_Mono>();
    }

    /**
     * @brief Initializes the wire visual from a Wire data object.
     *
     * @details Loads all knot GameObjects, stores pin transforms, and subscribes
     * to the wire's destruct event. Marks the wire as dirty so it renders on the next frame.
     *
     * @param[in] wire the Wire data object containing pin references and knot list
     **/
    public void Init(Wire wire)
    {
        this.wire = wire;

        if(wire.dataIn != null) sourcePin = wire.dataIn.transform;
        if(wire.dataOut != null) sinkPin = wire.dataOut.transform;

        foreach(Knot k in wire.knots)
        {
            Knot_Mono m = (Knot_Mono)k.Load();
            knots.Add(m);
        }

        this.wire.DestructEvent += Clear;
        dirty = true;
    }

    public CircuitComponent GetMain()
    {
        return this.wire;
    }

    public void SaveTransform()
    {
        this.wire.SaveTransform(this.transform);

        foreach (Knot_Mono k in knots)
        {
            k.SaveTransform();
        }
    }

    /// <summary>
    /// Returns true if this wire's source or sink pin is a child of the given transform.
    /// </summary>
    public bool IsConnectedTo(Transform parent)
    {
        if (sourcePin != null && sourcePin.IsChildOf(parent)) return true;
        if (sinkPin != null && sinkPin.IsChildOf(parent)) return true;
        return false;
    }

    /// <summary>
    /// Shows or hides this wire's visual elements (LineRenderer, knots, bus label).
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (lineRend != null) lineRend.enabled = visible;
        if (slashLine != null) slashLine.enabled = visible;
        if (busLabel != null) busLabel.SetActive(visible);
        foreach (Knot_Mono k in knots)
        {
            if (k != null) k.gameObject.SetActive(visible);
        }
    }

    public void Clear()
    {
        Destroy(this.gameObject);
    }


    private void OnDestroy()
    {
        projectManager.removeFromProject(this);

        //unsubscribe from wire event:
        this.wire.DestructEvent -= Clear;

        //destroy all the knots of this wire:
        foreach (Knot_Mono k in knots)
        {
            k.Clear();
        }

        if (busLabel != null) Destroy(busLabel);
    }

    /**
     * @brief Per-frame update: checks dirty flag and rebuilds wire geometry only when needed.
     *
     * @details The update performs three checks in order:
     * 1. If in draw mode, the wire is always dirty (mouse position changes every frame).
     * 2. If not dirty, compares source/sink/knot positions against cached values.
     *    Any mismatch sets dirty = true.
     * 3. If the wire's signal color changed, updates the LineRenderer colors and marks dirty
     *    (so the bus label color is also refreshed).
     *
     * Only when dirty is true does the expensive UpdateLineRenderer() (Bezier rebuild) run.
     * Afterwards, positions are cached and dirty is reset to false.
     **/
    private void Update()
    {
        if (wire == null || sourcePin == null)
        {
            Destroy(this.gameObject);
            return;
        }

        // Always dirty during wire creation (mouse moves every frame)
        if (draw)
        {
            dirty = true;
            drawWire();
        }

        // Check if source or sink moved
        if (!dirty)
        {
            if (sourcePin.position != cachedSourcePos) dirty = true;
            else if (sinkPin != null && sinkPin.position != cachedSinkPos) dirty = true;
            else
            {
                // Check if any knot moved
                for (int i = 0; i < wire.knots.Count; i++)
                {
                    Vector3 knotPos = wire.knots[i].transform.position;
                    if (i >= cachedKnotPositions.Count || knotPos != cachedKnotPositions[i])
                    {
                        dirty = true;
                        break;
                    }
                }
                if (wire.knots.Count != cachedKnotPositions.Count) dirty = true;
            }
        }

        // Update wire color (cheap — only set when changed)
        if (wire.coloring != cachedWireColor)
        {
            cachedWireColor = wire.coloring;
            lineRend.startColor = wire.coloring;
            lineRend.endColor = wire.coloring;
            dirty = true; // color change affects bus label too
        }

        if (dirty)
        {
            UpdateLineRenderer();
            CachePositions();
            dirty = false;
        }
    }

    /**
     * @brief Caches the current positions of source, sink and all knots
     * for dirty-checking on the next frame.
     **/
    private void CachePositions()
    {
        cachedSourcePos = sourcePin.position;
        if (sinkPin != null) cachedSinkPos = sinkPin.position;

        cachedKnotPositions.Clear();
        foreach (Knot knot in wire.knots)
        {
            cachedKnotPositions.Add(knot.transform.position);
        }
    }


    public void DrawModeOn()
    {
        connectionHandler.searchesConnection = wire.dataIn;
        draw = true;
    }

    /**
     * @brief Handles mouse input during wire creation (draw mode).
     *
     * @details Left click on a compatible pin completes the connection. Left click
     * on empty space adds a knot. Right click aborts wire creation and destroys the wire.
     * Holding Ctrl during connection aligns the target component to the wire endpoint.
     **/
    private void drawWire()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (connectionHandler.possibleConnection != null)
            {
                //output pin selected:
                wire.setOutput(connectionHandler.possibleConnection);

                //if ctrl: align connected pin
                if(Input.GetKey(KeyCode.LeftControl)){
                    Vector2 pinPos = wire.dataOut.transform.position;
                    Vector2 direction = lastMousePos - pinPos;

                    Transform parent = wire.dataOut.transform.parent;
                    Vector2 parentPosition = parent.position;
                    parent.transform.position = parentPosition + direction;
                }
                connectionHandler.possibleConnection.connectWire(wire);
                draw = false;
                connectionHandler.searchesConnection = null;
                sinkPin = wire.dataOut.transform;
            }
            else
            {
                addKnot();
            }
        }
        else if (Input.GetMouseButtonDown(1))
        {
            //abort wire creation
            connectionHandler.searchesConnection = null;
            wire.dataIn.disconnectWire();
            Destroy(this.gameObject);
        }
    }


    /**
     * @brief Rebuilds the wire's Bezier curve geometry and updates the LineRenderer.
     *
     * @details Collects control points (source -> knots -> sink/mouse), then for each
     * segment either draws a straight line (if axis-aligned) or a cubic Bezier curve
     * with L-shaped control points. Also updates the bus notation label for multi-bit wires.
     **/
    private void UpdateLineRenderer()
    {
        // Collect all control points in order: source -> knots -> sink/mouse
        List<Vector3> controlPoints = new List<Vector3>();
        controlPoints.Add(sourcePin.position);

        foreach(Knot knot in wire.knots)
        {
            controlPoints.Add(knot.transform.position);
        }

        if(draw)
        {
            lastMousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            if(Input.GetKey(KeyCode.LeftControl)){
                Vector2 knotPos;
                if(wire.knots.Count == 0){
                    knotPos = wire.dataIn.transform.position;
                }else{
                    knotPos = wire.knots[wire.knots.Count - 1].transform.position;
                }
                lastMousePos = fixedAngle(knotPos, lastMousePos);
            }
            controlPoints.Add(new Vector3(lastMousePos.x, lastMousePos.y, 0));
        }
        else if(wire.dataOut != null && sinkPin != null)
        {
            controlPoints.Add(sinkPin.position);
        }

        // Build the line: straight segments for aligned points, curves for others
        List<Vector3> renderPoints = new List<Vector3>();
        if(controlPoints.Count > 0) renderPoints.Add(controlPoints[0]);

        for(int i = 1; i < controlPoints.Count; i++)
        {
            Vector3 p0 = controlPoints[i - 1];
            Vector3 p1 = controlPoints[i];

            bool aligned = Mathf.Abs(p0.x - p1.x) < 0.1f || Mathf.Abs(p0.y - p1.y) < 0.1f;

            if(aligned)
            {
                renderPoints.Add(p1);
            }
            else
            {
                // Cubic bezier with L-shaped control points
                Vector3 c1, c2;
                if(Mathf.Abs(p1.x - p0.x) > Mathf.Abs(p1.y - p0.y))
                {
                    // Wider than tall: go horizontal first, then vertical
                    float midX = (p0.x + p1.x) * 0.5f;
                    c1 = new Vector3(midX, p0.y, 0);
                    c2 = new Vector3(midX, p1.y, 0);
                }
                else
                {
                    // Taller than wide: go vertical first, then horizontal
                    float midY = (p0.y + p1.y) * 0.5f;
                    c1 = new Vector3(p0.x, midY, 0);
                    c2 = new Vector3(p1.x, midY, 0);
                }

                int segments = 16;
                for(int s = 1; s <= segments; s++)
                {
                    float t = s / (float)segments;
                    Vector3 point = CubicBezier(p0, c1, c2, p1, t);
                    renderPoints.Add(point);
                }
            }
        }

        lineRend.positionCount = renderPoints.Count;
        for(int i = 0; i < renderPoints.Count; i++)
        {
            lineRend.SetPosition(i, renderPoints[i]);
        }

        // Update bus notation
        UpdateBusLabel(controlPoints);
    }

    /**
     * @brief Updates the bus notation label (slash + bit width) for multi-bit wires.
     *
     * @details Only shown for wires with width > 1. The slash is positioned perpendicular
     * to the wire direction at the first segment's midpoint. Text and color properties
     * are cached and only updated when they actually change, avoiding redundant TMP work.
     *
     * @param[in] controlPoints the wire's control points (source, knots, sink)
     **/
    private void UpdateBusLabel(List<Vector3> controlPoints)
    {
        int width = wire.dataIn != null ? wire.dataIn.width : 1;

        if (width <= 1)
        {
            // Single-bit wire — hide label if it exists
            if (busLabel != null) busLabel.SetActive(false);
            return;
        }

        // Create label on first use
        if (busLabel == null) CreateBusLabel();
        busLabel.SetActive(true);

        // Position on first segment midpoint
        if (controlPoints.Count < 2) return;

        Vector3 p0 = controlPoints[0];
        Vector3 p1 = controlPoints[1];
        bool aligned = Mathf.Abs(p0.x - p1.x) < 0.1f || Mathf.Abs(p0.y - p1.y) < 0.1f;

        Vector3 midpoint;
        Vector3 tangent;

        if (aligned)
        {
            midpoint = (p0 + p1) * 0.5f;
            tangent = (p1 - p0).normalized;
        }
        else
        {
            // Bezier midpoint at t=0.5
            Vector3 c1, c2;
            if (Mathf.Abs(p1.x - p0.x) > Mathf.Abs(p1.y - p0.y))
            {
                float midX = (p0.x + p1.x) * 0.5f;
                c1 = new Vector3(midX, p0.y, 0);
                c2 = new Vector3(midX, p1.y, 0);
            }
            else
            {
                float midY = (p0.y + p1.y) * 0.5f;
                c1 = new Vector3(p0.x, midY, 0);
                c2 = new Vector3(p1.x, midY, 0);
            }
            midpoint = CubicBezier(p0, c1, c2, p1, 0.5f);
            // Tangent = derivative of cubic bezier at t=0.5
            tangent = CubicBezierDerivative(p0, c1, c2, p1, 0.5f).normalized;
        }

        busLabel.transform.position = midpoint;

        // Only update TMP text when bit width actually changes
        string widthStr = width.ToString();
        if (cachedBusText != widthStr)
        {
            cachedBusText = widthStr;
            busText.text = widthStr;
        }

        // Position slash perpendicular to wire direction
        float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
        float slashAngle = angle + 85f; // nearly perpendicular to wire
        float slashSize = 2.5f;

        Vector3 slashDir = new Vector3(Mathf.Cos(slashAngle * Mathf.Deg2Rad), Mathf.Sin(slashAngle * Mathf.Deg2Rad), 0);
        slashLine.SetPosition(0, midpoint - slashDir * slashSize);
        slashLine.SetPosition(1, midpoint + slashDir * slashSize);
        slashLine.startWidth = 1f;
        slashLine.endWidth = 1f;

        // Only update bus colors when wire color actually changes
        if (cachedBusColor != wire.coloring)
        {
            cachedBusColor = wire.coloring;
            slashLine.startColor = wire.coloring;
            slashLine.endColor = wire.coloring;
            busText.color = wire.coloring;
        }

        // Position text above the slash
        Vector3 perpendicular = new Vector3(-tangent.y, tangent.x, 0);
        float textOffset = 5.5f;
        busText.transform.position = midpoint + perpendicular * textOffset;
    }

    /**
     * @brief Creates the bus notation GameObjects (slash line + text label) on first use.
     **/
    private void CreateBusLabel()
    {
        busLabel = new GameObject("BusLabel");
        busLabel.transform.SetParent(transform);

        // Create slash line
        GameObject slashObj = new GameObject("BusSlash");
        slashObj.transform.SetParent(busLabel.transform);
        slashLine = slashObj.AddComponent<LineRenderer>();
        slashLine.positionCount = 2;
        slashLine.startWidth = 0.3f;
        slashLine.endWidth = 0.3f;
        slashLine.material = lineRend.material;
        Color busNotationColor = new Color(0.65f, 0.65f, 0.65f, 1f);
        slashLine.startColor = busNotationColor;
        slashLine.endColor = busNotationColor;
        slashLine.sortingLayerName = "Wire";
        slashLine.sortingOrder = 10;

        // Create text
        GameObject textObj = new GameObject("BusText");
        textObj.transform.SetParent(busLabel.transform);
        busText = textObj.AddComponent<TextMeshPro>();
        busText.alignment = TextAlignmentOptions.Center;
        busText.fontSize = 30f;
        busText.fontStyle = TMPro.FontStyles.Bold;
        busText.color = busNotationColor;
        busText.sortingOrder = 10;
    }

    /**
     * @brief Computes the derivative of a cubic Bezier curve at parameter t.
     *
     * @param[in] p0 start point
     * @param[in] p1 first control point
     * @param[in] p2 second control point
     * @param[in] p3 end point
     * @param[in] t parameter (0..1)
     * @return tangent vector at t
     **/
    private Vector3 CubicBezierDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        return 3f * u * u * (p1 - p0) + 6f * u * t * (p2 - p1) + 3f * t * t * (p3 - p2);
    }

    /**
     * @brief Evaluates a cubic Bezier curve at parameter t.
     *
     * @param[in] p0 start point
     * @param[in] p1 first control point
     * @param[in] p2 second control point
     * @param[in] p3 end point
     * @param[in] t parameter (0..1)
     * @return point on the curve at t
     **/
    private Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        return u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
    }

    /**
     * @brief Snaps the destination point to the nearest axis-aligned angle relative to origin.
     *
     * @details Used during wire creation when Ctrl is held. Constrains the mouse position
     * to horizontal or vertical alignment with the previous knot/source.
     *
     * @param[in] origin reference point (last knot or source pin)
     * @param[in] dest raw mouse position
     * @return axis-snapped destination
     **/
    private Vector2 fixedAngle(Vector2 origin, Vector2 dest){
        float angle = CalcAngle(origin,dest);

        if(angle <= 25 && angle >= -25){
            dest.y = origin.y;
        }else if(angle <= 115 && angle >= 65){
            dest.x = origin.x;
        }
        else if(angle >= -115 && angle <= -65)
        {
            dest.x = origin.x;
        }
        else if(angle >= 155 || angle <= -155)
        {
            dest.y = origin.y;
        }
        return dest;
    }


    /**
     * @brief Calculates the angle in degrees between two 2D points.
     *
     * @param[in] pos1 first point
     * @param[in] pos2 second point
     * @return angle in degrees (-180..180)
     **/
    private float CalcAngle(Vector2 pos1, Vector2 pos2){
        Vector2 direction = pos2 - pos1;
        float angle = Mathf.Atan2(direction.y,direction.x) * Mathf.Rad2Deg;
        return angle;
    }

    /**
     * @brief Adds a knot at the current mouse position during wire creation.
     *
     * @details The knot is snapped to the grid, instantiated from the knot prefab,
     * and added to both the visual knots list and the Wire data object.
     **/
    private void addKnot()
    {
        Vector3 snappedPos = GridSnap.Snap(new Vector3(lastMousePos.x, lastMousePos.y, 0));
        GameObject o = Instantiate(knotPrefab, snappedPos, Quaternion.identity);
        Knot_Mono knot = o.GetComponent<Knot_Mono>();
        knots.Add(knot);

        Knot pin = new Knot(wire.dataIn);
        knot.Init(pin);

        this.wire.addKnot(pin);
    }

    /**
     * @brief Updates the EdgeCollider2D points to match the current LineRenderer path.
     *
     * @details This enables mouse interaction (hover highlight, click detection) along
     * the wire's rendered curve.
     **/
    private void UpdateEdgeCollider()
    {
        Vector3 linePos = lineRend.transform.position;
        List<Vector2> edges = new List<Vector2>();

        for (int p = 0; p < lineRend.positionCount; p++)
        {

            Vector3 point = lineRend.GetPosition(p);
            edges.Add(new Vector2(point.x, point.y));
        }
        edgeColl.SetPoints(edges);
    }

}

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
 * This class is used for drawing wires
 */
public class Wire_Mono : MonoBehaviour, IObjectMono
{
    private Wire wire;   //Main Wire Object that stores the state of this GameObject

    private Transform sourcePin;
    private Transform sinkPin;
    private List<Knot_Mono> knots;
    private PinConnectionHandler connectionHandler;

    //variables used for initializing wire connection
    private bool draw = false;
    private Transform mouseTransform;

    public GameObject knotPrefab;

    //line renderer and edge collider for drawing the wire
    private UnityEngine.LineRenderer lineRend;
    private EdgeCollider2D edgeColl;
    
    private Vector2 lastMousePos;

    private ProjectManager projectManager;
    private Camera cam;

    // Bus notation
    private GameObject busLabel;
    private TextMeshPro busText;
    private LineRenderer slashLine;

    // Start is called before the first frame update
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

    public void Init(Wire wire)
    {
        //init wire and knots
        this.wire = wire;

        if(wire.dataIn != null) sourcePin = wire.dataIn.transform;
        if(wire.dataOut != null) sinkPin = wire.dataOut.transform;

        foreach(Knot k in wire.knots)
        {
            //load eacht knot
            Knot_Mono m = (Knot_Mono)k.Load();
            knots.Add(m);
        }

        this.wire.DestructEvent += Clear;

        //string jsonString = JsonConvert.SerializeObject(wire, settings);
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

    // Update is called once per frame
    private void Update()
    {
        lineRend.startColor = wire.coloring;
        lineRend.endColor = wire.coloring;


        //=>make that update only happens when knot is moved
        UpdateLineRenderer();

        if (draw) drawWire();
    }


    public void DrawModeOn()
    {
        connectionHandler.searchesConnection = wire.dataIn;
        draw = true;
    }

    private void drawWire()
    {
        if (Input.GetMouseButtonDown(0))
        {
           //right mouse click...
            if (connectionHandler.possibleConnection != null)
            {
                //output pin selected:
                wire.setOutput(connectionHandler.possibleConnection);

                //if ctrl: align connected pin
                if(Input.GetKey(KeyCode.LeftControl)){
                    //calculate direction of movement
                    Vector2 pinPos = wire.dataOut.transform.position;
                    Vector2 direction = lastMousePos - pinPos;

                    //get parent parent object of pin and move
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


   //updates the points of the line renderer
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
                    knotPos = wire.knots.Last().transform.position;
                }
                lastMousePos = fixedAngle(knotPos, lastMousePos);
            }
            controlPoints.Add(new Vector3(lastMousePos.x, lastMousePos.y, 0));
        }
        else if(wire.dataOut != null)
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
        busText.text = width.ToString();

        // Position slash perpendicular to wire direction
        float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
        float slashAngle = angle + 85f; // nearly perpendicular to wire
        float slashSize = 2.5f;

        Vector3 slashDir = new Vector3(Mathf.Cos(slashAngle * Mathf.Deg2Rad), Mathf.Sin(slashAngle * Mathf.Deg2Rad), 0);
        slashLine.SetPosition(0, midpoint - slashDir * slashSize);
        slashLine.SetPosition(1, midpoint + slashDir * slashSize);
        slashLine.startWidth = 1f;
        slashLine.endWidth = 1f;
        slashLine.startColor = wire.coloring;
        slashLine.endColor = wire.coloring;

        // Position text above the slash
        Vector3 perpendicular = new Vector3(-tangent.y, tangent.x, 0);
        float textOffset = 5.5f;
        busText.transform.position = midpoint + perpendicular * textOffset;
        busText.fontSize = 30f;
        busText.fontStyle = TMPro.FontStyles.Bold;
        busText.color = wire.coloring;
    }

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
        busText.color = busNotationColor;
        busText.sortingOrder = 10;
    }

    private Vector3 CubicBezierDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        return 3f * u * u * (p1 - p0) + 6f * u * t * (p2 - p1) + 3f * t * t * (p3 - p2);
    }

    private Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        return u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
    }

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
    

    private float CalcAngle(Vector2 pos1, Vector2 pos2){
        Vector2 direction = pos2 - pos1;
        float angle = Mathf.Atan2(direction.y,direction.x) * Mathf.Rad2Deg;
        return angle;
    }

    private void addKnot()
    {
        //instantiate OutputPin Prefab:
        Vector3 snappedPos = GridSnap.Snap(new Vector3(lastMousePos.x, lastMousePos.y, 0));
        GameObject o = Instantiate(knotPrefab, snappedPos, Quaternion.identity);
        Knot_Mono knot = o.GetComponent<Knot_Mono>();
        knots.Add(knot);

        //init with new Knot object:
        Knot pin = new Knot(wire.dataIn);
        knot.Init(pin);

        //add knot to wire
        this.wire.addKnot(pin);
    }

    //updates the points of the edge collider
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

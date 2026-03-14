using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        lineRend.positionCount = wire.knotCount;
        lineRend.SetPosition(0, sourcePin.position);

        int i = 1;
        foreach(Knot knot in wire.knots)
        {
            lineRend.SetPosition(i, knot.transform.position);
            i = i + 1;
        }


        if(draw)
        {
            lineRend.positionCount = wire.knotCount + 1;
            lastMousePos = cam.ScreenToWorldPoint(Input.mousePosition);
             if(Input.GetKey(KeyCode.LeftControl)){
                //when control is pressed => draw lines with fixed angle
                Vector2 knotPos;
                if(wire.knots.Count == 0){
                    knotPos= wire.dataIn.transform.position;
                }else{
                    knotPos = wire.knots.Last().transform.position;
                }
                
                lastMousePos = fixedAngle(knotPos,lastMousePos);
             }
            lineRend.SetPosition(i, new Vector3(lastMousePos.x, lastMousePos.y, 0));
        }

        if(wire.dataOut!=null)
        {
            lineRend.SetPosition(i, sinkPin.position);
        }
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

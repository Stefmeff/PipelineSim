using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/**
 * @author: Stefan Moser
 *
 * @brief: this Monobhevaviour class is used for loading classes describing components (e.g. a flipflop) 
 * into their respective gameObjects
 *
 * @details: The classes describing the behaviour of a components and the respective gameObject (MonoBehaviour) classes
 * had to be seperated into differnt classes, because Monobehaviour classes cannot be serialized to json files.
 *
 * When loading a components object (e.g. FlipFlop) this monobehaviour class is used to couple the respective gameObject
 * with the object (ILoadable component) that describes its behaviour
 **/
public class ComponentMono : MonoBehaviour, IObjectMono 
{
    public CircuitComponent component;
    [SerializeField] private componentType componentID;
    [SerializeField] private InputPin_Mono[] inPins = new InputPin_Mono[]{};
    [SerializeField] private OutputPin_Mono[] outPins = new OutputPin_Mono[]{};

    private enum componentType { AND, OR, XOR, MullerC, Inverter, Delay, Delay2, CPLatch, FlipFlop, Latch, Clock, DataSource, ChipInputNode, ChipOutputNode, CustomChip, Splitter, Merger};
    private GameObject delayVisualizer;
    private GameObject ErrorMessage;
    private ProjectManager projectManager;

    private GameObject editor;  //opens component editor on left click

    void Awake()
    {
        //add to project:
        GameObject o = GameObject.FindGameObjectWithTag("ProjectManager");
        projectManager = o.GetComponent<ProjectManager>();
        projectManager.addToProject(this);

        this.component = newComponent();
        component.LoadPins(inPins,outPins);

        
        //init SelectCtrl to steer object => Delete, Rotate,...
        editor = GameObject.FindWithTag("Editor");


        try{
            delayVisualizer = gameObject.transform.Find("DelayVisualizer").gameObject;
            component.LoadDelay(delayVisualizer);
        }catch{

        }
    }

    //Initialize component according to identifier
    private CircuitComponent newComponent(){
        switch (componentID)
        {
            case componentType.AND:
                return new AND();
            case componentType.OR:
                return new OR();
            case componentType.XOR:
                return new XOR();
            case componentType.MullerC:
                return new MullerC();
            case componentType.Inverter:
                return new Inverter();
            case componentType.Delay:
                return new Delay();
            case componentType.CPLatch:
                CPLatch cp = new CPLatch();
                ErrorMessage = gameObject.transform.Find("ErrorMessage").gameObject;
                cp.errorMessage = ErrorMessage.GetComponent<TextMeshPro>();
                cp.sRend = GetComponent<SpriteRenderer>();
                return cp;
            case componentType.FlipFlop:
                FlipFlop flop = new FlipFlop();
                ErrorMessage = gameObject.transform.Find("ErrorMessage").gameObject;
                flop.errorMessage = ErrorMessage.GetComponent<TextMeshPro>();
                flop.sRend = GetComponent<SpriteRenderer>();
                return flop;
            case componentType.Latch:
                Latch latch = new Latch();
                ErrorMessage = gameObject.transform.Find("ErrorMessage").gameObject;
                latch.errorMessage = ErrorMessage.GetComponent<TextMeshPro>();
                latch.sRend = GetComponent<SpriteRenderer>();
                return latch;
            case componentType.Clock:
                return new Clock(100,100);
            case componentType.DataSource:
                DataSource d = new DataSource();
                d.LoadEditor();
                return d;
            case componentType.Delay2:
                return new Delay2();
            case componentType.ChipInputNode:
                ChipInputNode cin = new ChipInputNode();
                cin.LoadEditor();
                return cin;
            case componentType.ChipOutputNode:
                ChipOutputNode cout = new ChipOutputNode();
                cout.LoadEditor();
                return cout;
            case componentType.CustomChip:
                CustomChip chip = new CustomChip();
                chip.LoadEditor();
                chip.BuildVisual(this.gameObject);
                chip.InitInternalCircuit();
                return chip;
            case componentType.Splitter:
                Splitter splitter = new Splitter();
                splitter.LoadEditor();
                splitter.BuildVisual(this.gameObject);
                return splitter;
            case componentType.Merger:
                Merger merger = new Merger();
                merger.LoadEditor();
                merger.BuildVisual(this.gameObject);
                return merger;

        }
        return null;
    }

    public void Load(CircuitComponent component)
    {
        if(this.component != null)this.component.Dispose();
        this.component = component;
        component.LoadPins(inPins,outPins);
        component.LoadDelay(delayVisualizer);

        //Component specific parameters:
        switch(component){
            case FlipFlop:
                FlipFlop flop = (FlipFlop)component;
                flop.sRend = GetComponent<SpriteRenderer>();
                flop.errorMessage = ErrorMessage.GetComponent<TextMeshPro>();
                break;
            case Latch:
                Latch latch = (Latch)component;
                latch.errorMessage = ErrorMessage.GetComponent<TextMeshPro>();
                latch.sRend = GetComponent<SpriteRenderer>();
                break;
            case CPLatch:
                CPLatch cp = (CPLatch)component;
                cp.sRend = GetComponent<SpriteRenderer>();
                cp.errorMessage = ErrorMessage.GetComponent<TextMeshPro>();
                break;
            case DataSource:
                DataSource d = (DataSource)component;
                d.LoadEditor();
                break;
            case ChipInputNode:
                ((ChipInputNode)component).LoadEditor();
                break;
            case ChipOutputNode:
                ((ChipOutputNode)component).LoadEditor();
                break;
            case CustomChip:
                ((CustomChip)component).LoadEditor();
                ((CustomChip)component).BuildVisual(this.gameObject);
                ((CustomChip)component).InitInternalCircuit();
                break;
            case Splitter:
                ((Splitter)component).LoadEditor();
                ((Splitter)component).BuildVisual(this.gameObject);
                break;
            case Merger:
                ((Merger)component).LoadEditor();
                ((Merger)component).BuildVisual(this.gameObject);
                break;
        }
    }

    public CircuitComponent GetMain()
    {
        return this.component;
    }

    public void SaveTransform()
    {
        this.component.SaveTransform(this.transform);
    }

    public void Clear()
    {
        Destroy(this.gameObject);
    }
    private void OnDestroy()
    {
        projectManager.removeFromProject(this);
        component.Dispose();
        component = null;
    }

    public void OnMouseOver()
    {
        //if clicked => open editor of this item
        if (Input.GetMouseButtonDown(1))
        {
            for( int i = 0; i < editor.transform.childCount; ++i )
            {
                editor.transform.GetChild(i).gameObject.SetActive(false);
            }
            component.OpenEditor();

            // Only disable drag/zoom if an editor actually opened
            for( int i = 0; i < editor.transform.childCount; ++i )
            {
                if (editor.transform.GetChild(i).gameObject.activeSelf)
                {
                    projectManager.dragActive = false;
                    projectManager.zoomActive = false;
                    break;
                }
            }
        }
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * This class is used for opening the editors of components to parse the input values between the components
 * and its editor.
 */
public class Editor : MonoBehaviour
{
    public bool active;
    //reference to the different editor types
    private GameObject clockEditor;
    private GameObject delayEditor;
    private GameObject delay2Editor;
    private GameObject flopEditor;
    private GameObject latchEditor;
    private GameObject cplEditor;

    private GameObject dataSourceEditor;
    private ButtonPause buttonPause;

    void Start()
    {
        //get reference to simulation timer
        GameObject o = GameObject.FindWithTag("ButtonPause");
        buttonPause = o.GetComponent<ButtonPause>();

        clockEditor = gameObject.transform.GetChild(0).gameObject;
        delayEditor = gameObject.transform.GetChild(1).gameObject;
        delay2Editor = gameObject.transform.GetChild(2).gameObject;
        flopEditor = gameObject.transform.GetChild(3).gameObject;
        latchEditor = gameObject.transform.GetChild(4).gameObject;
        cplEditor = gameObject.transform.GetChild(5).gameObject;
        //dataSourceEditor = gameObject.transform.GetChild(6).gameObject;
        closeEditors();
    }

    private void closeEditors()
    {
        //close all existing editors
        clockEditor.SetActive(false);
        delayEditor.SetActive(false);
        delay2Editor.SetActive(false);
        flopEditor.SetActive(false);
        latchEditor.SetActive(false);
        cplEditor.SetActive(false);
        //dataSourceEditor.SetActive(false);
    }


    public void openEditor(Loadable component)
    {
        //TODO: access copmonent of Monobehaviour => then type 

        //close any open editors
        closeEditors();

        DelayEditor e;

        //stop simulation while editing components
        //buttonPause.pause();

        //open the editor of the imput component

        switch(component){
            case AND:
                delayEditor.SetActive(true);
                e = delayEditor.GetComponent<DelayEditor>();
                e.init((AND)component);
                e.SetTitle("AND-Gate");
                e.SetDescription("The AND gate output 1 if and only if all the inputs are 1, otherwise it outputs 0.");
                active = true;
                break;
            case OR:
                delayEditor.SetActive(true);
                e = delayEditor.GetComponent<DelayEditor>();
                e.init((OR)component);
                e.SetTitle("OR-Gate");
                e.SetDescription("The OR gate outputs 1 if any of its inputs is 1, otherwise it outputs 0.");
                active = true;
                break;
            case XOR:
                delayEditor.SetActive(true);
                e = delayEditor.GetComponent<DelayEditor>();
                e.init((XOR)component);
                e.SetTitle("XOR-Gate");
                e.SetDescription("The XOR gate outputs 1 if one, and only one, of the inputs is 1, otherwise it outputs 0.");
                active = true;
                break;
            case MullerC:
                delayEditor.SetActive(true);
                e = delayEditor.GetComponent<DelayEditor>();
                e.init((MullerC)component);
                e.SetTitle("Muller C-Element");
                e.SetDescription("The C-Elemet outputs 1, when all inputs are 1 and 0, when all inputs are 0.\n\nFor all other input combinations, the C-gate holds it’s current state.");
                active = true;
                break;
            case Inverter:
                delayEditor.SetActive(true);
                e = delayEditor.GetComponent<DelayEditor>();
                e.init((Inverter)component);
                e.SetTitle("Inverter");
                e.SetDescription("The inverter outputs a 0 when given a 1, and a 1 when given a 0");
                active = true;
                break;
            case Delay:
                delayEditor.SetActive(true);
                e = delayEditor.GetComponent<DelayEditor>();
                e.init((Delay)component);
                e.SetTitle("Propagation Delay");
                e.SetDescription("The propagation delay delays incoming signals by the given time.");
                active = true;
                break;
            case CPLatch:
                cplEditor.SetActive(true);
                cplEditor.GetComponent<CPLatchEditor>().init((CPLatch)component);
                active = true;
                break;
            case FlipFlop:
                flopEditor.SetActive(true);
                flopEditor.GetComponent<FlopEditor>().init((FlipFlop)component);
                active = true;
                break;
            case Latch:
                latchEditor.SetActive(true);
                latchEditor.GetComponent<LatchEditor>().init((Latch)component);
                active = true;
                break;
            case Clock:
                clockEditor.SetActive(true);
                clockEditor.GetComponent<ClockEditor>().init((Clock)component);
                break;
            case Delay2:
                delay2Editor.SetActive(true);
                delay2Editor.GetComponent<Delay2Editor>().init((Delay2)component);
                active = true;
                break;
            case DataSource:
                dataSourceEditor.SetActive(true);
                dataSourceEditor.GetComponent<DataSourceEditor>().init((DataSource)component);
                break;
        }

    }
}

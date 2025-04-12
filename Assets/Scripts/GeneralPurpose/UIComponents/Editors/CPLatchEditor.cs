using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CPLatchEditor : MonoBehaviour
{
    private CPLatch l;
    private ButtonCPDelay delayVisualizeButton;
    private TMP_InputField setup;
    private TMP_InputField hold;
    private TMP_InputField delay;

    void Awake()
    {
        //get references to editors input fields:
        GameObject o = gameObject.transform.Find("WindowArea/Content/ParameterSetup/Input").gameObject;
        setup = o.GetComponent<TMP_InputField>();
        o = gameObject.transform.Find("WindowArea/Content/ParameterHold/Input").gameObject;
        hold = o.GetComponent<TMP_InputField>();

        o = gameObject.transform.Find("WindowArea/Content/ParameterDelay/Input").gameObject;
        delay = o.GetComponent<TMP_InputField>();

        //get references to delay button:
        o = gameObject.transform.Find("WindowArea/Content/ParameterDelay/Button").gameObject;
        delayVisualizeButton = o.GetComponent<ButtonCPDelay>();


        //add input event listener:
        setup.onEndEdit.AddListener(delegate { SetupInput(setup); });
        hold.onEndEdit.AddListener(delegate { HoldInput(hold); });
        delay.onEndEdit.AddListener(delegate { DelayInput(delay); });
    }

    public void init(CPLatch l)
    {
        //initiliaze clock component
        this.l = l;
        fillParameters();
    }

    private void fillParameters()
    {
        //fill the parameters of the editor with the current clock values
        setup.text = l.parseSetup("") + "";
        hold.text = l.parseHold("") + "";
        delay.text = l.parseDelay("") + "";
        delayVisualizeButton.showDelay = l.IsVisualizerActive();
        delayVisualizeButton.UpdateButton();
    }

    public void VisualizeDelay(bool on)
    {
        l.VisualizeDelay(on);
    }

    private void SetupInput(TMP_InputField input)
    {
        setup.text = l.parseSetup(input.text) + "";
    }

    private void HoldInput(TMP_InputField input)
    {
        hold.text = l.parseHold(input.text) + "";
    }

    private void DelayInput(TMP_InputField input)
    {
        delay.text = l.parseDelay(input.text) + "";
    }
}


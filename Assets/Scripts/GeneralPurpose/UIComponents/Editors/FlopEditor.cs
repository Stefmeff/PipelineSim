using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FlopEditor : MonoBehaviour
{
    private FlipFlop flop;    //edited clock
    private TMP_InputField setup;
    private TMP_InputField hold;
    private TMP_InputField delay;
    private ButtonC2QDelay delayVisualizeButton;

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
        delayVisualizeButton = o.GetComponent<ButtonC2QDelay>();


        //add input event listener:
        setup.onEndEdit.AddListener(delegate { SetupInput(setup); });
        hold.onEndEdit.AddListener(delegate { HoldInput(hold); });
        delay.onEndEdit.AddListener(delegate { DelayInput(delay); });
    }

    public void init(FlipFlop flop)
    {
        //initiliaze clock component
        this.flop = flop;
        fillParameters();
    }

    private void fillParameters()
    {
        //fill the parameters of the editor with the current clock values
        setup.text = flop.parseSetup("") + "";
        hold.text = flop.parseHold("") + "";
        delay.text = flop.parseDelay("") + "";
        delayVisualizeButton.showDelay = flop.IsVisualizerActive();
        delayVisualizeButton.UpdateButton();
    }

    public void VisualizeDelay(bool on)
    {
        flop.VisualizeDelay(on);
    }

    private void SetupInput(TMP_InputField input)
    {
        setup.text = flop.parseSetup(input.text) + "";
    }

    private void HoldInput(TMP_InputField input)
    {
        hold.text = flop.parseHold(input.text) + "";
    }

    private void DelayInput(TMP_InputField input)
    {
        delay.text = flop.parseDelay(input.text) + "";
    }
}

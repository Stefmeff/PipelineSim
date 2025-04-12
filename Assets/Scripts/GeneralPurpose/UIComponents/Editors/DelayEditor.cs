using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/**
 * Editor for delay components. Is used to parse the user inputs, and adjust the timing parameters 
 * of the selected delay component.
 */
public class DelayEditor : MonoBehaviour
{
    private IDelay delay;    //edited clock

    private TMP_InputField delayInputField;

    private ButtonDelay delayVisualizeButton;
   
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI descriptionText;

    void Awake()
    {
        //get references to editors input fields:
        GameObject o = gameObject.transform.Find("WindowArea/Content/ParameterDelay/Input").gameObject;
        delayInputField = o.GetComponent<TMP_InputField>();

        //get references to delay button:
        o = gameObject.transform.Find("WindowArea/Content/ParameterDelay/Button").gameObject;
        delayVisualizeButton = o.GetComponent<ButtonDelay>();

        //add input event listener:
        delayInputField.onEndEdit.AddListener(delegate { LockInput(delayInputField); });

        GameObject title = gameObject.transform.Find("Title").gameObject;
        titleText = title.GetComponent<TextMeshProUGUI>();

        GameObject description = gameObject.transform.Find("Description").gameObject.transform.GetChild(0).gameObject;
        descriptionText = description.GetComponent<TextMeshProUGUI>();
    }

    public void init(IDelay delay)
    {
        this.delay = delay;
        fillParameters();
    }

    private void fillParameters()
    {
        //fill the parameters of the editor with the current delay values
        delayInputField.text = delay.parseDelay("") + "";
        delayVisualizeButton.showDelay = delay.IsVisualizerActive();
        delayVisualizeButton.UpdateButton();
    }

    public void SetTitle(string title){
        titleText.text = title;
    }

    public void SetDescription(string description){
        descriptionText.text = description;
    }

    public void VisualizeDelay(bool on)
    {
        delay.VisualizeDelay(on);
    }

    void LockInput(TMP_InputField input)
    {
        //empty => keep old parameters
        delayInputField.text = delay.parseDelay(input.text) + "";
    }
}


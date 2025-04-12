using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/**
 * Editor for delay components. Is used to parse the user inputs, and adjust the timing parameters 
 * of the selected delay component.
 */
public class Delay2Editor : MonoBehaviour
{
    private Delay2 delay;    //edited clock

    private TMP_InputField delayInputField;

    void Awake()
    {
        //get references to editors input fields:
        GameObject o = gameObject.transform.Find("WindowArea/Content/ParameterDelay/Input").gameObject;
        delayInputField = o.GetComponent<TMP_InputField>();

        //add input event listener:
        delayInputField.onEndEdit.AddListener(delegate { LockInput(delayInputField); });
    }

    public void init(Delay2 delayComponent)
    {
        this.delay = delayComponent;
        fillParameters();
    }

    private void fillParameters()
    {
        //fill the parameters of the editor with the current delay values
        delayInputField.text = delay.delay + "";
    }

    void LockInput(TMP_InputField input)
    {
        if (input.text.Length > 0)
        {
            int value = int.Parse(input.text);

            //not empty => set clock object to input
            delay.delay = value;
            return;
        }
        //empty => keep old parameters
        delayInputField.text = delay.delay + "";
    }
}


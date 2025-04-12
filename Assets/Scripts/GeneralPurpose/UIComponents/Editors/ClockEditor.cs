using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI; 

/**
 * Editor for clock components. Is used to parse the user inputs, and adjust the timing parameters 
 * of the selected clock component.
 */
public class ClockEditor : MonoBehaviour
{
    private Clock clock;    //edited clock

    private TMP_InputField high;
    private TMP_InputField low;

    void Awake()
    {
        //get references to editors input fields:
        GameObject o = gameObject.transform.Find("WindowArea/Content/ParameterHigh/Input").gameObject;
        high = o.GetComponent<TMP_InputField>();

        o =  gameObject.transform.Find("WindowArea/Content/ParameterLow/Input").gameObject;
        low = o.GetComponent<TMP_InputField>();

        //add input event listener:
        high.onEndEdit.AddListener(delegate { HighInput(high); });
        low.onEndEdit.AddListener(delegate { LowInput(low); });
    }

    public void init(Clock clock)
    {
        //initiliaze clock component
        this.clock = clock;
        fillParameters();
    }

    private void fillParameters()
    {
        //fill the parameters of the editor with the current clock values
        high.text = clock.high + "";
        low.text = clock.low + "";
    }

    void HighInput(TMP_InputField input)
    {
        if (input.text.Length > 0)
        {
            int value = int.Parse(input.text);
            if (value > 0)
            {
                //not empty => set clock object to input
                clock.high = value;
                return;
            }
        }
        //empty => keep old parameters
        high.text = clock.high + "";
    }

    void LowInput(TMP_InputField input)
    {
        if (input.text.Length > 0)
        {
            int value = int.Parse(input.text);
            if (value > 0)
            {
                //not empty => set clock object to input
                clock.low = value;
                return;
            }
            
        }
        //empty => keep old parameters
        low.text = clock.low + "";
    }
}

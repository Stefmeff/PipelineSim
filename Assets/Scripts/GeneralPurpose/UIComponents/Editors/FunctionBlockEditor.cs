using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/**
 * Editor for custom chip (function block) components. Displays the propagation delay
 * and provides an Edit button to open the ChipEditor for this chip.
 */
public class FunctionBlockEditor : MonoBehaviour
{
    private CustomChip chip;

    private TMP_InputField maxDelayField;
    private TMP_InputField minDelayField;
    private GameObject editButton;

    void Awake()
    {
        //get references to editors input fields:
        GameObject o = gameObject.transform.Find("WindowArea/Content/MaxDelay/Input").gameObject;
        maxDelayField = o.GetComponent<TMP_InputField>();
        maxDelayField.interactable = false;

        o = gameObject.transform.Find("WindowArea/Content/MinDelay/Input").gameObject;
        minDelayField = o.GetComponent<TMP_InputField>();
        minDelayField.interactable = false;

        //get reference to edit button:
        editButton = gameObject.transform.Find("Documentation/ButtonEdit").gameObject;
        EditButton eb = editButton.GetComponent<EditButton>();
        if (eb == null) eb = editButton.AddComponent<EditButton>();
        eb.editor = this;
    }

    public void init(CustomChip customChip)
    {
        this.chip = customChip;
        fillParameters();
    }

    private void fillParameters()
    {
        //fill the parameters of the editor with the current delay values
        maxDelayField.text = chip.GetDelay() + "";
        minDelayField.text = chip.GetMinDelay() + "";
    }

    public void OnEditClicked()
    {
        if (chip == null || string.IsNullOrEmpty(chip.chipName)) return;
        SceneTransition.OpenChipEditor(chip.chipName);
    }
}

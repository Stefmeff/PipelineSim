using TMPro;
using UnityEngine;

/// <summary>
/// Editor for ChipInputNode/ChipOutputNode interface pins.
/// Allows editing pin name and bit width.
/// </summary>
public class ChipPinEditor : MonoBehaviour
{
    private TMP_InputField pinNameField;
    private TMP_InputField bitWidthField;

    private ChipInputNode inputNode;
    private ChipOutputNode outputNode;

    private void EnsureFields()
    {
        if (pinNameField != null) return;

        GameObject o = gameObject.transform.Find("WindowArea/Content/PinName/Input").gameObject;
        pinNameField = o.GetComponent<TMP_InputField>();

        o = gameObject.transform.Find("WindowArea/Content/BitWidth/Input").gameObject;
        bitWidthField = o.GetComponent<TMP_InputField>();

        pinNameField.onEndEdit.AddListener(delegate { OnPinNameChanged(pinNameField); });
        bitWidthField.onEndEdit.AddListener(delegate { OnBitWidthChanged(bitWidthField); });
    }

    public void Init(ChipInputNode node)
    {
        EnsureFields();
        inputNode = node;
        outputNode = null;
        pinNameField.text = node.pinName;
        bitWidthField.text = node.pinWidth.ToString();
    }

    public void Init(ChipOutputNode node)
    {
        EnsureFields();
        outputNode = node;
        inputNode = null;
        pinNameField.text = node.pinName;
        bitWidthField.text = node.pinWidth.ToString();
    }

    private void OnPinNameChanged(TMP_InputField input)
    {
        if (string.IsNullOrEmpty(input.text)) return;

        if (inputNode != null) inputNode.SetPinName(input.text);
        if (outputNode != null) outputNode.SetPinName(input.text);
    }

    private void OnBitWidthChanged(TMP_InputField input)
    {
        if (int.TryParse(input.text, out int width) && width >= 1 && width <= 16)
        {
            if (inputNode != null) inputNode.SetPinWidth(width);
            if (outputNode != null) outputNode.SetPinWidth(width);
        }
        else
        {
            // Revert to current value
            if (inputNode != null) bitWidthField.text = inputNode.pinWidth.ToString();
            if (outputNode != null) bitWidthField.text = outputNode.pinWidth.ToString();
        }
    }
}

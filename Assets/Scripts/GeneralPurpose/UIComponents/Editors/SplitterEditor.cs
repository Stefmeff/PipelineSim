using TMPro;
using UnityEngine;

/// <summary>
/// Editor for Splitter and Merger bit width configuration.
/// Shared by both components.
/// </summary>
public class SplitterEditor : MonoBehaviour
{
    private TMP_InputField bitWidthField;
    private TextMeshProUGUI descriptionText;
    private Splitter splitter;
    private Merger merger;

    private void EnsureFields()
    {
        if (bitWidthField != null) return;

        GameObject o = gameObject.transform.Find("WindowArea/Content/BitWidth/Input").gameObject;
        bitWidthField = o.GetComponent<TMP_InputField>();
        bitWidthField.onEndEdit.AddListener(delegate { OnBitWidthChanged(bitWidthField); });

        Transform descT = gameObject.transform.Find("Documentation/Text");
        if (descT != null) descriptionText = descT.GetComponent<TextMeshProUGUI>();
    }

    public void Init(Splitter s)
    {
        EnsureFields();
        splitter = s;
        merger = null;
        bitWidthField.text = s.nBits.ToString();
        if (descriptionText != null)
            descriptionText.text = "The splitter allows you to split an n-bit bus wire into its individual bits.";
    }

    public void Init(Merger m)
    {
        EnsureFields();
        merger = m;
        splitter = null;
        bitWidthField.text = m.nBits.ToString();
        if (descriptionText != null)
            descriptionText.text = "The merger combines multiple individual bit wires into a single n-bit bus wire.";
    }

    private void OnBitWidthChanged(TMP_InputField input)
    {
        if (splitter != null)
        {
            int result = splitter.ParseBitSize(input.text);
            input.text = result.ToString();
        }
        else if (merger != null)
        {
            int result = merger.ParseBitSize(input.text);
            input.text = result.ToString();
        }
    }
}

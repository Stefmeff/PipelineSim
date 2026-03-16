using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DataSourceEditor : MonoBehaviour
{
    private DataSource dataSource;
    private TMP_InputField inputField;
    private int nBits;
    private GameObject scrollContent;
    private List<TokenInputField> tokenFields;
    public int tokenCount = 0;


    void Awake()
    {
        //get references to editors input fields:
        GameObject o = gameObject.transform.Find("WindowArea/Content/ParameterNBits/Input").gameObject;
        inputField = o.GetComponent<TMP_InputField>();

        //content of scroll window:
        scrollContent = gameObject.transform.Find("WindowArea/Menu/ScrollWindow/Content").gameObject;
        //add input event listener:
        inputField.onEndEdit.AddListener(delegate { ParseInput(inputField); });

        tokenFields = new List<TokenInputField>();
    }

    public void init(DataSource dataSource)
    {
        this.dataSource = dataSource;

        // Clear existing UI fields
        foreach (TokenInputField tf in tokenFields)
        {
            if (tf != null) GameObject.Destroy(tf.gameObject);
        }
        tokenFields.Clear();

        // Read token data from DataSource and create UI fields
        List<string> binaries = dataSource.GetTokenBinaries();
        for (int i = 0; i < binaries.Count; i++)
        {
            int colorIndex = i % DataSource.colorScheme.Count;
            AddTokenField(binaries[i], DataSource.colorScheme[colorIndex], i);
        }
        fillParameters();
    }

    private void fillParameters()
    {
        //fill the parameters of the editor with the current clock values
        nBits = dataSource.parseBitSize("");
        UpdateTokenSizes();
        inputField.text = nBits + "";

    }

    private void ParseInput(TMP_InputField input)
    {
        nBits = dataSource.parseBitSize(input.text);
        UpdateTokenSizes();
        input.text = nBits + "";
    }

    public int GetNBits()
    {
        return nBits;
    }

    private void AddTokenField(string binary, Color color, int index)
    {
        //instantiate prefab
        GameObject prefab = Resources.Load("Prefabs/TokenInputField") as GameObject;
        GameObject tokenInputFieldObj = Instantiate(prefab) as GameObject;
        TokenInputField f = tokenInputFieldObj.GetComponent<TokenInputField>();

        //Init BitToken inputfield with stored data
        f.Init(nBits, color, binary, dataSource, index);
        tokenFields.Add(f);

        //update content size fitter for newly added prefab:
        Transform t = tokenInputFieldObj.transform;
        t.SetParent(scrollContent.transform, false);
        LayoutRebuilder.ForceRebuildLayoutImmediate(t as RectTransform);
        UpdateContentSizeFitter();
    }

    public void AddToken(){
        dataSource.AddToken();
        int index = tokenFields.Count;
        int colorIndex = index % DataSource.colorScheme.Count;
        AddTokenField(new string('1', nBits), DataSource.colorScheme[colorIndex], index);
    }

    public void RemoveToken(){
        if (tokenFields.Count == 0) return;
        int lastIndex = tokenFields.Count - 1;
        TokenInputField last = tokenFields[lastIndex];
        tokenFields.RemoveAt(lastIndex);
        GameObject.Destroy(last.gameObject);
        dataSource.RemoveToken();
    }

    public Token GetNextToken(){
        return dataSource.GetNextToken();
    }

    private void UpdateTokenSizes(){
        foreach(TokenInputField tF in tokenFields){
            tF.UpdateInputSize(nBits);
        }
    }

    private void UpdateContentSizeFitter(){
        RectTransform t = scrollContent.transform as RectTransform;
        LayoutRebuilder.ForceRebuildLayoutImmediate(t);

    }
}

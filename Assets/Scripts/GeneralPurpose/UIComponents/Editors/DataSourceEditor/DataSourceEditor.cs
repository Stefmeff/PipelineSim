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
    private List<Color> colorScheme;  
    private List<TokenInputField> tokenFields;
    public int tokenCount = 0;
    private int colorIndex = 0;


    void Awake()
    {
        //get references to editors input fields:
        GameObject o = gameObject.transform.Find("WindowArea/Content/ParameterNBits/Input").gameObject;
        inputField = o.GetComponent<TMP_InputField>();

        //content of scroll window:
        scrollContent = gameObject.transform.Find("WindowArea/Menu/ScrollWindow/Content").gameObject;
        //add input event listener:
        inputField.onEndEdit.AddListener(delegate { ParseInput(inputField); });

        
        colorScheme = new List<Color>();
        colorScheme.Add(setTransp(Color.green));
        colorScheme.Add(setTransp(Color.blue));
        colorScheme.Add(setTransp(Color.yellow));
        colorScheme.Add(setTransp(Color.cyan));
        colorScheme.Add(setTransp(Color.magenta));
        colorScheme.Add(new Color32(0xff,0x88,0x00,0xFF));

        tokenFields = new List<TokenInputField>();
    }

    public void init(DataSource dataSource)
    {
        this.dataSource = dataSource;
        fillParameters();
        foreach(Color c in colorScheme){
            AddToken();
        }
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

    public void AddToken(){
        //instantiate prefab
        GameObject prefab = Resources.Load("Prefabs/TokenInputField") as GameObject;
        GameObject TokenInputField = Instantiate(prefab) as GameObject;
        TokenInputField f = TokenInputField.GetComponent<TokenInputField>();

        //Init BitToken inputfield
        if (colorIndex >= colorScheme.Count) colorIndex = 0;
        f.Init(nBits, colorScheme[colorIndex]);
        colorIndex++;
        tokenFields.Add(f);

        //update content size fitter for newly added prefab:
        Transform t = TokenInputField.transform;
        t.SetParent(scrollContent.transform,false);
        LayoutRebuilder.ForceRebuildLayoutImmediate(t as RectTransform);
        UpdateContentSizeFitter();    
    }

    public void RemoveToken(){
        int lastIndex = tokenFields.Count - 1;
        TokenInputField last = tokenFields[lastIndex];
        tokenFields.RemoveAt(lastIndex);
        GameObject.Destroy(last.gameObject);

        colorIndex--;
        if(colorIndex < 0)colorIndex = colorScheme.Count-1;
    }

    public TokenInputField GetNextToken(){
        if(tokenFields.Count == 0)return null;
        if(tokenCount >= tokenFields.Count)tokenCount=0;
        TokenInputField token = tokenFields[tokenCount];
        tokenCount++;
        return token;
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

    private Color32 setTransp(Color c){
        //Change the transparency of the token colours
        Color32 b = new Color(c.r,c.g,c.b);
        b.a = 0xD4;
        return b;
    }

}

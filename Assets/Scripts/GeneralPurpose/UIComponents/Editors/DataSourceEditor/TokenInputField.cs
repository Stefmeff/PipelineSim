using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.InputSystem.Controls;

public class TokenInputField : MonoBehaviour
{
    private TMP_InputField tokenField;
    private String lastValid;

    private Token token;
    private Image icon;
    private Color c;
    private int nBits;

    private DataSource dataSource;
    private int tokenIndex;

    void Awake()
    {
        GameObject o = gameObject.transform.Find("Rect/Input").gameObject;
        tokenField = o.GetComponent<TMP_InputField>();

        o = gameObject.transform.Find("Rect/Icon/Color").gameObject;
        icon = o.GetComponent<Image>();

        tokenField.onEndEdit.AddListener(delegate { ParseInput(tokenField); });
    }

    public void Init(int nBits, Color c, string binary, DataSource source, int index){
        //Set token color
        this.c = c;
        icon.color = c;

        //set token bitsize
        this.nBits = nBits;
        this.dataSource = source;
        this.tokenIndex = index;
        tokenField.characterLimit = nBits;
        lastValid = binary;
        tokenField.text = lastValid;
        token = new Token(lastValid, this.c);
    }

    // Backwards compat overload
    public void Init(int nBits, Color c){
        Init(nBits, c, new string('1', nBits), null, -1);
    }

    public void UpdateInputSize(int nBits){
        if(this.nBits != nBits){
            this.nBits = nBits;
            tokenField.characterLimit = nBits;
            lastValid = new string('1',nBits);
            token = new Token(lastValid, this.c);
            tokenField.text = lastValid;

            // Write back to DataSource
            if (dataSource != null && tokenIndex >= 0)
                dataSource.SetTokenBinary(tokenIndex, lastValid);
        }
    }

    private void ParseInput(TMP_InputField input){
        if(input.text.Length > 0)
        {
            string pattern = "^[01]{"+nBits+"}$";
            Regex binary = new Regex(pattern);

            if(binary.IsMatch(input.text)){
                lastValid = input.text;
                token = new Token(lastValid, this.c);

                // Write back to DataSource
                if (dataSource != null && tokenIndex >= 0)
                    dataSource.SetTokenBinary(tokenIndex, lastValid);
            }
        }

        tokenField.text = lastValid;
    }



    public Token GetToken(){
        return token;
    }
}

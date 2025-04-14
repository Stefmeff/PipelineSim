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
    void Awake()
    {
        GameObject o = gameObject.transform.Find("Rect/Input").gameObject;
        tokenField = o.GetComponent<TMP_InputField>();

        o = gameObject.transform.Find("Rect/Icon/Color").gameObject;
        icon = o.GetComponent<Image>();

        tokenField.onEndEdit.AddListener(delegate { ParseInput(tokenField); });
    }

    public void Init(int nBits, Color c){
        //Set token color
        this.c = c;
        icon.color = c;

        //set token bitsize
        this.nBits = nBits;
        tokenField.characterLimit = nBits;
        lastValid = new string('1',nBits);
        tokenField.text = lastValid;
        token = new Token(lastValid, this.c);
    }
    
    public void UpdateInputSize(int nBits){
        if(this.nBits != nBits){
            this.nBits = nBits;
            tokenField.characterLimit = nBits;
            lastValid = new string('1',nBits);
            token = new Token(lastValid, this.c);
            tokenField.text = lastValid;
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
            }
        }

        tokenField.text = lastValid;
    }



    public Token GetToken(){
        return token;
    }
}

using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class BitToken
{
    private int time;       //the current timestamp of this BitToken
    private bool value;
    private Color color;    //the color this BitToken is represented with
    private GameObject informationWindow;

    [JsonIgnore] public readonly Color low = new Color32(0xE0, 0xE0, 0xE0, 0xFF);

    public BitToken()
    {
        this.time = 0;
        this.value = false;
        this.color = low;
    }

    public BitToken(bool value, int time)
    {
        this.value = value;
        this.time = time;
        this.color = Color.red;

        
    }

    public BitToken(bool value, int time, Color color)
    {
        this.time = time;
        this.value = value;
        this.color = color;
        GameObject o = GameObject.FindWithTag("DialogBoxes");
        informationWindow = o.transform.GetChild(1).gameObject;
    }

    public int GetTime()
    {
        return this.time;
    }

    public Color TokenColor(){
        return color;
    }

    public Color ActiveColor()
    {
        if(value){
            return color;
        }else{
            return low;
        }
    }

    public bool GetValue(){

        //Warning: value of generic token is accessed!
        //informationWindow.SetActive(true);

        return this.value;
    }
    
    public BitToken NewToken(int newTime)
    {
        return new BitToken(value, newTime, color);
    }

    public bool EqualsToken(BitToken t)
    {
        if(this.color == t.color){
            if(this.value == t.value){
                return true;
            }
        }
        return false;
    }


}

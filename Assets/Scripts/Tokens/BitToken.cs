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
    [JsonIgnore] private Color red = new Color32(0xc4,0x0c,0x0c,0xff);
    [JsonIgnore] public readonly Color low = new Color32(0xE0, 0xE0, 0xE0, 0xFF);

    public BitToken()
    {
        this.time = 0;
        this.value = false;
        this.color = red;
    }

    public BitToken(bool value, int time)
    {
        this.value = value;
        this.time = time;
        this.color = red; 
    }

    public BitToken(bool value, int time, Color color)
    {
        this.time = time;
        this.value = value;
        this.color = color;
    }

    public int GetTime()
    {
        return this.time;
    }

    public Color TokenColor(){
        //returns the color that represents this token
        return color;
    }

    public Color ActiveColor()
    {
        //returns the color depending on the tokens value
        if(value){
            return color;
        }else{
            return low;
        }
    }

    public bool GetValue(){
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

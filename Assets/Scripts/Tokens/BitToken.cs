using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BitToken
{
    private int time;           //the current timestamp of this BitToken
    private bool value;         //single-bit value (backwards compat, always mirrors values[0])
    private bool[] values;      //n-bit values
    private int width;          //number of bits (1 = single-bit, default)
    private Color color;        //the color this BitToken is represented with
    [JsonIgnore] private Color red = new Color32(0xc4,0x0c,0x0c,0xff);
    [JsonIgnore] public readonly Color low = new Color32(0xE0, 0xE0, 0xE0, 0xFF);

    public BitToken()
    {
        this.time = 0;
        this.width = 1;
        this.value = false;
        this.values = new bool[] { false };
        this.color = red;
    }

    public BitToken(bool value, int time)
    {
        this.value = value;
        this.time = time;
        this.width = 1;
        this.values = new bool[] { value };
        this.color = red;
    }

    public BitToken(bool value, int time, Color color)
    {
        this.time = time;
        this.value = value;
        this.width = 1;
        this.values = new bool[] { value };
        this.color = color;
    }

    public BitToken(bool[] values, int time, Color color)
    {
        this.time = time;
        this.width = values.Length;
        this.values = (bool[])values.Clone();
        this.value = values[0];
        this.color = color;
    }

    public int GetTime()
    {
        return this.time;
    }

    public int GetWidth()
    {
        return this.width;
    }

    public Color TokenColor(){
        return color;
    }

    public Color ActiveColor()
    {
        if(width == 1){
            // Single-bit: original behavior
            if(value){
                return color;
            }else{
                return low;
            }
        }else{
            // N-bit bus: color if any bit is high, dimmed if all zero, grey if uninitialized
            if(values.Any(v => v)){
                return color;
            }else{
                // All zeros — dimmed token color
                return Color.Lerp(low, color, 0.35f);
            }
        }
    }

    public bool GetValue(){
        return this.value;
    }

    public bool[] GetValues(){
        return this.values;
    }

    public bool GetBit(int index){
        if(index >= 0 && index < width)
            return values[index];
        return false;
    }

    public BitToken NewToken(int newTime)
    {
        if(width == 1){
            return new BitToken(value, newTime, color);
        }else{
            return new BitToken(values, newTime, color);
        }
    }

    public bool EqualsToken(BitToken t)
    {
        if(this.color != t.color) return false;
        if(this.width != t.width) return false;
        if(this.width == 1){
            return this.value == t.value;
        }else{
            for(int i = 0; i < width; i++){
                if(this.values[i] != t.values[i]) return false;
            }
            return true;
        }
    }
}

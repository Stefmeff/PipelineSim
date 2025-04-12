using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Pin;

public class LUT
{
    [JsonProperty] private List<InputPin> inputPins;
    [JsonProperty] private OutputPin outputPin;
    [JsonProperty] private List<bool> truthtable;

    public LUT(){
        this.inputPins = new List<InputPin>();
        this.outputPin = new OutputPin();
        this.truthtable = new List<bool>();
    }

    public void AddInputPin(InputPin inPin){
        inputPins.Add(inPin);
        int size = 2^(inputPins.Count);
        truthtable = new List<bool>(new bool[size]);
    }



}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InformationWindow : MonoBehaviour
{
    private TimeTick timer;        //global simulation timer

    void Awake()
    {
        //acces timer:
        GameObject o = GameObject.FindWithTag("Timer");
        timer = o.GetComponent<TimeTick>();
    }

    void OnEnable()
    {
        //pause timer when information window pops up!
        
        timer.pause(true);
    }

    void OnDisable(){

        timer.restart();
    }
}

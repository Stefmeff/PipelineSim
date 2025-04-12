using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

/**
 * @author: Stefan Moser
 * 
 * @details: The TimeTick class is responsible for keeping track of a global simulation time and sending out
 * timing events to listening elements in the simulation. 
 * 
 * The variable "tick" keeps track of the current time tick in the simulation and gets incremented and sent out to the simulation
 * elements every tickTimerMax seconds.
 * 
 * For further details, please read the code and documentation below!
 * 
 * */
public class TimeTick : MonoBehaviour
{
    private ProjectManager projectManager;

    //Event: timer tick event
    public delegate void OnTick(int tick);
    public event OnTick TimerTickEvent;

    //Simulation parameters
    private float targetSimulationTime = 0.01f;     //last user input simulation time
    private float tickTimerMax = 0.01f;             //actual simulation time: how often per second ticks
    private int tick = 0;                           //current tick
    private float tickTimer = 0;                    //time during tick
    public bool paused = true;                      //simulation pause


    //Step through mode:
    public ButtonStep buttonStep;
    public bool stepModeOn = false;
    private int stepSize = 10;

    //UI-Elements
    private TMP_InputField InputSimulationSpeed;        //input field for entering tickTimerMax
    private TMP_InputField InputStepSize;               //input field for stepSize
    private ButtonPause pauseButton;

    private int slowCounter = 0;                    //counter => how many times was program slower than target simulation time                     
    private int fastCounter = 0;                    //counter => how many times was program faster than target simulation time


    void Awake()
    {
        //init input field:
        GameObject o = GameObject.FindWithTag("SimulationSpeed");
        InputSimulationSpeed = o.GetComponent<TMP_InputField>();
        InputSimulationSpeed.text = tickTimerMax + " sec";

        o = GameObject.FindWithTag("StepSize");
        InputStepSize = o.GetComponent<TMP_InputField>();
        InputStepSize.text = stepSize + "";
        

        pauseButton = GameObject.FindWithTag("ButtonPause").GetComponent<ButtonPause>();

        o = GameObject.FindGameObjectWithTag("ProjectManager");
        projectManager = o.GetComponent<ProjectManager>();

        //add input event listener:
        InputSimulationSpeed.onEndEdit.AddListener(delegate { LockInputSpeed(InputSimulationSpeed); });
        InputStepSize.onEndEdit.AddListener(delegate { LockInputStep(InputStepSize); });
        tick = 0;
    }

    //pauses (true) or resumes (false) the simulation
    public void pause(bool p)
    {
        if(p == false){
            stepModeOn = false;
            buttonStep.ChangeColor();
        }
        paused = p;
        pauseButton.ChangeIcon(paused);
    }

    //restarts the simulation
    public void restart()
    {
        tick = 0;

        stepModeOn = false;
        buttonStep.ChangeColor();

        //reset all the game elements to a default state
        projectManager.ResetComponents();
    }


    // Update is called once per frame
    void Update()
    {
        if(!paused){
            tickTimer += Time.deltaTime;    //measure delta time between frames

            //if delta Time slower than target simulation time:
            AdaptSimulationTime();

            if (tickTimer >= tickTimerMax)
            {
                //measured time larger than simulation time => invoke timing event.
                tickTimer -= tickTimerMax;
                tick++;
                TimerTickEvent?.Invoke(tick);
            }
        }
    }

    /**
    * @brief: this function allows for a "Step-through" mode where the
    * user can control the propagation of time.
    *
    * @details: this function produces n timing events according to the "stepSize".
    * Each time the user calls this function, "stepSize" time units pass within the simulation.
    **/
    public void Step()
    {
        for(int i=0; i < stepSize; i++){
            tick++;
            TimerTickEvent?.Invoke(tick);
        }
    }

    void LockInputSpeed(TMP_InputField input)
    {
        //user changes simulation time
        if (input.text.Length > 0)
        {
            try{
                float value = (float)decimal.Parse(input.text);
                if(10f >= value && value >= 0.001f){
                tickTimerMax = value;
                targetSimulationTime = value;
                InputSimulationSpeed.text = tickTimerMax + " sec"; 
                slowCounter = 0;
                fastCounter = 0;
                restart();
                return;
            }
            }catch{

            }
        }
        //empty => keep old parameters
        InputSimulationSpeed.text = tickTimerMax + " sec"; 
    }
    
    void LockInputStep(TMP_InputField input)
    {
        //user changes stepSize
        if (input.text.Length > 0)
        {
            stepSize = (int)int.Parse(input.text);
        }
        //empty => keep old parameters
        InputStepSize.text = stepSize + ""; 
    }


    //Slowly Adjust simulation time if performance not sufficient
    private void AdaptSimulationTime(){
        if(Time.deltaTime > targetSimulationTime){
            //elapsed time larger than target time => slow down timer
            //AdaptSimulationTime();
            if(Time.deltaTime > tickTimerMax){
                slowCounter++;
                if(slowCounter >= 15){
                    tickTimerMax = (float)Math.Round(tickTimerMax+0.001f,3);
                    InputSimulationSpeed.text = tickTimerMax + " sec"; 
                    slowCounter = 0;
                }
            }
        }else{
            //elapsed time smaller or equal to target time
            if(tickTimerMax > targetSimulationTime && Time.deltaTime < tickTimerMax){
                fastCounter++;
                slowCounter--;
                if(fastCounter >= 50){
                    tickTimerMax = (float)Math.Round(tickTimerMax-0.001f,3);
                    InputSimulationSpeed.text = tickTimerMax + " sec"; 
                    fastCounter=0;
                }
            }
        }
    }
}

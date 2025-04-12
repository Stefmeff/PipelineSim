using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonStep : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    private TimeTick timer;

    //reference to UI Images for color change upon interation:
    private Image button;
    public Image iconStep;
    public TMP_Text stepText;
    public Image image1;
    public Image image2;
    public Image image3;
    public Image image4;
    public Image image5;

    private Color c = new Color32(0x64,0x7B,0xCA,0x96);
    private TMP_Text toolTip;


    

    // Start is called before the first frame update
    void Start()
    {
        GameObject o = GameObject.FindWithTag("Timer");
        timer = o.GetComponent<TimeTick>();
        button =  gameObject.GetComponent<Image>();

        o = GameObject.FindWithTag("ToolTip");
        toolTip = o.GetComponent<TMP_Text>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if(timer.stepModeOn){
            timer.Step();
        }else{
            timer.pause(true);
            timer.stepModeOn = true;
            ChangeColor();
        }
    }

    public void ChangeColor(){
        if(timer.stepModeOn){
            Color red = new Color32(0xF8,0x49,0x41,0xFF);
            iconStep.color = red;
            stepText.color = red;
            image1.color = red; 
            image2.color = red;
            image3.color = red;
            image4.color = red;
            image5.color = red;
            c = red;
            button.color = red;
        }else{
            Color grey = new Color32(0x2A,0x2A,0x2A,0xFF);
            Color blue = new Color32(0x64,0x7B,0xCA,0x96);
            Color white = new Color32(0xCB,0xCB,0xCB,0xFF);
            //Color col = new Color32(0xF8,0xAE,0x40,0xFF);
            iconStep.color = blue;
            stepText.color = white;
            image1.color = white;
            image2.color = white;
            image3.color = grey;
            image4.color = grey;
            image5.color = grey;
            c = blue;//new Color32(0x88,0x6A,0x3D,0xFF);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        button.color = c; 
        toolTip.text = "  step through";
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        button.color = new Color32(0x2B,0x2B,0x2B,0xFF);
        toolTip.text = "  tool tip:";
    }
}

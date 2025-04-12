using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonPause : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    private TimeTick timer;

    private GameObject pauseIcon;
    private GameObject playIcon;

    
    private TMP_Text toolTip;

    // Start is called before the first frame update
    void Start()
    {
        GameObject o = GameObject.FindWithTag("Timer");
        timer = o.GetComponent<TimeTick>();

        pauseIcon = gameObject.transform.Find("PauseIcon").gameObject;
        playIcon = gameObject.transform.Find("PlayIcon").gameObject;

        o = GameObject.FindWithTag("ToolTip");
        toolTip = o.GetComponent<TMP_Text>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        timer.pause(!timer.paused);
    }

    public void ChangeIcon(bool paused){
        pauseIcon.SetActive(paused);
        playIcon.SetActive(!paused);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Image i = gameObject.GetComponent<Image>();
        i.color = new Color32(0x64,0x7B,0xCA,0x96);
        toolTip.text = "  start/pause";
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(timer.paused){
            Image i = gameObject.GetComponent<Image>();
            i.color = new Color32(0x2B,0x2B,0x2B,0xFF);
        }
        toolTip.text = "  tool tip:";
    }
}

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InformationWindow : MonoBehaviour
{
    private TimeTick timer;        //global simulation timer
    private static InformationWindow instance;
    private TextMeshProUGUI messageText;

    void Awake()
    {
        instance = this;

        //access timer:
        GameObject o = GameObject.FindWithTag("Timer");
        timer = o.GetComponent<TimeTick>();

        // Auto-find text element in children
        messageText = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    void OnEnable()
    {
        //pause timer when information window pops up!
        timer.pause(true);
    }

    void OnDisable()
    {
        timer.restart();
    }

    public static void Show(string message)
    {
        if (instance != null)
        {
            if (instance.messageText != null)
            {
                instance.messageText.text = message;
            }
            instance.gameObject.SetActive(true);
        }
    }
}

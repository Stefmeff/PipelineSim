using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InformationWindow : MonoBehaviour
{
    private TimeTick timer;        //global simulation timer
    private static InformationWindow instance;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI messageText;

    void Awake()
    {
        instance = this;

        //access timer:
        GameObject o = GameObject.FindWithTag("Timer");
        timer = o.GetComponent<TimeTick>();

        FindTextElements();
    }

    private void FindTextElements()
    {
        // "Description" child = title/headline
        Transform desc = transform.Find("Description");
        if (desc != null) titleText = desc.GetComponent<TextMeshProUGUI>();

        // "Message" child = detailed message body
        Transform msg = transform.Find("Message");
        if (msg != null) messageText = msg.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    void OnEnable()
    {
        //pause timer when information window pops up!
        timer.pause(true);
    }

    void OnDisable()
    {
        timer.pause(false);
    }

    public static void Show(string title, string message)
    {
        // Find instance if not yet set (Awake doesn't run on inactive objects)
        if (instance == null)
        {
            instance = FindObjectOfType<InformationWindow>(true);
            if (instance == null) return;

            if (instance.timer == null)
            {
                GameObject o = GameObject.FindWithTag("Timer");
                instance.timer = o.GetComponent<TimeTick>();
            }
            instance.FindTextElements();
        }

        if (instance.titleText != null) instance.titleText.text = title;
        if (instance.messageText != null) instance.messageText.text = message;
        instance.gameObject.SetActive(true);
    }

    // Overload for backwards compat
    public static void Show(string message)
    {
        Show("Error", message);
    }
}

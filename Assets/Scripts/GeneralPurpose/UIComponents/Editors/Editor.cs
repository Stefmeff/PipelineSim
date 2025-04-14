using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * This class is used for opening the editors of components to parse the input values between the components
 * and its editor.
 */
public class Editor : MonoBehaviour
{
    public bool active;
    //reference to the different editor types
    private GameObject clockEditor;
    private GameObject delayEditor;
    private GameObject delay2Editor;
    private GameObject flopEditor;
    private GameObject latchEditor;
    private GameObject cplEditor;

    private GameObject dataSourceEditor;
    private ButtonPause buttonPause;

    void Start()
    {
        //get reference to simulation timer
        GameObject o = GameObject.FindWithTag("ButtonPause");
        buttonPause = o.GetComponent<ButtonPause>();

        clockEditor = gameObject.transform.GetChild(0).gameObject;
        delayEditor = gameObject.transform.GetChild(1).gameObject;
        delay2Editor = gameObject.transform.GetChild(2).gameObject;
        flopEditor = gameObject.transform.GetChild(3).gameObject;
        latchEditor = gameObject.transform.GetChild(4).gameObject;
        cplEditor = gameObject.transform.GetChild(5).gameObject;
        //dataSourceEditor = gameObject.transform.GetChild(6).gameObject;
        closeEditors();
    }

    private void closeEditors()
    {
        //close all existing editors
        clockEditor.SetActive(false);
        delayEditor.SetActive(false);
        delay2Editor.SetActive(false);
        flopEditor.SetActive(false);
        latchEditor.SetActive(false);
        cplEditor.SetActive(false);
        //dataSourceEditor.SetActive(false);
    }


    public void openEditor(Loadable component)
    {
        //close any open editors
        closeEditors();
        component.OpenEditor();
    }
}

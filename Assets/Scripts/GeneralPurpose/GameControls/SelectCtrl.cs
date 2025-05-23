using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;


/**
 * @author: Stefan Moser
 * 
 * @brief: This class is used for selecting game objects via mouse click and performing operations like delete,rotate,copy/paste
 * */
[RequireComponent(typeof(UIDocument))]
public class SelectCtrl : MonoBehaviour
{
    public GameObject over = null;          //GameObject that mouse is currently over (set by Draggable2D script)
    private GameObject selected = null;
    public IObjectMono overMono  = null;
    private IObjectMono selectedMono = null;
    private IObjectMono copy = null;

    private bool rotateOn = false;

    // Start is called before the first frame update
    private void Awake()
    {
        GameObject o = GameObject.FindWithTag("ButtonRotate");
        ButtonRotate b1 = o.GetComponent<ButtonRotate>();
        b1.RotateObjectEvent += OnRotate;

        o = GameObject.FindWithTag("ButtonDelete");
        ButtonDelete b2 = o.GetComponent<ButtonDelete>();
        b2.DeleteObjectEvent += OnDelete;
    }

    private void OnDelete()
    {
        if (selected)
        {
            Destroy(selected);
        }
    }

    private void OnRotate()
    {
        if (selected)
        {
            selected.transform.Rotate(0, 0, 90);
            rotateOn = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if(rotateOn){
                rotateOn = false;
            }else{
                //select the element the mouse is currenty over
                selected = over;
                selectedMono = overMono;
            }
        }

        if(selected){
            //parse variouse keyboard inputs
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                Destroy(selected);
            }

            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.R))
            {
                selected.transform.Rotate(0, 0, 90);
            }
        }


        
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("Copy!");
            //TODO:
        }

        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.V))
        {
            Debug.Log("Paste!");
            
            //TODO:
        }
    }
}

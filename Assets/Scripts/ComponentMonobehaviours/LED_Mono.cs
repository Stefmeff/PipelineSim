using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LED_Mono : MonoBehaviour, IObjectMono
{
    public LED led;
    public InputPin_Mono input;

    private SpriteRenderer sRend;
    private ProjectManager projectManager;

    // Start is called before the first frame update
    void Awake()
    {
        this.led = new LED();
        led.UpdateColorEvent += UpdateColor;
        InitPins();

        //add to project:
        GameObject o = GameObject.FindGameObjectWithTag("ProjectManager");
        projectManager = o.GetComponent<ProjectManager>();
        projectManager.addToProject(this);


        //init variable for animation:
        sRend = this.GetComponent<SpriteRenderer>();
        sRend.color = (Color)new Color32(188, 188, 188, 255);
    }

    public void Init(LED led)
    {
        if(this.led != null)this.led.Dispose();
        this.led = led;
        UpdateColor(new BitToken().ActiveColor());
        led.UpdateColorEvent += UpdateColor;
        InitPins();
    }

    private void InitPins()
    {
        input.Init(led.input);
    }

    private void UpdateColor(Color c)
    {
        sRend.color = c;
    }

    public ILoadable GetMain()
    {
        return this.led;
    }

    public void SaveTransform()
    {
        this.led.SaveTransform(this.transform);
    }

    public void Clear()
    {
        Destroy(this.gameObject);
    }

    private void OnDestroy()
    {
        projectManager.removeFromProject(this);
        led.UpdateColorEvent += UpdateColor;
        led.Dispose();
        led = null;
    }
}

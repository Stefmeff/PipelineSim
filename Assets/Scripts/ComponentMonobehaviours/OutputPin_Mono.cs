using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * This class handles mouse inputs of an OutputPin prefab. It also hold a reference
 * to the OutputPin Object that describes this pins state.
 */
public class OutputPin_Mono : MonoBehaviour
{
    public OutputPin pin;   //Main OutputPin Object that stores the state of this GameObject

    //Variables for Wire creation
    public GameObject wirePrefab;   //prefab used for initializing wire
    private Wire_Mono wire_mono = null;

    //variables used for animation

    private SpriteRenderer rend;
    private Color original;
    private Color animate;

    void Awake()
    {
        //init variable for animation:
        rend = this.GetComponent<SpriteRenderer>();
        original = new Color32(0x78,0x85, 0x8D, 0xFF);
        animate = new Color32(0x98,0xA8,0xB3,0xFF);

        if(pin!=null)pin.transform = this.transform;
    }

    /**
     * Initialize this prefab instance with an OutputPin Object.
     * */
    public void Init(OutputPin pin)
    {
        this.pin = pin;
        pin.transform = this.transform;
    }

    private void OnMouseOver()
    {
        //animate:
        rend.color = animate;
    }

    private void OnMouseExit()
    {
        rend.color = original;
    }

    private void OnMouseDown()
    {
        //Destroy existing wire:
        if (!pin.disconnectWire())
        {
            //create wire and add this pin as input + knot
            GameObject o = Instantiate(wirePrefab, new Vector3(0, 0, 0), Quaternion.identity);
            wire_mono = o.GetComponent<Wire_Mono>();
            Wire wire = pin.connectWire();
            wire_mono.Init(wire);
        }
    }

    private void OnMouseUp()
    {
        if(wire_mono != null) wire_mono.DrawModeOn();
    }

    private void OnDestroy()
    {
        //destroy connected wire
        pin.disconnectWire();
    }
}

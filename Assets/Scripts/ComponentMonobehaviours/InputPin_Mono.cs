using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * This class handles user inputs of an InputPin.
 */
public class InputPin_Mono : MonoBehaviour
{
    private InputPin pin;
    private PinConnectionHandler connectionHandler;

    //variables used for animation
    private SpriteRenderer rend;
    private Color original;
    private Color animate;

    // Start is called before the first frame update
    void Awake()
    {
        //init connection handler
        GameObject o = GameObject.FindGameObjectWithTag("PinConnectionHandler");
        connectionHandler = o.GetComponent<PinConnectionHandler>();

        //init variable for animation:
        rend = this.GetComponent<SpriteRenderer>();
        original = new Color32(0x78,0x85, 0x8D, 0xFF);
        animate = new Color32(0x98,0xA8,0xB3,0xFF);

        if(pin!=null) pin.transform = transform;
    }

    public void Init(InputPin pin)
    {
        this.pin = pin;
        pin.transform = transform;
    }

    private void OnMouseOver()
    {
        //animate:
        rend.color = animate;

        OutputPin dataIn = connectionHandler.searchesConnection;
        if (dataIn != null)
        {
            // Check width compatibility
            if (dataIn.width != pin.width)
            {
                // Width mismatch — show error, don't allow connection
                InformationWindow.Show("Cannot connect: wire carries " + dataIn.width + "-bit signal but pin expects " + pin.width + "-bit");
                connectionHandler.possibleConnection = null;
            }
            else
            {
                connectionHandler.possibleConnection = pin;
            }
        }
    }

    private void OnMouseExit()
    {
        rend.color = original;

        connectionHandler.possibleConnection = null;

    }


    private void OnDestroy()
    {
        //destroy connected wire
        pin.disconnectWire();
    }
}

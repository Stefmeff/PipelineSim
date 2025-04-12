using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/**
 * Used to get the mouse poisition as a transfrom component!
 */
public class MouseTracker : MonoBehaviour
{
    // Start is called before the first frame update
    public new Transform transform;

    void Start()
    {
        transform = this.GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }
}

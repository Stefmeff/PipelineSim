using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/**
 * Used to get the mouse poisition as a transfrom component!
 */
public class MouseTracker : MonoBehaviour
{
    public new Transform transform;
    private Camera cam;

    void Start()
    {
        transform = this.GetComponent<Transform>();
        cam = Camera.main;
    }

    void Update()
    {
        transform.position = cam.ScreenToWorldPoint(Input.mousePosition);
    }
}

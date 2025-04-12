using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Wire))]
public class Wire_Mouse : MonoBehaviour
{
    private LineRenderer lineRend;



    // Start is called before the first frame update
    void Start()
    {
        //init variable for animation:
        lineRend = this.GetComponent<LineRenderer>();

    }

    public void OnMouseOver()
    {
        //animate:
        lineRend.startWidth = 1.5f;
        lineRend.endWidth = 1.5f;
    }

    public void OnMouseDown()
    {
        /*
        Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        GameObject wireKnot = Instantiate(wireKnotPrefab, pos, Quaternion.identity);
        pointsCount += 1;
        points.Add(wireKnot.transform);
        */
    }


    public void OnMouseExit()
    {
        lineRend.startWidth = 1.0f;
        lineRend.endWidth = 1.0f;
    }
}

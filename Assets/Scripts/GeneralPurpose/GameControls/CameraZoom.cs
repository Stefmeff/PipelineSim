using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This class is used for zooming the camera in and out via mouse scroll
public class CameraZoom : MonoBehaviour
{
    private float zoom;
    private float zoomMultiplier = 16f;
    private float minZoom = 16f;
    private float maxZoom = 400f;
    private float velocity = 0f;
    private float smoothTime = 0.2f;

    [SerializeField] private Camera cam;

    private ProjectManager projectManager;

    private int i = 0;

    private void Start()
    {
        zoom = cam.orthographicSize;

        GameObject o = GameObject.FindGameObjectWithTag("ProjectManager");
        projectManager = o.GetComponent<ProjectManager>();
    }

    public void SetZoom(float newZoom)
    {
        zoom = Mathf.Clamp(newZoom, minZoom, maxZoom);
        cam.orthographicSize = zoom;
        velocity = 0f;
    }

    private void Update()
    {
        if(projectManager.zoomActive){
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            zoom -= scroll * zoomMultiplier;
            zoom = Mathf.Clamp(zoom, minZoom, maxZoom);
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, zoom, ref velocity, smoothTime);
        }

    }
}

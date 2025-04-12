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

    private int i = 0;

    private void Start()
    {
        zoom = cam.orthographicSize;
    }

    private void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        
        zoom -= scroll * zoomMultiplier;
        zoom = Mathf.Clamp(zoom, minZoom, maxZoom);
        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, zoom, ref velocity, smoothTime);

        
    }
}

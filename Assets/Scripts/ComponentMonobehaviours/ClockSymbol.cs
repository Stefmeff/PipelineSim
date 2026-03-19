using UnityEngine;

/// <summary>
/// Draws a square wave clock symbol on the component.
/// Attach to the Clock prefab.
/// </summary>
public class ClockSymbol : MonoBehaviour
{
    private LineRenderer symbolLine;

    void Start()
    {
        DrawSymbol();
    }

    private void DrawSymbol()
    {
        GameObject symbolObj = new GameObject("ClockWaveSymbol");
        symbolObj.transform.SetParent(transform);
        symbolObj.transform.localPosition = Vector3.zero;
        symbolObj.transform.localRotation = Quaternion.identity;
        symbolObj.transform.localScale = Vector3.one;

        symbolLine = symbolObj.AddComponent<LineRenderer>();
        symbolLine.useWorldSpace = false;
        symbolLine.sortingLayerName = "ChildLayer";
        symbolLine.sortingOrder = 5;

        // Use same material as existing wires
        Shader shader = Shader.Find("Sprites/Default");
        symbolLine.material = new Material(shader);

        Color symbolColor = new Color32(0x78, 0x85, 0x8D, 0xFF);
        symbolLine.startColor = symbolColor;
        symbolLine.endColor = symbolColor;

        // Line width relative to component scale
        float lineWidth = 0.6f;
        symbolLine.startWidth = lineWidth;
        symbolLine.endWidth = lineWidth;

        // Draw square wave: 2 full cycles centered on the component
        // Coordinates in local space (component scale is 15x15, so ±0.5 = full extent)
        float w = 0.25f;  // half-width of the wave
        float h = 0.12f;  // half-height of the wave
        float cy = 0f;    // center Y

        Vector3[] points = new Vector3[]
        {
            new Vector3(-w,       cy - h, -0.1f),  // start low
            new Vector3(-w * 0.5f,cy - h, -0.1f),  // horizontal low
            new Vector3(-w * 0.5f,cy + h, -0.1f),  // vertical up
            new Vector3(0,        cy + h, -0.1f),   // horizontal high
            new Vector3(0,        cy - h, -0.1f),   // vertical down
            new Vector3(w * 0.5f, cy - h, -0.1f),  // horizontal low
            new Vector3(w * 0.5f, cy + h, -0.1f),  // vertical up
            new Vector3(w,        cy + h, -0.1f),   // horizontal high
        };

        symbolLine.positionCount = points.Length;
        symbolLine.SetPositions(points);
    }
}

using UnityEngine;

/// <summary>
/// Static utility for snapping positions to the grid.
/// </summary>
public static class GridSnap
{
    public static float gridSize = 5f;

    public static Vector3 Snap(Vector3 position)
    {
        float x = Mathf.Round(position.x / gridSize) * gridSize;
        float y = Mathf.Round(position.y / gridSize) * gridSize;
        return new Vector3(x, y, position.z);
    }
}

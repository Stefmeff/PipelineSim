using UnityEngine;

/// <summary>
/// Static utility for snapping positions to the grid.
/// Grid size matches GridRenderer's baseGridSize.
/// </summary>
public static class GridSnap
{
    public static float gridSize = 10f;

    public static Vector3 Snap(Vector3 position)
    {
        float x = Mathf.Round(position.x / gridSize) * gridSize;
        float y = Mathf.Round(position.y / gridSize) * gridSize;
        return new Vector3(x, y, position.z);
    }
}

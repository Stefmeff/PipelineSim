using UnityEngine;

/// <summary>
/// Renders a background grid that auto-scales with zoom.
/// Finer lines disappear when zoomed out. Updates GridSnap
/// to match the currently visible grid level.
/// </summary>
public class GridRenderer : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private float baseGridSize = 5f;
    [SerializeField] private Color gridColor = new Color(0.3f, 0.3f, 0.3f, 0.06f);
    [SerializeField] private Color majorGridColor = new Color(0.4f, 0.4f, 0.4f, 0.12f);
    [SerializeField] private int majorGridEvery = 5;
    [SerializeField] private float maxLinesPerAxis = 30f;

    private Material lineMaterial;
    private Mesh gridMesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private float lastOrthoSize;
    private Vector3 lastCamPos;

    private void Start()
    {
        CreateLineMaterial();
        CreateMeshComponents();
        RebuildGrid();
    }

    private void CreateLineMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        lineMaterial = new Material(shader);
        lineMaterial.hideFlags = HideFlags.HideAndDontSave;
    }

    private void CreateMeshComponents()
    {
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = lineMaterial;
        meshRenderer.sortingLayerName = "Background";
        meshRenderer.sortingOrder = 0;

        gridMesh = new Mesh();
        gridMesh.name = "GridMesh";
        meshFilter.mesh = gridMesh;
    }

    private void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        if (Mathf.Abs(cam.orthographicSize - lastOrthoSize) > 0.1f ||
            Vector3.Distance(cam.transform.position, lastCamPos) > 0.5f)
        {
            RebuildGrid();
            lastOrthoSize = cam.orthographicSize;
            lastCamPos = cam.transform.position;
        }
    }

    private void RebuildGrid()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float orthoSize = cam.orthographicSize;
        float aspect = cam.aspect;
        float height = orthoSize * 2f;
        float width = height * aspect;
        Vector3 camPos = cam.transform.position;

        // Auto-scale: double grid size until it fits within maxLinesPerAxis
        float gridSize = baseGridSize;
        while (height / gridSize > maxLinesPerAxis) gridSize *= 2f;

        // Snap always uses base grid size so components stay aligned at all zoom levels
        GridSnap.gridSize = baseGridSize;

        float startX = Mathf.Floor((camPos.x - width / 2f) / gridSize) * gridSize;
        float endX = Mathf.Ceil((camPos.x + width / 2f) / gridSize) * gridSize;
        float startY = Mathf.Floor((camPos.y - height / 2f) / gridSize) * gridSize;
        float endY = Mathf.Ceil((camPos.y + height / 2f) / gridSize) * gridSize;

        int verticalLines = Mathf.CeilToInt((endX - startX) / gridSize) + 1;
        int horizontalLines = Mathf.CeilToInt((endY - startY) / gridSize) + 1;
        int totalLines = verticalLines + horizontalLines;

        Vector3[] vertices = new Vector3[totalLines * 2];
        Color[] colors = new Color[totalLines * 2];
        int[] indices = new int[totalLines * 2];
        int v = 0;

        for (float x = startX; x <= endX && v + 1 < vertices.Length; x += gridSize)
        {
            int lineIndex = Mathf.RoundToInt(x / gridSize);
            bool isMajor = ((lineIndex % majorGridEvery) + majorGridEvery) % majorGridEvery == 0;
            Color c = isMajor ? majorGridColor : gridColor;
            vertices[v] = new Vector3(x, startY, 0); colors[v] = c; indices[v] = v; v++;
            vertices[v] = new Vector3(x, endY, 0); colors[v] = c; indices[v] = v; v++;
        }

        for (float y = startY; y <= endY && v + 1 < vertices.Length; y += gridSize)
        {
            int lineIndex = Mathf.RoundToInt(y / gridSize);
            bool isMajor = ((lineIndex % majorGridEvery) + majorGridEvery) % majorGridEvery == 0;
            Color c = isMajor ? majorGridColor : gridColor;
            vertices[v] = new Vector3(startX, y, 0); colors[v] = c; indices[v] = v; v++;
            vertices[v] = new Vector3(endX, y, 0); colors[v] = c; indices[v] = v; v++;
        }

        gridMesh.Clear();
        gridMesh.vertices = vertices;
        gridMesh.colors = colors;
        gridMesh.SetIndices(indices, MeshTopology.Lines, 0);
    }

    private void OnDestroy()
    {
        if (lineMaterial != null) DestroyImmediate(lineMaterial);
        if (gridMesh != null) DestroyImmediate(gridMesh);
    }
}

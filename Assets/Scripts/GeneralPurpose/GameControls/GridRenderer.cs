using UnityEngine;

/// <summary>
/// Renders a background grid that adapts to camera zoom level.
/// Attach this script to the Main Camera.
/// </summary>
public class GridRenderer : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private float baseGridSize = 1f;
    [SerializeField] private Color gridColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
    [SerializeField] private Color majorGridColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
    [SerializeField] private int majorGridEvery = 5;

    private Material lineMaterial;
    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();
        CreateLineMaterial();
    }

    private void CreateLineMaterial()
    {
        // Unity built-in shader for GL line drawing
        Shader shader = Shader.Find("Hidden/Internal-Colored");
        lineMaterial = new Material(shader);
        lineMaterial.hideFlags = HideFlags.HideAndDontSave;
        lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        lineMaterial.SetInt("_ZWrite", 0);
    }

    private void OnPostRender()
    {
        if (cam == null || lineMaterial == null) return;

        // Determine grid spacing based on zoom level
        float gridSize = baseGridSize;
        float orthoSize = cam.orthographicSize;

        // Scale grid so it doesn't become too dense or too sparse
        // At zoom 16 (close): gridSize = 1
        // At zoom 64: gridSize = 4
        // At zoom 256: gridSize = 16
        while (orthoSize / gridSize > 80) gridSize *= 2f;
        while (orthoSize / gridSize < 10) gridSize /= 2f;

        // Calculate visible area
        float aspect = cam.aspect;
        float height = orthoSize * 2f;
        float width = height * aspect;

        Vector3 camPos = cam.transform.position;

        // Snap grid origin to grid lines so grid doesn't slide with camera
        float startX = Mathf.Floor((camPos.x - width / 2f) / gridSize) * gridSize;
        float endX = Mathf.Ceil((camPos.x + width / 2f) / gridSize) * gridSize;
        float startY = Mathf.Floor((camPos.y - height / 2f) / gridSize) * gridSize;
        float endY = Mathf.Ceil((camPos.y + height / 2f) / gridSize) * gridSize;

        lineMaterial.SetPass(0);

        GL.PushMatrix();
        GL.LoadProjectionMatrix(cam.projectionMatrix);
        GL.modelview = cam.worldToCameraMatrix;

        GL.Begin(GL.LINES);

        // Draw vertical lines
        for (float x = startX; x <= endX; x += gridSize)
        {
            bool isMajor = Mathf.Abs(Mathf.Round(x / gridSize) % majorGridEvery) < 0.01f;
            GL.Color(isMajor ? majorGridColor : gridColor);
            GL.Vertex3(x, startY, 0);
            GL.Vertex3(x, endY, 0);
        }

        // Draw horizontal lines
        for (float y = startY; y <= endY; y += gridSize)
        {
            bool isMajor = Mathf.Abs(Mathf.Round(y / gridSize) % majorGridEvery) < 0.01f;
            GL.Color(isMajor ? majorGridColor : gridColor);
            GL.Vertex3(startX, y, 0);
            GL.Vertex3(endX, y, 0);
        }

        GL.End();
        GL.PopMatrix();
    }

    private void OnDestroy()
    {
        if (lineMaterial != null)
        {
            DestroyImmediate(lineMaterial);
        }
    }
}

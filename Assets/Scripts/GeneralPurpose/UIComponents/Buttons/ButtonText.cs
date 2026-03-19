using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/**
 * @brief Toolbar button that activates text placement mode.
 *
 * @details On click, toggles placement mode. While active, the next left-click
 * in the sandbox places a TextAnnotation at that position. The button stays
 * highlighted while placement mode is active.
 **/
public class ButtonText : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler
{
    private TMP_Text toolTip;
    private Camera cam;
    private Image image;
    private Texture2D penCursor;
    private Vector2 penHotspot;

    private static readonly Color32 defaultColor = new Color32(0x2B, 0x2B, 0x2B, 0xFF);
    private static readonly Color32 hoverColor = new Color32(0x64, 0x7B, 0xCA, 0x96);
    private static readonly Color32 activeColor = new Color32(0x4A, 0x6B, 0xAA, 0xFF);

    /**
     * @brief Static flag — when true, the next sandbox click places a TextAnnotation.
     **/
    public static bool placementMode = false;
    private static ButtonText activeButton;
    private int activationFrame;

    void Awake()
    {
        GameObject o = GameObject.FindWithTag("ToolTip");
        toolTip = o.GetComponent<TMP_Text>();
        cam = Camera.main;
        image = GetComponent<Image>();
        penCursor = Resources.Load<Texture2D>("Art/Cursors/pen");
        if (penCursor != null)
            penHotspot = new Vector2(4, 4);
    }

    void Update()
    {
        if (!placementMode) return;
        if (Time.frameCount == activationFrame) return;

        if (Input.GetMouseButtonDown(0))
        {
            // Check if clicking real UI (not the background canvas)
            if (EventSystem.current.IsPointerOverGameObject())
            {
                // Ignore if it's the background canvas (tagged BackgroundCanvas)
                var results = new System.Collections.Generic.List<RaycastResult>();
                var pointerData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
                EventSystem.current.RaycastAll(pointerData, results);

                bool isRealUI = false;
                foreach (var r in results)
                {
                    if (r.gameObject.CompareTag("BackgroundCanvas") ||
                        r.gameObject.transform.parent?.CompareTag("BackgroundCanvas") == true)
                        continue;
                    isRealUI = true;
                    break;
                }

                if (isRealUI)
                {
                    Deactivate();
                    return;
                }
            }

            Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            mousePos = GridSnap.Snap(mousePos);

            GameObject prefab = Resources.Load<GameObject>("Prefabs/TextAnnotation");
            if (prefab != null)
                GameObject.Instantiate(prefab, mousePos, Quaternion.identity);

            Deactivate();
        }

        // Cancel on right-click or Escape
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            Deactivate();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Toggle placement mode
        if (placementMode && activeButton == this)
        {
            Deactivate();
        }
        else
        {
            placementMode = true;
            activeButton = this;
            activationFrame = Time.frameCount;
            image.color = activeColor;
            Cursor.SetCursor(penCursor, penHotspot, CursorMode.Auto);
        }
    }

    private void Deactivate()
    {
        placementMode = false;
        activeButton = null;
        image.color = defaultColor;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    public void OnPointerUp(PointerEventData eventData) { }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!placementMode) image.color = hoverColor;
        toolTip.text = "  add text annotation";
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!placementMode) image.color = defaultColor;
        toolTip.text = "  tool tip:";
    }
}

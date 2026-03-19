using UnityEngine;
using TMPro;

/**
 * @brief MonoBehaviour for a resizable text annotation box in the sandbox.
 *
 * @details Handles all interaction directly (no Draggable2D):
 * - Short click inside → edit text (with blinking caret)
 * - Long press + drag → move the component
 * - Drag edges/corners → resize (cursor changes on hover)
 * - Text auto-grows the box when it overflows
 **/
public class TextAnnotation_Mono : MonoBehaviour, IObjectMono
{
    private TextAnnotation data;
    private ProjectManager projectManager;

    private SpriteRenderer border;
    private SpriteRenderer inner;
    private TextMeshPro textMesh;
    private BoxCollider2D boxCollider;

    // Caret
    private SpriteRenderer caretRenderer;
    private int caretPosition = 0;
    private float caretBlinkTimer = 0f;
    private bool caretVisible = true;

    // Drag/resize state
    private CameraMouseDrag camDrag;
    private SelectCtrl selector;
    private bool isDragging = false;
    private bool isResizing = false;
    private ResizeEdge activeEdge = ResizeEdge.None;
    private ResizeEdge edgeOnMouseDown = ResizeEdge.None;
    private Vector3 resizeStartMouse;
    private float resizeStartWidth;
    private float resizeStartHeight;
    private Vector3 resizeStartPos;

    // Long-press detection
    private bool mouseDownInside = false;
    private Vector3 mouseDownScreenPos;
    private bool longPressTriggered = false;
    private static readonly float dragPixelThreshold = 5f;

    // Mouse tracking
    private bool mouseIsOver = false;
    private bool cursorOverridden = false;

    // Editing state
    private bool isEditing = false;

    private Camera cam;

    private static Sprite squareSprite;
    private static Sprite caretSprite;

    // Cursors
    private static Texture2D cursorEW;
    private static Texture2D cursorNS;
    private static Texture2D cursorNWSE;
    private static Texture2D cursorNESW;
    private static Texture2D dragCursor;
    private static Vector2 resizeCursorHotspot;
    private static Vector2 dragCursorHotspot;
    private static bool cursorsLoaded = false;

    private static readonly Color32 borderColor = new Color32(0x78, 0x85, 0x8D, 35);
    private static readonly Color32 borderEditColor = new Color32(0x90, 0x9D, 0xA5, 35);
    private static readonly Color32 fillColor = new Color32(0x1A, 0x1A, 0x2E, 154);
    private static readonly Color32 fillEditColor = new Color32(0x2A, 0x2A, 0x3E, 154);
    private static readonly float borderThickness = 0.5f;
    private static readonly float minWidth = 10f;
    private static readonly float minHeight = 10f;

    private enum ResizeEdge
    {
        None, Right, Left, Top, Bottom,
        TopLeft, TopRight, BottomLeft, BottomRight
    }

    private void Awake()
    {
        GameObject o = GameObject.FindGameObjectWithTag("ProjectManager");
        projectManager = o.GetComponent<ProjectManager>();
        projectManager.addToProject(this);
        cam = Camera.main;

        o = GameObject.FindWithTag("BackgroundCanvas");
        camDrag = o.transform.GetChild(0).GetComponent<CameraMouseDrag>();

        o = GameObject.FindWithTag("Selector");
        selector = o.GetComponent<SelectCtrl>();

        if (squareSprite == null)
        {
            squareSprite = Resources.Load<Sprite>("Art/RoundedSquare");
            if (squareSprite == null)
            {
                Texture2D tex = Resources.Load<Texture2D>("Art/RoundedSquare");
                if (tex != null)
                {
                    squareSprite = Sprite.Create(tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f), 16f,
                        0, SpriteMeshType.FullRect,
                        new Vector4(6, 6, 6, 6));
                }
                else
                {
                    tex = new Texture2D(1, 1);
                    tex.SetPixel(0, 0, Color.white);
                    tex.Apply();
                    squareSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
                }
            }
        }

        if (caretSprite == null)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            caretSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
        }

        if (!cursorsLoaded)
        {
            cursorEW = Resources.Load<Texture2D>("Art/Cursors/resize_ew");
            cursorNS = Resources.Load<Texture2D>("Art/Cursors/resize_ns");
            cursorNWSE = Resources.Load<Texture2D>("Art/Cursors/resize_nwse");
            cursorNESW = Resources.Load<Texture2D>("Art/Cursors/resize_nesw");
            dragCursor = Resources.Load<Texture2D>("Art/Cursors/drag");
            if (cursorEW != null)
                resizeCursorHotspot = new Vector2(cursorEW.width / 2f, cursorEW.height / 2f);
            if (dragCursor != null)
                dragCursorHotspot = new Vector2(dragCursor.width / 2f, dragCursor.height / 2f);
            cursorsLoaded = true;
        }
    }

    private void Start()
    {
        if (data == null) InitNew();
    }

    public void Init(TextAnnotation annotation)
    {
        this.data = annotation;
        BuildVisual();
    }

    public void InitNew()
    {
        this.data = new TextAnnotation();
        data.SaveTransform(transform);
        BuildVisual();
    }

    private void BuildVisual()
    {
        if (border != null) Destroy(border.gameObject);
        if (inner != null) Destroy(inner.gameObject);
        if (textMesh != null) Destroy(textMesh.gameObject);
        if (caretRenderer != null) Destroy(caretRenderer.gameObject);

        float w = data.width;
        float h = data.height;
        float t = borderThickness;

        // Border (9-sliced)
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(transform, false);
        borderObj.transform.localPosition = Vector3.zero;
        border = borderObj.AddComponent<SpriteRenderer>();
        border.sprite = squareSprite;
        border.drawMode = SpriteDrawMode.Sliced;
        border.size = new Vector2(w, h);
        border.color = borderColor;
        border.sortingOrder = -2;

        // Inner fill (9-sliced)
        GameObject innerObj = new GameObject("Inner");
        innerObj.transform.SetParent(transform, false);
        innerObj.transform.localPosition = Vector3.zero;
        inner = innerObj.AddComponent<SpriteRenderer>();
        inner.sprite = squareSprite;
        inner.drawMode = SpriteDrawMode.Sliced;
        inner.size = new Vector2(w - t * 2, h - t * 2);
        inner.color = fillColor;
        inner.sortingOrder = -1;

        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(transform, false);
        textObj.transform.localPosition = Vector3.zero;
        textMesh = textObj.AddComponent<TextMeshPro>();
        textMesh.text = data.text;
        textMesh.fontSize = 56;
        textMesh.alignment = TextAlignmentOptions.TopLeft;
        textMesh.color = Color.white;
        textMesh.enableWordWrapping = true;
        textMesh.overflowMode = TextOverflowModes.Overflow;
        textMesh.sortingOrder = 0;
        textMesh.margin = new Vector4(1, 1, 1, 1);

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.sizeDelta = new Vector2(w - t * 4, h - t * 4);

        // Caret (thin vertical line, hidden by default)
        GameObject caretObj = new GameObject("Caret");
        caretObj.transform.SetParent(textObj.transform, false);
        caretRenderer = caretObj.AddComponent<SpriteRenderer>();
        caretRenderer.sprite = caretSprite;
        caretRenderer.color = Color.white;
        caretRenderer.sortingOrder = 1;
        caretObj.transform.localScale = new Vector3(0.15f, 3f, 1f);
        caretRenderer.enabled = false;

        // Collider
        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null) boxCollider = gameObject.AddComponent<BoxCollider2D>();
        boxCollider.size = new Vector2(w, h);
    }

    private void UpdateVisual()
    {
        float w = data.width;
        float h = data.height;
        float t = borderThickness;

        border.size = new Vector2(w, h);
        inner.size = new Vector2(w - t * 2, h - t * 2);

        RectTransform textRt = textMesh.GetComponent<RectTransform>();
        textRt.sizeDelta = new Vector2(w - t * 4, h - t * 4);

        boxCollider.size = new Vector2(w, h);
    }

    // --- Update ---

    private void Update()
    {
        if (data == null) return;

        // Click outside to exit editing
        if (isEditing && Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0;
            if (!IsInsideBox(mouseWorld))
                StopEditing();
        }

        // Drag detection: if mouse moved beyond threshold, it's a drag (no time requirement)
        if (mouseDownInside && !isEditing && !isResizing && !longPressTriggered)
        {
            float movedPixels = Vector3.Distance(Input.mousePosition, mouseDownScreenPos);

            if (movedPixels > dragPixelThreshold)
            {
                longPressTriggered = true;
                isDragging = true;
                camDrag.camDragOn = false;
                Cursor.SetCursor(dragCursor, dragCursorHotspot, CursorMode.Auto);
            }
        }

        // Continue drag
        if (isDragging && projectManager.dragActive)
        {
            Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            transform.position = new Vector3(mousePos.x, mousePos.y, 0);
        }

        // Handle active resize
        if (isResizing)
        {
            HandleResize();
            if (Input.GetMouseButtonUp(0))
            {
                isResizing = false;
                activeEdge = ResizeEdge.None;
                camDrag.camDragOn = true;
                transform.position = GridSnap.Snap(transform.position);
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                cursorOverridden = false;
            }
            return;
        }

        // Cursor on hover (works both in and out of edit mode)
        if (mouseIsOver && !isDragging)
        {
            ResizeEdge edge = DetectEdge();

            if (edge != ResizeEdge.None)
            {
                SetResizeCursor(edge);
                cursorOverridden = true;
            }
            else if (cursorOverridden)
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                cursorOverridden = false;
            }
        }

        // Blinking caret
        if (isEditing)
        {
            caretBlinkTimer += Time.deltaTime;
            if (caretBlinkTimer >= 0.5f)
            {
                caretVisible = !caretVisible;
                caretBlinkTimer = 0f;
            }
            if (caretRenderer != null)
                caretRenderer.enabled = caretVisible;

            UpdateCaretPosition();
        }
    }

    private bool IsInsideBox(Vector3 worldPos)
    {
        Vector3 local = transform.InverseTransformPoint(worldPos);
        float w = data.width;
        float h = data.height;
        return local.x > -w / 2f && local.x < w / 2f &&
               local.y > -h / 2f && local.y < h / 2f;
    }

    // --- Caret positioning ---

    private void UpdateCaretPosition()
    {
        if (textMesh == null || caretRenderer == null) return;

        textMesh.ForceMeshUpdate();
        var textInfo = textMesh.textInfo;

        float caretX;
        float lineAscender, lineDescender;

        if (textInfo.characterCount == 0)
        {
            // Empty text — temporarily insert a char to get real line metrics
            textMesh.text = "|";
            textMesh.ForceMeshUpdate();
            var tmpInfo = textMesh.textInfo;
            RectTransform rt = textMesh.GetComponent<RectTransform>();
            caretX = -rt.sizeDelta.x / 2f + 1f;
            if (tmpInfo.lineCount > 0)
            {
                lineAscender = tmpInfo.lineInfo[0].ascender;
                lineDescender = tmpInfo.lineInfo[0].descender;
            }
            else
            {
                lineAscender = 2f;
                lineDescender = -1f;
            }
            textMesh.text = "";
            textMesh.ForceMeshUpdate();
        }
        else if (caretPosition >= textInfo.characterCount)
        {
            // After last character — use xAdvance for reliable X position
            var lastChar = textInfo.characterInfo[textInfo.characterCount - 1];

            if (lastChar.character == '\n')
            {
                // After newline: caret goes to start of next line
                RectTransform rt = textMesh.GetComponent<RectTransform>();
                caretX = -rt.sizeDelta.x / 2f + 1f;
                // Use next line if it exists, otherwise estimate from current line
                int nextLine = lastChar.lineNumber + 1;
                if (nextLine < textInfo.lineCount)
                {
                    lineAscender = textInfo.lineInfo[nextLine].ascender;
                    lineDescender = textInfo.lineInfo[nextLine].descender;
                }
                else
                {
                    var curLine = textInfo.lineInfo[lastChar.lineNumber];
                    float lh = curLine.ascender - curLine.descender;
                    lineAscender = curLine.ascender - lh * 1.2f;
                    lineDescender = curLine.descender - lh * 1.2f;
                }
            }
            else
            {
                caretX = lastChar.xAdvance;
                int lineIdx = lastChar.lineNumber;
                lineAscender = textInfo.lineInfo[lineIdx].ascender;
                lineDescender = textInfo.lineInfo[lineIdx].descender;
            }
        }
        else
        {
            // Before character at caretPosition
            var charInfo = textInfo.characterInfo[caretPosition];
            caretX = charInfo.bottomLeft.x;
            int lineIdx = charInfo.lineNumber;
            lineAscender = textInfo.lineInfo[lineIdx].ascender;
            lineDescender = textInfo.lineInfo[lineIdx].descender;
        }

        float centerY = (lineAscender + lineDescender) / 2f;
        float height = lineAscender - lineDescender;

        // Check if caret would be outside the text area
        RectTransform textRt = textMesh.GetComponent<RectTransform>();
        float boxBottom = -textRt.sizeDelta.y / 2f;
        if (lineDescender < boxBottom)
        {
            caretRenderer.enabled = false;
            return;
        }
        // Ensure caret is visible if within bounds
        if (isEditing) caretRenderer.enabled = caretVisible;

        caretRenderer.transform.localPosition = new Vector3(caretX, centerY, 0);
        caretRenderer.transform.localScale = new Vector3(0.15f, height, 1f);
    }

    private void SetCaretFromMouse()
    {
        if (textMesh == null) return;

        textMesh.ForceMeshUpdate();
        var textInfo = textMesh.textInfo;
        if (textInfo.characterCount == 0)
        {
            caretPosition = 0;
            ResetCaretBlink();
            return;
        }

        // Get mouse position in text's local space
        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = textMesh.transform.position.z;
        Vector3 localMouse = textMesh.transform.InverseTransformPoint(mouseWorld);

        // Find the closest line first, then closest character on that line
        int bestLine = 0;
        float bestLineDist = float.MaxValue;
        for (int l = 0; l < textInfo.lineCount; l++)
        {
            var line = textInfo.lineInfo[l];
            float lineCenterY = (line.ascender + line.descender) / 2f;
            float dist = Mathf.Abs(localMouse.y - lineCenterY);
            if (dist < bestLineDist)
            {
                bestLineDist = dist;
                bestLine = l;
            }
        }

        // Find closest character edge on that line
        var bestLineInfo = textInfo.lineInfo[bestLine];
        float bestDist = float.MaxValue;
        int bestIndex = bestLineInfo.lastCharacterIndex + 1;

        for (int i = bestLineInfo.firstCharacterIndex; i <= bestLineInfo.lastCharacterIndex; i++)
        {
            var c = textInfo.characterInfo[i];
            float lineCenterY = (bestLineInfo.ascender + bestLineInfo.descender) / 2f;

            // Distance to left edge
            float dist = Mathf.Abs(localMouse.x - c.bottomLeft.x);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestIndex = i;
            }

            // Distance to right edge
            dist = Mathf.Abs(localMouse.x - c.topRight.x);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestIndex = i + 1;
            }
        }

        caretPosition = Mathf.Clamp(bestIndex, 0, textMesh.text.Length);
        ResetCaretBlink();
    }

    // --- Mouse events ---

    private void OnMouseEnter()
    {
        mouseIsOver = true;
        selector.over = gameObject;
        selector.overMono = this;
    }

    private void OnMouseExit()
    {
        mouseIsOver = false;
        selector.over = null;
        if (cursorOverridden && !isResizing)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            cursorOverridden = false;
        }
    }

    private void OnMouseDown()
    {
        if (isEditing)
        {
            ResizeEdge edge = DetectEdge();
            if (edge != ResizeEdge.None)
            {
                // Edge click during editing → exit edit, start resize
                StopEditing();
                camDrag.camDragOn = false;
                isResizing = true;
                activeEdge = edge;
                resizeStartMouse = cam.ScreenToWorldPoint(Input.mousePosition);
                resizeStartMouse.z = 0;
                resizeStartWidth = data.width;
                resizeStartHeight = data.height;
                resizeStartPos = transform.position;
            }
            else
            {
                // Click inside while editing → reposition caret
                SetCaretFromMouse();
                camDrag.camDragOn = false;
            }
            return;
        }

        edgeOnMouseDown = DetectEdge();
        camDrag.camDragOn = false;

        if (edgeOnMouseDown != ResizeEdge.None)
        {
            isResizing = true;
            activeEdge = edgeOnMouseDown;
            resizeStartMouse = cam.ScreenToWorldPoint(Input.mousePosition);
            resizeStartMouse.z = 0;
            resizeStartWidth = data.width;
            resizeStartHeight = data.height;
            resizeStartPos = transform.position;
        }
        else
        {
            // Start tracking for long-press vs short-click
            mouseDownInside = true;
            mouseDownScreenPos = Input.mousePosition;
            longPressTriggered = false;
        }
    }

    private void OnMouseDrag()
    {
        // Drag is handled in Update via long-press detection
    }

    private void OnMouseUp()
    {
        edgeOnMouseDown = ResizeEdge.None;
        camDrag.camDragOn = true;

        if (isDragging)
        {
            isDragging = false;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            transform.position = GridSnap.Snap(transform.position);
        }
        else if (mouseDownInside && !longPressTriggered && !isResizing)
        {
            // Short click → enter edit mode
            StartEditing();
        }

        mouseDownInside = false;
        longPressTriggered = false;
    }

    private void OnMouseOver() { }

    // --- Resize ---

    private void HandleResize()
    {
        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;
        Vector3 delta = mouseWorld - resizeStartMouse;

        float newWidth = resizeStartWidth;
        float newHeight = resizeStartHeight;

        bool right = activeEdge == ResizeEdge.Right || activeEdge == ResizeEdge.TopRight || activeEdge == ResizeEdge.BottomRight;
        bool left = activeEdge == ResizeEdge.Left || activeEdge == ResizeEdge.TopLeft || activeEdge == ResizeEdge.BottomLeft;
        bool top = activeEdge == ResizeEdge.Top || activeEdge == ResizeEdge.TopLeft || activeEdge == ResizeEdge.TopRight;
        bool bottom = activeEdge == ResizeEdge.Bottom || activeEdge == ResizeEdge.BottomLeft || activeEdge == ResizeEdge.BottomRight;

        if (right) newWidth = resizeStartWidth + delta.x;
        if (left) newWidth = resizeStartWidth - delta.x;
        if (top) newHeight = resizeStartHeight + delta.y;
        if (bottom) newHeight = resizeStartHeight - delta.y;

        float grid = GridSnap.gridSize;
        newWidth = Mathf.Max(minWidth, Mathf.Round(newWidth / grid) * grid);
        newHeight = Mathf.Max(minHeight, Mathf.Round(newHeight / grid) * grid);

        float widthDiff = newWidth - resizeStartWidth;
        float heightDiff = newHeight - resizeStartHeight;

        Vector3 newPos = resizeStartPos;
        if (right) newPos.x = resizeStartPos.x + widthDiff / 2f;
        if (left) newPos.x = resizeStartPos.x - widthDiff / 2f;
        if (top) newPos.y = resizeStartPos.y + heightDiff / 2f;
        if (bottom) newPos.y = resizeStartPos.y - heightDiff / 2f;

        data.width = newWidth;
        data.height = newHeight;
        transform.position = newPos;
        UpdateVisual();
    }

    // --- Edge detection ---

    private ResizeEdge DetectEdge()
    {
        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;
        Vector3 local = transform.InverseTransformPoint(mouseWorld);

        float w = data.width;
        float h = data.height;
        float margin = 1.5f;

        bool nearRight  = Mathf.Abs(local.x - w / 2f) < margin;
        bool nearLeft   = Mathf.Abs(local.x + w / 2f) < margin;
        bool nearTop    = Mathf.Abs(local.y - h / 2f) < margin;
        bool nearBottom = Mathf.Abs(local.y + h / 2f) < margin;

        bool inX = local.x > -w / 2f - margin && local.x < w / 2f + margin;
        bool inY = local.y > -h / 2f - margin && local.y < h / 2f + margin;

        if (!inX || !inY) return ResizeEdge.None;

        if (nearRight && nearTop) return ResizeEdge.TopRight;
        if (nearRight && nearBottom) return ResizeEdge.BottomRight;
        if (nearLeft && nearTop) return ResizeEdge.TopLeft;
        if (nearLeft && nearBottom) return ResizeEdge.BottomLeft;

        if (nearRight) return ResizeEdge.Right;
        if (nearLeft) return ResizeEdge.Left;
        if (nearTop) return ResizeEdge.Top;
        if (nearBottom) return ResizeEdge.Bottom;

        return ResizeEdge.None;
    }

    private void SetResizeCursor(ResizeEdge edge)
    {
        Texture2D cursor = null;
        switch (edge)
        {
            case ResizeEdge.Right:
            case ResizeEdge.Left:
                cursor = cursorEW;
                break;
            case ResizeEdge.Top:
            case ResizeEdge.Bottom:
                cursor = cursorNS;
                break;
            case ResizeEdge.TopLeft:
            case ResizeEdge.BottomRight:
                cursor = cursorNWSE;
                break;
            case ResizeEdge.TopRight:
            case ResizeEdge.BottomLeft:
                cursor = cursorNESW;
                break;
        }

        if (cursor != null)
            Cursor.SetCursor(cursor, resizeCursorHotspot, CursorMode.Auto);
    }

    // --- Text editing ---

    private void StartEditing()
    {
        isEditing = true;
        border.color = borderEditColor;
        inner.color = fillEditColor;

        if (textMesh.text == "Text...")
            textMesh.text = "";

        // Position caret near click point
        SetCaretFromMouse();
        caretBlinkTimer = 0f;
        caretVisible = true;
        if (caretRenderer != null) caretRenderer.enabled = true;
    }

    private void StopEditing()
    {
        if (!isEditing) return;
        isEditing = false;
        border.color = borderColor;
        inner.color = fillColor;

        if (caretRenderer != null) caretRenderer.enabled = false;

        if (string.IsNullOrEmpty(textMesh.text))
            textMesh.text = "Text...";

        data.text = textMesh.text;
    }

    private void OnGUI()
    {
        if (!isEditing) return;

        Event e = Event.current;
        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.Escape)
            {
                StopEditing();
                e.Use();
                return;
            }

            // Arrow keys to move caret
            if (e.keyCode == KeyCode.LeftArrow)
            {
                caretPosition = Mathf.Max(0, caretPosition - 1);
                ResetCaretBlink();
                e.Use();
                return;
            }
            if (e.keyCode == KeyCode.RightArrow)
            {
                caretPosition = Mathf.Min(textMesh.text.Length, caretPosition + 1);
                ResetCaretBlink();
                e.Use();
                return;
            }
            if (e.keyCode == KeyCode.Home)
            {
                caretPosition = 0;
                ResetCaretBlink();
                e.Use();
                return;
            }
            if (e.keyCode == KeyCode.End)
            {
                caretPosition = textMesh.text.Length;
                ResetCaretBlink();
                e.Use();
                return;
            }

            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                if (!WouldOverflow("\n"))
                {
                    textMesh.text = textMesh.text.Insert(caretPosition, "\n");
                    caretPosition++;
                    OnTextModified();
                }
                e.Use();
                return;
            }

            if (e.keyCode == KeyCode.Backspace)
            {
                if (caretPosition > 0 && textMesh.text.Length > 0)
                {
                    textMesh.text = textMesh.text.Remove(caretPosition - 1, 1);
                    caretPosition--;
                    OnTextModified();
                }
                e.Use();
                return;
            }

            if (e.keyCode == KeyCode.Delete)
            {
                if (caretPosition < textMesh.text.Length)
                {
                    textMesh.text = textMesh.text.Remove(caretPosition, 1);
                    OnTextModified();
                }
                e.Use();
                return;
            }

            if (e.character != '\0' && e.character != '\n' && e.character != '\r'
                && e.character != '\t' && !char.IsControl(e.character))
            {
                if (!WouldOverflow(e.character.ToString()))
                {
                    textMesh.text = textMesh.text.Insert(caretPosition, e.character.ToString());
                    caretPosition++;
                    OnTextModified();
                }
                e.Use();
            }
        }
    }

    /// Checks if inserting text at caretPosition would place the caret outside the box.
    private bool WouldOverflow(string insertion)
    {
        string original = textMesh.text;
        string test = original.Insert(caretPosition, insertion);
        textMesh.text = test;
        textMesh.ForceMeshUpdate();

        var textInfo = textMesh.textInfo;
        int testCaretPos = caretPosition + insertion.Length;
        RectTransform textRt = textMesh.GetComponent<RectTransform>();
        float boxBottom = -textRt.sizeDelta.y / 2f;

        bool overflow = false;
        if (testCaretPos < textInfo.characterCount)
        {
            var c = textInfo.characterInfo[testCaretPos];
            int lineIdx = c.lineNumber;
            if (textInfo.lineInfo[lineIdx].descender < boxBottom)
                overflow = true;
        }
        else if (textInfo.characterCount > 0)
        {
            var lastChar = textInfo.characterInfo[textInfo.characterCount - 1];
            int lineIdx = lastChar.lineNumber;
            if (lastChar.character == '\n')
            {
                // New line after newline — check if next line would fit
                float lineH = textInfo.lineInfo[lineIdx].ascender - textInfo.lineInfo[lineIdx].descender;
                if (textInfo.lineInfo[lineIdx].descender - lineH < boxBottom)
                    overflow = true;
            }
            else if (textInfo.lineInfo[lineIdx].descender < boxBottom)
            {
                overflow = true;
            }
        }

        // Restore original text
        textMesh.text = original;
        textMesh.ForceMeshUpdate();
        return overflow;
    }

    private void OnTextModified()
    {
        data.text = textMesh.text;
        ResetCaretBlink();
    }

    private void ResetCaretBlink()
    {
        caretBlinkTimer = 0f;
        caretVisible = true;
    }

    // --- IObjectMono ---

    public CircuitComponent GetMain() { return data; }

    public void SaveTransform()
    {
        data.SaveTransform(transform);
    }

    public void Clear()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        projectManager.removeFromProject(this);
    }
}

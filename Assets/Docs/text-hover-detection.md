# Text Hover Detection — Investigation Notes

## Goal
Detect when the mouse hovers directly over rendered text characters in a world-space `TextMeshPro` component, to distinguish between:
- **Over text** → show edit cursor, single click enters edit mode
- **Over empty inner area** → allow body drag
- **Over border edge** → show resize cursor

## What we tried

### 1. `TMP_TextUtilities.FindIntersectingCharacter`
```csharp
int charIndex = TMP_TextUtilities.FindIntersectingCharacter(textMesh, Input.mousePosition, cam, true);
```
- Should work with world-space TMP + `Camera.main` per documentation and TMP examples
- Always returns -1 in our case
- Tried both `visibleOnly: true` and `false`

### 2. `TMP_TextUtilities.FindIntersectingLine`
- Also returns -1

### 3. Manual character bounds check
```csharp
Vector3 localMouse = textMesh.transform.InverseTransformPoint(mouseWorld);
// Check against charInfo.bottomLeft / topRight for each visible character
```
- Also doesn't match — likely a coordinate space issue between the mouse position and the character bounds
- The TMP text object is a child of the TextAnnotation at localPosition (0,0,0)
- RectTransform sizeDelta is set to the text area dimensions
- `ForceMeshUpdate()` is called before checking

### Possible root causes
- The RectTransform pivot/anchor setup on the dynamically created TMP may not match what the intersection methods expect
- The text is created programmatically (not from a prefab), so the RectTransform may have unexpected defaults
- World-space TMP character bounds may be in a different local space than expected

## User's proposed workaround
Instead of detecting exact text glyph hover, use a simpler approach:
- Check if mouse is within the **text content bounds** (the area where text has been rendered)
- Use `textMesh.textBounds` or `textMesh.GetRenderedValues()` to get the actual rendered text size
- If mouse is within that rect → treat as "over text"
- If mouse is inside the box but outside the text bounds → treat as "over empty area" (allow drag)

This gives a good-enough distinction: the text area (top-left region where text is rendered) triggers edit mode, while empty space below/right of the text allows dragging.

## TODO
- Try the workaround approach (textBounds-based detection)
- Set up a proper text edit cursor icon (user will provide)
- Remove debug logs once working
- Investigate RectTransform setup if exact glyph detection is needed later

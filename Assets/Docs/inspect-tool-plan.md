# Inspect Tool — Implementation Plan

**Goal:** Click a placed CustomChip in Sandbox → a floating viewport opens showing the internal circuit rendered live with signals flowing in real time.

---

## Architecture Overview

```
Sandbox (main camera)                  Off-screen region (x+10000)
┌──────────────────────┐               ┌──────────────────────┐
│  [AND]──[MyChip]──>  │               │  Internal circuit of │
│          ↑ click     │               │  MyChip, fully       │
│                      │               │  rendered as real     │
│  ┌─────────────┐     │               │  GameObjects          │
│  │ RawImage UI │◄────────────────────│  ← inspectCamera     │
│  │ (viewport)  │     │  RenderTexture│                      │
│  └─────────────┘     │               └──────────────────────┘
└──────────────────────┘
```

The internal circuit already exists as pure-data objects (no GameObjects). The inspect tool will:
1. Call `Load()` on each internal component at an off-screen position to create real GameObjects
2. Point a second orthographic camera at them, rendering to a RenderTexture
3. Display the RenderTexture in a draggable UI panel
4. The internal components share the same Timer, so signals flow live

---

## Step-by-step Implementation

### Step 1: InspectPanel prefab (UI)

Create a Canvas-based UI panel prefab with:
- **RawImage** — displays the RenderTexture
- **Title bar** — shows chip name, has a close button (X)
- **Drag handle** — title bar is draggable (implement `IDragHandler`)
- **Resize handle** — bottom-right corner for resizing (optional, can defer)

The panel lives in Screen Space — Overlay canvas so it floats above everything.

**Files:**
- `Assets/Resources/Prefabs/InspectPanel.prefab` — the UI prefab
- `Assets/Scripts/GeneralPurpose/InspectPanel.cs` — MonoBehaviour for the panel

### Step 2: ChipInspector class

Core class that manages the lifecycle of an inspection session.

```
ChipInspector
├── targetChip         : CustomChip (the chip being inspected)
├── inspectCamera      : Camera (second ortho camera, renders to RT)
├── renderTexture      : RenderTexture (piped to UI RawImage)
├── spawnedObjects     : List<GameObject> (cleanup tracking)
├── inspectRoot        : GameObject (parent for all spawned objects)
├── offsetPosition     : Vector3 (x+10000 to avoid overlap)
│
├── Open(CustomChip chip)
│   1. Create inspectRoot at offsetPosition
│   2. Deserialize chip's internalCircuit JSON
│   3. For each CircuitComponent: call Load() (creates GameObjects)
│   4. Offset all spawned transforms by offsetPosition
│   5. Create inspectCamera pointing at the region
│   6. Create RenderTexture, assign to camera and UI panel
│   7. Wire external pin values → internal ChipInputNodes (live feed)
│
├── Close()
│   1. Destroy all spawnedObjects
│   2. Destroy inspectCamera
│   3. Release RenderTexture
│   4. Destroy inspectRoot
│   5. Destroy UI panel
│
└── UpdateLiveSignals()  (called each tick or via events)
    Forward current external input values to internal ChipInputNodes
```

**File:** `Assets/Scripts/GeneralPurpose/ChipInspector.cs`

### Step 3: Triggering the inspect

Since there's no toolbar/tool-mode system yet, use the simplest trigger:

**Option:** Add an "Inspect" button to the component's right-click context menu or to the `Draggable2D` hover description. When `CustomChip.OpenEditor()` is called (currently empty), open the inspector instead.

Concretely:
- `CustomChip.OpenEditor()` → `ChipInspector.Open(this)`
- This method is already wired up from the component interaction system (the base class has it as `abstract void OpenEditor()`)

### Step 4: Off-screen rendering setup

**Camera configuration:**
- New `GameObject("InspectCamera")` with `Camera` component
- `orthographic = true`
- `cullingMask` = a dedicated layer (e.g. "Inspect") so it doesn't render Sandbox objects
- `targetTexture` = the RenderTexture
- All spawned internal GameObjects are assigned to the "Inspect" layer

**Why a dedicated layer:** Prevents the main camera from rendering inspect objects and vice versa. Both cameras can coexist without visual bleed.

**RenderTexture:** Start at 512x512 or match panel size. Recreate on panel resize.

### Step 5: Loading internal circuit as GameObjects

This is the core challenge. Currently `InitInternalCircuit()` deserializes components as pure data. We need a variant that also calls `Load()` on each component to create GameObjects.

```csharp
public List<GameObject> LoadInternalCircuitVisual(Vector3 offset)
{
    // 1. Deserialize internalCircuit JSON → List<CircuitComponent>
    // 2. For each component:
    //    a. Offset component.pos by offset
    //    b. Call component.Load() → creates GameObject, registers with ProjectManager
    //    c. Track the GameObject for cleanup
    // 3. Return list of spawned GameObjects
}
```

**Key concern — ProjectManager registration:** `Load()` calls `ProjectManager.addToProject()`. The inspect objects should NOT be added to the main project. Solutions:
- **A)** Add a static `bool suppressRegistration` flag to ProjectManager — set it true during inspect loading
- **B)** Use a separate ProjectManager instance on the inspect root — but `Load()` finds PM via `FindWithTag`, so this is harder
- **C)** After loading, immediately remove inspect objects from the main PM's list

**Recommendation:** Option A is simplest — a static flag checked in `addToProject()`.

### Step 6: Live signal forwarding

The inspect view should show signals flowing. Two approaches:

**Approach A — Event mirroring (recommended):**
Subscribe to the target CustomChip's external input pins. When they fire `NewDataEvent`, forward the value to the corresponding internal ChipInputNode in the visual circuit. The visual circuit's Timer subscriptions handle the rest.

**Approach B — Shared internal circuit:**
Don't create a second internal circuit. Instead, make the *existing* pure-data internal circuit create GameObjects. This is cleaner but means modifying `InitInternalCircuit()` significantly.

**Recommendation:** Approach A. Keep the pure-data circuit untouched. The visual inspect circuit is a parallel copy that receives the same inputs.

### Step 7: Camera controls for the inspect viewport

The inspect panel needs its own pan/zoom within the RenderTexture viewport:
- Detect mouse events on the RawImage
- Translate mouse delta → camera position changes (pan)
- Scroll wheel on the panel → camera orthographicSize changes (zoom)
- Auto-center on open using the same centroid logic as `ProjectManager.FindCenter()`

---

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| Timer double-subscription | Internal components subscribe in constructor during deserialization. Loading visual copies = double tick handlers for same logic | The visual circuit is a *separate* deserialization — different object instances. Each has its own Timer subscription. No conflict. |
| ProjectManager pollution | `Load()` registers objects with main PM | Static `suppressRegistration` flag during inspect loading |
| Performance (large chips) | Rendering a complex chip twice (data + visual) | Lazy — only render on demand. Destroy on close. Could add component count warning. |
| Nested CustomChips in inspect | Nested chips also need visual loading | Recursive — `LoadInternalCircuitVisual` handles nested chips the same way |
| Layer exhaustion | Unity has 32 layers max | Use one shared "Inspect" layer for all inspect sessions |

---

## File Summary

| File | Type | Purpose |
|------|------|---------|
| `Scripts/GeneralPurpose/ChipInspector.cs` | New | Core inspect logic: open/close/signal forwarding |
| `Scripts/GeneralPurpose/InspectPanel.cs` | New | UI panel: drag, resize, close, camera controls |
| `Resources/Prefabs/InspectPanel.prefab` | New | UI prefab with RawImage + title bar |
| `Scripts/ComponentClasses/CustomComponent/CustomChip.cs` | Modified | `OpenEditor()` → opens inspector |
| `Scripts/GeneralPurpose/ProjectManager.cs` | Modified | `suppressRegistration` flag |

---

## Implementation Order

1. **InspectPanel.prefab + InspectPanel.cs** — Get the UI shell working with a placeholder texture
2. **ChipInspector.cs** — Off-screen loading + camera + RenderTexture pipeline
3. **ProjectManager.cs** — Add `suppressRegistration` flag
4. **CustomChip.cs** — Wire `OpenEditor()` to `ChipInspector.Open()`
5. **Live signals** — Event mirroring from external pins to visual circuit
6. **Viewport controls** — Pan/zoom within the inspect panel
7. **Polish** — Auto-center, title, close cleanup, layer setup

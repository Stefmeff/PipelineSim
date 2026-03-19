# Text Annotation — Design Specification

## 1. Toolbar Button (ButtonText)

| Trigger | Action |
|---|---|
| Click text button | Activate placement mode: button highlights (blue), cursor switches to pen |
| Click in sandbox (empty space) | Place text annotation at grid-snapped position, deactivate |
| Click on UI (menu, toolbar, etc.) | Cancel placement, deactivate |
| Right-click or Escape | Cancel placement, deactivate |

"Deactivate" means: reset cursor to default, button color to default.

## 2. Text Box — Visual

- **Border**: thin (0.5 unit) outline, subtle gray (`#78858D`), visible but not dominant
- **Border hover**: lighter gray (`#909DA5`) + resize cursor when over an edge/corner
- **Background**: semi-transparent dark fill — grid lines visible through it
- **Text**: plain white, left-aligned, top-aligned, word-wrapping enabled
- **Sorting**: background behind components, text above background

## 3. Text Box — Sizing

- **Default size**: fits on grid (e.g. 30×15 units)
- **Minimum size**: ~2×1 grid cells (10×5 units)
- **Maximum size**: none (grows indefinitely)
- **Size snaps to grid**: width and height are always multiples of grid size (5)
- **Auto-grow** (during text editing):
  - Typing expands width when text hits the right edge (snaps to next grid increment)
  - New lines expand height downward (snaps to next grid increment)
  - Box never auto-shrinks — only manual resize reduces size

## 4. Text Box — Interaction

| Action | Behavior |
|---|---|
| **Drag body** | Move the text box (via Draggable2D), snaps to grid on release |
| **Drag border edge** | Resize in that direction, snaps to grid on release |
| **Drag corner** | Resize both axes, snaps to grid on release |
| **Double-click body** | Enter text editing mode |
| **Click outside text box** | Exit editing mode |
| **Escape or Enter** | Exit editing mode |
| **Backspace** | Delete last character |
| **Typing** | Append characters, auto-grow box if needed |

## 5. Resize Cursors

| Edge/Corner | Cursor |
|---|---|
| Right edge | `resize_ew` |
| Left edge | `resize_ew` |
| Bottom edge | `resize_ns` |
| Top edge | `resize_ns` |
| Bottom-right corner | `resize_nwse` |
| Top-left corner | `resize_nwse` |
| Bottom-left corner | `resize_nesw` |
| Top-right corner | `resize_nesw` |
| Body (no edge) | default cursor |

Border highlights to hover color (`#909DA5`) when mouse is over any edge or corner.

## 6. Persistence

- Saved and loaded with project like any other component
- Serialized fields: position, rotation, text content, width, height
- Registered with ProjectManager via `IObjectMono`

## 7. Implementation Notes

- Prefab: `Resources/Prefabs/TextAnnotation`
- Data class: `TextAnnotation` (extends `CircuitComponent`)
- MonoBehaviour: `TextAnnotation_Mono` (implements `IObjectMono`)
- Button: `ButtonText` loads prefab from Resources (no Inspector assignment needed)
- Placement position and box dimensions always grid-snapped (multiples of `GridSnap.gridSize`)

# Custom Components — Design Plan

## Overview

Allow users to design reusable sub-circuits ("chips") in a dedicated editor, save them, and place them as single blocks in the main circuit. An inspect tool lets users view the inner circuit of any placed custom component in real-time.

---

## Chip Creator Editor

### Entry Point
- New button in top menu: **"Chip Creator"**
- Opens a **separate Unity scene** (`ChipEditor.unity`)
- Main scene state is preserved (SceneManager handles this)

### Editor Layout
- **Left bar**: Input pin panel — user adds input pins for the chip
- **Right bar**: Output pin panel — user adds output pins for the chip
- **Center**: Standard workspace (grid, camera, drag-and-drop) — same interaction as Sandbox
- **Top bar**: Chip name field, color picker, Save button, Back button

### Pin Panel UI
Each pin entry in the left/right bar:
```
[Drag handle] [Name field] [Bit-width spinner (1-16)] [Delete button]
```
- Drag handle for vertical reordering
- Name defaults to "In0", "In1" / "Out0", "Out1" etc.
- Each pin gets a **stable GUID** (`System.Guid.NewGuid().ToString()`) assigned on creation
- GUID is used internally for wire binding — renaming or reordering doesn't break connections

### Available Components Inside Editor
Everything from the main editor:
- **Combinational**: AND, OR, XOR, Inverter, MullerC
- **Sequential**: FlipFlop, Latch, CPLatch, Delay, Delay2
- **I/O**: Switch, LED (useful for debugging in inspect view)
- **Bus**: Splitter, Merger
- **Sources**: Clock, DataSource
- **Custom components**: other saved chips (nesting supported)

### Interface Pins in the Workspace
- Input pins from the left bar appear as **source nodes on the left edge** of the workspace
  - Behave like OutputPins internally (they output data into the sub-circuit)
- Output pins from the right bar appear as **sink nodes on the right edge**
  - Behave like InputPins internally (they receive data from the sub-circuit)
- Visual: small labeled blocks pinned to the edge, not freely draggable (only vertical reorder via the panel)

---

## Saving & Storage

### File Format
- JSON using same Newtonsoft.Json settings as `.pip` files
- File extension: `.chip`
- Stored in: `Application.persistentDataPath + "/CustomComponents/"`

### Chip Definition Schema
```json
{
  "chipId": "guid",
  "name": "4-bit Adder",
  "color": "#4A90D9",
  "interfacePins": {
    "inputs": [
      { "id": "guid", "name": "A", "width": 4, "order": 0 },
      { "id": "guid", "name": "B", "width": 4, "order": 1 },
      { "id": "guid", "name": "Cin", "width": 1, "order": 2 }
    ],
    "outputs": [
      { "id": "guid", "name": "Sum", "width": 4, "order": 0 },
      { "id": "guid", "name": "Cout", "width": 1, "order": 1 }
    ]
  },
  "internalCircuit": {
    "components": [ ... ],
    "wires": [ ... ]
  }
}
```

### Chip ID
- Each chip definition gets a stable `chipId` (GUID)
- Placed instances reference this `chipId`
- Enables update propagation when chip is edited

---

## Placing Custom Components

### Component Bar Integration
- New category in drag-and-drop bar: **"Custom"**
- On startup / after save: scan `CustomComponents/` folder, populate entries
- Each entry shows chip name with the user-chosen color as background

### Placed Instance Appearance
- Rectangular block, colored with user-chosen chip color
- Chip name displayed in center
- Input pins on left edge, output pins on right edge
- Pin labels shown next to each pin
- Size scales with pin count (height) — width is fixed

### Prefab
- Single `CustomComponent.prefab` used for all custom chips
- `CustomComponentMono.cs` handles:
  - Loading chip definition from file
  - Dynamically creating input/output pin GameObjects
  - Positioning pins vertically based on order

### Simulation
- Internally expands the sub-circuit in memory (not visually in the main scene)
- Interface input pins feed data into the internal circuit's source nodes
- Internal circuit simulates normally (all delays, timing, handshaking preserved)
- Internal output nodes feed results back to the interface output pins
- Each placed instance has its **own internal state** (independent simulation)

---

## Editing Existing Chips

### Entry Point
- Right-click custom component in the component bar → "Edit"
- Or: right-click a placed instance → "Edit Chip Definition"
- Opens Chip Creator scene with the chip loaded

### Update Propagation (on save)
When user saves an edited chip:

1. Compare new interface with old interface (by pin GUID)
2. Find all placed instances of this chip in the current project
3. Apply rules:

| Change | Action |
|--------|--------|
| No interface change | Update all instances silently |
| Pin removed | Delete wires connected to that pin on all instances |
| Pin added | New pin appears on instances, unwired |
| Pin renamed | Label updates, wires stay (bound by GUID) |
| Pin reordered | Visual position updates, wires stay (bound by GUID) |
| Bit-width changed | Wires stay, runtime error via InformationWindow on width mismatch |
| Internal circuit changed | All instances get new internals, simulation state resets |

---

## Inspect Tool

### Activation
- New tool mode: **Inspect** (button in toolbar, or keyboard shortcut)
- Click on a placed custom component while in Inspect mode
- Or: right-click placed instance → "Inspect"

### Inspect Window
- Panel appears in **top-right corner** of the screen
- Shows the internal circuit of the selected instance rendered live
- Implementation: **RenderTexture approach**
  - Internal circuit GameObjects exist at an off-screen position (e.g., y = -10000)
  - Secondary camera renders only the "InspectLayer"
  - Camera output → RenderTexture → UI RawImage in the inspect panel

### Live Signal Visualization
- Since internal components are real simulating objects, token colors and wire states update automatically
- Input pins on left edge light up as signals arrive from the main circuit
- Signals propagate through internal gates visually
- Output pins on right edge show results flowing back out
- No extra work needed — existing Wire_Mono coloring and token visualization handles this

### Inspect Window Controls
- **Close button** (X)
- **Instance label**: shows which component is being inspected (chip name + instance identifier)
- **Zoom**: scroll wheel within the panel adjusts the secondary camera's orthographic size
- **Pan**: click-drag within the panel moves the secondary camera
- Only one inspect window open at a time (clicking another component switches)

---

## ComponentMono Integration

### Enum Extension
```csharp
private enum componentType {
    AND, OR, XOR, MullerC, Inverter,
    Delay, Delay2, CPLatch, FlipFlop, Latch,
    Clock, DataSource, Splitter, Merger, CustomComponent
};
```

### CustomComponent Class
New class: `Assets/Scripts/ComponentClasses/CustomComponent/CustomComponent.cs`
- Extends `CircuitComponent`
- Fields:
  - `string chipId` — reference to chip definition
  - `string chipName`
  - `Color chipColor`
  - `List<InterfacePin> interfaceInputs`
  - `List<InterfacePin> interfaceOutputs`
  - Internal: `List<CircuitComponent> internalComponents`
  - Internal: `List<Wire> internalWires`

### CustomComponentMono
New MonoBehaviour: `Assets/Scripts/ComponentMonobehaviours/CustomComponentMono.cs`
- Handles visual representation (colored block, pin labels)
- Manages internal circuit GameObjects (off-screen for inspect)
- Routes data between external pins and internal source/sink nodes

---

## Serialization in Projects

When saving a `.pip` project that contains custom components:
- The placed instance stores the `chipId` reference (not the full definition)
- On load: looks up chip definition from `CustomComponents/` folder by `chipId`
- If chip file is missing: show warning, place component as "broken" (greyed out, no simulation)

### Sharing Projects
- To share a project that uses custom chips, user must also share the `.chip` files
- Future: could embed chip definitions inline in the `.pip` file for portability

---

## Nesting & Recursion

### Nesting
- Custom components can contain other custom components
- Inspect window shows one level — click on a nested custom component in the inspect view to drill down (replaces inspect view content)
- Breadcrumb trail at top of inspect panel: `Main > ALU > Adder > FullAdder`

### Recursion Prevention
- When opening Chip Creator for chip X, chip X is **not available** in the component bar
- Transitive: if chip A contains chip B, then when editing chip B, chip A is also unavailable
- Check dependency graph on save to prevent circular references

---

## Implementation Order

```
Step 1: Chip definition schema + save/load (.chip files)
Step 2: Chip Creator scene (basic — workspace + pin panels + save)
Step 3: Interface pin nodes in editor (source/sink at edges)
Step 4: CustomComponent class + Mono (place in main circuit, static block)
Step 5: Internal circuit expansion + simulation routing
Step 6: Component bar "Custom" category (scan folder, populate)
Step 7: Edit existing chips (load into editor, update propagation)
Step 8: Inspect tool (RenderTexture, secondary camera, live view)
Step 9: Nesting support + recursion prevention
Step 10: Polish (breadcrumb navigation, broken chip warnings, sharing)
```

---

## Key Files (to create)

| File | Purpose |
|------|---------|
| `Assets/Scenes/ChipEditor.unity` | Chip editor scene |
| `Assets/Scripts/ComponentClasses/CustomComponent/CustomComponent.cs` | Logic class |
| `Assets/Scripts/ComponentClasses/CustomComponent/ChipDefinition.cs` | Serialization schema |
| `Assets/Scripts/ComponentClasses/CustomComponent/InterfacePin.cs` | Pin definition |
| `Assets/Scripts/ComponentMonobehaviours/CustomComponentMono.cs` | Visual + lifecycle |
| `Assets/Scripts/GeneralPurpose/ChipEditor/ChipEditorManager.cs` | Editor scene controller |
| `Assets/Scripts/GeneralPurpose/ChipEditor/PinPanelUI.cs` | Left/right pin bar UI |
| `Assets/Scripts/GeneralPurpose/UIComponents/InspectWindow.cs` | Inspect panel |
| `Assets/Resources/Prefabs/CustomComponent.prefab` | Placed instance prefab |

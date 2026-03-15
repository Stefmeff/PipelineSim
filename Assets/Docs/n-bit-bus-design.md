# N-Bit Bus Wire System — Design Plan

## Overview

Extend PipelineSim to support multi-bit (bus) wires alongside existing single-bit wires. This enables building larger circuits without needing one wire per bit. Sequential elements work with any width; combinational logic requires matching widths via custom components.

---

## Phase 1: Token & Pin Width Support

### 1.1 Extend BitToken to support n-bit
- Add `int width` field (default 1 for backwards compatibility)
- Replace `bool value` with `bool[] values` internally
- Keep existing API working: `GetValue()` returns `values[0]` for single-bit
- Add new API: `GetValues()`, `GetWidth()`, `SetValues(bool[])`
- Serialization: `[JsonProperty] int width`, `[JsonProperty] bool[] values`

### 1.2 Add width to InputPin / OutputPin
- Add `[JsonProperty] int width = 1` to both pin classes
- Width is set at construction or via editor
- Pins display their width visually (thicker dot or label)

### 1.3 Width validation on wire connection
- When connecting a wire: check `outputPin.width == inputPin.width`
- If mismatch: reject connection, show error via InformationWindow
- Single-bit (width=1) is just the default case — no special handling

#### Error handling
- Extend `InformationWindow` with a static `Show(string message)` method:
  - Finds the InformationWindow GameObject
  - Sets a child TextMeshProUGUI text to the message
  - Activates the window (which triggers pause via OnEnable)
- Error messages:
  - Width mismatch: `"Cannot connect: wire carries {n}-bit signal but pin expects {m}-bit"`
  - Bus to basic gate: `"Cannot connect: {gate} only accepts 1-bit signals"`
- Wire connection is rejected — wire is not created
- Validation happens in `Wire_Mono.drawWire()` where `connectionHandler.possibleConnection` is checked

#### Existing error systems (reference)
1. **Per-component TextMeshPro** — `errorMessage.text = "SETUP!"` on FlipFlop/Latch/CPLatch for timing violations
2. **InformationWindow** — global UI popup (starts inactive), pauses simulation on enable, restarts on disable. Currently has no dynamic message method.

---

## Phase 2: Visual Bus Notation on Wires

### 2.1 Bus indicator on wire segments
- For each straight segment of a wire (between two knots/pins):
  - Draw a short diagonal slash (/) across the midpoint
  - Display the bit width number above the slash
- Only show for width > 1 (single-bit wires look unchanged)

### 2.2 Implementation approach
- In `Wire_Mono.UpdateLineRenderer()`: after drawing the line, calculate midpoints of each segment
- Create small UI elements (TextMeshPro or sprite) at midpoints
- Update positions when wire moves
- Consider: create once, reposition in Update, destroy on wire delete

### 2.3 Visual wire thickness
- Bus wires (width > 1): thicker LineRenderer width
- Single-bit wires: current thin width
- `lineRend.startWidth` / `lineRend.endWidth` based on `wire.width`

### 2.4 Wire coloring rules

#### Current behavior (single-bit)
- `BitToken` has a `color` (default red) and a `value` (bool)
- `ActiveColor()` returns **token color** if value=1, **grey** (0xE0E0E0) if value=0
- `Wire.SetValue()` sets `wire.coloring = data.ActiveColor()`
- `Wire_Mono.Update()` applies coloring to LineRenderer every frame
- Result: wire = token color when high, grey when low

#### New behavior for n-bit buses
| Wire state | Color |
|-----------|-------|
| **1-bit, value=1** | Token color (red, etc.) — unchanged |
| **1-bit, value=0** | Grey — unchanged |
| **n-bit, uninitialized** | Grey — no data flowing yet |
| **n-bit, all bits=0** | Dimmed token color — data is flowing, value happens to be zero |
| **n-bit, any bit=1** | Full token color — active data |

#### Rationale
- Grey means **"no data / uninitialized"**, not "value is zero"
- A bus carrying `0000` is valid data — should look different from an empty wire
- Dimmed color for all-zeros distinguishes it from both "no data" and "has ones"
- Single-bit wires keep their existing behavior for backwards compatibility

#### Implementation
- `ActiveColor()` on n-bit token:
  - If uninitialized: return `low` (grey)
  - If all values are false: return dimmed version of `color` (e.g. 50% alpha or blended with grey)
  - If any value is true: return `color`

---

## Phase 3: Update DataSource

### 3.1 Single output pin with configurable width
- DataSource gets one OutputPin instead of up to 8
- Add width selector in DataSourceEditor (dropdown or input field: 1-16 bits)
- Token input UI changes: instead of 1/0 per bit, enter binary string or hex value
- Each token in the queue becomes an n-bit value

### 3.2 DataSource editor changes
- Show width selector
- Token display: show binary value (e.g., "1010" for 4-bit)
- Add/remove tokens still works, but each token is now n-bit

---

## Phase 4: Sequential Element Compatibility

### 4.1 FlipFlop, Latch, CPLatch — no logic changes needed
- They store and forward tokens without inspecting bit content
- Timing behavior (setup/hold/delay) is independent of width
- Just need to pass through the full BitToken (already n-bit from Phase 1)

### 4.2 Pin width auto-matching (optional)
- When an n-bit wire connects to a sequential element input:
  - The element's output pin automatically adopts the same width
  - Or: sequential element pins have width=0 meaning "any width"
- Alternative: user must set width manually on sequential elements

---

## Phase 5: Bus Splitter & Merger Components

### 5.1 Splitter component
- Input: one n-bit pin
- Output: n single-bit pins
- Configurable n (1-16)
- Each output carries one bit from the bus
- Pin labels: bit[0], bit[1], ... bit[n-1]

### 5.2 Merger component
- Input: n single-bit pins
- Output: one n-bit pin
- Configurable n (1-16)
- Combines individual bits into a bus token
- Pin labels: bit[0], bit[1], ... bit[n-1]

### 5.3 These bridge between bus world and gate world
- Use splitter to break a bus into individual bits
- Route bits through AND/OR/XOR gates
- Use merger to combine results back into a bus

### 5.4 Merger color handling
- When merging single-bit tokens with different colors, use the color of the **most recently updated** token
- Track timestamp of each input: whichever bit was set last, its color is used for the merged bus token
- This keeps it simple and the color always reflects the most recent activity

---

## Phase 6: Custom Components

### 6.1 Custom component editor
- Separate scene/view where user builds a sub-circuit
- Define interface pins: named inputs and outputs with bit widths
- Uses all existing components + splitter/merger
- Internal wires can be single-bit (after splitting)

### 6.2 Saving custom components
- Serialize the sub-circuit as a JSON file
- Store in a "Components" library folder
- Include metadata: name, description, pin definitions

### 6.3 Using custom components in main editor
- Appears as a single block in the component palette
- Shows named pins with correct widths
- Internally: simulation expands the sub-circuit
- Delay: sum of internal path delays, or user-configurable

### 6.4 Lookup Table as a special case
- LUT is essentially a custom component with a truth table
- Can be implemented as a built-in component type
- OR: user builds it from splitter + gates + merger
- Probably worth having as a dedicated component for convenience

---

## Phase 7: LED / Output Display for Buses

### 7.1 Bus-aware LED
- LED connected to n-bit wire shows the full value
- Display options: binary, hex, decimal
- Visual: multi-segment display or simple text label

---

## Implementation Order (recommended)

```
Phase 1 (Token + Pin width)     — Foundation, everything depends on this
  |
Phase 2 (Wire visuals)          — Immediate visual feedback
  |
Phase 3 (DataSource update)     — Can create bus signals
  |
Phase 4 (Sequential compat)     — Can build basic bus pipelines
  |
Phase 5 (Splitter/Merger)       — Bridge between bus and gate world
  |
Phase 6 (Custom components)     — Full power, reusable designs
  |
Phase 7 (Bus LED display)       — Polish
```

Phases 1-4 give you a working bus system.
Phase 5 connects it to existing gates.
Phase 6 is the big feature for large designs.
Phase 7 is polish.

---

## Compatibility Notes

- All existing single-bit circuits work unchanged (width=1 is default)
- Saved projects with old BitToken format need migration (add width=1)
- Basic gates (AND, OR, XOR, Inverter, MullerC) stay single-bit only
- New JSON field `width` on tokens and pins — old files without it default to 1

# Waveform Viewer — Implementation Plan

**Goal:** A horizontal panel at the bottom of the screen showing signal waveforms over time. The user activates a probe tool, clicks wires in the circuit, and each probed wire's signal history appears as a trace in the viewer — like a logic analyzer / oscilloscope.

---

## UX Flow

```
1. User clicks "Waveform" button in toolbar → bottom panel slides up
2. Panel is empty, shows hint: "Select the probe tool and click a wire"
3. User clicks probe icon (in the panel or toolbar) → cursor changes to probe
4. User clicks a wire in the circuit → new trace row appears in the panel
5. Simulation runs → traces scroll left in real time, new values drawn on the right
6. User can add more wires, remove traces, pause/resume, zoom time axis
7. Close panel → probes cleared, panel slides down
```

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│  Sandbox (main view)                                    │
│                                                         │
│    [Clock]──Wire_A──[AND]──Wire_B──[LED]               │
│                        ↑ probe click                    │
│                                                         │
├─────────────────────────────────────────────────────────┤
│  WaveformPanel (bottom, Screen Space — Overlay)         │
│  ┌──────┬──────────────────────────────────────────┐    │
│  │ CLK  │ ┌┐┌┐┌┐┌┐┌┐┌┐┌┐┌┐┌┐┌┐┌┐┌┐┌┐┌┐┌┐┌┐┌┐┌┐  │    │
│  │      │ ┘└┘└┘└┘└┘└┘└┘└┘└┘└┘└┘└┘└┘└┘└┘└┘└┘└┘└┘└  │    │
│  ├──────┼──────────────────────────────────────────┤    │
│  │ OUT  │ ────────┐         ┌──────┐               │    │
│  │      │         └─────────┘      └───────────    │    │
│  ├──────┼──────────────────────────────────────────┤    │
│  │ BUS  │ ==03== ==07== ==0F== ==1A== ==00==       │    │
│  │ [4b] │                                          │    │
│  └──────┴──────────────────────────────────────────┘    │
│  [Probe] [Clear All] [───time zoom───]   tick: 4072    │
└─────────────────────────────────────────────────────────┘
```

---

## Data Model

### SignalProbe

One per probed wire. Records signal history by subscribing to the wire's output pin.

```
SignalProbe
├── label           : string        (auto-generated or user-editable)
├── wire            : Wire          (reference to the probed wire)
├── outputPin       : OutputPin     (the wire's source pin — we subscribe here)
├── width           : int           (bit width, from pin)
├── history         : List<SignalSample>  (time-ordered signal log)
├── color           : Color         (trace color, from token or user-chosen)
│
├── Attach(Wire wire)
│   Subscribe to wire.dataIn.NewDataEvent (or OutputPin event)
│   Record initial value as first sample
│
├── Detach()
│   Unsubscribe from events
│
└── OnSignalChange()
    Append new SignalSample(tick, token.GetValues(), token.ActiveColor())
```

### SignalSample

```
SignalSample
├── tick    : int       (simulation time)
├── values  : bool[]    (bit values at this tick)
├── color   : Color     (display color at this tick)
```

Only store samples on **value change** (not every tick). This keeps memory bounded — a wire that never changes has 1 sample regardless of simulation length.

---

## Step-by-step Implementation

### Step 1: WaveformPanel UI

A `Canvas` panel anchored to the bottom of the screen.

**Layout (vertical, bottom-up):**
- **Toolbar row** (bottom): Probe toggle button, Clear All button, time zoom slider, tick counter label
- **Trace area** (above toolbar): `ScrollRect` with vertical layout of trace rows
- Each **trace row**: label on the left (fixed width), waveform `RawImage` on the right

The panel starts hidden. A button in the main toolbar toggles it. When shown, it takes ~25% of screen height (resizable via drag handle on top edge).

**Files:**
- `Assets/Resources/Prefabs/WaveformPanel.prefab`
- `Assets/Scripts/GeneralPurpose/WaveformViewer/WaveformPanel.cs` — panel show/hide, manages trace list

### Step 2: SignalProbe + SignalSample classes

Pure C# data classes (no MonoBehaviour).

```csharp
public class SignalSample
{
    public int tick;
    public bool[] values;
    public Color color;
}

public class SignalProbe
{
    public string label;
    public Wire wire;
    public int width;
    public List<SignalSample> history = new List<SignalSample>();
    public Color traceColor;

    public void Attach(Wire wire) { ... }
    public void Detach() { ... }
}
```

**Subscribing:** `wire.dataIn` is the OutputPin feeding the wire. Subscribe to the OutputPin's connected InputPin events — or more directly, tap into `Wire.SetValue()`.

**Best hook point:** Add an event to `Wire.cs`:
```csharp
public event Action<BitToken> OnValueChanged;
```
Fire it inside `Wire.SetValue()`. This is the cleanest — one subscription per probe, fires on every signal change through that wire.

**File:** `Assets/Scripts/GeneralPurpose/WaveformViewer/SignalProbe.cs`

### Step 3: Probe tool (wire selection mode)

When probe mode is active:
- Cursor changes to a crosshair/probe icon
- Clicking a wire (detected via its `EdgeCollider2D`) creates a new `SignalProbe` attached to that wire
- A new trace row appears in the panel
- Clicking the same wire again removes the probe (toggle behavior)

**Implementation:** Use a raycast from mouse position. Wires already have `EdgeCollider2D`. Check if the hit object has `Wire_Mono`, then access `Wire_Mono.wire`.

```csharp
if (Input.GetMouseButtonDown(0) && probeMode)
{
    Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
    if (hit.collider != null)
    {
        Wire_Mono wireMono = hit.collider.GetComponent<Wire_Mono>();
        if (wireMono != null)
            ToggleProbe(wireMono.wire);
    }
}
```

**Visual feedback:** Highlight probed wires with a subtle glow or marker sprite at the probe point.

**File:** `Assets/Scripts/GeneralPurpose/WaveformViewer/ProbeTool.cs`

### Step 4: WaveformRenderer (drawing traces)

Each trace row has a `RawImage` backed by a `Texture2D` that we draw into. On each frame (or each tick), redraw the visible portion of the waveform.

**Rendering logic for single-bit signals:**
```
For each pair of adjacent samples (s1, s2):
    - Draw horizontal line at y=HIGH from s1.tick to s2.tick if s1.value is true
    - Draw horizontal line at y=LOW  from s1.tick to s2.tick if s1.value is false
    - Draw vertical edge at s2.tick connecting HIGH↔LOW on value change
```

**Rendering logic for multi-bit bus signals:**
```
For each pair of adjacent samples (s1, s2):
    - Draw filled "bus bar" (two horizontal lines with X-crossings at transitions)
    - Render hex value text centered in each stable region (e.g., "0F", "1A")
    - Use sample color for the fill
```

**Time axis:**
- `viewStartTick` and `viewEndTick` define the visible window
- Pixels per tick = panel width / (viewEndTick - viewStartTick)
- Auto-scroll: `viewEndTick` tracks current tick, window slides right
- Zoom slider adjusts window width (more ticks = zoomed out)

**Performance:** Don't redraw every pixel every frame. Use a ring buffer approach:
- Keep a Texture2D as wide as the panel
- Each tick, draw only the new column(s) on the right
- When scrolling left (history), do a full redraw from history

**File:** `Assets/Scripts/GeneralPurpose/WaveformViewer/WaveformRenderer.cs`

### Step 5: Wire.cs modification

Add the event hook that probes subscribe to.

```csharp
// In Wire.cs
[JsonIgnore] public event Action<BitToken> OnValueChanged;

public void SetValue(BitToken data)
{
    // ... existing propagation logic ...
    OnValueChanged?.Invoke(data);
}
```

This is the only change to existing code outside of adding the toolbar button.

**File:** `Assets/Scripts/ComponentClasses/Wires&Pins/Wire/Wire.cs` (modified)

### Step 6: Trace row UI

Each trace row in the ScrollRect:

```
┌────────────┬──────────────────────────────────────┐
│ [X] CLK   │  ████ waveform texture ████           │
│     1-bit  │                                       │
└────────────┴──────────────────────────────────────┘
```

- **Left column (fixed 100px):** Remove button [X], label (editable `TMP_InputField`), bit width indicator
- **Right column (flexible):** `RawImage` showing the `Texture2D` from WaveformRenderer
- Right-click label → context menu: rename, change color, remove

**File:** `Assets/Scripts/GeneralPurpose/WaveformViewer/TraceRow.cs`

### Step 7: Time cursor and tick sync

Subscribe to `TimeTick.TimerTickEvent`:
- Update tick counter display
- Auto-scroll waveform if following live
- Trigger redraw of new samples

Add a vertical "playhead" line at the current tick position. When paused, the user can drag the playhead to inspect past values — display the value at that tick in each trace's label area.

---

## Interaction Details

| Action | Behavior |
|--------|----------|
| Toggle panel | Button in toolbar, or keyboard shortcut (W) |
| Activate probe | Click probe button in panel toolbar; ESC or right-click to deactivate |
| Probe a wire | Left-click wire while probe active → adds trace |
| Remove trace | Click [X] on trace row, or probe same wire again |
| Zoom time | Scroll wheel over panel, or drag zoom slider |
| Pan time | Click-drag horizontally on trace area (when not in probe mode) |
| Auto-scroll | On by default; panning manually disables it; button to re-enable |
| Pause inspect | Pausing simulation freezes waveforms; traces remain for inspection |
| Clear all | Button removes all probes and traces |

---

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| Memory growth from long recordings | History list grows unbounded | Cap at N samples (e.g. 100k). Oldest samples dropped. Or circular buffer. |
| Texture2D redraw performance | Redrawing full texture every frame is expensive | Incremental drawing — only append new columns. Full redraw only on pan/zoom. |
| Wire click precision | EdgeCollider2D on thin wires is hard to click | Temporarily increase collider thickness in probe mode, or use `Physics2D.OverlapCircle` with generous radius |
| Multi-bit display readability | Hex values crammed into narrow time windows | Only render text when stable region is wide enough in pixels. Below threshold, show color block only. |
| Interaction conflict | Probe click vs. normal wire/component interaction | Probe mode disables normal interaction (set `ProjectManager.dragActive = false`, suppress component clicks) |

---

## File Summary

| File | Type | Purpose |
|------|------|---------|
| `Scripts/GeneralPurpose/WaveformViewer/WaveformPanel.cs` | New | Panel show/hide, manages probes and trace rows |
| `Scripts/GeneralPurpose/WaveformViewer/SignalProbe.cs` | New | Per-wire data recorder (subscribes to Wire event) |
| `Scripts/GeneralPurpose/WaveformViewer/ProbeTool.cs` | New | Probe mode: raycast wires, toggle probes |
| `Scripts/GeneralPurpose/WaveformViewer/WaveformRenderer.cs` | New | Draws waveform traces onto Texture2D |
| `Scripts/GeneralPurpose/WaveformViewer/TraceRow.cs` | New | UI for one trace row (label + waveform image) |
| `Resources/Prefabs/WaveformPanel.prefab` | New | UI prefab for the bottom panel |
| `Scripts/ComponentClasses/Wires&Pins/Wire/Wire.cs` | Modified | Add `OnValueChanged` event in `SetValue()` |

---

## Implementation Order

1. **Wire.cs** — Add `OnValueChanged` event (1 line change, unblocks everything)
2. **SignalProbe.cs** — Data recording, subscribe/unsubscribe to wire events
3. **WaveformPanel.prefab + WaveformPanel.cs** — Bottom panel UI shell, show/hide
4. **TraceRow.cs** — Individual trace row layout
5. **WaveformRenderer.cs** — Single-bit trace drawing (horizontal lines + edges)
6. **ProbeTool.cs** — Wire click detection, probe toggle, cursor change
7. **Multi-bit rendering** — Bus-style traces with hex values
8. **Time controls** — Zoom, pan, auto-scroll, playhead cursor
9. **Polish** — Probe visual marker on wires, editable labels, keyboard shortcut

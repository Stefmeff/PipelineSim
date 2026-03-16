# Custom Chip Internal Circuit — Architecture Issues & Fix Plan

## Current Problems

### Problem 1: Internal circuit leaks into ProjectManager
When a `CustomChip` is placed in the Sandbox, `InitInternalCircuit()` calls `comp.Load()` on each internal component. Each `Load()` instantiates a prefab, which triggers `ComponentMono.Awake()`, which calls `projectManager.addToProject(this)`. Same for wires via `Wire_Mono.Awake()`.

**Result:** Internal components and wires are tracked by ProjectManager. When the sandbox is saved (including auto-save to temp file), the internal circuit components get serialized alongside the top-level components. On restore, they get loaded as top-level components AND the CustomChip loads them again internally → duplicates.

### Problem 2: Visual hiding is fragile
We snapshot existing GameObjects before loading, then hide new ones. This breaks when:
- Multiple CustomChips load simultaneously (during LoadWorld)
- Nested CustomChips create more objects during their own InitInternalCircuit
- Knots, bus labels, and other secondary objects are created outside the main Load() call

### Problem 3: Nested CustomChips don't work
A CustomChip containing another CustomChip would trigger recursive `InitInternalCircuit()`. The inner chip's internal components also get added to ProjectManager, compounding Problem 1. The event wiring (input → ChipInputNode → internal circuit → ChipOutputNode → output) needs to work across multiple nesting levels.

### Problem 4: Save/Load corruption
When ProjectManager.SaveWorld() runs:
1. It serializes ALL tracked components (including internal ones)
2. On load, CustomChip.Load() also loads the internal circuit from .chip file
3. Result: internal components exist twice — once from the .pip file, once from .chip file

## Root Cause
**Internal circuit components should NOT be managed by ProjectManager.** They are owned by the CustomChip and should be invisible to the project save/load system.

## Proposed Fix

### Approach: Don't instantiate GameObjects for internal circuits

Instead of calling `comp.Load()` (which creates GameObjects and registers with ProjectManager), keep the internal circuit as **pure data objects**. The components already work as C# objects — they subscribe to the Timer, process signals, etc. They only need GameObjects for:
1. Visual rendering (not needed — hidden)
2. Pin transforms for wire drawing (not needed — no internal wires visible)
3. Colliders for mouse interaction (not needed — hidden)

**The only thing we need from the internal circuit is signal propagation.**

### Implementation Plan

#### Step 1: Pure-data internal circuit loading
In `InitInternalCircuit()`:
- Deserialize the internal components from JSON (already done)
- **Do NOT call `comp.Load()`** — no GameObjects created
- Wire events between external pins and internal ChipInputNode/ChipOutputNode (already done)
- The internal components just need to subscribe to Timer events

#### Step 2: Subscribe internal components to Timer without GameObjects
Problem: Components subscribe to Timer in their constructors via `FindWithTag("Timer")`. This works because constructors run during deserialization. But components like AND also find the Editor tag in their constructor, which would fail.

Fix: Components already subscribe in their constructors (which run during `JsonConvert.DeserializeObject`). The deserialization already creates fully functional component objects. We just need to make sure:
- Constructor `FindWithTag` calls don't fail (they shouldn't — Timer exists in Sandbox)
- No `Load()` is called (no GameObjects)

#### Step 3: Handle nested CustomChips
When a CustomChip is found inside an internal circuit during deserialization:
- Its constructor runs, loads the chip definition, creates pins
- We need to also call `InitInternalCircuit()` on it — but without a root GameObject
- Since we're not creating GameObjects, the root parameter is only needed for BuildVisual. We can make InitInternalCircuit work without a root.

#### Step 4: Clean up ProjectManager interaction
- CustomChip's external pins/visual are managed by ProjectManager (normal)
- Internal components are NOT in ProjectManager (no GameObjects = no ComponentMono.Awake = no registration)
- Save/Load only serializes the CustomChip itself (chipName, inputs, outputs, position) — not its internals
- On load, CustomChip.Load() rebuilds internals from .chip file

### Signal Flow (fixed)

```
External Wire → CustomChip.InputPin[0] → NewDataEvent
  → ChipInputNode[0].InjectValue() → ChipInputNode[0].output.SetValue()
    → [internal wire connection preserved from JSON deserialization]
    → AND.dataA receives value (via pin reference in deserialized Wire)
    → AND processes on Timer tick
    → AND.dataOut.SetValue()
    → [internal wire to ChipOutputNode]
    → ChipOutputNode[0].input receives value → NewDataEvent
      → CustomChip.OutputPin[0].SetValue()
        → External Wire propagates signal out
```

The key insight: **JSON deserialization preserves all internal wire connections between pins** via `PreserveReferencesHandling.Objects`. The pins are connected as objects in memory — no GameObjects needed for signal propagation.

### What changes

| File | Change |
|------|--------|
| `CustomChip.cs` | `InitInternalCircuit()`: remove `comp.Load()`, remove snapshot/hide logic. Just deserialize and wire events. |
| `CustomChip.cs` | Handle nested chips: after deserialization, find internal CustomChips and call their `InitInternalCircuit()` recursively (without root GameObject). |
| `CustomChip.cs` | `Dispose()`: just unsubscribe events, no GameObjects to destroy. |
| Component constructors | May need null guards for `FindWithTag("Editor")` since Editor tag is only needed for visual mode. Most already have this from ChipEditor scene work. |

### Risk: Timer subscription in constructors
Components subscribe to Timer in their constructors. During deserialization, the constructor runs and `FindWithTag("Timer")` succeeds (Timer exists in Sandbox). This means internal components WILL receive timer ticks and process signals — which is exactly what we want.

### Risk: Editor references in constructors
Some components (AND, OR, etc.) find the "Editor" tag in their constructor. These already have null-conditional (`?.`) from our earlier fix. Internal components will have `editor = null` which is fine — they never need to open an editor.

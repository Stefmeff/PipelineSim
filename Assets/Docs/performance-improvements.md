# Performance Improvements

Prioritized list of performance optimizations identified in the PipelineSim codebase.

---

## P1 - Critical (per-frame impact on every circuit)

### 1. ~~Wire rendering rebuilds every frame unconditionally~~ DONE

**Files:** `Wire_Mono.cs`

~~`UpdateLineRenderer()` runs every frame for every wire, even when nothing has moved.~~

**Fix applied:** Added dirty flag system. Source/sink/knot positions are cached and compared each frame (6 float comparisons per wire). `UpdateLineRenderer()` only runs when something actually moved. Wire color changes are also gated behind a comparison. See `Wire_Mono.Update()` and `Wire_Mono.CachePositions()`.

---

### 2. ~~LINQ allocations in hot paths~~ DONE

~~**`BitToken.cs:78`** — `values.Any(v => v)` in `ActiveColor()` allocates an enumerator on every call.~~

**Fix applied:** Removed `.Any()` entirely. Multi-bit signals always return token color (high/low dimming only applies to 1-bit signals). This was also a logic fix — bus wires should show their color regardless of bit values.

~~**`Wire_Mono.cs:204`** — `wire.knots.Last()` allocates an enumerator just to get the final element.~~

**Fix applied:** Replaced with `wire.knots[wire.knots.Count - 1]`.

---

### 3. ~~Bus label updates every frame regardless of changes~~ DONE

**File:** `Wire_Mono.cs`

~~`UpdateBusLabel()` sets `busText.text`, `fontSize`, `fontStyle`, and `color` every frame even when unchanged.~~

**Fix applied:** `busText.text` only set when width string changes. Bus colors only set when `wire.coloring` changes. `fontSize`/`fontStyle` moved to `CreateBusLabel()` (set once at creation). Additionally, `UpdateBusLabel()` is now gated behind the dirty flag — it doesn't run at all for static wires.

---

## P2 - High (per-frame or per-tick, adds up with scale)

### 4. ~~Delay visualization creates/destroys GameObjects per signal~~ DONE

**File:** `DelayHandler.cs`

~~Every signal passing through a delay-visualized component creates a new `GameObject` with `AddComponent<SpriteRenderer>()`, which is later `Destroy()`-ed.~~

**Fix applied:** Added `static Stack<GameObject> pool` to `DelayHandler`. `NewSquare()` pops from pool when available, creates new only when pool is empty. `ReturnSquare()` deactivates and pushes back. Pool grows organically and stabilizes once throughput balances (especially in synchronous circuits). All 12 component files updated to use `ReturnSquare()` instead of `GameObject.Destroy()`.

---

### 5. ~~String allocations in simulation timing loop~~ DONE

**File:** `TimeTick.cs`

~~`AdaptSimulationTime()` runs every frame during active simulation and does string concatenation to update UI text.~~

**Fix applied:** Extracted `UpdateSpeedDisplay()` method that caches `lastDisplayedSpeed` and only updates the TMP text when `tickTimerMax` actually changes. All call sites now use this method.

---

### 6. ~~Tuple allocations in signal queues~~ DONE

**Files:** All component files with signal queues + `DelayHandler.cs`

~~Signal queues use `List<Tuple<BitToken, GameObject>>`. `Tuple` is a reference type that allocates on the heap and requires GC.~~

**Fix applied:** Created `SignalEntry` struct (`Assets/Scripts/ComponentClasses/SignalEntry.cs`) with `token` and `visual` fields. Replaced all `Tuple<BitToken, GameObject>` declarations, `Tuple.Create()` calls, `.Item1`/`.Item2` accesses across 12 component files and `DelayHandler.cs`.

---

## P3 - Medium (minor per-frame cost or initialization-only)

### 7. ~~`Camera.main` repeated lookups~~ DONE

**Files:** `MouseTracker.cs`, `Switch_Mono.cs`, `Draggable2D.cs`, `Knot_Mono.cs`

~~`Camera.main` performs a `FindGameObjectWithTag("MainCamera")` internally on each access.~~

**Fix applied:** Added `private Camera cam` field cached in `Awake()`/`Start()` in MouseTracker, Draggable2D, Switch_Mono, and Knot_Mono. All `Camera.main.ScreenToWorldPoint` calls replaced with `cam.ScreenToWorldPoint`. (Wire_Mono and CameraMouseDrag already cached it.)

---

### 8. `Resources.Load()` without caching

**Files:** `Clock.cs:112`, `AND.cs:149`, `DataSourceEditor.cs:74`, and similar `Load()` methods across components.

Each component instantiation calls `Resources.Load()` for its prefab. While only hit during loading (not per-frame), it does a disk/asset lookup each time.

**Fix:** Cache loaded prefabs in a static dictionary or use a central prefab registry.

---

## Summary

| # | Issue | Scope | Status |
|---|-------|-------|--------|
| 1 | Wire geometry rebuilt every frame | Every wire, every frame | DONE |
| 2 | LINQ in hot paths | Every bus wire, every frame | DONE |
| 3 | Bus label always updated | Every bus wire, every frame | DONE |
| 4 | Delay viz GameObject churn | Per signal, per delayed component | DONE |
| 5 | String alloc in timing loop | Every frame during simulation | DONE |
| 6 | Tuple signal queue allocations | Per signal, per delayed component | DONE |
| 7 | Camera.main lookups | Per input event | DONE |
| 8 | Resources.Load without cache | Per component instantiation | TODO |

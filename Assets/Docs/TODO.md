# PipelineSim — TODO

## Custom Components

### Done
- [x] Chip definition schema + save/load (.chip files)
- [x] Chip Creator scene (workspace, pin panels, save/load)
- [x] Interface pin nodes in editor
- [x] CustomComponent placement in sandbox
- [x] Internal circuit simulation (pure-data mode)
- [x] Component bar "Custom" category
- [x] Pure-data mode hardening (null guards, delay self-init, bus width reset)
- [x] Delay simulation inside custom chips ([OnDeserialized] fix)
- [x] Delay path calculation + visualizer on custom chip body
- [x] DataSource works in pure-data mode (token data owned by DataSource)
- [x] Setup/Hold violation popups via InformationWindow
- [x] Nesting support (chip inside chip)

### TODO
- [ ] Edit existing chips + update propagation (edit a chip, all placed instances update)
- [ ] Inspect tool — live view inside a placed chip (RenderTexture + secondary camera)
- [ ] Recursion prevention — prevent chip A containing chip B which contains chip A
- [ ] Chip color customization — user-chosen color for chip appearance
- [ ] Token color propagation through custom chips (currently always shows default red)

## Core Components
- [ ] Splitter — split an N-bit bus into individual bit outputs
- [ ] Merger — merge individual bit inputs into an N-bit bus output

## Quality of Life
- [ ] Copy/paste components + wires
- [ ] Undo/redo
- [ ] Waveform viewer — signal history over time (like a logic analyzer)

## Performance
- [ ] Performance improvements — profiling and optimization for large circuits

## Future Ideas
- [ ] Verilog/VHDL export — map circuit to HDL for external simulation/synthesis
- [ ] Breakpoints — pause simulation on signal condition
- [ ] Constant/Tie-off component — fixed 0 or 1 output

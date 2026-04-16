# Component Reference

This section documents every available component in PipSim, including its function, pins, and configurable parameters.

## Combinational Logic

These components perform logic operations with no internal state. Output depends only on current inputs.

### AND Gate

Outputs 1 if and only if **all** inputs are 1, otherwise outputs 0.

| Pins | Description |
|------|-------------|
| Input A | First input |
| Input B | Second input |
| Output | A AND B |

**Editor Parameters:**
- **Propagation Delay** — Number of time units before the output reflects a new input (default: 0)
- **Delay Visualization** — Toggle progress bar showing signal propagation

---

### OR Gate

Outputs 1 if **any** input is 1, otherwise outputs 0.

| Pins | Description |
|------|-------------|
| Input A | First input |
| Input B | Second input |
| Output | A OR B |

**Editor Parameters:**
- **Propagation Delay** — Number of time units before the output reflects a new input (default: 0)
- **Delay Visualization** — Toggle progress bar showing signal propagation

---

### XOR Gate

Outputs 1 if **exactly one** input is 1, otherwise outputs 0.

| Pins | Description |
|------|-------------|
| Input A | First input |
| Input B | Second input |
| Output | A XOR B |

**Editor Parameters:**
- **Propagation Delay** — Number of time units before the output reflects a new input (default: 0)
- **Delay Visualization** — Toggle progress bar showing signal propagation

---

### Inverter

Outputs the logical negation of the input. If input is 1, output is 0, and vice versa.

| Pins | Description |
|------|-------------|
| Input | Signal to invert |
| Output | NOT Input |

**Editor Parameters:**
- **Propagation Delay** — Number of time units before the output reflects a new input (default: 0)
- **Delay Visualization** — Toggle progress bar showing signal propagation

---

### Muller C-Element

A fundamental building block for asynchronous circuits. The output is set to 1 when **all** inputs are 1, and to 0 when **all** inputs are 0. For any other input combination, the output holds its current state.

| Pins | Description |
|------|-------------|
| Input A | First input |
| Input B | Second input |
| Output | C-Element output |

**Editor Parameters:**
- **Propagation Delay** — Number of time units before the output reflects a new input (default: 0)
- **Delay Visualization** — Toggle progress bar showing signal propagation

The Muller C-Element is essential for handshake-based asynchronous pipeline designs, where it is used to synchronize request and acknowledge signals.

---

### Delay (Propagation Delay)

A pure delay element that passes its input to the output after a configurable number of time units. Acts like a FIFO — signals enter and exit in order, delayed by the specified amount.

| Pins | Description |
|------|-------------|
| Input | Signal to delay |
| Output | Delayed signal |

**Editor Parameters:**
- **Propagation Delay** — Number of time units to delay the signal (default: 0)
- **Delay Visualization** — Toggle progress bar showing signal propagation

Accepts any bit width on the input.

---

## Sequential Elements

These components have internal state and are controlled by clock or control signals. They are used to build pipeline stages.

### Flip-Flop

Samples the data input on the **rising clock edge** (when clock transitions from 0 to 1) and holds the sampled value at the output until the next rising edge.

| Pins | Description |
|------|-------------|
| CLK | Clock input |
| Data In | Data to sample |
| Data Out | Sampled and held output |

**Editor Parameters:**
- **Clock-to-Q Delay** — Propagation delay from clock edge to output change (default: 0)
- **Setup Time** — Data must be stable this many ticks before the clock edge
- **Hold Time** — Data must remain stable this many ticks after the clock edge
- **Delay Visualization** — Toggle progress bar

If setup or hold time constraints are violated, the simulation pauses and an error popup is shown.

Accepts any bit width on the data input.

---

### Latch (Transparent Latch)

When the clock input is **high (1)**, the latch is transparent — the data input passes directly to the output. When the clock goes **low (0)**, the latch captures and holds the last data value.

| Pins | Description |
|------|-------------|
| CLK | Clock/Enable input |
| Data In | Data input |
| Data Out | Output (transparent when CLK=1, held when CLK=0) |

**Editor Parameters:**
- **Delay** — Propagation delay (default: 0)
- **Setup Time** — Data must be stable this many ticks before clock goes low
- **Hold Time** — Data must remain stable this many ticks after clock goes low
- **Delay Visualization** — Toggle progress bar

Accepts any bit width on the data input.

---

### Capture-Pass Latch (CP-Latch)

A two-phase latch with separate **capture** and **pass** control signals. Used in asynchronous pipeline designs.

- When both capture and pass are in the **same phase** (both low or both high): the latch is in **pass mode** — data flows through transparently.
- When capture and pass are in **opposite phases**: the latch is in **capture mode** — data is sampled and held.

| Pins | Description |
|------|-------------|
| Capture | Capture control signal |
| Pass | Pass control signal |
| Data In | Data input |
| Data Out | Output |

**Editor Parameters:**
- **Delay** — Propagation delay (default: 0)
- **Setup Time** — Data must be stable before capture edge
- **Hold Time** — Data must remain stable after capture edge
- **Delay Visualization** — Toggle progress bar

Accepts any bit width on the data input.

---

### Delay Element (Delay2)

A standalone delay component specifically designed for pipeline timing. Functions identically to the Propagation Delay in the combinational section but is categorized separately for organizational purposes.

| Pins | Description |
|------|-------------|
| Input | Signal to delay |
| Output | Delayed signal |

**Editor Parameters:**
- **Delay** — Number of time units (default: 0)

Accepts any bit width on the input.

---

## Sources and Sinks

### Switch

A user-interactive input source. Click the switch during simulation to toggle its output between 0 and 1.

| Pins | Description |
|------|-------------|
| Output | Current switch state (0 or 1) |

The switch can only be toggled while the simulation is running (not paused). Each toggle generates a new signal with the current simulation timestamp.

---

### Clock

Generates a periodic square wave signal that oscillates between 0 and 1. Used to drive synchronous pipeline elements.

| Pins | Description |
|------|-------------|
| Output | Periodic clock signal |

**Editor Parameters:**
- **High Time** — Number of ticks the clock stays at 1 (default: 100)
- **Low Time** — Number of ticks the clock stays at 0 (default: 100)

The clock period equals High Time + Low Time.

---

### Data Source

Generates configurable multi-bit data tokens on each rising clock edge. Used to feed data into pipeline circuits.

| Pins | Description |
|------|-------------|
| CLK In | Clock input — generates a token on each rising edge |
| Data Out | N-bit data output |

**Editor Parameters:**
- **Bit Width** — Number of bits per token (1–16, default: 1)
- **Token List** — Configurable list of binary token values, each with a unique color. Tokens are output in round-robin order.

The data source cycles through its token list, outputting one token per rising clock edge. Each token has a distinct color, making it easy to track individual data items as they propagate through a pipeline.

---

### LED

A visual output indicator that displays the color of the incoming signal. Useful for debugging and observing circuit outputs.

| Pins | Description |
|------|-------------|
| Input | Signal to display |

The LED changes color based on the incoming token's active color: grey for logic low, and the token's color for logic high.

Accepts any bit width on the input.

---

## Bus Components

### Splitter

Splits an N-bit bus wire into its individual bits. Each bit of the input bus is routed to a separate 1-bit output.

| Pins | Description |
|------|-------------|
| Input | N-bit bus input |
| Output 0..N-1 | Individual 1-bit outputs |

**Editor Parameters:**
- **Bit Width** — Number of bits to split (2–16, default: 2)

Changing the bit width recreates all pins and disconnects existing wires.

---

### Merger

Combines multiple individual 1-bit wires into a single N-bit bus wire. The inverse of the Splitter.

| Pins | Description |
|------|-------------|
| Input 0..N-1 | Individual 1-bit inputs |
| Output | N-bit bus output |

**Editor Parameters:**
- **Bit Width** — Number of bits to merge (2–16, default: 2)

The output token inherits its color from the most recent input signal. Changing the bit width recreates all pins and disconnects existing wires.

---

## Wires

Wires connect output pins to input pins and carry signals between components. Wire color reflects the current signal:

- **Grey** — Logic low or no signal
- **Colored** — Logic high, with color matching the token

### Bus Wires

When a wire carries a multi-bit signal (bit width > 1), it is displayed with a thicker line and a bus width annotation showing the number of bits.

### Wire Knots

Click on a wire to add a knot (bend point). Knots can be used to route wires around components. Wire knots also serve as branch points — connecting a new wire from a knot creates a signal split.

---

## Custom Components

Custom components allow you to design reusable sub-circuits and place them as single blocks in your main circuit.

### Creating a Custom Chip

1. Click **Chip Creator** in the top menu to open the chip editor
2. **Add interface pins** — Click on the left bar to add input pins, right bar for output pins
3. **Build the internal circuit** — Place and connect components in the workspace, just like in the main editor
4. **Configure pins** — Right-click on interface pins to set their name and bit width
5. **Set component delays** — Right-click on internal components to configure their propagation delays
6. **Save** — Click Save to store the chip as a `.chip` file

The chip's propagation delay is automatically calculated as the longest delay path through the internal circuit.

### Placing a Custom Chip

Saved chips appear in the **Custom** category in the component library. Drag and drop them into the workspace like any other component.

Custom chips display:
- Grey border with dark green interior
- Pin labels on left (inputs) and right (outputs)
- Chip name on hover
- Delay visualization bar (if the chip has internal delays)

### Nesting

Custom chips can contain other custom chips, enabling hierarchical design. For example, you could build a Full Adder chip from basic gates, then use it inside a 4-bit Adder chip.

### Important Notes

- Custom chips reference their `.chip` file by name. If the file is deleted or moved, the chip will not load and an error message is shown.
- Editing a chip definition and re-saving it does not yet automatically update placed instances (this feature is planned).

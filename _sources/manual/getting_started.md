# Getting Started

## Overview

PipSim is a digital circuit simulator designed for building and visualizing pipeline architectures. It supports both synchronous and asynchronous circuit design with real-time signal visualization, propagation delays, and timing constraint checking.

## Interface

The main interface consists of the following areas:

### Component Library (Left Panel)

The component library on the left side contains all available circuit components organized into categories:

- **Combinational** — Logic gates (AND, OR, XOR, Inverter, Muller C-Element)
- **Sequential** — Timing elements (Flip-Flop, Latch, Capture-Pass Latch, Delay)
- **Sources & Sinks** — Signal generators and indicators (Switch, Clock, Data Source, LED)
- **Bus** — Multi-bit wire utilities (Splitter, Merger)
- **Custom** — User-created chip definitions

Click on a category folder to expand it and see the available components. Drag a component from the library into the workspace to place it.

### Workspace (Center)

The workspace is where you build your circuit. You can:

- **Pan** — Click and drag on the background to move the view
- **Zoom** — Scroll the mouse wheel to zoom in and out
- **Place components** — Drag from the library into the workspace
- **Move components** — Click and drag a placed component to reposition it. Components snap to a grid on release.

### Top Menu Bar

The top menu contains simulation controls and file operations:

- **Play/Pause** — Start or pause the simulation
- **Restart** — Reset the simulation to its initial state
- **Step** — Advance the simulation by a configurable number of time units
- **Simulation Speed** — Adjust how fast the simulation runs
- **Save** — Save the current circuit to a `.pip` file
- **Load** — Load a previously saved circuit
- **Chip Creator** — Open the chip editor to create reusable custom components

## Basic Workflow

### 1. Place Components

Drag components from the library on the left into the workspace. Each component snaps to the grid when released.

### 2. Connect Components

To create a wire connection:

1. Click on an **output pin** (right side of a component) — this starts drawing a wire
2. Click in the workspace to place intermediate wire points (knots)
3. Click on an **input pin** (left side of another component) to complete the connection

Wires can be branched by clicking on an existing wire knot.

### 3. Configure Components

Right-click on any component to open its editor. Depending on the component type, you can configure:

- **Propagation delay** — How many time units it takes for a signal to pass through
- **Delay visualization** — Check the box to show a progress bar visualizing signal propagation
- **Timing parameters** — Setup time, hold time (for sequential elements)
- **Clock period** — High and low times (for Clock components)
- **Bit width** — Number of bits (for Data Source, Splitter, Merger)

### 4. Run the Simulation

Press the **Play** button to start the simulation. Interact with switches to inject signals into your circuit. Observe how signals propagate through the components, with colors indicating different data tokens.

### 5. Save and Load

- **Save** — Click the save button to store your circuit as a `.pip` file
- **Load** — Click the load button to open a previously saved circuit

## Signal Visualization

PipSim uses color-coded tokens to visualize signal propagation:

- **Grey/Light** — Logic low (0) or no signal
- **Colored** — Logic high (1), with the color representing the token identity
- Tokens from different data sources are colored differently, allowing you to track individual data items as they flow through a pipeline

When delay visualization is enabled on a component, a progress bar appears below it showing signal waves traveling through the component. This makes propagation delays visible and helps understand pipeline timing.

## Timing Violations

PipSim checks timing constraints on sequential elements:

- **Setup time violation** — Data arrived too late before the clock edge
- **Hold time violation** — Data changed too early after the clock edge

When a violation occurs, the simulation pauses and an information window displays the details of the violation, including how many time units the signal was early or late.

## Keyboard Shortcuts

- **Delete** — Delete the selected component
- **Ctrl + Click** — Constrain wire drawing to horizontal/vertical

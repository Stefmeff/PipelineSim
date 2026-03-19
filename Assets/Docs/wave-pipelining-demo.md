# Wave Pipelining Demo Circuit

## Concept

Wave pipelining allows clocking data **faster than the total propagation delay** of a combinational block. Multiple data "waves" propagate through the same logic simultaneously without intermediate flip-flops.

**Key constraint:** clock period > (max path delay - min path delay), not total delay.

This works when delay paths are well-balanced — if all paths take roughly the same time, you can launch new data before the previous result exits.

## Why PipelineSim is Uniquely Suited

- **Delay visualizer** shows multiple colored squares in flight — literally the "waves"
- **Token-based timing** tracks arrival time per signal
- **Setup/hold checking** detects when waves collide (timing violations)
- No other educational simulator can visualize wave pipelining this directly

## Proposed Demo Circuit: 4-bit Carry-Lookahead Adder (CLA)

The CLA is the canonical wave pipelining example from the definitive textbook (Gray, Liu, Cavin, *"Wave Pipelining: Theory and CMOS Implementation"*, Springer 1994). A 16-bit CLA achieved 9 waves at 250 MHz in 2μm CMOS.

### Why CLA is Suitable

- **Real purpose:** addition — students immediately understand it
- **Balanced paths:** log-depth tree structure gives relatively equal input-to-output delays
- **Small enough:** ~15-20 gates for a 4-bit version
- **Historically accurate:** CLA + wave pipelining is a real technique used in high-performance ALUs

### Architecture

Generate and propagate signals:
```
Gi = Ai AND Bi       (generate: this bit produces a carry)
Pi = Ai XOR Bi       (propagate: this bit passes a carry through)
```

Carry lookahead (all computed in parallel):
```
C1 = G0 OR (P0 AND C0)
C2 = G1 OR (P1 AND G0) OR (P1 AND P0 AND C0)
C3 = G2 OR (P2 AND G1) OR (P2 AND P1 AND G0) OR (P2 AND P1 AND P0 AND C0)
```

Sum outputs:
```
Si = Pi XOR Ci
```

All sum outputs have similar logic depth: generate/propagate layer -> carry lookahead layer -> final XOR.

### Demo Comparison

**Traditional 2-stage pipeline** (FF between layers):
```
DataSource -> FF -> [G/P logic] -> FF -> [Carry + Sum] -> FF
Clock period = max stage delay
3 flip-flops
```

**Wave pipeline** (no middle FF):
```
DataSource -> FF -> [G/P logic] -> [Carry + Sum] -> FF
Clock period = path spread (much smaller than total delay)
2 flip-flops, multiple waves in flight
```

The delay visualizer on the carry/sum block would show multiple squares in flight — multiple additions propagating through the same logic simultaneously. Same throughput, fewer flip-flops.

### Teaching Points

1. **Why path balance matters:** compare balanced CLA vs unbalanced ripple-carry adder
2. **Wave collision:** set clock too fast and setup/hold checker flags violations
3. **Traditional vs wave:** same throughput, fewer flip-flops, but harder to design
4. **Async boundaries:** use CPLatch between stages with different optimal clock rates (GALS design)

## Other Wave Pipelining Examples from Literature

| Circuit | Source | Notes |
|---|---|---|
| 63-bit population counter | Stanford 1992 (first wave-pipelined chip) | Balanced binary tree, 2.5x speedup |
| 16-bit CLA adder | Gray/Liu/Cavin 1994 textbook | 9 waves, 250 MHz in 2μm CMOS |
| 16x16 multiplier | Stanford/Klass 1992 | 600 MHz, NAND+INV only for delay uniformity |
| 8x8 multipliers | Multiple papers 1993-2013 | Most popular demo circuit, up to 6.25 GHz |
| 4-MB SRAM | 1995 | Regular array structure, 300 MHz |
| Viterbi decoder | 2010 | Repetitive add-compare-select structure, 10 GHz |

## References

- Gray, Liu, Cavin, *"Wave Pipelining: Theory and CMOS Implementation"*, Springer 1994
- Burleson et al., *"Wave-Pipelining: A Tutorial and Research Survey"*, IEEE Trans. VLSI Systems, 1998 ([PDF](https://www.cs.princeton.edu/courses/archive/fall01/cs597a/wave.pdf))
- Wong et al., *"First bipolar wave-pipelined LSI chip"*, IEEE JSSC, 1992

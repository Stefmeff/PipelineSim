# 2.4 Pulsed Latch 

The pulsed latch pipeline operates similarly to a flip-flop-based system. However, instead of sampling data at the rising edge of the clock, a brief pulse of width $t_{pw}$ is applied to a latch each cycle. This pulse samples the current input and holds it for the remainder of the clock cycle. Again, data tokens advance through the pipeline stages one clock cycle at a time, as illustrated in the figure below.

<img src="plots/2PhasePip.png" width="70%" style="display:inline-block;">

**Figure 2.4:** *Pulsed Latches {cite}`weste2011cmos`*


### Timing Constraints

**Max-Delay.** For the max-delay constraint concerning pulsed latches, we need to make a distinction between the following cases. If the setup time $t_{setup}$ is larger than the pulsewidth $t_{pw}$, arriving data must setup some time ($t_{setup} - t_{pw}$) before the rising clock edge. Resulting in the following constraint:

$$
T_{c} \geq t_{pcq} + t_{pd} + (t_{setup} - t_{pw})
$$

If the setup time is smaller than the pulsewidth, it does not need to be taken into account. The clock period $T_c$ must simply offer enough time for data to travel through the latch and the combinational logic:

$$
T_{c} \geq t_{pdq} + t_{pd}
$$

If we combine these two cases, we need to take the maximum of both:

$$
T_{c} \geq \max(t_{pdq} + t_{pd}, \ t_{pcq} + t_{pd} + t_{setup} - t_{pw})
$$

---

**Min-Delay.** The min-delay constraint for pulsed latches is expressed in Equation below. After data being launched from $L_1$ at the rising clock edge, it must not arrive at the subsequent latch $L_2$ until the hold time of the falling edge has passed. Compared to the flip-flop based pipelines, the additional pulse width delays the hold time window that has to be met:

$$
t_{ccq} + t_{cd} \geq t_{pw} + t_{hold}
$$
---
## Simulation

<div style="width:100%; overflow:hidden; display:block;">
  <div style="transform: scale(0.8); transform-origin: 0 0; width: calc(100% / 0.8); display:block;">
    <iframe
      src="https://stefmeff.github.io/PipSim_Web/?file=https://raw.githubusercontent.com/Stefmeff/PipelineSim/refs/heads/main/ProjectTemplates/SyncPipelines/PulsedLatchPipeline.pip"
      style="width:100%; height:600px; border:1px solid #ddd; border-radius:8px; display:block;"
      loading="lazy">
    </iframe>
  </div>
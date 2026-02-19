# 2.3 2-Phase Transparent Latch Pipeline

Two-phase transparent latch pipelines, as the name indicates, use latches instead of flip-flops. The pipeline utilizes the fact that flip-flops consist of a pair of latches that are clocked complementarily. The main idea is to split these latch pairs and the combinational logic and divide the full clock cycle into two half cycles. The subsequent latches are then controlled by 2 complementary clocks $\phi_1$ and $\phi_2$. While one clock is high the other is always low and vice-versa. This results in one latch always being transparent, allowing data to pass through, and the next latch being opaque, preventing tokens from catching up.

<img src="plots/2PhasePip.png" width="70%" style="display:inline-block;">

**Figure 2.3:** *2-Phase Transparent Latch Pipeline \cite{weste2011cmos}*

<div style="width:100%; overflow:hidden; display:block;">
  <div style="transform: scale(0.8); transform-origin: 0 0; width: calc(100% / 0.8); display:block;">
    <iframe
      src="https://stefmeff.github.io/PipSim_Web/?file=https://raw.githubusercontent.com/Stefmeff/PipelineSim/refs/heads/main/ProjectTemplates/SyncPipelines/2PhaseLatchPipeline.pip"
      style="width:100%; height:600px; border:1px solid #ddd; border-radius:8px; display:block;"
      loading="lazy">
    </iframe>
  </div>

Figure 2.3 shows the general structure of such pipelines and how the complementary clocks are applied to the latches. Note that the clocks $\phi_1$ and $\phi_2$ do not have to be exact complements of each other, but may also be non-overlapping when $t_{nonoverlap} > 0$. Similar to the flip-flop-based pipeline, we can now define maximum and minimum delay constraints for the combinational logic, ensuring the circuit functions as intended.

### Timing Constraints

**Max-Delay.** For two-phase transparent latches, new data can pass through the first latch $L_1$, as soon as $\phi_1$ becomes high making the latch transparent. The data then propagates through $L_1$ ($t_{pdq1}$), the first stage of combinational logic ($t_{pd1}$), $L_2$ ($t_{pdq2}$) and finally the second stage of combination logic ($t_{pd2}$). At the third latch $L_3$, the data could theoretically arrive as late as the falling edge of $\phi_1$'s next cycle without being lost. This could, however, significantly cut into the time that is reserved for a possible subsequent stage. In general, we cannot assume every stage to consume more than a clock cycle, resulting in the following max-delay condition:

$$T_{c} \ge t_{pdq1} + t_{pd1} + t_{pdq2} + t_{pd2}$$

In some cases, we will be able to make use of the extra time provided by the latch, borrowing time from the next stage. \Cref{timeBorrowing} will explore how this technique can lead to faster designs.

---

**Min-Delay.** The min-delay constraint for two-phase transparent latch pipelines can be derived as follows. The data begins to pass through the first latch $L_1$ at the rising edge of $\phi_1$. After it travels through $L_1$ and the combinational logic, the first signal changes that arrive at $L_2$ must not violate the hold time of the previous falling edge of $\phi_2$. Taking into account a delay of $t_{nonoverlap}$ between the falling edge of $\phi_2$ and the rising edge of $\phi_1$, we can derive the following minimum for the logic contamination delay $t_{cd}$:

$$t_{nonoverlap} + t_{ccq} + t_{cd} \ge t_{hold}$$

Note that this constraint applies to both paths $t_{cd1}$ and $t_{cd2}$ of the two-phased pipeline stage. One can also see that choosing a large enough value for $t_{nonoverlap}$ can completely rule out the possibility of hold-time violations. Generating nonoverlapping complementary clock signals at high speeds can, however, be challenging. As a result, commercial designs often rely on using the clock and its complement instead.
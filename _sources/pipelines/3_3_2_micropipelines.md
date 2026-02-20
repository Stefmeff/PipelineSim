# 3.3.2 Micropipelines

The so-called `Micropipelines` were introduced by Ivan Sutherland in his 1988 Turing Award lecture [MicroPip89] and implement a `2-phase bundled-data` protocol. The basic control structure for the handshakes is still the Muller pipeline. However, for the 2-phased approach the *req* and *ack* events are now encoded in signal transitions, rather than absolute values as before. To capture these signal transition events and control the data flow, the ordinary level-sensitive D-latches are replaced by specially designed **capture-pass latches**.


<img src="plots/CPLatch.png" width="70%" style="display:inline-block;">

**Figure 3.6:** *Implementation and function of a capture-pass latch {cite}`Sparso20`* 

Figure 3.6 shows the implementation and function of such a capture-pass latch. It has a **capture (C)** and a **pass (P)** input that control the flow of the input data. Initially, when both `C = 0` and `P = 0`, the latch is in *pass* mode and data is directly transferred to the output. Once a transition occurs on the capture input (`C = 1`, `P = 0`), the latch switches to *capture* mode, stores the previous data input, and holds it at the output. A transition on `P` (`C = 1`, `P = 1`) again causes the latch to be in *pass* mode, transferring the input directly to the output. Another signal transition on `C` (`C = 0`, `P = 1`) once more puts the latch into *capture* mode.

Notice how the capture-pass latch cyclically switches between *capture* and *pass* mode, making it possible to react to signal transition events and making no distinction between rising or falling edges. A transition on either the `C` or `P` wire always has the same meaning and represents the same type of event. This property is exactly what is needed for implementing a protocol like the 2-phase approach, which is based on encoding *req* and *ack* messages into such signal transitions.

Replacing the ordinary latches with capture-pass latches, but keeping the Muller pipeline as the basic control structure, results in a pipeline structure as shown below.

<img src="plots/Micropipeline.png" width="70%" style="display:inline-block;">

**Figure 3.7:** *Structure of a Micropipeline for 2-phase bundled-data protocols* {cite}`Sparso`

Here, the capture inputs (`C`) are connected to the output of the respective C-gates and the pass inputs (`P`) are connected to the *ack* signal from the successor stages. Assuming the default state where each C-gate holds the value `0`, all latches are in *pass* mode. Once the left-hand side issues a *req* message, encoded as a `0 → 1` transition, the latch switches to *capture* mode (`C = 1`, `P = 0`), storing the data and preventing new data from entering the latch. Once the right-hand side responds with an *ack* signal, encoded as a `0 → 1` transition, the latch returns to *pass* mode (`C = 1`, `P = 1`), allowing new data into the pipeline. New data is then issued analogously, except that both *req* and *ack* signals are encoded as the inverse transition `1 → 0`. once again, feel free to try out the protocol and behaviour of the pipeline in `Pipsim`:

<div style="width:100%; overflow:hidden;">
  <div style="transform: scale(0.8); transform-origin: 0 0; width: calc(100% / 0.8);">
    <iframe
      src="https://stefmeff.github.io/PipSim_Web/?file=https://raw.githubusercontent.com/Stefmeff/PipelineSim/refs/heads/main/ProjectTemplates/AysncPipelines/Micropipeline.pip"
      style="width:100%; height:600px; border:1px solid #ddd; border-radius:8px"
      loading="lazy">
    </iframe>
  </div>
</div>  

Compared to the *4-phase bundled-data* approach shown in the previous section, this design has the benefit of avoiding unnecessary signal transitions. In addition, the pipeline stages are now completely filled with different tokens, unlike before where only every second latch held different data. On the downside, the pipeline relies on non-standard components in the form of CP-latches. Their control logic, consisting of two storage loops to respond to signal transitions, is both larger and slower than that of conventional D-latches.

### Timing Constraints

The intended operation requires the satisfaction of several timing constraints. As shown in {cite}`Zhou22`, it is necessary that the *setup* and *hold* times of the capture-pass latches are not violated.

**Setup time.**  

Once a capture-pass latch switches into *pass* mode, new data along with the *req* signal can enter the stage. The entering *req* signal causes a transition at the C input of the latch, again activating *capture* mode. If the switch from *pass* to *capture* mode happens too fast, a setup violation may occur. To avoid setup violations, the propagation delay of the C-gate (t_C) must be larger than the setup time of the capture-pass latch (t_setup):

$$t_{C} > t_{setup}$$

**Hold time.**  

Hold time violations can occur when new data arrives at a latch input before the previous data has been properly captured. Once data together with a *req* signal enters stage N, a race condition arises. The *req* signal not only switches the latch of stage N into capture mode, but also acknowledges the previous stage N − 1, allowing new data to pass through. If this data arrives at the input of latch N before the hold time window has expired, an error may occur. To avoid such hold time violations, the following constraint must be met:

$$t_{inv_{N-1}} + t_{C_{N-1}} + t_{pass_{N-1}} + t_{logic_{N-1}}> t_{hold_{N}}$$

The left-hand side of the expression marks the path of the *ack* signal and the resulting data through stage N − 1.














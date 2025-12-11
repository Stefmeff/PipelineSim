# 3.3.1 D-Latch-Based Bundled-Data Implementation

**Figure 3.5** shows a pipeline that implements a `4-phase bundled-data` protocol. The lower
part of the pipeline implements the `data path` and consists of the different combinational logic stages
separated by ordinary D-latches. The upper `control` part consists of the `Muller pipeline`,
where the output of each C-gate is connected to the Enable input of its associated latch.
In order to work properly, the `req` signals should not arrive until after the `data` has arrived.
This condition is satisfied by introducing `delay elements` in the req paths larger than the
delays of the associated data path.

<img src="plots/4PhasePipeline.png" width="70%" style="display:inline-block;">

**Figure 3.5:** *4-phase bundled-data pipeline implementation* [13]

Additionally, each message has to be sent into the pipeline according to the 4-phase
protocol. If not acknowledged by the right-hand side, the pipeline will slowly fill with
the different tokens, until it blocks the left-hand side from issuing new data. This pipeline design is simple and robust but has some downsides regarding speed
and throughput. Generally the C-gates will have alternating values (0,1,0,1,...) stored in
them, resulting in only every second latch holding a new data token. The throughput
essentially gets halved by this property. The following sections will introduce pipelines
that are more efficient in this aspect. For better understanding you can test the pipeline in `PipSim`:


<div style="width:100%; overflow:hidden;">
  <div style="transform: scale(0.8); transform-origin: 0 0; width: calc(100% / 0.8);">
    <iframe
      src="https://stefmeff.github.io/PipSim_Web/?file=https://raw.githubusercontent.com/Stefmeff/PipelineSim/refs/heads/main/ProjectTemplates/AysncPipelines/4PhaseBundledData.pip"
      style="width:100%; height:600px; border:1px solid #ddd; border-radius:8px"
      loading="lazy">
    </iframe>
  </div>
</div>  

### Timing constraints

Similar to synchronous circuits, there are also timing constraints for asynchronous circuits that need to be taken into account for the design to work as intended.  
As already mentioned above, the *req* paths should have a matching or longer propagation delay than the associated *data* paths:

$$
t_{req} \geq t_{logic}
$$

Request signals that arrive earlier than the data would otherwise cause the receiver to absorb incorrect or obsolete data.

**Hold time.** Once a Muller C element changes its state to 0, this not only results in the respective latch being opaque, but also acknowledges the previous stage. This creates a racing condition between a new token arriving from the previous stage and the hold time window of the current stage not being violated. It is key to assure that the path of *$ack_{N-1}$* and data traveling through the previous stage takes longer than the latch's hold time, as expressed in:

$$
t_{inv_{N-1}} + t_{C_{N-1}} + t_{pcq_{N-1}} + t_{logic_{N-1}} \geq t_{hold_{N}}
$$




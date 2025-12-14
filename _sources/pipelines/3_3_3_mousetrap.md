# 3.3.3 Mousetrap

The so-called `Mousetrap` pipeline introduced by [Mouse01, Mouse07] uses the `2-phase bundled-data` protocol, where the *req* and *ack* information is encoded via signal transitions.

The pipelines we have looked at so far all used some variation of the Muller pipeline to implement the handshakes. Here, the control structure of the pipeline is different and consists only of basic XNOR gates and latches. Figure below shows the general pipeline structure of a *Mousetrap*. It consists of a `latch controller` that handles the handshakes and controls the data flow between the *data latches*. The *data latches* separate the different pipeline stages and are either transparent, allowing data to flow between stages, or opaque, blocking the data from flowing.

<img src="plots/AbbMouseTrap.png" width="650px">

**Figure 3.8:** *Pipeline structure of a “Mousetrap” circuit* [Mouse01]

At the start, all *req* and *ack* signals are low, leading to a logical 1 output at the XNOR gates, making all *data latches* transparent. Once new data together with a *req* signal enters a stage, the transition in *req* causes the XNOR gate to output a logical 0, blocking the connected *data latch* from letting in new requests. Only when the next stage acknowledges the request, by making a transition in the *ack* signal, does the XNOR gate output 1 and allow new data into the stage.Generally, the Nth stage in the pipeline takes three actions once it receives new data:

1. The received data is passed along to the next stage **N+1** together with a transition on the request signal **$req_{N+1}$**.
2. The previous stage **N−1** is acknowledged via the **$ack_{N−1}$** signal, allowing it to process new data.
3. The current stage is blocked from receiving new data until it receives its acknowledgement **$ack_N$** from the next stage **N+1**.

Try to comprehend the functionality of the pipeline through simulation:


<div style="width:100%; overflow:hidden; display:block;">
  <div style="transform: scale(0.8); transform-origin: 0 0; width: calc(100% / 0.8); display:block;">
    <iframe
      src="https://stefmeff.github.io/PipSim_Web/?file=https://raw.githubusercontent.com/Stefmeff/PipelineSim/refs/heads/main/ProjectTemplates/AysncPipelines/Mousetrap.pip"
      style="width:100%; height:600px; border:1px solid #ddd; border-radius:8px; display:block;"
      loading="lazy">
    </iframe>
  </div>

The name `Mousetrap` is inspired by its behavior, which is reminiscent of a real-life mousetrap. As soon as new data enters the “trap”, it snaps shut by closing the latches and only reopens once the data has exited the stage and the pipeline is ready to accept new data. The circuit is efficient and has the benefit of avoiding the use of C-elements or the slow and complex capture-pass latches used for micropipelines (see Section 3.3.2). According to [Mouse01], its performance can be compared to that of wave-pipelined circuits, with the benefit of not being dependent on accurate path delays and not being vulnerable to temperature and voltage variations.


### Timing Constraints

To assure the intended functionality of the pipeline, two timing constraints must be met: *setup time* and *data overrun*.

**Setup time.**  
Setup time violations can affect the correct functionality of the data latches. When the data latch of stage N is enabled, new data, along with a *$req_{N}$* signal, can enter the stage. The $req_{N}$ signal is then immediately forwarded to the XNOR gate, which subsequently disables the latch. If the delay between $req_{N}$ entering the stage and the latch being disabled is shorter than the latches setup time, a timing violation occurs.

$$t_{pcq} + t_{XNOR} > t_{setup}$$

To avoid this, the delay of $req_{N}$ propagating through the latch ($t_{pcq}$) and the XNOR-gate ($t_{XNOR}$) must be larger than the setup time of the latch ($t_{setup}$), as expressed in the equation above.

**Data overrun.**  
Data overrun errors occur when arriving data catches up to previous data that has not yet been properly captured by the latches.

When data arrives at stage N, a race condition is created between the *$done_N$* signal, which attempts to close the stage, and the *$ack_{N−1}$* signal, which allows new data into the previous stage. If the new data issued by *$ack_{N−1}$* reaches stage N before the latches of stage N are properly closed, a hazard occurs. To prevent this, the following timing constraint must be satisfied:

$$t_{XNOR_{N-1}} + t_{pcq_{N-1}} + t_{logic_{N-1}}  >  t_{XNOR_N} + t_{hold_{N}}$$

The left term of the equation expresses the path *$ack_{N-1}$* and resulting new data takes through stage N-1, and the right-hand side of the equation expresses the time it takes for the latch to be closed, also accounting for the hold time.

These timing constraints must be accounted for, but are easily satisfied, since the gate delays usually exceed the setup and hold times of the latches.
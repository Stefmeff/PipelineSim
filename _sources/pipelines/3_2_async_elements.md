# 3.2 Basic Asynchronous Circuit Elements

In `Section 3.1`, we concerned ourselves with the theoretical protocols that are used to implement handshaking in asynchronous circuits. In this section we will look at fundamental circuit elements that act as the basis of asynchronous circuits.

The handshake protocols discussed so far were all based on mutual agreement (request and acknowledgment) before transferring data between each other. Many asynchronous circuits rely on the so called `Muller Pipeline` as the fundamental control structure to implement these handshakes. While there are some variations in implementations,
depending on the specific protocol, the concept of the Muller pipeline remains as the backbone of most asynchronous circuits. Having understood the behaviour of the Muller pipeline will be key to understand the behaviour of the specific pipeline architectures introduced in Section 3.3.

This section will first go over the `Muller C-element` or `C-gate`, which is a fundamental building block for the Muller pipeline and asynchronous circuits in general. After that we will look at the implementation and behaviour of the Muller pipeline itself. These concepts date back as early as the 1950s and were pioneered by `David E. Muller` {cite}`Muller63,Muller59`.

## 3.2.1 Muller C-Element

The `Muller C-element` or `C-Gate` is a sequencing element for asynchronous circuits. Its function is quite straightforward: the output is **1**, when all inputs are **1** and **0**, when all inputs are **0**. For all other input combinations, the C-gate holds its current state.

<img src="plots/MullerC.png" width="50%" style="display:inline-block;">

**Figure 3.3:** *Specification of the Muller C-element* {cite}`Sparso20`

**Figure 3.3** shows the symbol and four possible formal specifications of the Muller C
element. The element exhibits the behaviour that it only changes its state, when all
inputs agree. Additionally, a state change at the output clearly indicates the states of
the inputs: When the element changes its output from **0** to **1**, we know that both inputs
have to be **1**. The same goes for the output changing from **1** to **0**, indicating that both
inputs are now **0**.

When dealing with protocols, where both sides have to agree via req and ack singals,
that cyclically transition between **0** and **1**, the Muller C element is useful in capturing
those events.

## 3.2.2 The Muller Pipeline

**Figure 3.4** depicts the general structure of the Muller pipeline2. It consists of a series of
interconnected C-gates **C[i]**, where each gate receives its inputs from the output of its
predecessor **C[i-1]** and the inverted output of its successor **C[i+1]**.

<img src="plots/MullerPipeline.png" width="70%" style="display:inline-block;">

**Figure 3.4:** *The Muller Pipeline* {cite}`Sparso20`

Initially, all C-gates **C[i]** store the value **0**. An input value **1** from predecessor **C[i-1]** is
propagated, when the successor **C[i+1]** outputs a **0**. Similarly a **0** from **C[i-1]** will only
propagate, if **C[i+1]** holds a **1**. Generally one can form the following simple state rule {cite}`MicroPip89`:

<p align="center">

| |
|---|
| "**IF** predecessor and successor differ in state  <br> **THEN** copy predecessor's state  <br> **ELSE** hold present state" |

</p>


The pipeline basically works as a FIFO for the transitions on the request signals. When
the left side issues a request, it travels through all the stages until it arrives at the
right-hand side. If never acknowledged by the right-hand side, the pipeline will slowly
fill with every transition made on the request signal. Once filled, the C-gates will store
alternating 0 and 1 values and the pipeline will stop handshaking with the left-hand side
and block new requests from entering. For a more intuitive of the Pipeline understanding you can use the `PipSim` simulation tool below:

<div style="width:100%; overflow:hidden;">
  <div style="transform: scale(0.8); transform-origin: 0 0; width: calc(100% / 0.8);">
    <iframe
      src="https://stefmeff.github.io/PipSim_Web/?file=https://raw.githubusercontent.com/Stefmeff/PipelineSim/refs/heads/main/ProjectTemplates/AysncPipelines/MullerPipeline.pip"
      style="width:100%; height:600px; border:1px solid #ddd; border-radius:8px"
      loading="lazy">
    </iframe>
  </div>
</div>  
The power of the Muller pipeline lies in its ability, to capture all the different handshaking
events made by req and ack, no matter if a 2-phased or 4-phased approach is used. It
just depends on how one decides to interpret the signals and use the Muller pipeline in
a specific context. The Muller pipeline also has the benefit of being delay-insensitive,
meaning that delays in gates or wires do not affect its function.


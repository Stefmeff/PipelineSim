# 2.2 Flip-Flop-Based Pipeline

One of the most widely used architectures is the classical pipeline using flip-flop's[^1]. If we recall the function of the flip-flop, it samples the input data D at the rising clock edge and holds it at the output Q until arrival of the next rising edge. This means that the combinational logic that lies between two flip-flops, has about one clock cycle of time, to process its current data token before the next one arrives. In this manner the data tokens advance from one pipeline stage to the next, while the clock together with the flip-flops ensure that everything happens in the desired order.

<img src="plots/FlopPip.png" width="70%" style="display:inline-block;">

**Figure 2.2:** *Flip-Flop Based Pipeline {cite}`weste2011cmos`*

However, designing a pipeline this way also brings along some timing constraints, concerning the delay of the combinational logic and the chosen clock period, that must be adhered for the system to work correctly.  

We will distinguish between max-delay constraints, that define upper bounds for the propagation delay of a stage and min-delay constraints, that define the lower bounds for the propagation delay. To define these constraints, we will use the timing notation introduced in `Chapter 2.1.3`.

## Timing Constraints

**Max-Delay.** Generally, the chosen clock period $T_c$ must offer a large enough time interval, so that a data token can pass through the first flip-flop, stabilize at its output ($t_{pcq}$), then pass through the combinational logic ($t_{pd}$) and still arrive early enough at the second flip-flop, for it to be sampled correctly ($t_{setup}$). Hence, the chosen clock period $T_c$ has to meet the following constraint:

$$
T_c \ge t_{pcq} + t_{pd} + t_{setup}
$$

When dealing with a pipeline that consists of multiple stages, each of them having different combinational delays, the clock period has to account for all the occurring delays. The path with the highest latency, also called *critical path*, therefore limits the maximum allowed clock frequency. This characteristic is inherent in all synchronous circuits and can significantly impact the circuit's speed. It is key to consider this property during the design process. Note that while the timing constraints presented in this thesis are adequate in a theoretical context, in practice additional safety margins have to be introduced to account for PVT variations[^2].

**Min-Delay.** We also need to ask the question, what the minimum delay between two subsequent flip-flops is, such that the pipeline still functions correctly. Ideally one should be able to place sequencing elements back-to-back, while upholding the intended function. If we assume the delay of the combinational logic to be really small[^3], then the token sampled at the first flip-flop will arrive at the second flip-flop almost immediately. This could potentially lead to a hold-time violation at the second flip-flop. The following constraint needs to be adhered to, so tokens launched by the first flip-flop, arrive after the hold-time at the second flip-flop has passed:

$$
t_{ccq} + t_{cd} \ge t_{hold}
$$

Note that for this constraint, we use the contamination delays of the components, since we are interested in the first signal changes that could violate the hold time.

[^1]: Process, voltage and temperature variations impact the timing behaviour of a circuit.

## Simulation

<div style="width:100%; overflow:hidden; display:block;">
  <div style="transform: scale(0.8); transform-origin: 0 0; width: calc(100% / 0.8); display:block;">
    <iframe
      src="https://stefmeff.github.io/PipSim_Web/?file=https://raw.githubusercontent.com/Stefmeff/PipelineSim/refs/heads/main/ProjectTemplates/SyncPipelines/FlipFlopPipeline.pip"
      style="width:100%; height:600px; border:1px solid #ddd; border-radius:8px; display:block;"
      loading="lazy">
    </iframe>
  </div>


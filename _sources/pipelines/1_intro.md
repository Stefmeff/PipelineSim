# 1. Introduction

Modern computer hardware operates within the digital domain, encoding data in binary form through high and low voltage levels and performing operations by processing data through a sequence of logic gates. In general, digital circuits can be modeled as seen in **Figure 1:** A data source provides an input `token`[^1] **x**, representing a vector of *n* bits. This input token is processed by a circuit that implements a logic function *f*, mapping the input **x** to a corresponding output **y = f(x)**. The output **y** is then absorbed and stored by a data sink [Stein06, Ferr11].

<img src="plots/DataFlowModel.png" width="50%" style="display:inline-block;">

**Figure 1:** *Data flow model of a digital circuit* [Ferr11]

When assessing the performance of a digital circuit, two key metrics need to be taken into account: `latency` and `throughput`. Latency describes the amount of time it takes for a result to be produced. Throughput describes the rate at which results are produced. In digital circuits, latency is equivalent to the logic propagation delay, i.e., the time it takes for the electrical signals to pass through all the logic gates and produce a certain output. Reducing the latency of a circuit almost always leads to a higher throughput. Higher throughput, on the other hand, can also come at the cost of latency, as will be the case with `pipelining`.

When dealing with purely `combinational`[^2] circuits, new input tokens can only be issued once the previous token has finished processing. In this case, throughput depends entirely on the circuit's latency. Pipelined circuits achieve higher throughput by dividing the combinational logic of a circuit into multiple stages, allowing these stages to process different tokens concurrently. The idea is quite similar to that of an assembly line, where people perform different tasks concurrently on work pieces passing down the line. 

The advantage of humans working on an assembly line is, however, that they can easily communicate and coordinate themselves. They know when to pass the product down to the next stage and when they have to wait for the other stages to finish. The data flow inside digital circuits is not inherently ordered due to delay differences in the signal paths. It is the task of the hardware designer to implement control structures and protocols that assure coordinated and consistent data flow between the stages. It is key to enforce a *sequence* onto a stream of tokens, ensuring that they all reach their designated stages in the correct order, there is a clear distinction from the current, previous, and next token, and they do not get mixed up or lost on the way [Weste2011a, Weste2011b].

<img src="plots/GeneralPipeline.png" width="80%" style="display:inline-block;">

**Figure 2:** *General pipeline structure*

The basic structure of a pipelined circuit is shown in Figure 2. It depicts the partition of the combinational logic into different stages C₁, C₂, ..., Cₙ. This mirrors the organization of classical processor pipelines, where instructions are processed by a sequence of consecutive stages such as fetch, decode, execute, etc. To enforce a clear separation of these stages and control the flow of the tokens, so-called `sequencing` elements are introduced R₁, R₂, ..., Rₙ. These are memory elements used for storing the current token of a stage and either allowing or preventing data of previous stages from entering. Although the added sequencing elements introduce some additional delay, known as `sequencing overhead`, the resulting increase in throughput is a highly beneficial trade-off.

Sequencing elements can be controlled `synchronously`or `asynchronously`. Synchronous circuits are controlled by a global `clock` signal that periodically produces events, triggering data flow from one stage to another. All stages transfer their data at the same time, and tokens move through the pipeline at a constant rate. Asynchronous circuits, on the other hand, are based on stages communicating via `handshakes`, signaling when they are ready to send and receive new data. `Chapter 2` will focus on the sequencing elements and implementations of synchronous pipelines, which are the most widely used class of pipelines. Asynchronous pipelines will be the topic of `Chapter 3`. During reading, there will always be examplary circuits created in `PipSim`, that you can use alongside the text to simulate the behaviour of the circuits and understand the theoretical concepts more intuitively.

[^1]: A ‘token’ refers to a set of binary data that is logically related and serves as the input of a circuit (e.g., a processor instruction).  
[^2]: Combinational circuits describe the set of circuits where the output is only dependent on the current inputs.  
[^3]: Link to GitHub Project: [https://github.com/Stefmeff/PipelineSim](https://github.com/Stefmeff/PipelineSim)

# 3.1 Handshake Protocols 

This section presents several key handshaking protocols commonly used in modern asynchronous circuits. It is important to note that here we solely focus on the conceptual aspects of these handshakes; the detailed circuits that realize these protocols will be discussed in subsequent sections.

While there are numerous handshake protocols, all share a fundamental structure: one side `requests` data to be processed, while the other side `acknowledges` receipt of the data. The set of handshake protocols which we will focus on are referred to as `bundled-data` protocols[^1], meaning that the data is sent in a bundle, while the handshaking information is sent along in separate request (*req*) and acknowledge (*ack*) wires, as seen in Figure 1. On the contrary, there are also so-called `delay-insensitive` protocols, where the request information is encoded into the data signals by using two wires for each bit of information. Delay-insensitive protocols are, however, not covered in this section, since they are not relevant for the pipeline architectures presented in this thesis.

<!-- Figure 1 -->
<img src="plots/Handshake.png" width="70%" style="display:inline-block; margin-right:5%;">

**Figure 1:** *Bundled data channel with separate request and acknowledge wires* {cite}`Sparso20`

One specific approach is a `4-phase bundled-data` protocol that requires the following steps:

**1.** `Sender` issues new data and sets *req* high  
**2.** `Receiver` consumes data and sets *ack* high  
**3.** `Sender` responds by taking *req* low  
**4.** `Receiver` acknowledges this by also taking *ack* low  

After this, the sender can start the next communication cycle. Looking at the left picture in Figure 2, one can see all the signal transitions that have to be made within one handshake. It is a straightforward approach but has the disadvantage that the request and acknowledge signals make unnecessary `return-to-zero` transitions. In practice, this will cost time and energy.

<!-- Figure 2 -->

<img src="plots/4PhaseProtocol.png" width="90%" style="display:inline-block;">

**Figure 2:** *Timing diagram of bundled-data protocols* {cite}`Sparso20`


Another approach that is more efficient in this aspect is the `2-phase bundled-data` protocol, seen to the right of Figure 2. It encodes request and acknowledge as signal transitions, rather than absolute values. Both transitions 0→1 and 1→0 represent the same event. This avoids the excessive use of signal transitions that were made in the 4-phase protocol.

[^1]: Bundled-data protocols are also often referred to as `single-rail` protocols.
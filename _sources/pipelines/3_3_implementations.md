# 3.3 Pipeline Implementations

As explained earlier the `Muller pipeline` acts as the control structure to handle the
handshakes. The data usually gets transported separately through pipelines that are
similar to the synchronous pipelines introduced in `Chapter 2`. The key difference is that
the events that control the data flow are now triggered by handshaking instead of a
periodic clock signal.



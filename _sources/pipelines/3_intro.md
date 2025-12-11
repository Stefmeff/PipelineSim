# 3 Asynchronous Pipelines

The pipeline architectures we have dealt with so far, can all be classified as synchronous
circuits. This means that they are run with a global clock, that dictates when and in what
order things happen and introduces a common notion of time among all the components.
Asynchronous circuits on the other hand, are implemented entirely without a clock.
Instead the circuit components coordinate themselves via handshakes. At a basic level,
these handshakes must be able to communicate when a stage can provide new data and
when it can receive new data, so the components can decide when to initiate a data
transfer between each other.

Since asynchronous circuits represent a very broad subject area, this chapter will only
provide an overview of the foundations and a couple of prominent examples for asynchronous pipelines and handshake protocols. The chapter is primarily based on the book ’Introduction to Asynchronous Circuits Design’ by Jens Sparso [13] and related papers.

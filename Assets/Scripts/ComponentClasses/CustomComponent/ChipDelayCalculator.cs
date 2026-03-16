using System.Collections.Generic;

/// <summary>
/// Calculates the maximum delay path through a custom chip's internal circuit.
/// Uses topological traversal: starts from ChipInputNodes, propagates max delay
/// through components via their pin connections, and reads max delay at ChipOutputNodes.
/// Handles nested custom chips recursively via GetDelay().
/// </summary>
public static class ChipDelayCalculator
{
    /// <summary>
    /// Calculates the maximum propagation delay across all paths from any input to any output.
    /// </summary>
    public static int CalculateMaxDelay(List<CircuitComponent> components)
    {
        if (components == null || components.Count == 0) return 0;

        // Step 1: Build InputPin → owning component map
        Dictionary<InputPin, CircuitComponent> pinOwner = new Dictionary<InputPin, CircuitComponent>();
        foreach (CircuitComponent comp in components)
        {
            List<InputPin> ins = new List<InputPin>();
            List<OutputPin> outs = new List<OutputPin>();
            comp.CollectPins(ins, outs);
            foreach (InputPin ip in ins)
            {
                pinOwner[ip] = comp;
            }
        }

        // Step 2: Build adjacency list (component → downstream components)
        // and collect each component's output pins for traversal
        Dictionary<CircuitComponent, List<OutputPin>> compOutputs = new Dictionary<CircuitComponent, List<OutputPin>>();
        foreach (CircuitComponent comp in components)
        {
            List<InputPin> ins = new List<InputPin>();
            List<OutputPin> outs = new List<OutputPin>();
            comp.CollectPins(ins, outs);
            compOutputs[comp] = outs;
        }

        // Step 3: BFS/longest-path from ChipInputNodes
        // maxDelay[comp] = the maximum cumulative delay reaching this component's output
        Dictionary<CircuitComponent, int> maxDelay = new Dictionary<CircuitComponent, int>();
        Queue<CircuitComponent> queue = new Queue<CircuitComponent>();

        // Seed: ChipInputNodes have 0 delay (they're just pass-throughs)
        foreach (CircuitComponent comp in components)
        {
            if (comp is ChipInputNode)
            {
                maxDelay[comp] = 0;
                queue.Enqueue(comp);
            }
        }

        // Process in BFS order, updating max delay at each downstream component
        while (queue.Count > 0)
        {
            CircuitComponent current = queue.Dequeue();
            int currentDelay = maxDelay[current];

            if (!compOutputs.ContainsKey(current)) continue;

            foreach (OutputPin outPin in compOutputs[current])
            {
                List<InputPin> connected = outPin.GetConnectedPins();
                if (connected == null) continue;

                foreach (InputPin downstreamPin in connected)
                {
                    if (!pinOwner.ContainsKey(downstreamPin)) continue;

                    CircuitComponent downstream = pinOwner[downstreamPin];
                    int newDelay = currentDelay + downstream.GetDelay();

                    // Only update and re-process if we found a longer path
                    if (!maxDelay.ContainsKey(downstream) || newDelay > maxDelay[downstream])
                    {
                        maxDelay[downstream] = newDelay;
                        queue.Enqueue(downstream);
                    }
                }
            }
        }

        // Step 4: Find max delay at ChipOutputNodes
        int result = 0;
        foreach (CircuitComponent comp in components)
        {
            if (comp is ChipOutputNode && maxDelay.ContainsKey(comp))
            {
                if (maxDelay[comp] > result)
                    result = maxDelay[comp];
            }
        }

        return result;
    }

    /// <summary>
    /// Calculates the max delay per output pin (indexed matching chipDef.outputs order).
    /// Returns an array where each element is the max delay to that output.
    /// </summary>
    public static int[] CalculatePerOutputDelay(List<CircuitComponent> components, List<ChipOutputNode> outputNodes)
    {
        if (components == null || outputNodes == null) return new int[0];

        // Run the same algorithm as CalculateMaxDelay
        Dictionary<InputPin, CircuitComponent> pinOwner = new Dictionary<InputPin, CircuitComponent>();
        Dictionary<CircuitComponent, List<OutputPin>> compOutputs = new Dictionary<CircuitComponent, List<OutputPin>>();

        foreach (CircuitComponent comp in components)
        {
            List<InputPin> ins = new List<InputPin>();
            List<OutputPin> outs = new List<OutputPin>();
            comp.CollectPins(ins, outs);
            foreach (InputPin ip in ins) pinOwner[ip] = comp;
            compOutputs[comp] = outs;
        }

        Dictionary<CircuitComponent, int> maxDelay = new Dictionary<CircuitComponent, int>();
        Queue<CircuitComponent> queue = new Queue<CircuitComponent>();

        foreach (CircuitComponent comp in components)
        {
            if (comp is ChipInputNode)
            {
                maxDelay[comp] = 0;
                queue.Enqueue(comp);
            }
        }

        while (queue.Count > 0)
        {
            CircuitComponent current = queue.Dequeue();
            int currentDelay = maxDelay[current];

            if (!compOutputs.ContainsKey(current)) continue;

            foreach (OutputPin outPin in compOutputs[current])
            {
                List<InputPin> connected = outPin.GetConnectedPins();
                if (connected == null) continue;

                foreach (InputPin downstreamPin in connected)
                {
                    if (!pinOwner.ContainsKey(downstreamPin)) continue;

                    CircuitComponent downstream = pinOwner[downstreamPin];
                    int newDelay = currentDelay + downstream.GetDelay();

                    if (!maxDelay.ContainsKey(downstream) || newDelay > maxDelay[downstream])
                    {
                        maxDelay[downstream] = newDelay;
                        queue.Enqueue(downstream);
                    }
                }
            }
        }

        int[] result = new int[outputNodes.Count];
        for (int i = 0; i < outputNodes.Count; i++)
        {
            if (maxDelay.ContainsKey(outputNodes[i]))
                result[i] = maxDelay[outputNodes[i]];
        }

        return result;
    }
}

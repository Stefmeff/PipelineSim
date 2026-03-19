using System.Collections.Generic;

/**
 * @brief Calculates the maximum delay path through a custom chip's internal circuit.
 *
 * @details Uses a BFS-based longest-path traversal: starts from ChipInputNodes (delay 0),
 * propagates cumulative delay through components via their pin connections, and reads
 * the maximum delay at ChipOutputNodes. Handles nested custom chips recursively via GetDelay().
 **/
public static class ChipDelayCalculator
{
    /**
     * @brief Calculates the maximum propagation delay across all paths from any input to any output.
     *
     * @details Builds an adjacency graph from pin connections, then runs BFS from all
     * ChipInputNodes simultaneously, tracking the longest path to each component.
     *
     * @param[in] components all components in the internal circuit
     * @return the maximum cumulative delay in ticks from any input to any output
     **/
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

        // Step 2: Build output pin map for traversal
        Dictionary<CircuitComponent, List<OutputPin>> compOutputs = new Dictionary<CircuitComponent, List<OutputPin>>();
        foreach (CircuitComponent comp in components)
        {
            List<InputPin> ins = new List<InputPin>();
            List<OutputPin> outs = new List<OutputPin>();
            comp.CollectPins(ins, outs);
            compOutputs[comp] = outs;
        }

        // Step 3: BFS longest-path from ChipInputNodes
        Dictionary<CircuitComponent, int> maxDelay = new Dictionary<CircuitComponent, int>();
        Queue<CircuitComponent> queue = new Queue<CircuitComponent>();

        // Seed: ChipInputNodes have 0 delay (pass-throughs)
        foreach (CircuitComponent comp in components)
        {
            if (comp is ChipInputNode)
            {
                maxDelay[comp] = 0;
                queue.Enqueue(comp);
            }
        }

        // Process in BFS order, updating max delay at each downstream component
        // Track visit count per component to detect and break cycles
        Dictionary<CircuitComponent, int> visitCount = new Dictionary<CircuitComponent, int>();
        int maxVisits = components.Count + 1; // in a DAG, no component is visited more than N times

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
                        // Break cycles: stop re-enqueueing if visited too many times
                        if (!visitCount.ContainsKey(downstream)) visitCount[downstream] = 0;
                        visitCount[downstream]++;
                        if (visitCount[downstream] > maxVisits) continue;

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

    /**
     * @brief Calculates the max delay per output pin, indexed matching chipDef.outputs order.
     *
     * @details Runs the same BFS longest-path algorithm as CalculateMaxDelay, but returns
     * per-output results instead of a single maximum.
     *
     * @param[in] components all components in the internal circuit
     * @param[in] outputNodes the ChipOutputNodes in chipDef.outputs order
     * @return array where element i is the max delay to outputNodes[i]
     **/
    public static int[] CalculatePerOutputDelay(List<CircuitComponent> components, List<ChipOutputNode> outputNodes)
    {
        if (components == null || outputNodes == null) return new int[0];

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

        Dictionary<CircuitComponent, int> visitCount = new Dictionary<CircuitComponent, int>();
        int maxVisits = components.Count + 1;

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
                        if (!visitCount.ContainsKey(downstream)) visitCount[downstream] = 0;
                        visitCount[downstream]++;
                        if (visitCount[downstream] > maxVisits) continue;

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

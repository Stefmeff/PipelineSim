using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDelay
{
    public int parseDelay(string inputDelay);

    public void VisualizeDelay(bool on);

    public bool IsVisualizerActive();
}

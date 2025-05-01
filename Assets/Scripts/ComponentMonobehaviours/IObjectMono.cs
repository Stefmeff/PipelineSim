using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IObjectMono
{
    public CircuitComponent GetMain();
    public void SaveTransform();
    public void Clear();
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IObjectMono
{
    public Loadable GetMain();
    public void SaveTransform();
    public void Clear();
}

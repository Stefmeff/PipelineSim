using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IObjectMono
{
    public ILoadable GetMain();
    public void SaveTransform();
    public void Clear();
}

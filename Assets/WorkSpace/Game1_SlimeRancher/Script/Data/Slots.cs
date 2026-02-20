using System;
using UnityEngine;

[Serializable]
public struct Slots
{
    public int id;
    public int count;

    public bool IsEmpty => id == 0 || count <= 0;

    public void Clear()
    {
        id = 0;
        count = 0;
    }
}

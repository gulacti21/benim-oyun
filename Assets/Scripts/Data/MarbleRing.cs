using System;
using UnityEngine;

[Serializable]
public struct MarbleRing
{
    [Min(0)] public int count;
    [Range(0f, 1f)] public float radiusFactor;
    public float angleOffset;
}

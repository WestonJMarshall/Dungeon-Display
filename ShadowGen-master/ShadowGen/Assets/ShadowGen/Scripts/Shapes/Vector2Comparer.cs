using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vector2Comparer : IEqualityComparer<Vector2>
{
    public bool Equals(Vector2 v1, Vector2 v2)
    {
        return (v1.x == v2.x && v1.y == v2.y);
    }

    public int GetHashCode(Vector2 vector)
    {
        return Mathf.FloorToInt(vector.x) ^ Mathf.FloorToInt(vector.y) << 2;
    }
}
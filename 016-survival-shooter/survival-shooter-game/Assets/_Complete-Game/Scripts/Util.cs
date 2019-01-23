using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Util  {
    public static readonly Vector2 MinimapMinBoundary = new Vector2(-1, -1);
    public static readonly Vector2 MinimapMaxBoundary = new Vector2(2, 2);

    public static float ScaleValue(float value, float valueMin, float valueMax, float outMin, float outMax)
    {
        return (value - valueMin) / (valueMax - valueMin) * (outMax - outMin) + outMin;
    }
}

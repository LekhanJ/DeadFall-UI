using UnityEngine;

public static class FloatExtensions
{
    public static float TwoDecimals(this float value)
    {
        return Mathf.Round(value * 100f) / 100f;
    }
}

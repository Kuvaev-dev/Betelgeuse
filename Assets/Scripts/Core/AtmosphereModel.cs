using UnityEngine;

public static class AtmosphereModel
{
    public static float GetDensity(float altitude)
    {
        if (altitude < 0)
        {
            return 1.225f;
        }

        if (altitude > 85000f)
        {
            return 0f;
        }

        return 1.225f * Mathf.Exp(-altitude * 0.0001184f);
    }

    public static float GetGravity(float altitude)
    {
        const float R = 6371000f;
        return 9.80665f * Mathf.Pow(R / (R + altitude), 2);
    }
}
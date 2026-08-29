using UnityEngine;

/// <summary>
/// Seeded PRNG для відтворюваних Monte-Carlo / збурень польоту.
/// Обгортка над <see cref="System.Random"/>; незалежно від стану UnityEngine.Random.
/// </summary>
public static class SimRng
{
    static System.Random rng = new System.Random(42);
    static int currentSeed = 42;

    public static int CurrentSeed => currentSeed;

    /// <summary>Скинути потік з абсолютного seed (старт експерименту).</summary>
    public static void Reseed(int seed)
    {
        currentSeed = seed;
        rng = new System.Random(seed);
    }

    /// <summary>Отримати стабільний дочірній seed (напр. за індексом trial) без споживання потоку.</summary>
    public static int DeriveSeed(int baseSeed, int salt)
    {
        unchecked
        {
            uint x = (uint)baseSeed;
            x ^= (uint)salt * 0x9E3779B9u;
            x ^= x >> 16;
            x *= 0x7FEB352Du;
            x ^= x >> 15;
            x *= 0x846CA68Bu;
            x ^= x >> 16;
            return (int)(x & 0x7FFFFFFF);
        }
    }

    public static float Value => (float)rng.NextDouble();

    public static float Range(float minInclusive, float maxInclusive)
    {
        if (maxInclusive < minInclusive)
        {
            float t = minInclusive;
            minInclusive = maxInclusive;
            maxInclusive = t;
        }
        return minInclusive + (float)rng.NextDouble() * (maxInclusive - minInclusive);
    }

    public static int Range(int minInclusive, int maxExclusive) => rng.Next(minInclusive, maxExclusive);

    public static Vector3 InsideUnitSphere()
    {
        // Метод Marsaglia на поверхні одиничної кулі, далі масштаб радіуса cube-root
        float u, v, s;
        do
        {
            u = Value * 2f - 1f;
            v = Value * 2f - 1f;
            s = u * u + v * v;
        } while (s >= 1f || s < 1e-8f);
        float f = Mathf.Sqrt(-2f * Mathf.Log(s) / s);
        float x = u * f;
        float y = v * f;
        float z = Range(-1f, 1f);
        Vector3 d = new Vector3(x, y, z);
        float m = d.magnitude;
        if (m < 1e-6f) return Vector3.forward;
        return d / m * Mathf.Pow(Value, 1f / 3f);
    }
}

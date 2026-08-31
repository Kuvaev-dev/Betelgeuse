using UnityEngine;

/// <summary>
/// Бортова навігація 1-го ступеня (автономна система, не god-mode).
/// Сенсори: IMU (акселерометр + гіроскоп), радіовисотомір, GPS-подібний.
/// Оцінювач: complementary filter (IMU predict + GNSS/висотомір correct).
/// Фізичний plant лишається істинним <see cref="RocketState"/>; GNC споживає оцінку.
/// Це не industrial Kalman/INS — дипломний замкнений контур.
/// </summary>
public sealed class NavigationEstimator
{
    public struct Snapshot
    {
        public Vector3 position;
        public Vector3 velocity;
        public Quaternion rotation;
        public Vector3 angularVelocity;
        public float heightResid;
        public float horizResid;
        public bool valid;
    }

    public Snapshot Current;
    public float NoiseScale;
    public uint Seed = 1;
    public int Tick { get; private set; }

    Vector3 pos;
    Vector3 vel;
    Quaternion att = Quaternion.identity;
    Vector3 omega;
    bool initialized;

    // 1σ при NoiseScale = 1 (скромні, щоб MC не падав у 0%)
    public const float SigmaGyroRps = 0.004f;
    public const float SigmaAccel = 0.08f;
    public const float SigmaAlt = 0.35f;
    public const float SigmaGpsHoriz = 1.0f;
    public const float SigmaGpsVel = 0.12f;

    const float GpsPosAlpha = 0.12f;
    const float AltAlpha = 0.18f;
    const float GpsVelAlpha = 0.10f;
    const float AccelTiltAlpha = 0.04f;

    public void Reset(RocketState truth, uint seed = 1)
    {
        Seed = seed == 0 ? 1u : seed;
        Tick = 0;
        initialized = truth != null;
        if (!initialized)
        {
            Current = default;
            return;
        }

        pos = truth.position;
        vel = truth.velocity;
        att = truth.rotation;
        omega = truth.angularVelocity;
        Current = MakeSnapshot(truth);
    }

    public void Step(RocketState truth, Vector3 trueAccelWorld, float dt, float noiseScale)
    {
        if (truth == null || dt <= 1e-6f) return;
        NoiseScale = Mathf.Max(0f, noiseScale);
        if (!initialized) Reset(truth, Seed);

        Tick++;
        float n = NoiseScale;
        float g = AtmosphereModel.GetGravity(Mathf.Max(0f, truth.position.y));

        // --- Сенсори (детермінований шум від seed+tick — paired MC) ---
        Vector3 gyro = truth.angularVelocity
                       + n * SigmaGyroRps * new Vector3(N(1), N(2), N(3));

        Vector3 specWorld = trueAccelWorld + Vector3.up * g;
        Vector3 specBody = Quaternion.Inverse(truth.rotation) * specWorld
                           + n * SigmaAccel * new Vector3(N(4), N(5), N(6));

        float altMeas = truth.position.y + n * SigmaAlt * N(7);
        Vector3 gpsPos = truth.position + n * SigmaGpsHoriz * new Vector3(N(8), N(10) * 0.35f, N(9));
        Vector3 gpsVel = truth.velocity + n * SigmaGpsVel * new Vector3(N(11), N(13), N(12));

        // --- Predict (IMU) ---
        omega = gyro;
        float wMag = omega.magnitude;
        if (wMag > 1e-8f)
        {
            float deg = wMag * dt * Mathf.Rad2Deg;
            att = Quaternion.Normalize(att * Quaternion.AngleAxis(deg, omega / wMag));
        }

        Vector3 specWorldEst = att * specBody;
        Vector3 aEst = specWorldEst - Vector3.up * g;
        vel += aEst * dt;
        pos += vel * dt;

        // --- Correct (GNSS + висотомір) ---
        pos.x = Mathf.Lerp(pos.x, gpsPos.x, GpsPosAlpha);
        pos.z = Mathf.Lerp(pos.z, gpsPos.z, GpsPosAlpha);
        pos.y = Mathf.Lerp(pos.y, altMeas, AltAlpha);
        vel = Vector3.Lerp(vel, gpsVel, GpsVelAlpha);

        // Легка корекція крену акселерометром, коли |f| ≈ g (мала тяга)
        float specMag = specBody.magnitude;
        if (specMag > 1f && Mathf.Abs(specMag - g) < g * 0.28f)
        {
            Vector3 accUpWorld = att * specBody.normalized;
            Quaternion delta = Quaternion.FromToRotation(accUpWorld, Vector3.up);
            att = Quaternion.Slerp(att, delta * att, AccelTiltAlpha);
        }

        Current = MakeSnapshot(truth);
    }

    public ControlContext ToContext(RocketState truth, float dt)
    {
        Vector3 up = att * Vector3.up;
        float pitchError = Vector3.SignedAngle(up, Vector3.up, Vector3.right);
        float yawError = Vector3.SignedAngle(up, Vector3.up, Vector3.forward);
        float pitchRate = omega.x * Mathf.Rad2Deg;
        float yawRate = omega.z * Mathf.Rad2Deg;
        float horizSpeed = new Vector2(vel.x, vel.z).magnitude;
        float tilt = Vector3.Angle(up, Vector3.up);
        return new ControlContext(
            pos.y, vel.y, truth.TotalMass, truth.currentThrust, truth.maxThrust,
            pitchError, yawError, pitchRate, yawRate, horizSpeed, tilt, dt,
            att, omega,
            pos.x, pos.z, vel.x, vel.z);
    }

    Snapshot MakeSnapshot(RocketState truth)
    {
        return new Snapshot
        {
            position = pos,
            velocity = vel,
            rotation = att,
            angularVelocity = omega,
            heightResid = Mathf.Abs(pos.y - truth.position.y),
            horizResid = new Vector2(pos.x - truth.position.x, pos.z - truth.position.z).magnitude,
            valid = true
        };
    }

    /// <summary>Детермінований N(0,1) з (seed, tick, channel) — однакова послідовність на paired trial.</summary>
    float N(int channel)
    {
        uint h = Seed ^ (uint)(Tick * 747796405u) ^ (uint)(channel * 2891336453u);
        h ^= h >> 16;
        h *= 0x7feb352du;
        h ^= h >> 15;
        h *= 0x846ca68bu;
        h ^= h >> 16;
        uint h2 = h * 0x9E3779B9u + 0x85ebca6bu;
        h2 ^= h2 >> 13;
        float u1 = ((h & 0xFFFFFFu) + 1f) / 16777216f;
        float u2 = ((h2 & 0xFFFFFFu) + 1f) / 16777216f;
        return Mathf.Sqrt(-2f * Mathf.Log(Mathf.Max(1e-6f, u1))) * Mathf.Cos(2f * Mathf.PI * u2);
    }
}

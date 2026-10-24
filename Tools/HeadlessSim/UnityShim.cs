// Мінімальна заміна UnityEngine для headless-збирання ядра симуляції поза Unity
// (навчання ES та пакетний Monte-Carlo на .NET). Семантика повторює UnityEngine
// для типів, які використовує ядро: Vector2/3, Quaternion (порядок Euler ZXY), Mathf, Debug.
// У Unity цей файл НЕ компілюється (лежить поза Assets/).
using System;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Field)] public sealed class HeaderAttribute : Attribute { public HeaderAttribute(string h) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string t) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class RangeAttribute : Attribute { public RangeAttribute(float a, float b) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public sealed class HideInInspector : Attribute { }

    public static class Debug
    {
        public static bool Quiet;
        public static void Log(object m) { if (!Quiet) Console.WriteLine(m); }
        public static void LogWarning(object m) { Console.WriteLine("WARN: " + m); }
        public static void LogError(object m) { Console.WriteLine("ERROR: " + m); }
    }

    public static class Mathf
    {
        public const float PI = (float)Math.PI;
        public const float Deg2Rad = PI / 180f;
        public const float Rad2Deg = 180f / PI;
        public const float Epsilon = 1.401298E-45f;
        public static float Sin(float f) => MathF.Sin(f);
        public static float Cos(float f) => MathF.Cos(f);
        public static float Tan(float f) => MathF.Tan(f);
        public static float Asin(float f) => MathF.Asin(f);
        public static float Acos(float f) => MathF.Acos(f);
        public static float Atan(float f) => MathF.Atan(f);
        public static float Atan2(float y, float x) => MathF.Atan2(y, x);
        public static float Sqrt(float f) => MathF.Sqrt(f);
        public static float Abs(float f) => MathF.Abs(f);
        public static int Abs(int v) => Math.Abs(v);
        public static float Min(float a, float b) => a < b ? a : b;
        public static float Max(float a, float b) => a > b ? a : b;
        public static int Min(int a, int b) => a < b ? a : b;
        public static int Max(int a, int b) => a > b ? a : b;
        public static float Pow(float f, float p) => MathF.Pow(f, p);
        public static float Exp(float p) => MathF.Exp(p);
        public static float Log(float f) => MathF.Log(f);
        public static float Log10(float f) => MathF.Log10(f);
        public static float Floor(float f) => MathF.Floor(f);
        public static float Ceil(float f) => MathF.Ceiling(f);
        public static float Round(float f) => MathF.Round(f);
        public static int FloorToInt(float f) => (int)MathF.Floor(f);
        public static int CeilToInt(float f) => (int)MathF.Ceiling(f);
        public static int RoundToInt(float f) => (int)MathF.Round(f);
        public static float Sign(float f) => f >= 0f ? 1f : -1f;
        public static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
        public static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);
        public static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float LerpUnclamped(float a, float b, float t) => a + (b - a) * t;
        public static float InverseLerp(float a, float b, float v) => a != b ? Clamp01((v - a) / (b - a)) : 0f;
        public static float MoveTowards(float c, float t, float d) => Abs(t - c) <= d ? t : c + Sign(t - c) * d;
        public static float SmoothStep(float from, float to, float t)
        {
            t = Clamp01(t);
            t = -2f * t * t * t + 3f * t * t;
            return to * t + from * (1f - t);
        }
        public static bool Approximately(float a, float b)
            => Abs(b - a) < Max(1E-06f * Max(Abs(a), Abs(b)), Epsilon * 8f);
    }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public float magnitude => MathF.Sqrt(x * x + y * y);
        public float sqrMagnitude => x * x + y * y;
        public static Vector2 zero => new Vector2(0, 0);
        public Vector2 normalized { get { float m = magnitude; return m > 1e-5f ? new Vector2(x / m, y / m) : zero; } }
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator -(Vector2 a) => new Vector2(-a.x, -a.y);
        public static Vector2 operator *(Vector2 a, float d) => new Vector2(a.x * d, a.y * d);
        public static Vector2 operator *(float d, Vector2 a) => new Vector2(a.x * d, a.y * d);
        public static Vector2 operator /(Vector2 a, float d) => new Vector2(a.x / d, a.y / d);
        public static Vector2 ClampMagnitude(Vector2 v, float max) => v.sqrMagnitude > max * max ? v.normalized * max : v;
        public override string ToString() => $"({x:F2}, {y:F2})";
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3(0, 0, 0);
        public static Vector3 one => new Vector3(1, 1, 1);
        public static Vector3 up => new Vector3(0, 1, 0);
        public static Vector3 down => new Vector3(0, -1, 0);
        public static Vector3 right => new Vector3(1, 0, 0);
        public static Vector3 forward => new Vector3(0, 0, 1);
        public float magnitude => MathF.Sqrt(x * x + y * y + z * z);
        public float sqrMagnitude => x * x + y * y + z * z;
        public Vector3 normalized { get { float m = magnitude; return m > 1e-5f ? new Vector3(x / m, y / m, z / m) : zero; } }
        public void Normalize() { this = normalized; }
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator -(Vector3 a) => new Vector3(-a.x, -a.y, -a.z);
        public static Vector3 operator *(Vector3 a, float d) => new Vector3(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator *(float d, Vector3 a) => new Vector3(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator /(Vector3 a, float d) => new Vector3(a.x / d, a.y / d, a.z / d);
        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public static Vector3 Cross(Vector3 a, Vector3 b)
            => new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) { t = Mathf.Clamp01(t); return a + (b - a) * t; }
        public static float Distance(Vector3 a, Vector3 b) => (a - b).magnitude;
        public static Vector3 ClampMagnitude(Vector3 v, float max) => v.sqrMagnitude > max * max ? v.normalized * max : v;
        public static float Angle(Vector3 from, Vector3 to)
        {
            float d = MathF.Sqrt(from.sqrMagnitude * to.sqrMagnitude);
            if (d < 1e-15f) return 0f;
            float dot = Mathf.Clamp(Dot(from, to) / d, -1f, 1f);
            return MathF.Acos(dot) * Mathf.Rad2Deg;
        }
        public static float SignedAngle(Vector3 from, Vector3 to, Vector3 axis)
        {
            float a = Angle(from, to);
            var c = Cross(from, to);
            return a * Mathf.Sign(axis.x * c.x + axis.y * c.y + axis.z * c.z);
        }
        public static Vector3 RotateTowards(Vector3 current, Vector3 target, float maxRadians, float maxMag)
        {
            float cm = current.magnitude, tm = target.magnitude;
            if (cm < 1e-6f || tm < 1e-6f) return Vector3.MoveTowards(current, target, maxMag);
            Vector3 cn = current / cm, tn = target / tm;
            float dot = Mathf.Clamp(Dot(cn, tn), -1f, 1f);
            float ang = MathF.Acos(dot);
            float newMag = Mathf.MoveTowards(cm, tm, maxMag);
            if (ang <= maxRadians || ang < 1e-7f) return tn * newMag;
            Vector3 axis = Cross(cn, tn);
            if (axis.sqrMagnitude < 1e-12f) axis = Cross(cn, MathF.Abs(cn.x) < 0.9f ? right : up);
            var q = Quaternion.AngleAxis(maxRadians * Mathf.Rad2Deg, axis.normalized);
            return (q * cn) * newMag;
        }
        public static Vector3 MoveTowards(Vector3 c, Vector3 t, float d)
        {
            var v = t - c; float m = v.magnitude;
            return m <= d || m == 0f ? t : c + v / m * d;
        }
        public float this[int i] => i == 0 ? x : (i == 1 ? y : z);
        public override string ToString() => $"({x:F2}, {y:F2}, {z:F2})";
    }

    public struct Quaternion
    {
        public float x, y, z, w;
        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Quaternion identity => new Quaternion(0, 0, 0, 1);

        public static Quaternion operator *(Quaternion a, Quaternion b) => new Quaternion(
            a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y,
            a.w * b.y + a.y * b.w + a.z * b.x - a.x * b.z,
            a.w * b.z + a.z * b.w + a.x * b.y - a.y * b.x,
            a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z);

        public static Vector3 operator *(Quaternion q, Vector3 p)
        {
            float x2 = q.x * 2f, y2 = q.y * 2f, z2 = q.z * 2f;
            float xx = q.x * x2, yy = q.y * y2, zz = q.z * z2;
            float xy = q.x * y2, xz = q.x * z2, yz = q.y * z2;
            float wx = q.w * x2, wy = q.w * y2, wz = q.w * z2;
            return new Vector3(
                (1f - (yy + zz)) * p.x + (xy - wz) * p.y + (xz + wy) * p.z,
                (xy + wz) * p.x + (1f - (xx + zz)) * p.y + (yz - wx) * p.z,
                (xz - wy) * p.x + (yz + wx) * p.y + (1f - (xx + yy)) * p.z);
        }

        public static Quaternion AngleAxis(float angleDeg, Vector3 axis)
        {
            var n = axis.normalized;
            float h = angleDeg * Mathf.Deg2Rad * 0.5f, s = MathF.Sin(h);
            return new Quaternion(n.x * s, n.y * s, n.z * s, MathF.Cos(h));
        }

        /// <summary>Порядок Unity: спочатку Z, потім X, потім Y.</summary>
        public static Quaternion Euler(float x, float y, float z)
            => AngleAxis(y, Vector3.up) * AngleAxis(x, Vector3.right) * AngleAxis(z, Vector3.forward);
        public static Quaternion Euler(Vector3 e) => Euler(e.x, e.y, e.z);

        public static Quaternion Inverse(Quaternion q)
        {
            float n = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
            if (n < 1e-12f) return identity;
            return new Quaternion(-q.x / n, -q.y / n, -q.z / n, q.w / n);
        }

        public static Quaternion Normalize(Quaternion q)
        {
            float m = MathF.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            if (m < 1e-12f) return identity;
            return new Quaternion(q.x / m, q.y / m, q.z / m, q.w / m);
        }

        public static float Dot(Quaternion a, Quaternion b) => a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;

        public static Quaternion FromToRotation(Vector3 from, Vector3 to)
        {
            var f = from.normalized; var t = to.normalized;
            float d = Vector3.Dot(f, t);
            if (d > 0.999999f) return identity;
            if (d < -0.999999f)
            {
                var axis = Vector3.Cross(Vector3.right, f);
                if (axis.sqrMagnitude < 1e-6f) axis = Vector3.Cross(Vector3.up, f);
                return AngleAxis(180f, axis);
            }
            var c = Vector3.Cross(f, t);
            return Normalize(new Quaternion(c.x, c.y, c.z, 1f + d));
        }

        public static Quaternion Slerp(Quaternion a, Quaternion b, float t)
        {
            t = Mathf.Clamp01(t);
            float cos = Dot(a, b);
            if (cos < 0f) { b = new Quaternion(-b.x, -b.y, -b.z, -b.w); cos = -cos; }
            if (cos > 0.9995f)
                return Normalize(new Quaternion(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t, a.w + (b.w - a.w) * t));
            float th = MathF.Acos(cos), s = MathF.Sin(th);
            float wa = MathF.Sin((1 - t) * th) / s, wb = MathF.Sin(t * th) / s;
            return new Quaternion(a.x * wa + b.x * wb, a.y * wa + b.y * wb, a.z * wa + b.z * wb, a.w * wa + b.w * wb);
        }
    }
}

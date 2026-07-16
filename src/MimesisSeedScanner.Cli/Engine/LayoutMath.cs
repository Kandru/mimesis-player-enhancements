namespace MimesisSeedScanner.Cli.Engine
{
    internal readonly struct LVec3
    {
        internal LVec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        internal float X { get; }

        internal float Y { get; }

        internal float Z { get; }

        internal static LVec3 Zero => new(0f, 0f, 0f);

        internal static LVec3 Up => new(0f, 1f, 0f);

        internal float Magnitude => (float)Math.Sqrt(X * X + Y * Y + Z * Z);

        internal LVec3 Normalized
        {
            get
            {
                float mag = Magnitude;
                return mag > 1e-6f ? new LVec3(X / mag, Y / mag, Z / mag) : Zero;
            }
        }

        public static LVec3 operator +(LVec3 a, LVec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static LVec3 operator -(LVec3 a, LVec3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static LVec3 operator *(LVec3 a, float s) => new(a.X * s, a.Y * s, a.Z * s);

        internal static float Dot(LVec3 a, LVec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        internal static float Angle(LVec3 a, LVec3 b)
        {
            float denom = a.Magnitude * b.Magnitude;
            if (denom < 1e-6f)
            {
                return 0f;
            }

            return (float)(Math.Acos(Math.Clamp(Dot(a, b) / denom, -1f, 1f)) * (180.0 / Math.PI));
        }
    }

    internal readonly struct LQuat
    {
        internal LQuat(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        internal float X { get; }

        internal float Y { get; }

        internal float Z { get; }

        internal float W { get; }

        internal static LQuat Identity => new(0f, 0f, 0f, 1f);

        internal static LQuat LookRotation(LVec3 forward, LVec3 up)
        {
            forward = forward.Normalized;
            LVec3 right = Cross(up, forward).Normalized;
            up = Cross(forward, right);

            float m00 = right.X;
            float m01 = right.Y;
            float m02 = right.Z;
            float m10 = up.X;
            float m11 = up.Y;
            float m12 = up.Z;
            float m20 = forward.X;
            float m21 = forward.Y;
            float m22 = forward.Z;

            float trace = m00 + m11 + m22;
            if (trace > 0f)
            {
                float s = (float)Math.Sqrt(trace + 1f) * 2f;
                return new LQuat(
                    (m12 - m21) / s,
                    (m20 - m02) / s,
                    (m01 - m10) / s,
                    0.25f * s);
            }

            if (m00 > m11 && m00 > m22)
            {
                float s = (float)Math.Sqrt(1f + m00 - m11 - m22) * 2f;
                return new LQuat(0.25f * s, (m01 + m10) / s, (m20 + m02) / s, (m12 - m21) / s);
            }

            if (m11 > m22)
            {
                float s = (float)Math.Sqrt(1f + m11 - m00 - m22) * 2f;
                return new LQuat((m01 + m10) / s, 0.25f * s, (m12 + m21) / s, (m20 - m02) / s);
            }

            {
                float s = (float)Math.Sqrt(1f + m22 - m00 - m11) * 2f;
                return new LQuat((m20 + m02) / s, (m12 + m21) / s, 0.25f * s, (m01 - m10) / s);
            }
        }

        internal static LQuat Inverse(LQuat q) => new(-q.X, -q.Y, -q.Z, q.W);

        internal static LQuat Multiply(LQuat a, LQuat b) =>
            new(
                a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
                a.W * b.Y + a.Y * b.W + a.Z * b.X - a.X * b.Z,
                a.W * b.Z + a.Z * b.W + a.X * b.Y - a.Y * b.X,
                a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z);

        internal static LVec3 Rotate(LQuat q, LVec3 v)
        {
            LVec3 u = new(q.X, q.Y, q.Z);
            float s = q.W;
            return u * (2f * LVec3.Dot(u, v))
                   + v * (s * s - LVec3.Dot(u, u))
                   + Cross(u, v) * (2f * s);
        }

        private static LVec3 Cross(LVec3 a, LVec3 b) =>
            new(
                a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X);
    }

    internal struct LBounds
    {
        internal LVec3 Center;
        internal LVec3 Size;

        internal LBounds(LVec3 center, LVec3 size)
        {
            Center = center;
            Size = size;
        }

        internal LVec3 Min => Center - Size * 0.5f;

        internal LVec3 Max => Center + Size * 0.5f;

        internal void Encapsulate(LBounds other)
        {
            LVec3 min = Min;
            LVec3 max = Max;
            LVec3 otherMin = other.Min;
            LVec3 otherMax = other.Max;
            LVec3 newMin = new(
                Math.Min(min.X, otherMin.X),
                Math.Min(min.Y, otherMin.Y),
                Math.Min(min.Z, otherMin.Z));
            LVec3 newMax = new(
                Math.Max(max.X, otherMax.X),
                Math.Max(max.Y, otherMax.Y),
                Math.Max(max.Z, otherMax.Z));
            Center = (newMin + newMax) * 0.5f;
            Size = newMax - newMin;
        }

        internal static bool AreOverlapping(LBounds a, LBounds b, float maxOverlap)
        {
            LVec3 overlap = CalculateOverlap(a, b);
            return Math.Min(overlap.X, Math.Min(overlap.Y, overlap.Z)) > maxOverlap;
        }

        private static LVec3 CalculateOverlap(LBounds a, LBounds b)
        {
            float ax = a.Max.X - b.Min.X;
            float bx = b.Max.X - a.Min.X;
            float ay = a.Max.Y - b.Min.Y;
            float by = b.Max.Y - a.Min.Y;
            float az = a.Max.Z - b.Min.Z;
            float bz = b.Max.Z - a.Min.Z;
            return new LVec3(Math.Min(ax, bx), Math.Min(ay, by), Math.Min(az, bz));
        }
    }

    internal static class LayoutMath
    {
        internal static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;

        internal static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;

        internal static float EvaluateDepthWeight(float[] samples, float normalizedDepth)
        {
            if (samples.Length == 0)
            {
                return 1f;
            }

            if (samples.Length == 1)
            {
                return samples[0];
            }

            float t = Clamp01(normalizedDepth) * (samples.Length - 1);
            int i0 = (int)Math.Floor(t);
            int i1 = Math.Min(i0 + 1, samples.Length - 1);
            float frac = t - i0;
            return samples[i0] * (1f - frac) + samples[i1] * frac;
        }
    }
}

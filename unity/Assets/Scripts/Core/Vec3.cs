using System;

namespace NeonArena.Core
{
    /// <summary>
    /// Vetor 3D próprio. Existe para que a pasta Core não dependa de UnityEngine:
    /// assim o mesmo código compila dentro do Unity e dentro do projeto de testes
    /// em .NET puro. Converta para Vector3 só na borda (ver UnityBridge.cs).
    /// </summary>
    public struct Vec3 : IEquatable<Vec3>
    {
        public float X, Y, Z;

        public Vec3(float x, float y, float z) { X = x; Y = y; Z = z; }

        public static readonly Vec3 Zero = new Vec3(0, 0, 0);
        public static readonly Vec3 Up   = new Vec3(0, 1, 0);

        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator -(Vec3 a)         => new Vec3(-a.X, -a.Y, -a.Z);
        public static Vec3 operator *(Vec3 a, float s) => new Vec3(a.X * s, a.Y * s, a.Z * s);
        public static Vec3 operator *(float s, Vec3 a) => a * s;
        public static Vec3 operator /(Vec3 a, float s) => new Vec3(a.X / s, a.Y / s, a.Z / s);

        public float this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0: return X;
                    case 1: return Y;
                    case 2: return Z;
                    default: throw new IndexOutOfRangeException(nameof(i));
                }
            }
        }

        public float LengthSq => X * X + Y * Y + Z * Z;
        public float Length => (float)Math.Sqrt(LengthSq);

        /// <summary>Normaliza; vetor nulo volta nulo em vez de virar NaN.</summary>
        public Vec3 Normalized
        {
            get
            {
                float l = Length;
                return l > 1e-8f ? this / l : Zero;
            }
        }

        public static float Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        public static Vec3 Cross(Vec3 a, Vec3 b) => new Vec3(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X);

        public static float Distance(Vec3 a, Vec3 b) => (a - b).Length;

        /// <summary>Distância no plano XZ — a arena é plana, quase toda lógica ignora Y.</summary>
        public static float DistanceXZ(Vec3 a, Vec3 b)
        {
            float dx = a.X - b.X, dz = a.Z - b.Z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        public bool Equals(Vec3 o) => X.Equals(o.X) && Y.Equals(o.Y) && Z.Equals(o.Z);
        public override bool Equals(object o) => o is Vec3 v && Equals(v);
        public override int GetHashCode() => (X, Y, Z).GetHashCode();
        public override string ToString() => $"({X:0.###}, {Y:0.###}, {Z:0.###})";
    }
}

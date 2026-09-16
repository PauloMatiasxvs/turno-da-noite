using System;
using System.Collections.Generic;

namespace NeonArena.Core
{
    /// <summary>Caixa alinhada aos eixos. É toda a geometria sólida da arena.</summary>
    public struct Box
    {
        public Vec3 Min, Max;

        public Box(Vec3 min, Vec3 max) { Min = min; Max = max; }

        /// <summary>Constrói pelo centro e pelo tamanho total (não meio-tamanho).</summary>
        public static Box FromCenter(float cx, float cy, float cz, float sx, float sy, float sz)
        {
            return new Box(
                new Vec3(cx - sx / 2f, cy - sy / 2f, cz - sz / 2f),
                new Vec3(cx + sx / 2f, cy + sy / 2f, cz + sz / 2f));
        }

        public Vec3 Center => new Vec3((Min.X + Max.X) / 2f, (Min.Y + Max.Y) / 2f, (Min.Z + Max.Z) / 2f);
        public Vec3 Size => new Vec3(Max.X - Min.X, Max.Y - Min.Y, Max.Z - Min.Z);

        public bool Contains(Vec3 p) =>
            p.X >= Min.X && p.X <= Max.X &&
            p.Y >= Min.Y && p.Y <= Max.Y &&
            p.Z >= Min.Z && p.Z <= Max.Z;
    }

    /// <summary>
    /// Interseções e resolução de colisão. Mesma matemática da versão WebGL —
    /// é aqui que mora o que o Unity normalmente esconde atrás de Physics.Raycast.
    /// </summary>
    public static class Geometry
    {
        public const float NoHit = float.PositiveInfinity;

        /// <summary>
        /// Raio contra caixa pelo método das fatias (slab method).
        /// Devolve a distância até a entrada, ou NoHit. Espera <paramref name="dir"/> normalizado.
        /// Origem dentro da caixa devolve 0.
        /// </summary>
        public static float RayBox(Vec3 origin, Vec3 dir, Box box)
        {
            float tNear = float.NegativeInfinity;
            float tFar = float.PositiveInfinity;

            for (int i = 0; i < 3; i++)
            {
                float o = origin[i], d = dir[i], mn = box.Min[i], mx = box.Max[i];

                if (Math.Abs(d) < 1e-8f)
                {
                    // raio paralelo a esta fatia: só passa se já estiver entre os planos
                    if (o < mn || o > mx) return NoHit;
                    continue;
                }

                float a = (mn - o) / d;
                float b = (mx - o) / d;
                if (a > b) { float t = a; a = b; b = t; }
                if (a > tNear) tNear = a;
                if (b < tFar) tFar = b;
            }

            if (tFar < Math.Max(tNear, 0f)) return NoHit;
            if (tNear > 0f) return tNear;
            return tFar > 0f ? 0f : NoHit;
        }

        /// <summary>Raio contra esfera. Devolve a distância à primeira interseção, ou NoHit.</summary>
        public static float RaySphere(Vec3 origin, Vec3 dir, Vec3 center, float radius)
        {
            Vec3 oc = origin - center;
            float b = Vec3.Dot(oc, dir);
            float c = oc.LengthSq - radius * radius;
            float disc = b * b - c;
            if (disc < 0f) return NoHit;

            float s = (float)Math.Sqrt(disc);
            float t = -b - s;
            if (t < 0f) t = -b + s;
            return t < 0f ? NoHit : t;
        }

        /// <summary>Existe geometria sólida entre os dois pontos? Usado pela IA para não atirar na parede.</summary>
        public static bool Blocked(Vec3 from, Vec3 to, IReadOnlyList<Box> boxes)
        {
            Vec3 delta = to - from;
            float dist = delta.Length;
            if (dist < 1e-3f) return false;

            Vec3 dir = delta / dist;
            for (int i = 0; i < boxes.Count; i++)
                if (RayBox(from, dir, boxes[i]) < dist) return true;

            return false;
        }

        /// <summary>
        /// Empurra um círculo (no plano XZ) para fora das caixas que o atravessam.
        /// Caixas cujo topo esteja abaixo de feetY + StepHeight são ignoradas: são
        /// degraus em que se anda por cima, não paredes.
        /// </summary>
        public const float StepHeight = 0.4f;

        public static Vec3 PushOut(Vec3 pos, float radius, float feetY, float bodyHeight, IReadOnlyList<Box> boxes)
        {
            for (int i = 0; i < boxes.Count; i++)
            {
                Box b = boxes[i];
                if (b.Max.Y <= feetY + StepHeight) continue;   // dá para pisar em cima
                if (b.Min.Y >= feetY + bodyHeight) continue;   // passa por baixo

                float cx = Clamp(pos.X, b.Min.X, b.Max.X);
                float cz = Clamp(pos.Z, b.Min.Z, b.Max.Z);
                float dx = pos.X - cx, dz = pos.Z - cz;
                float d2 = dx * dx + dz * dz;
                if (d2 >= radius * radius) continue;

                float d = (float)Math.Sqrt(d2);
                if (d < 1e-5f)
                {
                    // centro exatamente dentro da caixa: sai pela face mais próxima
                    float px = Math.Min(pos.X - b.Min.X, b.Max.X - pos.X);
                    float pz = Math.Min(pos.Z - b.Min.Z, b.Max.Z - pos.Z);
                    if (px < pz) pos.X += pos.X < b.Center.X ? -(px + radius) : (px + radius);
                    else pos.Z += pos.Z < b.Center.Z ? -(pz + radius) : (pz + radius);
                }
                else
                {
                    float push = (radius - d) / d;
                    pos.X += dx * push;
                    pos.Z += dz * push;
                }
            }
            return pos;
        }

        /// <summary>Altura do chão sob o círculo: 0 (piso) ou o topo da caixa mais alta pisável.</summary>
        public static float GroundHeight(Vec3 pos, float radius, IReadOnlyList<Box> boxes)
        {
            float ground = 0f;
            for (int i = 0; i < boxes.Count; i++)
            {
                Box b = boxes[i];
                if (b.Max.Y > pos.Y + StepHeight || b.Max.Y <= ground) continue;

                float cx = Clamp(pos.X, b.Min.X, b.Max.X);
                float cz = Clamp(pos.Z, b.Min.Z, b.Max.Z);
                float dx = pos.X - cx, dz = pos.Z - cz;
                if (dx * dx + dz * dz < radius * radius) ground = b.Max.Y;
            }
            return ground;
        }

        public static float Clamp(float v, float a, float b) => v < a ? a : (v > b ? b : v);
    }
}

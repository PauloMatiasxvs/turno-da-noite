using System.Collections.Generic;

namespace NeonArena.Core
{
    public enum Surface { Wall, Pillar, Crate }

    /// <summary>Uma peça da arena: a caixa de colisão mais o papel visual dela.</summary>
    public struct ArenaPiece
    {
        public Box Box;
        public Surface Surface;
        public bool Neon;       // recebe a faixa luminosa no topo

        public ArenaPiece(Box box, Surface surface, bool neon)
        {
            Box = box; Surface = surface; Neon = neon;
        }
    }

    /// <summary>
    /// A planta da arena, em unidades de mundo. É a mesma lista da versão WebGL —
    /// manter as duas idênticas é o que faz as duas jogarem igual.
    /// </summary>
    public static class ArenaLayout
    {
        public const float Half = 30f;          // meia-largura: arena de 60x60
        public const float WallHeight = 9f;
        public const float WallThickness = 2f;

        public static List<ArenaPiece> Build()
        {
            var pieces = new List<ArenaPiece>();

            void Add(float x, float y, float z, float sx, float sy, float sz, Surface s, bool neon)
                => pieces.Add(new ArenaPiece(Box.FromCenter(x, y, z, sx, sy, sz), s, neon));

            // paredes externas
            float h = WallHeight, t = WallThickness, span = Half * 2f + t * 2f;
            Add(0, h / 2, -Half - t / 2, span, h, t, Surface.Wall, true);
            Add(0, h / 2, Half + t / 2, span, h, t, Surface.Wall, true);
            Add(-Half - t / 2, h / 2, 0, t, h, span, Surface.Wall, true);
            Add(Half + t / 2, h / 2, 0, t, h, span, Surface.Wall, true);

            // pilares altos nas diagonais
            foreach (var p in new[] { (-16f, -16f), (16f, -16f), (-16f, 16f), (16f, 16f) })
                Add(p.Item1, 4, p.Item2, 3.4f, 8, 3.4f, Surface.Pillar, true);

            // pilares menores nos eixos
            foreach (var p in new[] { (0f, -22f), (0f, 22f), (-22f, 0f), (22f, 0f) })
                Add(p.Item1, 3, p.Item2, 2.2f, 6, 2.2f, Surface.Pillar, true);

            // coberturas baixas em cruz — bloqueiam tiro na altura dos olhos
            Add(0, 1.4f, -9, 13, 2.8f, 1.6f, Surface.Crate, true);
            Add(0, 1.4f, 9, 13, 2.8f, 1.6f, Surface.Crate, true);
            Add(-9, 1.4f, 0, 1.6f, 2.8f, 13, Surface.Crate, true);
            Add(9, 1.4f, 0, 1.6f, 2.8f, 13, Surface.Crate, true);

            // engradados: dá para subir e ganhar ângulo
            var crates = new[]
            {
                (-24f, -6f), (-21f, -9f), (24f, 7f), (21f, 10f), (-6f, 24f),
                (7f, -24f), (13f, -14f), (-13f, 14f), (25f, -20f), (-25f, 20f)
            };
            for (int i = 0; i < crates.Length; i++)
            {
                Add(crates[i].Item1, 1.1f, crates[i].Item2, 2.2f, 2.2f, 2.2f, Surface.Crate, true);
                if (i % 3 == 0)
                    Add(crates[i].Item1 + 0.3f, 3.2f, crates[i].Item2 + 0.3f, 1.8f, 1.8f, 1.8f, Surface.Crate, true);
            }

            // plataforma central
            Add(0, 0.55f, 0, 8, 1.1f, 8, Surface.Pillar, true);

            return pieces;
        }

        /// <summary>Só as caixas — é o que a colisão consome.</summary>
        public static List<Box> BuildBoxes()
        {
            var pieces = Build();
            var boxes = new List<Box>(pieces.Count);
            foreach (var p in pieces) boxes.Add(p.Box);
            return boxes;
        }

        /// <summary>Um ponto livre para nascer: dentro da arena e fora de qualquer caixa.</summary>
        public static bool IsClear(float x, float z, float pad, IReadOnlyList<Box> boxes)
        {
            for (int i = 0; i < boxes.Count; i++)
            {
                Box b = boxes[i];
                if (x > b.Min.X - pad && x < b.Max.X + pad &&
                    z > b.Min.Z - pad && z < b.Max.Z + pad) return false;
            }
            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using NeonArena.Core;
using Xunit;

namespace NeonArena.Tests
{
    public class RayBoxTests
    {
        static readonly Box Unit = Box.FromCenter(0, 0, 0, 2, 2, 2); // -1..1 em cada eixo

        [Fact]
        public void AcertaDeFrente()
        {
            float t = Geometry.RayBox(new Vec3(0, 0, -5), new Vec3(0, 0, 1), Unit);
            Assert.Equal(4f, t, 4);
        }

        [Fact]
        public void ErraQuandoAponta_ParaOutroLado()
        {
            float t = Geometry.RayBox(new Vec3(0, 0, -5), new Vec3(0, 0, -1), Unit);
            Assert.Equal(Geometry.NoHit, t);
        }

        [Fact]
        public void ErraPorFora()
        {
            float t = Geometry.RayBox(new Vec3(0, 5, -5), new Vec3(0, 0, 1), Unit);
            Assert.Equal(Geometry.NoHit, t);
        }

        [Fact]
        public void OrigemDentroDaCaixaDaDistanciaZero()
        {
            float t = Geometry.RayBox(Vec3.Zero, new Vec3(1, 0, 0), Unit);
            Assert.Equal(0f, t);
        }

        [Fact]
        public void RaioParaleloRenteAFace_NaoAcerta()
        {
            // anda em X passando de raspão acima do topo da caixa
            float t = Geometry.RayBox(new Vec3(-5, 1.001f, 0), new Vec3(1, 0, 0), Unit);
            Assert.Equal(Geometry.NoHit, t);
        }

        [Fact]
        public void AcertaNaDiagonal()
        {
            var dir = new Vec3(1, 1, 0).Normalized;
            float t = Geometry.RayBox(new Vec3(-3, -3, 0), dir, Unit);
            Assert.True(t < Geometry.NoHit);
            // ponto de entrada tem de cair na superfície da caixa
            Vec3 hit = new Vec3(-3, -3, 0) + dir * t;
            Assert.True(Math.Abs(hit.X + 1f) < 1e-3f || Math.Abs(hit.Y + 1f) < 1e-3f);
        }
    }

    public class RaySphereTests
    {
        [Fact]
        public void AcertaOCentro()
        {
            float t = Geometry.RaySphere(new Vec3(0, 0, -10), new Vec3(0, 0, 1), Vec3.Zero, 1f);
            Assert.Equal(9f, t, 4);
        }

        [Fact]
        public void ErraPorPouco()
        {
            float t = Geometry.RaySphere(new Vec3(0, 1.01f, -10), new Vec3(0, 0, 1), Vec3.Zero, 1f);
            Assert.Equal(Geometry.NoHit, t);
        }

        [Fact]
        public void RaspaTangente()
        {
            float t = Geometry.RaySphere(new Vec3(0, 0.999f, -10), new Vec3(0, 0, 1), Vec3.Zero, 1f);
            Assert.True(t < Geometry.NoHit);
        }

        [Fact]
        public void EsferaAtrasDoRaioNaoConta()
        {
            float t = Geometry.RaySphere(new Vec3(0, 0, 10), new Vec3(0, 0, 1), Vec3.Zero, 1f);
            Assert.Equal(Geometry.NoHit, t);
        }

        [Fact]
        public void OrigemDentroDaEsferaDaASaida()
        {
            float t = Geometry.RaySphere(Vec3.Zero, new Vec3(0, 0, 1), Vec3.Zero, 2f);
            Assert.Equal(2f, t, 4);
        }
    }

    public class CollisionTests
    {
        static List<Box> OneCrate() => new List<Box> { Box.FromCenter(0, 1.1f, 0, 2.2f, 2.2f, 2.2f) };

        [Fact]
        public void EmpurraParaForaDoEngradado()
        {
            var boxes = OneCrate();
            var inside = new Vec3(1f, 0f, 0f);           // dentro da margem do raio
            var outPos = Geometry.PushOut(inside, 0.42f, 0f, 1.7f, boxes);
            Assert.True(outPos.X >= 1.1f + 0.42f - 1e-3f);
            Assert.Equal(0f, outPos.Z, 4);
        }

        [Fact]
        public void NaoEmpurraQuemEstaLonge()
        {
            var boxes = OneCrate();
            var far = new Vec3(8f, 0f, 8f);
            Assert.Equal(far, Geometry.PushOut(far, 0.42f, 0f, 1.7f, boxes));
        }

        [Fact]
        public void QuemEstaEmCimaNaoEEmpurrado()
        {
            // pé na altura do topo (2.2): a caixa vira degrau, não parede
            var boxes = OneCrate();
            var onTop = new Vec3(0f, 2.2f, 0f);
            Assert.Equal(onTop, Geometry.PushOut(onTop, 0.42f, 2.2f, 1.7f, boxes));
        }

        [Fact]
        public void ChaoSobeEmCimaDoEngradado()
        {
            var boxes = OneCrate();
            float g = Geometry.GroundHeight(new Vec3(0, 2.2f, 0), 0.42f, boxes);
            Assert.Equal(2.2f, g, 4);
        }

        [Fact]
        public void ChaoEZeroForaDoEngradado()
        {
            var boxes = OneCrate();
            Assert.Equal(0f, Geometry.GroundHeight(new Vec3(9, 0, 9), 0.42f, boxes));
        }

        [Fact]
        public void NaoSobeEmCaixaAcimaDaCabeca()
        {
            var boxes = OneCrate();
            // no chão, debaixo do engradado: não deve teleportar para o topo
            Assert.Equal(0f, Geometry.GroundHeight(new Vec3(0, 0, 0), 0.42f, boxes));
        }

        [Fact]
        public void CoberturaBloqueiaLinhaDeVisao()
        {
            var boxes = ArenaLayout.BuildBoxes();
            // altura dos olhos, atravessando a cobertura em z = -9
            var from = new Vec3(0, 1.68f, 0);
            var to = new Vec3(0, 1.7f, -20);
            Assert.True(Geometry.Blocked(from, to, boxes));
        }

        [Fact]
        public void EspacoAbertoNaoBloqueia()
        {
            var boxes = ArenaLayout.BuildBoxes();
            var from = new Vec3(-20, 1.68f, -20);
            var to = new Vec3(-20, 1.7f, -14);
            Assert.False(Geometry.Blocked(from, to, boxes));
        }
    }

    public class ArenaTests
    {
        [Fact]
        public void TemAsQuatroParedes()
        {
            var pieces = ArenaLayout.Build();
            int walls = 0;
            foreach (var p in pieces) if (p.Surface == Surface.Wall) walls++;
            Assert.Equal(4, walls);
        }

        [Fact]
        public void ParedesFechamOPerimetro()
        {
            var boxes = ArenaLayout.BuildBoxes();

            // O caminho até a parede não é a meia-largura: num ângulo qualquer o raio
            // anda mais, e o pior caso é a diagonal (45°), onde vale meia-largura * raiz(2).
            float maxReach = (ArenaLayout.Half + ArenaLayout.WallThickness) * 1.4143f;

            for (int i = 0; i < 36; i++)
            {
                double a = i * Math.PI / 18.0;
                var dir = new Vec3((float)Math.Cos(a), 0, (float)Math.Sin(a));
                float best = Geometry.NoHit;
                foreach (var b in boxes)
                {
                    float t = Geometry.RayBox(new Vec3(0, 5f, 0), dir, b);
                    if (t < best) best = t;
                }
                Assert.True(best < maxReach, $"vazou no ângulo {i * 10}° (alcance {best})");
            }
        }

        [Fact]
        public void PontoDentroDeCaixaNaoEstaLivre()
        {
            var boxes = ArenaLayout.BuildBoxes();
            Assert.False(ArenaLayout.IsClear(0, 0, 0.5f, boxes));   // plataforma central
        }
    }
}

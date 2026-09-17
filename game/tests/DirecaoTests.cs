using System;
using TurnoDaNoite.Core;
using Xunit;

namespace TurnoDaNoite.Tests
{
    /// <summary>
    /// Testes do bug que fez o jogador apertar W e andar para trás.
    /// A conta tinha o sinal trocado num termo, o que espelha o movimento:
    /// olhando para o norte parecia certo, olhando para leste ia ao contrário.
    /// </summary>
    public class DirecaoTests
    {
        const float W = -1f;   // tecla W: frente é -Z
        const float S = 1f;
        const float D = 1f;    // tecla D: direita é +X local
        const float A = -1f;

        static void Perto(P2 esperado, P2 veio, string oQue)
        {
            Assert.True(P2.Distancia(esperado, veio) < 1e-4f,
                $"{oQue}: esperava {esperado}, veio {veio}");
        }

        [Fact]
        public void SemGiroWVaiParaMenosZ()
        {
            Perto(new P2(0, -1), Direcao.LocalParaMundo(0, W, 0), "W com giro 0");
        }

        [Fact]
        public void SemGiroDVaiParaMaisX()
        {
            Perto(new P2(1, 0), Direcao.LocalParaMundo(D, 0, 0), "D com giro 0");
        }

        [Fact]
        public void AndarParaFrenteSempreBateComOnde_A_CameraOlha()
        {
            // este é o teste que teria pego o bug: em 16 direções, W tem de
            // coincidir com a frente da câmera. Com o sinal trocado, bate em
            // 0 e 180 graus e erra em todas as outras.
            for (int i = 0; i < 16; i++)
            {
                float giro = i * MathF.PI / 8f;
                Perto(Direcao.Frente(giro), Direcao.LocalParaMundo(0, W, giro),
                      $"W com giro {i * 22.5f:0}°");
            }
        }

        [Fact]
        public void AndarParaDireitaSempreBateComADireitaDaCamera()
        {
            for (int i = 0; i < 16; i++)
            {
                float giro = i * MathF.PI / 8f;
                Perto(Direcao.Direita(giro), Direcao.LocalParaMundo(D, 0, giro),
                      $"D com giro {i * 22.5f:0}°");
            }
        }

        [Fact]
        public void SEhExatamenteOContrarioDeW()
        {
            for (int i = 0; i < 8; i++)
            {
                float giro = i * MathF.PI / 4f;
                var frente = Direcao.LocalParaMundo(0, W, giro);
                var tras = Direcao.LocalParaMundo(0, S, giro);
                Perto(new P2(-frente.X, -frente.Z), tras, $"S com giro {i * 45}°");
            }
        }

        [Fact]
        public void AEhExatamenteOContrarioDeD()
        {
            for (int i = 0; i < 8; i++)
            {
                float giro = i * MathF.PI / 4f;
                var direita = Direcao.LocalParaMundo(D, 0, giro);
                var esquerda = Direcao.LocalParaMundo(A, 0, giro);
                Perto(new P2(-direita.X, -direita.Z), esquerda, $"A com giro {i * 45}°");
            }
        }

        [Fact]
        public void FrenteEDireitaSaoPerpendiculares()
        {
            for (int i = 0; i < 12; i++)
            {
                float giro = i * MathF.PI / 6f;
                float escalar = P2.Escalar(Direcao.Frente(giro), Direcao.Direita(giro));
                Assert.True(MathF.Abs(escalar) < 1e-5f, $"não são perpendiculares em {i * 30}°");
            }
        }

        [Fact]
        public void GirarNaoMudaAVelocidade()
        {
            for (int i = 0; i < 12; i++)
            {
                float giro = i * MathF.PI / 6f;
                Assert.Equal(1f, Direcao.LocalParaMundo(0, W, giro).Comprimento, 4);
                Assert.Equal(1f, Direcao.LocalParaMundo(D, 0, giro).Comprimento, 4);
            }
        }

        [Fact]
        public void AndarNaDiagonalRespeitaOGiro()
        {
            // W+D com giro 90° tem de dar a soma das duas direções daquele giro
            float giro = MathF.PI / 2f;
            var esperado = new P2(
                Direcao.Frente(giro).X + Direcao.Direita(giro).X,
                Direcao.Frente(giro).Z + Direcao.Direita(giro).Z);
            Perto(esperado, Direcao.LocalParaMundo(D, W, giro), "W+D com giro 90°");
        }
    }
}

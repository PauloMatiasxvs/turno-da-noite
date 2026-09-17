using System;
using System.Linq;
using TurnoDaNoite.Core;
using Xunit;

namespace TurnoDaNoite.Tests
{
    /// <summary>
    /// "e eu consigo passar pelos móveis".
    ///
    /// Durante um bom tempo só o cenário solto tinha corpo: caixote e tambor
    /// empurravam, mas gaveteiro, prateleira, armário e quadro elétrico eram
    /// desenho. Justamente os móveis que o jogo pede para você usar. Estes
    /// testes existem para que nenhum móvel volte a ser fantasma em silêncio.
    /// </summary>
    public class ColisaoTests
    {
        static Partida Nova(int semente = 4242)
        {
            var p = new Partida(semente);
            Assert.True(p.Comecar());
            return p;
        }

        [Fact]
        public void TodoMovelEntraNaListaDeSolidos()
        {
            var p = Nova();
            int esperado = p.Recipientes.Count
                         + p.Armarios.Count
                         + p.Adornos.Count(a => a.Solido)
                         + 1;                                  // o quadro elétrico
            Assert.Equal(esperado, p.Solidos.Count);
            Assert.All(p.Solidos, s => Assert.True(s.Raio > 0.01f));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1234)]
        [InlineData(4242)]
        [InlineData(99999)]
        public void AndarContraUmRecipienteNaoAtravessa(int semente)
        {
            var p = Nova(semente);

            foreach (var r in p.Recipientes.Take(12))
            {
                var q = Nova(semente);
                float raio = Regras.RaioDoRecipiente(r.Tipo);

                // encosta a três metros e anda em cima dele por dois segundos
                q.IrPara(new P2(r.Pos.X, r.Pos.Z + 3f), r.Andar);
                q.Jogador.Giro = 0;                 // frente é -Z; queremos +Z
                for (int i = 0; i < 120; i++)
                    q.Passo(1f / 60f, new Comando { FrenteZ = 1f, Giro = 0 });

                float d = P2.Distancia(q.Jogador.Pos, r.Pos);
                Assert.True(d >= raio + Regras.RaioJogador - 0.05f,
                    $"entrei dentro de um {r.Nome}: {d:0.00} m do centro dele");
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(4242)]
        public void AndarContraUmArmarioNaoAtravessa(int semente)
        {
            var p = Nova(semente);
            foreach (var a in p.Armarios.Take(10))
            {
                var q = Nova(semente);
                q.IrPara(new P2(a.Pos.X, a.Pos.Z + 3f), a.Andar);
                for (int i = 0; i < 120; i++)
                    q.Passo(1f / 60f, new Comando { FrenteZ = 1f, Giro = 0 });

                // Ou ele foi barrado, ou entrou no armário (que é a interação
                // legítima e teleporta para dentro dele). O que não pode é
                // estar no meio dele, de pé, andando.
                if (q.Jogador.Escondido) continue;
                Assert.True(P2.Distancia(q.Jogador.Pos, a.Pos) >= Regras.RaioArmario + Regras.RaioJogador - 0.05f,
                    "atravessei o armário");
            }
        }

        [Fact]
        public void OQuadroEletricoTambemBarra()
        {
            var p = Nova();
            p.IrPara(new P2(p.Quadro.X, p.Quadro.Z + 3f), p.QuadroAndar);
            for (int i = 0; i < 120; i++)
                p.Passo(1f / 60f, new Comando { FrenteZ = 1f, Giro = 0 });

            Assert.True(P2.Distancia(p.Jogador.Pos, p.Quadro) >= Regras.RaioQuadro + Regras.RaioJogador - 0.05f,
                "atravessei o quadro elétrico");
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1234)]
        [InlineData(4242)]
        [InlineData(99999)]
        public void NenhumMovelTrancaUmComodo(int semente)
        {
            // Este é o risco de dar corpo aos móveis: um deles atravessado num
            // vão fecha a sala, e a partida fica impossível sem nada na tela
            // dizendo por quê. Validar() roda a busca que respeita mobília.
            var p = Nova(semente);
            Assert.Empty(p.Validar());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1234)]
        [InlineData(4242)]
        [InlineData(99999)]
        public void OsMoveisFicamEncostadosNaParede(int semente)
        {
            // móvel no meio do cômodo é obstáculo de labirinto; encostado é
            // arrumação. E é o que mantém o miolo da sala livre para andar.
            var p = Nova(semente);

            foreach (var r in p.Recipientes)
            {
                var c = p.Predio.ParaCelula(r.Pos, r.Andar);
                var centro = p.Predio.ParaMundo(c);
                float desvio = P2.Distancia(r.Pos, centro);
                Assert.True(desvio > 0.15f,
                    $"{r.Nome} está no meio da célula, não encostado ({desvio:0.00} m do centro)");
            }
        }

        [Fact]
        public void ElaContinuaAtravessandoOPredioComOsMoveisNoLugar()
        {
            // O risco do outro lado: com corpo nos móveis, a criatura pode
            // ficar presa — o caminho dela ignora mobília. Raio menor existe
            // para ela espremer; este teste prova que ela ainda circula.
            var p = Nova();
            p.AlternarLanterna();
            var inicio = p.Ela.Pos;
            float maior = 0;

            for (int i = 0; i < 60 * 40; i++)
            {
                p.Passo(1f / 60f, new Comando { Agachar = true });
                if (p.Ela.Andar == 0 || true)
                    maior = MathF.Max(maior, P2.Distancia(inicio, p.Ela.Pos));
                if (p.Fase != Fase.Jogando) break;
            }
            Assert.True(maior > 15f, $"ela só andou {maior:0.0} m em 40 s: ficou presa na mobília");
        }
    }
}

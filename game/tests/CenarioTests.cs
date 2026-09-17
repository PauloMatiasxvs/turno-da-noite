using System;
using System.Linq;
using TurnoDaNoite.Core;
using Xunit;

namespace TurnoDaNoite.Tests
{
    /// <summary>
    /// O cenário é enfeite, mas enfeite sólido: se um caixote nascer em cima de
    /// um gaveteiro ou no meio de uma porta, a partida trava e o jogador não tem
    /// como saber por quê. Estes testes existem para isso não acontecer calado.
    /// </summary>
    public class CenarioTests
    {
        static Partida Nova(int semente) { var p = new Partida(semente); Assert.True(p.Comecar()); return p; }

        [Theory]
        [InlineData(1)]
        [InlineData(1234)]
        [InlineData(4242)]
        [InlineData(99999)]
        public void NenhumAdornoNasceEmCimaDeOutraCoisa(int semente)
        {
            var p = Nova(semente);
            // só os sólidos: cano de teto passa por cima de tudo, e é para passar
            // so vale comparar no MESMO andar: um caixote em cima e um gaveteiro
            // embaixo tem quase a mesma coordenada no plano e nao se tocam
            foreach (var a in p.Adornos.Where(x => x.Solido))
            {
                foreach (var r in p.Recipientes)
                    if (r.Andar == a.Andar)
                        Assert.True(P2.Distancia(a.Pos, r.Pos) > 1.0f,
                            $"{a.Tipo} em cima de um {r.Nome}");
                foreach (var m in p.Armarios)
                    if (m.Andar == a.Andar)
                        Assert.True(P2.Distancia(a.Pos, m.Pos) > 1.0f, $"{a.Tipo} em cima de um armário");
                if (a.Andar == p.QuadroAndar)
                    Assert.True(P2.Distancia(a.Pos, p.Quadro) > 1.0f, "adorno em cima do quadro");
                if (a.Andar == 0)
                    Assert.True(P2.Distancia(a.Pos, p.Portao) > 1.0f, "adorno na porta");
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1234)]
        [InlineData(4242)]
        [InlineData(99999)]
        public void NenhumAdornoNasceDentroDeParede(int semente)
        {
            var p = Nova(semente);
            foreach (var a in p.Adornos)
                Assert.False(p.Predio.EhParede(p.Predio.ParaCelula(a.Pos, a.Andar)),
                    $"{a.Tipo} dentro da parede em {a.Pos} andar {a.Andar}");
        }

        [Fact]
        public void NadaDeCenarioNoPatio()
        {
            var p = Nova(4242);
            foreach (var a in p.Adornos)
                Assert.NotEqual(TipoSala.Patio, p.Predio.SalaEm(a.Pos, a.Andar)?.Tipo);
        }

        [Fact]
        public void OsAdornosSolidosDeixamPassarPeloMeioDaCelula()
        {
            var p = Nova(4242);
            foreach (var a in p.Adornos.Where(x => x.Solido))
            {
                var centro = p.Predio.ParaMundo(p.Predio.ParaCelula(a.Pos, a.Andar));
                float folga = P2.Distancia(a.Pos, centro) - a.Raio;
                Assert.True(folga >= Cenario.CorredorLivre - 0.001f,
                    $"{a.Tipo} deixa só {folga:0.00} m de folga no meio da célula");
                Assert.True(a.Raio <= Cenario.RaioMaximo, $"{a.Tipo} é largo demais");
            }
        }

        [Fact]
        public void AdornoSolidoEmpurraOJogador()
        {
            var p = Nova(4242);
            var a = p.Adornos.First(x => x.Solido);

            var dentro = new P2(a.Pos.X + a.Raio * 0.2f, a.Pos.Z);
            var fora = Cenario.Empurrar(dentro, Regras.RaioJogador, p.Adornos, a.Andar);
            Assert.True(P2.Distancia(fora, a.Pos) >= a.Raio + Regras.RaioJogador - 0.001f,
                "entrou dentro do móvel e continuou lá");
        }

        [Fact]
        public void AdornoDeTetoNaoAtrapalha()
        {
            var p = Nova(4242);
            var teto = p.Adornos.Where(x => x.Tipo == TipoAdorno.CanoTeto).ToList();
            Assert.NotEmpty(teto);
            foreach (var a in teto)
            {
                Assert.False(a.Solido);
                var antes = a.Pos;
                Assert.Equal(antes, Cenario.Empurrar(antes, Regras.RaioJogador, teto.ToList(), a.Andar));
            }
        }

        [Fact]
        public void CadaSalaGanhaCenario()
        {
            var p = Nova(4242);
            foreach (var sala in p.Predio.Salas)
            {
                if (sala.Tipo == TipoSala.Patio) continue;
                int quantos = p.Adornos.Count(a => sala.Contem(p.Predio.ParaCelula(a.Pos, a.Andar)));
                Assert.True(quantos >= 2, $"{sala.Nome} ficou vazia ({quantos} peças)");
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1234)]
        [InlineData(4242)]
        [InlineData(99999)]
        public void DaParaAndarDaEntradaAteOQuadroComOCenarioNoLugar(int semente)
        {
            // Não basta o BFS achar caminho: ele ignora os móveis. Aqui o boneco
            // anda de verdade, esbarrando em tudo, e tem de chegar — subindo a
            // escada no meio, porque o quadro fica no andar de cima.
            var p = Nova(semente);
            var destino = p.Quadro;
            int andarDestino = p.QuadroAndar;

            bool Chegou() => p.Jogador.Andar == andarDestino &&
                             P2.Distancia(p.Jogador.Pos, destino) <= 2.2f;

            for (int i = 0; i < 60 * 400 && !Chegou(); i++)
            {
                var caminho = p.Predio.Caminho(p.Predio.ParaCelula(p.Jogador.Pos, p.Jogador.Andar),
                                               p.Predio.ParaCelula(destino, andarDestino));
                Assert.NotNull(caminho);

                var proximo = caminho.Count > 1
                    ? p.Predio.ParaMundo(caminho[1])
                    : destino;
                var d = (proximo - p.Jogador.Pos).Normalizado;
                // já em cima do próximo nó (é uma escada): empurra numa direção
                // qualquer, senão o comando fica nulo e o passo não acontece
                if (d.Comprimento < 0.01f) d = new P2(1, 0);

                // converte a direção do mundo para o comando local do jogador
                p.Jogador.Giro = MathF.Atan2(-d.X, -d.Z);
                p.Passo(1f / 60f, new Comando { FrenteZ = -1f });
                p.Jogador.Bateria = 1f;
                if (p.Fase != Fase.Jogando) break;   // ela pegou: outro teste cuida disso
            }

            Assert.True(Chegou() || p.Fase != Fase.Jogando,
                $"empacou a {P2.Distancia(p.Jogador.Pos, destino):0.0} m do quadro, " +
                $"andar {p.Jogador.Andar} de {andarDestino}");
        }
    }
}

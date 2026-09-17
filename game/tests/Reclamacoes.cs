using System.Linq;
using TurnoDaNoite.Core;
using Xunit;

namespace TurnoDaNoite.Tests
{
    /// <summary>
    /// Um teste para cada coisa que quebrou na mão de quem jogou. Ficam juntos
    /// de propósito: é a lista do que já falhou de verdade, e nenhum deles
    /// pode voltar a falhar em silêncio.
    /// </summary>
    public class Reclamacoes
    {
        static Partida Nova(int semente = 4242)
        {
            var p = new Partida(semente);
            Assert.True(p.Comecar());
            return p;
        }

        /// <summary>"e nao tem como se esconder no armario?"</summary>
        [Theory]
        [InlineData(1)]
        [InlineData(1234)]
        [InlineData(4242)]
        [InlineData(99999)]
        public void ParadoNaFrenteDoArmarioDaParaEsconder(int semente)
        {
            var p = Nova(semente);

            // testa TODOS os armários: basta um com um gaveteiro por perto
            // roubando a interação para o jogador achar que o jogo não deixa
            foreach (var a in p.Armarios)
            {
                var q = Nova(semente);
                var mesmo = q.Armarios.First(x => P2.Distancia(x.Pos, a.Pos) < 0.01f && x.Andar == a.Andar);
                q.IrPara(mesmo.Pos, mesmo.Andar);

                Assert.Equal(Partida.Alvo.Armario, q.AlvoMaisPerto(out _));
                q.Interagir();
                Assert.True(q.Jogador.Escondido,
                    $"em cima do armário em {mesmo.Pos} e o jogo não deixou entrar");
            }
        }

        /// <summary>"que bosta é esse de som chato, até mesmo quando eu tô andando calmo"</summary>
        [Fact]
        public void AndandoCalmoLongeDelaSoSeOuveOPasso()
        {
            var p = Nova();
            // estaciona a criatura do outro lado do prédio
            p.PorElaEm(Ajuda.LongeDoJogador(p), p.Jogador.Andar);

            int coracao = 0, respiracao = 0, passos = 0;
            for (int i = 0; i < 60 * 20; i++)
            {
                p.PorElaEm(Ajuda.LongeDoJogador(p), p.Jogador.Andar);
                p.Passo(1f / 60f, Ajuda.Andando());
                foreach (var ev in p.Eventos)
                {
                    if (ev == Evento.Batida) coracao++;
                    if (ev == Evento.Respiracao) respiracao++;
                    if (ev == Evento.Passo) passos++;
                }
            }

            Assert.True(passos > 20, $"andei vinte segundos e dei {passos} passos");
            Assert.Equal(0, coracao);
            Assert.Equal(0, respiracao);
        }

        /// <summary>Mas o coração ainda tem de bater quando ela chega perto.</summary>
        [Fact]
        public void ComElaEmCimaOCoracaoDispara()
        {
            var p = Nova();
            int coracao = 0;
            for (int i = 0; i < 60 * 10; i++)
            {
                p.PorElaEm(new P2(p.Jogador.Pos.X + 3f, p.Jogador.Pos.Z), p.Jogador.Andar);
                p.Ela.Estado = EstadoCriatura.Caca;
                p.Passo(1f / 60f, Ajuda.Parado);
                foreach (var ev in p.Eventos) if (ev == Evento.Batida) coracao++;
                if (p.Fase != Fase.Jogando) break;
            }
            Assert.True(coracao > 0, "ela em cima de mim e o coração não bateu");
        }

        /// <summary>"e nao tem comodos?" — corredor de nove metros não é corredor.</summary>
        [Theory]
        [InlineData(1)]
        [InlineData(1234)]
        [InlineData(4242)]
        [InlineData(99999)]
        public void OPredioTemComodosDeVerdade(int semente)
        {
            var p = Nova(semente);
            var doTerreo = p.Predio.Salas.Where(s => s.Andar == 0 && s.Tipo != TipoSala.Patio).ToList();

            Assert.True(doTerreo.Count >= 8, $"só {doTerreo.Count} cômodos no térreo");
            foreach (var s in doTerreo)
            {
                float larg = s.Larg * Predio.Celula, alt = s.Alt * Predio.Celula;
                Assert.InRange(larg, 5f, 22f);
                Assert.InRange(alt, 5f, 22f);
            }
        }

        /// <summary>"primeiro e segundo andar?" — e dá para ir de um ao outro andando.</summary>
        [Theory]
        [InlineData(1)]
        [InlineData(1234)]
        [InlineData(4242)]
        [InlineData(99999)]
        public void DaParaSubirEDescerEmTodaPlanta(int semente)
        {
            var p = Nova(semente);
            var entrada = p.Predio.ParaCelula(p.Jogador.Pos, 0);

            foreach (var s in p.Predio.Salas)
                Assert.True(p.Predio.Alcancavel(entrada, s.Centro),
                    $"não dá para chegar em {s.Nome} (andar {s.Andar})");
        }

        /// <summary>"item para eu ve o mapa do predio?"</summary>
        [Fact]
        public void OMapaEhUmItemQueSeAchaENaoUmBotao()
        {
            var p = Nova();
            Assert.False(p.Jogador.TemMapa, "o mapa já começa na mão");
            Assert.NotNull(p.MapaItem);
            Assert.True(p.MapaItem.Dentro >= 0, "a planta está largada no chão");

            // e não dá para pegar sem revistar o móvel primeiro
            p.IrPara(p.MapaItem.Pos, p.MapaItem.Andar);
            p.Interagir();
            Assert.False(p.Jogador.TemMapa, "peguei a planta sem abrir o móvel");
            p.Interagir();
            Assert.True(p.Jogador.TemMapa);
        }
    }
}

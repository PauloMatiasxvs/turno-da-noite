using System;
using TurnoDaNoite.Core;
using Xunit;

namespace TurnoDaNoite.Tests
{
    /// <summary>
    /// Testes do estalo no ouvido. Estalo não é opinião: é um salto grande
    /// entre duas amostras vizinhas, e dá para medir. Consertar "no olho" já
    /// falhou uma vez, então agora cada efeito passa por esta régua.
    /// </summary>
    public class SomTests
    {
        static readonly Evento[] TodosOsEfeitos =
        {
            Evento.Passo, Evento.PassoDela, Evento.Respiracao, Evento.Ofegante,
            Evento.Batida, Evento.PegouFusivel, Evento.PegouBateria,
            Evento.InstalouFusivel, Evento.EnergiaVoltou, Evento.AbriuArmario,
            Evento.FechouArmario, Evento.LanternaLigou, Evento.LanternaApagou,
            Evento.ElaViuVoce, Evento.ElaRugiu, Evento.ElaArranhou,
            Evento.VocePegou, Evento.VoceEscapou, Evento.PortaoTrancado,
            Evento.PortaoBateu
        };

        [Fact]
        public void TodoEfeitoComecaEmSilencio()
        {
            foreach (var ev in TodosOsEfeitos)
            {
                var onda = Sintetizador.Gerar(Sintetizador.Para(ev));
                Assert.True(Math.Abs(onda[0]) < 100, $"{ev} começa em {onda[0]}, não em silêncio");
            }
        }

        [Fact]
        public void TodoEfeitoTerminaEmSilencio()
        {
            foreach (var ev in TodosOsEfeitos)
            {
                var onda = Sintetizador.Gerar(Sintetizador.Para(ev));
                Assert.True(Math.Abs(onda[^1]) < 100, $"{ev} termina em {onda[^1]}, não em silêncio");
            }
        }

        [Fact]
        public void NenhumEfeitoEstala()
        {
            // Estalo é salto ALÉM do que a onda exige. Uma senoide de 900 Hz
            // precisa andar bastante entre amostras; isso não é defeito. O que
            // é defeito é saltar muito mais que isso — foi essa distinção que
            // faltou na primeira tentativa de consertar o estalo.
            foreach (var ev in TodosOsEfeitos)
            {
                var receita = Sintetizador.Para(ev);
                var onda = Sintetizador.Gerar(receita);
                float salto = Sintetizador.MaiorSalto(onda);
                float limite = Sintetizador.SaltoEsperado(receita) + 0.02f;
                Assert.True(salto < limite,
                    $"{ev} estala: saltou {salto:P1}, a onda só exigia {limite:P1}");
            }
        }

        [Fact]
        public void TaxaDeAmostragemEhAltaOSuficiente()
        {
            // a 22 kHz, uma senoide de 400 Hz já anda 11% por amostra e soa áspera
            Assert.True(Sintetizador.Taxa >= 44100);
        }

        [Fact]
        public void SubidaEhSuaveNosPrimeirosMilissegundos()
        {
            foreach (var ev in TodosOsEfeitos)
            {
                var onda = Sintetizador.Gerar(Sintetizador.Para(ev));
                // nos primeiros 5 ms nada pode estar perto do volume cheio
                int cincoMs = Sintetizador.Taxa / 200;
                for (int i = 0; i < Math.Min(cincoMs, onda.Length); i++)
                {
                    float nivel = Math.Abs(onda[i]) / (float)short.MaxValue;
                    Assert.True(nivel < 0.55f, $"{ev} sobe rápido demais: {nivel:P0} em {i} amostras");
                }
            }
        }

        [Fact]
        public void NenhumEfeitoEstoura()
        {
            // deixa margem para dois ou três sons somarem sem clipar
            foreach (var ev in TodosOsEfeitos)
            {
                var onda = Sintetizador.Gerar(Sintetizador.Para(ev));
                foreach (short s in onda)
                    Assert.True(Math.Abs(s) <= short.MaxValue * 0.86f, $"{ev} estoura em {s}");
            }
        }

        [Fact]
        public void MesmaSementeGeraAMesmaOnda()
        {
            var a = Sintetizador.Gerar(Sintetizador.Para(Evento.ElaRugiu), 7);
            var b = Sintetizador.Gerar(Sintetizador.Para(Evento.ElaRugiu), 7);
            Assert.Equal(a, b);
        }

        [Fact]
        public void EfeitoTemADuracaoPedida()
        {
            var r = Sintetizador.Para(Evento.Batida);
            var onda = Sintetizador.Gerar(r);
            float segundos = onda.Length / (float)Sintetizador.Taxa;
            Assert.Equal(r.Segundos, segundos, 2);
        }

        [Fact]
        public void TodoEventoDeSomTemReceita()
        {
            foreach (var ev in TodosOsEfeitos)
                Assert.True(Sintetizador.Existe(ev), $"{ev} ficou sem som");
        }

        [Fact]
        public void EventoSemSomNaoGeraOndaVazia()
        {
            // eventos que nao tocam nada devem dizer isso, e nao devolver lixo
            Assert.False(Sintetizador.Existe((Evento)999));
        }
    }
}

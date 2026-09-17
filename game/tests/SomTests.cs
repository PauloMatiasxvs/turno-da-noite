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

        [Fact]
        public void OPassoTemVariasGravacoesDiferentes()
        {
            Assert.True(Sintetizador.Variacoes(Evento.Passo) >= 4,
                "passo com uma gravação só soa como aparelho apitando");

            var vistas = new System.Collections.Generic.List<short[]>();
            for (int v = 0; v < Sintetizador.Variacoes(Evento.Passo); v++)
                vistas.Add(Sintetizador.Gerar(Sintetizador.Para(Evento.Passo, v),
                                              (int)Evento.Passo * 977 + 13 + v * 7919));

            for (int a = 0; a < vistas.Count; a++)
                for (int b = a + 1; b < vistas.Count; b++)
                    Assert.False(Iguais(vistas[a], vistas[b]),
                        $"as variações {a} e {b} do passo são a mesma onda");
        }

        static bool Iguais(short[] x, short[] y)
        {
            if (x.Length != y.Length) return false;
            for (int i = 0; i < x.Length; i++) if (x[i] != y[i]) return false;
            return true;
        }

        [Fact]
        public void NenhumaVariacaoEstalaNemEstoura()
        {
            // a variação muda tom, força e duração: nada disso pode reintroduzir
            // o estalo que custou dois consertos para sumir
            foreach (var ev in TodosOsEfeitos)
                for (int v = 0; v < Sintetizador.Variacoes(ev); v++)
                {
                    var r = Sintetizador.Para(ev, v);
                    // a MESMA semente que o jogo usa: medir outra onda que nao a
                    // que toca no ouvido do jogador nao prova nada
                    var onda = Sintetizador.Gerar(r, (int)ev * 977 + 13 + v * 7919);
                    float limite = Sintetizador.SaltoEsperado(r) * 1.35f;

                    Assert.True(Sintetizador.MaiorSalto(onda) <= limite,
                        $"{ev} variação {v} estala: {Sintetizador.MaiorSalto(onda):0.000} > {limite:0.000}");
                    foreach (short s in onda)
                        Assert.True(Math.Abs(s) <= short.MaxValue * 0.86f, $"{ev} variação {v} estoura");
                    Assert.Equal(0, onda[0]);
                    Assert.Equal(0, onda[^1]);
                }
        }

        [Fact]
        public void APisadaEhImpactoENaoNota()
        {
            var r = Sintetizador.Para(Evento.Passo);
            Assert.True(r.Ataque <= 0.008f, "pisada com subida lenta vira bipe macio");
            Assert.True(r.Ruido >= 0.85f, "sobra senoide demais: dá para escutar a nota");
            Assert.True(r.Segundos <= 0.20f, "pisada comprida demais soa como zumbido");

            // e não pode ser o som mais alto do jogo: você ouve isso duas vezes
            // por segundo a partida inteira, e o que incomoda é o volume
            Assert.True(r.Volume <= Sintetizador.Para(Evento.ElaRugiu).Volume * 0.4f,
                "pisada alta demais perto do resto do jogo");
        }

        [Fact]
        public void ARajadaDePassosNaoRepeteAMesmaOnda()
        {
            // simula dez passos tirando uma variação de cada vez, como o jogo faz
            int quantas = Sintetizador.Variacoes(Evento.Passo);
            var duracoes = new System.Collections.Generic.HashSet<int>();
            for (int v = 0; v < quantas; v++)
                duracoes.Add(Sintetizador.Gerar(Sintetizador.Para(Evento.Passo, v)).Length);

            Assert.True(duracoes.Count >= 3,
                "as variações têm quase todas o mesmo tamanho: a repetição continua audível");
        }
    }

    /// <summary>
    /// O ritmo da caminhada. Som certo no compasso errado continua soando
    /// errado: uma passada a cada 1,3 segundos não é andar, é passear.
    /// </summary>
    public class CadenciaTests
    {
        static int PassosEm(float segundos, Comando cmd)
        {
            var p = Ajuda.Nova();
            int passos = 0;
            int quadros = (int)(segundos * 60);
            for (int i = 0; i < quadros; i++)
            {
                p.Eventos.Clear();
                p.Passo(1f / 60f, cmd);
                foreach (var ev in p.Eventos) if (ev == Evento.Passo) passos++;
                if (p.Fase != Fase.Jogando) break;
            }
            return passos;
        }

        [Fact]
        public void AndandoDaUmaPassadaPorMeioSegundoMaisOuMenos()
        {
            float intervalo = 10f / Math.Max(1, PassosEm(10f, Ajuda.Andando()));
            Assert.InRange(intervalo, 0.42f, 0.72f);
        }

        [Fact]
        public void CorrendoEhMaisRapidoQueAndando()
        {
            Assert.True(PassosEm(10f, Ajuda.Andando(correr: true)) > PassosEm(10f, Ajuda.Andando()),
                "correr tem de bater mais passos que andar");
        }

        [Fact]
        public void AgachadoEhMaisLentoQueAndando()
        {
            Assert.True(PassosEm(10f, Ajuda.Andando(agachar: true)) < PassosEm(10f, Ajuda.Andando()),
                "andar agachado tem de ser mais devagar");
        }

        [Fact]
        public void ParadoNaoFazPasso()
        {
            Assert.Equal(0, PassosEm(5f, Ajuda.Parado));
        }
    }
}

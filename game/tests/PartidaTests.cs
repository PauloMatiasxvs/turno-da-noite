using System;
using System.Collections.Generic;
using TurnoDaNoite.Core;
using Xunit;

namespace TurnoDaNoite.Tests
{
    static class Ajuda
    {
        public static Partida Nova(int semente = 1234)
        {
            var p = new Partida(semente);
            Assert.True(p.Comecar(), "a planta não passou na validação");
            return p;
        }

        public static Comando Parado => new Comando();

        /// <summary>Roda n segundos em passos de 1/60.</summary>
        public static void Rodar(this Partida p, float segundos, Comando? cmd = null)
        {
            var c = cmd ?? Parado;
            int passos = (int)MathF.Round(segundos * 60f);
            for (int i = 0; i < passos; i++) p.Passo(1f / 60f, c);
        }

        /// <summary>
        /// Vai até o ponto e recolhe o que estiver ali. Desde que os itens passaram
        /// a ficar guardados, pegar algo são dois gestos: abrir e então pegar.
        /// </summary>
        public static void Recolher(this Partida p, P2 onde)
        {
            p.Jogador.Pos = onde;
            p.Interagir();      // abre o recipiente
            p.Interagir();      // pega o que estava dentro
        }

        /// <summary>Anda para a frente (norte).</summary>
        public static Comando Andando(bool correr = false, bool agachar = false) =>
            new Comando { FrenteZ = -1f, Correr = correr, Agachar = agachar };
    }

    public class PlantaTests
    {
        [Fact]
        public void PlantaEhConectadaEValidada()
        {
            var p = Ajuda.Nova();
            Assert.Empty(p.Validar());
        }

        [Fact]
        public void TemAreaCaminhavelSuficiente()
        {
            var p = Ajuda.Nova();
            Assert.True(p.Predio.CelulasLivres() > 400,
                $"só {p.Predio.CelulasLivres()} células livres");
        }

        [Fact]
        public void NoveSalasComNomes()
        {
            var p = Ajuda.Nova();
            // 10 com o patio externo, que entrou quando o jogo passou a comecar
            // do lado de fora e voce precisar entrar no predio
            Assert.Equal(10, p.Predio.Salas.Count);
            Assert.NotNull(p.Predio.Sala(TipoSala.Patio));
            foreach (var s in p.Predio.Salas) Assert.False(string.IsNullOrWhiteSpace(s.Nome));
        }

        [Fact]
        public void JogadorNasceEmChaoLivre()
        {
            var p = Ajuda.Nova();
            Assert.False(p.Predio.EhParede(p.Predio.ParaCelula(p.Jogador.Pos)));
        }

        [Fact]
        public void CriaturaNasceLonge()
        {
            var p = Ajuda.Nova();
            Assert.True(P2.Distancia(p.Ela.Pos, p.Jogador.Pos) >= Regras.DistanciaInicialMinima);
        }

        [Fact]
        public void BordaEhSolida()
        {
            var p = Ajuda.Nova();
            for (int x = 0; x < p.Predio.Largura; x++)
            {
                Assert.True(p.Predio.EhParede(x, 0));
                Assert.True(p.Predio.EhParede(x, p.Predio.Profundidade - 1));
            }
        }

        [Fact]
        public void ParedeBloqueiaLinhaDeVisao()
        {
            var p = Ajuda.Nova();
            // Portaria e camara ficam no mesmo eixo vertical e os corredores se alinham:
            // existe linha reta livre entre elas, e isso e intencional. Para provar que
            // a parede bloqueia, use o par diagonal, que tem massica no meio.
            var portaria = p.Predio.ParaMundo(p.Predio.Sala(TipoSala.Portaria).Centro);
            var escritorio = p.Predio.ParaMundo(p.Predio.Sala(TipoSala.Escritorio).Centro);
            Assert.False(p.Predio.Visivel(portaria, escritorio));

            // e o ponto sempre enxerga a si mesmo
            Assert.True(p.Predio.Visivel(portaria, portaria));
        }

        [Fact]
        public void CaminhoLigaAsPontasDoPredio()
        {
            var p = Ajuda.Nova();
            var a = p.Predio.Sala(TipoSala.Portaria).Centro;
            var b = p.Predio.Sala(TipoSala.Camara).Centro;
            var caminho = p.Predio.Caminho(a, b);
            Assert.NotNull(caminho);
            Assert.True(caminho.Count > 10);
        }

        [Fact]
        public void MesmaSementeDaMesmaPlanta()
        {
            var a = Ajuda.Nova(999);
            var b = Ajuda.Nova(999);
            Assert.Equal(a.Fusiveis.Count, b.Fusiveis.Count);
            for (int i = 0; i < a.Fusiveis.Count; i++)
            {
                Assert.Equal(a.Fusiveis[i].Pos.X, b.Fusiveis[i].Pos.X, 3);
                Assert.Equal(a.Fusiveis[i].Pos.Z, b.Fusiveis[i].Pos.Z, 3);
            }
        }
    }

    public class MovimentoTests
    {
        [Fact]
        public void AndarMove()
        {
            var p = Ajuda.Nova();
            var antes = p.Jogador.Pos;
            p.Rodar(1f, Ajuda.Andando());
            Assert.True(P2.Distancia(antes, p.Jogador.Pos) > 2f);
        }

        [Fact]
        public void CorrerEhMaisRapidoQueAndarQueEhMaisRapidoQueAgachar()
        {
            float Percorrido(Comando c)
            {
                var p = Ajuda.Nova();
                var antes = p.Jogador.Pos;
                p.Rodar(1f, c);
                return P2.Distancia(antes, p.Jogador.Pos);
            }
            float correndo = Percorrido(Ajuda.Andando(correr: true));
            float andando = Percorrido(Ajuda.Andando());
            float agachado = Percorrido(Ajuda.Andando(agachar: true));

            Assert.True(correndo > andando, $"correndo {correndo} <= andando {andando}");
            Assert.True(andando > agachado, $"andando {andando} <= agachado {agachado}");
        }

        [Fact]
        public void NaoAtravessaParede()
        {
            var p = Ajuda.Nova();
            // empurra contra a parede por tempo de sobra, em todas as direções
            foreach (var dir in new[] { (0f, -1f), (0f, 1f), (1f, 0f), (-1f, 0f) })
            {
                var q = Ajuda.Nova();
                q.Rodar(12f, new Comando { FrenteX = dir.Item1, FrenteZ = dir.Item2, Correr = true });
                var c = q.Predio.ParaCelula(q.Jogador.Pos);
                Assert.False(q.Predio.EhParede(c), $"vazou andando para {dir}");
            }
        }

        [Fact]
        public void QuadroGiganteNaoAtravessaParede()
        {
            var p = Ajuda.Nova();
            // um quadro de 2 segundos: sem a trava, atravessaria o prédio inteiro
            for (int i = 0; i < 40; i++)
                p.Passo(2f, new Comando { FrenteZ = -1f, Correr = true });
            Assert.False(p.Predio.EhParede(p.Predio.ParaCelula(p.Jogador.Pos)));
        }

        [Fact]
        public void CorrerGastaFolegoEPararRecupera()
        {
            var p = Ajuda.Nova();
            p.Rodar(2f, Ajuda.Andando(correr: true));
            float depoisDeCorrer = p.Jogador.Folego;
            Assert.True(depoisDeCorrer < 1f);

            p.Rodar(3f);
            Assert.True(p.Jogador.Folego > depoisDeCorrer);
        }

        [Fact]
        public void SemFolegoNaoCorre()
        {
            var p = Ajuda.Nova();
            p.Rodar(10f, Ajuda.Andando(correr: true));   // esgota
            Assert.True(p.Jogador.Folego < Regras.FolegoMinimoParaCorrer + 0.02f);
            Assert.False(p.Jogador.Correndo);
        }
    }

    public class LanternaTests
    {
        [Fact]
        public void BateriaSoGastaComALuzAcesa()
        {
            var acesa = Ajuda.Nova();
            acesa.Rodar(10f);
            Assert.True(acesa.Jogador.Bateria < 1f);

            var apagada = Ajuda.Nova();
            apagada.AlternarLanterna();
            float antes = apagada.Jogador.Bateria;
            apagada.Rodar(10f);
            Assert.Equal(antes, apagada.Jogador.Bateria, 4);
        }

        [Fact]
        public void BateriaAcabaEALanternaApaga()
        {
            var p = Ajuda.Nova();
            // Ela precisa ficar longe: se pegar o jogador, a simulacao congela e a
            // bateria para de drenar — foi assim que este teste falhou da primeira vez.
            var longe = p.Predio.ParaMundo(p.Predio.Sala(TipoSala.Tunel).Centro);

            void Correr(int segundos)
            {
                for (int s = 0; s < segundos; s++) { p.Ela.Pos = longe; p.Rodar(1f); }
            }

            // Dois minutos ainda tem luz: a lanterna durava 95 s e isso parecia
            // defeito — você acendia, andava um pouco e ficava no escuro.
            Correr(120);
            Assert.True(p.Jogador.Lanterna, "a lanterna apagou cedo demais");
            Assert.True(p.Jogador.Bateria > 0.4f, $"bateria caiu rápido demais: {p.Jogador.Bateria:0.00}");

            // mas a escassez continua sendo mecânica: em cinco minutos acaba
            Correr(200);
            Assert.Equal(0f, p.Jogador.Bateria, 4);
            Assert.False(p.Jogador.Lanterna);
        }

        [Fact]
        public void LanternaDuraPerto_De_CincoMinutos()
        {
            var p = Ajuda.Nova();
            var longe = p.Predio.ParaMundo(p.Predio.Sala(TipoSala.Tunel).Centro);

            int segundos = 0;
            while (p.Jogador.Lanterna && segundos < 600)
            {
                p.Ela.Pos = longe;
                p.Rodar(1f);
                segundos++;
            }
            Assert.InRange(segundos, 240, 360);
        }

        [Fact]
        public void LuzAcesaAumentaMuitoOAlcanceDaVisaoDela()
        {
            var p = Ajuda.Nova();
            float comLuz = p.AlcanceDeVisao();
            p.AlternarLanterna();
            float semLuz = p.AlcanceDeVisao();
            Assert.True(comLuz > semLuz * 3, $"luz {comLuz} vs escuro {semLuz}");
        }

        [Fact]
        public void AgacharNoEscuroReduzOAlcance()
        {
            var p = Ajuda.Nova();
            p.AlternarLanterna();
            p.Rodar(0.2f);
            float emPe = p.AlcanceDeVisao();
            p.Rodar(0.2f, new Comando { Agachar = true });
            Assert.True(p.AlcanceDeVisao() < emPe);
        }

        [Fact]
        public void EsconderZeraOAlcance()
        {
            var p = Ajuda.Nova();
            p.Jogador.Pos = p.Armarios[0].Pos;
            p.Interagir();
            Assert.True(p.Jogador.Escondido);
            Assert.Equal(0f, p.AlcanceDeVisao());
        }
    }

    public class ObjetivoTests
    {
        static void PegarTodosOsFusiveis(Partida p)
        {
            foreach (var f in p.Fusiveis) p.Recolher(f.Pos);
        }

        [Fact]
        public void CincoFusiveisNoMapa()
        {
            var p = Ajuda.Nova();
            Assert.Equal(Regras.FusiveisNecessarios, p.Fusiveis.Count);
        }

        [Fact]
        public void PegarFusivelAumentaAMao()
        {
            var p = Ajuda.Nova();
            p.Recolher(p.Fusiveis[0].Pos);
            Assert.Equal(1, p.Jogador.FusiveisNaMao);
            Assert.True(p.Fusiveis[0].Recolhido);
        }

        [Fact]
        public void QuadroSemFusivelNaoInstalaNada()
        {
            var p = Ajuda.Nova();
            p.Jogador.Pos = p.Quadro;
            p.Interagir();
            Assert.Equal(0, p.Jogador.FusiveisInstalados);
            // a porta comeca ABERTA agora: voce entra por ela. O que impede de
            // vencer nao e a porta, e a falta dos cinco fusiveis instalados.
            Assert.NotEqual(Fase.Escapou, p.Fase);
        }

        [Fact]
        public void PortaoSoAbreComOsCincoInstalados()
        {
            var p = Ajuda.Nova();
            PegarTodosOsFusiveis(p);
            Assert.Equal(5, p.Jogador.FusiveisNaMao);

            p.Jogador.Pos = p.Quadro;
            p.Interagir();
            Assert.Equal(5, p.Jogador.FusiveisInstalados);
            Assert.True(p.PortaoAberto);
        }

        [Fact]
        public void PortaoTrancadoNaoDeixaSair()
        {
            var p = Ajuda.Nova();
            p.Jogador.Pos = p.Portao;
            p.Interagir();
            Assert.Equal(Fase.Jogando, p.Fase);
            Assert.Contains(Evento.PortaoTrancado, p.Eventos);
        }

        [Fact]
        public void DaParaGanharOJogo()
        {
            var p = Ajuda.Nova();
            PegarTodosOsFusiveis(p);
            p.Jogador.Pos = p.Quadro;
            p.Interagir();
            p.Jogador.Pos = p.Portao;
            p.Interagir();
            Assert.Equal(Fase.Escapou, p.Fase);
        }

        [Fact]
        public void InstalarFusivelDeixaElaMaisRapida()
        {
            var p = Ajuda.Nova();
            float antes = p.Ela.Agressao;
            PegarTodosOsFusiveis(p);
            p.Jogador.Pos = p.Quadro;
            p.Interagir();
            Assert.True(p.Ela.Agressao > antes);
        }

        [Fact]
        public void BateriaRecarregaALanterna()
        {
            var p = Ajuda.Nova();
            p.Rodar(40f);
            float gasta = p.Jogador.Bateria;
            p.Recolher(p.Baterias[0].Pos);
            Assert.True(p.Jogador.Bateria > gasta);
            Assert.True(p.Jogador.Bateria <= 1f);
        }
    }

    public class CriaturaTests
    {
        [Fact]
        public void PatrulhaCruzaOPredio()
        {
            var p = Ajuda.Nova();
            p.AlternarLanterna();            // apaga para ela não te achar cedo
            var inicio = p.Ela.Pos;
            float maior = 0;
            for (int i = 0; i < 60 * 30; i++)
            {
                p.Passo(1f / 60f, new Comando { Agachar = true });
                maior = MathF.Max(maior, P2.Distancia(inicio, p.Ela.Pos));
                if (p.Fase != Fase.Jogando) break;
            }
            Assert.True(maior > 15f, $"só andou {maior:0.0} m em 30 s");
        }

        [Fact]
        public void BarulhoAltoChamaElaParaInvestigar()
        {
            var p = Ajuda.Nova();
            p.AlternarLanterna();
            // teleporta para perto e corre: o barulho de correr tem raio de 24 m
            p.Jogador.Pos = new P2(p.Ela.Pos.X + 10f, p.Ela.Pos.Z);
            p.Rodar(2f, Ajuda.Andando(correr: true));
            Assert.True(p.Ela.Estado != EstadoCriatura.Patrulha,
                $"continuou em {p.Ela.Estado} mesmo com barulho ao lado");
        }

        [Fact]
        public void AgacharNaoChamaAtencaoDeLonge()
        {
            var p = Ajuda.Nova();
            p.AlternarLanterna();
            p.Jogador.Pos = new P2(p.Ela.Pos.X + 12f, p.Ela.Pos.Z);
            p.Rodar(2f, Ajuda.Andando(agachar: true));
            Assert.Equal(EstadoCriatura.Patrulha, p.Ela.Estado);
        }

        [Fact]
        public void ElaAlcancaOJogadorParadoComALuzAcesa()
        {
            var p = Ajuda.Nova();
            bool pego = false;
            float segundos = 0;
            for (int i = 0; i < 60 * 240; i++)
            {
                p.Passo(1f / 60f, Ajuda.Parado);
                p.Jogador.Bateria = 1f;          // luz infinita: isola o teste da bateria
                segundos = i / 60f;
                if (p.Fase == Fase.Morto) { pego = true; break; }
            }
            Assert.True(pego, $"não alcançou em {segundos:0} s");
        }

        [Fact]
        public void EscondidoComOArPresoSobrevive()
        {
            var p = Ajuda.Nova();
            p.Jogador.Pos = p.Armarios[0].Pos;
            p.Interagir();
            Assert.True(p.Jogador.Escondido);

            p.Ela.Pos = new P2(p.Jogador.Pos.X + 1f, p.Jogador.Pos.Z);
            p.Ela.Estado = EstadoCriatura.Caca;
            p.Ela.Paciencia = 10f;

            // 3 s cabe no folego de ar; segurar mais que isso e outro teste
            p.Rodar(3f, new Comando { PrenderAr = true });
            Assert.Equal(Fase.Jogando, p.Fase);
            Assert.True(p.Jogador.Ar > 0f);
        }

        [Fact]
        public void EscondidoRespirandoElaTeAcha()
        {
            var p = Ajuda.Nova();
            p.Jogador.Pos = p.Armarios[0].Pos;
            p.Interagir();

            p.Ela.Pos = new P2(p.Jogador.Pos.X + 1f, p.Jogador.Pos.Z);
            p.Ela.Estado = EstadoCriatura.Caca;
            p.Ela.Paciencia = 10f;

            p.Rodar(8f);
            Assert.Equal(Fase.Morto, p.Fase);
        }

        [Fact]
        public void OArAcabaSeVoceSegurarDemais()
        {
            var p = Ajuda.Nova();
            p.Jogador.Pos = p.Armarios[0].Pos;
            p.Interagir();
            p.Rodar(6f, new Comando { PrenderAr = true });
            Assert.Equal(0f, p.Jogador.Ar, 3);
        }

        [Fact]
        public void MedoSobeQuandoElaChegaPerto()
        {
            var p = Ajuda.Nova();
            p.Rodar(1f);
            float longe = p.Jogador.Medo;

            p.Ela.Pos = new P2(p.Jogador.Pos.X + 3f, p.Jogador.Pos.Z);
            p.Rodar(1.5f);
            Assert.True(p.Jogador.Medo > longe, $"medo não subiu: {longe} -> {p.Jogador.Medo}");
        }

        [Fact]
        public void NadaViraNaNEmPartidaLonga()
        {
            var p = Ajuda.Nova();
            for (int i = 0; i < 400; i++)
            {
                var cmd = new Comando
                {
                    FrenteX = (i % 7) - 3,
                    FrenteZ = (i % 5) - 2,
                    Giro = i * 0.05f,
                    Correr = i % 3 == 0,
                    Agachar = i % 11 == 0
                };
                p.Rodar(0.3f, cmd);
                if (p.Fase != Fase.Jogando) { p.Comecar(); }

                Assert.False(float.IsNaN(p.Jogador.Pos.X) || float.IsNaN(p.Jogador.Pos.Z),
                    $"jogador virou NaN no passo {i}");
                Assert.False(float.IsNaN(p.Ela.Pos.X) || float.IsNaN(p.Ela.Pos.Z),
                    $"criatura virou NaN no passo {i}");
            }
        }
    }
}

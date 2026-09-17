using System;
using System.Linq;
using TurnoDaNoite.Core;
using Xunit;

namespace TurnoDaNoite.Tests
{
    /// <summary>
    /// O prédio tem dois pisos. O que isso precisa garantir: dá para subir, a
    /// criatura sobe atrás de você, e a laje é opaca de verdade — nem visão,
    /// nem medo, nem interação atravessam o chão.
    /// </summary>
    public class AndarTests
    {
        static Partida Nova(int semente = 4242)
        {
            var p = new Partida(semente);
            Assert.True(p.Comecar());
            return p;
        }

        static P2 PeDaEscada(Partida p, int indice = 0)
        {
            var e = p.Predio.Escadas[indice];
            return p.Predio.ParaMundo(new Celula(e.Cx, e.Cz, e.De));
        }

        [Fact]
        public void PisarNaEscadaTrocaDeAndar()
        {
            var p = Nova();
            var e = p.Predio.Escadas[0];
            p.IrPara(PeDaEscada(p), e.De);

            // anda um passo em cima do poço: a troca é ao pisar, não por tecla
            p.Passo(1f / 60f, Ajuda.Andando());
            Assert.Equal(e.Para, p.Jogador.Andar);
            Assert.Contains(Evento.Escada, p.Eventos);
        }

        [Fact]
        public void NaoFicaSubindoEDescendoNoMesmoLugar()
        {
            // Sem trava, você chega em cima já em cima de uma escada e desce no
            // quadro seguinte, para sempre. Depois de trocar, o andar tem de
            // ficar parado por um tempo.
            var p = Nova();
            var e = p.Predio.Escadas[0];
            p.IrPara(PeDaEscada(p), e.De);

            p.Passo(1f / 60f, Ajuda.Andando());
            int depoisDeSubir = p.Jogador.Andar;

            for (int i = 0; i < 20; i++) p.Passo(1f / 60f, Ajuda.Parado);
            Assert.Equal(depoisDeSubir, p.Jogador.Andar);
        }

        [Fact]
        public void ElaNaoTeVeDoOutroAndar()
        {
            var p = Nova();
            // cara a cara, mas em pisos diferentes
            p.IrPara(new P2(0, 0), 0);
            p.PorElaEm(new P2(0, 0), 1);
            Assert.False(p.ElaTeVe(), "ela enxergou através da laje");
        }

        [Fact]
        public void MedoNaoAtravessaALaje()
        {
            var p = Nova();
            p.IrPara(new P2(0, 0), 0);
            p.PorElaEm(new P2(0.5f, 0), 1);
            p.Ela.Estado = EstadoCriatura.Caca;

            p.Rodar(3f);
            Assert.True(p.Jogador.Medo < 0.2f,
                $"medo em {p.Jogador.Medo:0.00} com ela num andar diferente");
        }

        [Fact]
        public void NaoDaParaPegarCoisaDoOutroAndar()
        {
            var p = Nova();
            var r = p.Recipientes.First(x => x.Andar == 1);

            // em cima do móvel, mas um andar abaixo dele
            p.IrPara(r.Pos, 0);
            Assert.Equal(Partida.Alvo.Nenhum, AlvoIgnorandoOChao(p, r));
        }

        static Partida.Alvo AlvoIgnorandoOChao(Partida p, Recipiente r)
        {
            var alvo = p.AlvoMaisPerto(out var obj);
            // pode haver um móvel do térreo por perto: o que não pode é ser ESTE
            return ReferenceEquals(obj, r) ? alvo : Partida.Alvo.Nenhum;
        }

        [Fact]
        public void ACriaturaConsegueChegarNoAndarDeCima()
        {
            var p = Nova();
            // ela começa em cima; manda descer e voltar prova os dois sentidos
            var alvoNoTerreo = p.Predio.Salas.First(s => s.Andar == 0 && s.Tipo == TipoSala.Comum);
            var caminho = p.Predio.Caminho(
                p.Predio.ParaCelula(p.Ela.Pos, p.Ela.Andar), alvoNoTerreo.Centro);

            Assert.NotNull(caminho);
            Assert.Contains(caminho, c => c.Andar == 0);
            Assert.Contains(caminho, c => c.Andar == 1);
        }

        [Fact]
        public void ElaAtravessaAndarPerseguindoVoce()
        {
            var p = Nova();
            // jogador no térreo com a luz acesa, ela em cima e já caçando
            var sala = p.Predio.Salas.First(s => s.Andar == 0 && s.Tipo == TipoSala.Comum);
            p.IrPara(p.Predio.ParaMundo(sala.Centro), 0);

            bool desceu = false;
            for (int i = 0; i < 60 * 240 && !desceu; i++)
            {
                p.Passo(1f / 60f, Ajuda.Parado);
                p.Jogador.Bateria = 1f;
                if (p.Ela.Andar == 0) desceu = true;
                if (p.Fase != Fase.Jogando) break;
            }

            Assert.True(desceu || p.Fase == Fase.Morto,
                "ela ficou presa no andar de cima: o piso virou abrigo seguro");
        }

        [Fact]
        public void OQuadroFicaNoAndarDeCima()
        {
            var p = Nova();
            Assert.Equal(1, p.QuadroAndar);
        }

        [Fact]
        public void OMapaComecaVazioEEnchePorOndeVoceAnda()
        {
            var p = Nova();
            int antes = p.CelulasVistas.Count;

            p.Rodar(6f, Ajuda.Andando());
            Assert.True(p.CelulasVistas.Count > antes,
                "andei seis segundos e o mapa não registrou nada");

            // e continua muito longe de conhecer o prédio inteiro
            Assert.True(p.CelulasVistas.Count < p.Predio.CelulasLivres() / 4,
                "o mapa revelou demais: não sobrou prédio para descobrir");
        }

        [Fact]
        public void BarulhoAtravessaALajeSoQuandoEhAlto()
        {
            var p = Nova();
            p.IrPara(new P2(0, 0), 0);

            // agachado, no andar de baixo: raio 2,5 m vira 1,1 m pela laje
            p.PorElaEm(new P2(4f, 0), 1);
            p.Ela.Estado = EstadoCriatura.Patrulha;
            p.Rodar(2f, Ajuda.Andando(agachar: true));
            Assert.Equal(EstadoCriatura.Patrulha, p.Ela.Estado);

            // correndo: raio 24 m vira quase 11 m, e ela ouve
            var q = Nova();
            q.IrPara(new P2(0, 0), 0);
            q.PorElaEm(new P2(4f, 0), 1);
            q.Ela.Estado = EstadoCriatura.Patrulha;
            q.Rodar(2f, Ajuda.Andando(correr: true));
            Assert.NotEqual(EstadoCriatura.Patrulha, q.Ela.Estado);
        }
    }
}

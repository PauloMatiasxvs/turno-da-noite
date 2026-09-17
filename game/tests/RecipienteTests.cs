using System.Linq;
using TurnoDaNoite.Core;
using Xunit;

namespace TurnoDaNoite.Tests
{
    /// <summary>
    /// Testes da mecânica de revistar. O que ela precisa garantir: nada fica
    /// largado no chão, a maioria dos recipientes está vazia (senão procurar
    /// não custa nada), e revistar faz barulho suficiente para ser arriscado.
    /// </summary>
    public class RecipienteTests
    {
        static Partida Nova(int semente = 4242)
        {
            var p = new Partida(semente);
            Assert.True(p.Comecar());
            return p;
        }

        [Fact]
        public void TodoFusivelEstaDentroDeAlgo()
        {
            var p = Nova();
            foreach (var f in p.Fusiveis)
                Assert.True(f.Dentro >= 0, "fusível ficou largado no chão");
        }

        [Fact]
        public void FusivelNaoPodeSerPegoAntesDeAbrir()
        {
            var p = Nova();
            var f = p.Fusiveis[0];
            p.Jogador.Pos = f.Pos;

            p.Interagir();                       // isto abre o recipiente
            Assert.Equal(0, p.Jogador.FusiveisNaMao);
            Assert.True(p.Recipientes[f.Dentro].Aberto);

            p.Interagir();                       // agora sim pega o fusível
            Assert.Equal(1, p.Jogador.FusiveisNaMao);
        }

        [Fact]
        public void AMaioriaDosRecipientesEstaVazia()
        {
            var p = Nova();
            int comAlgo = p.Recipientes.Count(r => r.TemAlgo);
            // 5 fusíveis + 4 baterias em muitos recipientes: procurar tem de custar
            Assert.Equal(Regras.FusiveisNecessarios + Regras.BateriasNoMapa, comAlgo);
            Assert.True(p.Recipientes.Count > comAlgo * 2,
                $"só {p.Recipientes.Count} recipientes para {comAlgo} itens: acha fácil demais");
        }

        [Fact]
        public void RevistarFazBarulhoEChamaACriatura()
        {
            var p = Nova();
            var r = p.Recipientes.First(x => !x.Aberto);

            // põe a criatura ao lado e tira ela do estado de caça
            p.Ela.Pos = new P2(r.Pos.X + 4f, r.Pos.Z);
            p.Ela.Estado = EstadoCriatura.Patrulha;
            p.Jogador.Pos = r.Pos;

            p.Interagir();
            Assert.NotEqual(EstadoCriatura.Patrulha, p.Ela.Estado);
        }

        [Fact]
        public void RecipienteVazioAvisaQueEstaVazio()
        {
            var p = Nova();
            var vazio = p.Recipientes.First(r => !r.TemAlgo);
            p.Jogador.Pos = vazio.Pos;
            p.Interagir();
            Assert.Contains(Evento.RecipienteVazio, p.Eventos);
        }

        [Fact]
        public void RecipienteComAlgoAvisaDiferente()
        {
            var p = Nova();
            var cheio = p.Recipientes.First(r => r.TemAlgo);
            p.Jogador.Pos = cheio.Pos;
            p.Interagir();
            Assert.Contains(Evento.AbriuRecipiente, p.Eventos);
        }

        [Fact]
        public void NadaFicaNaPortaria()
        {
            var p = Nova();
            foreach (var f in p.Fusiveis)
            {
                var sala = p.Predio.SalaEm(f.Pos);
                Assert.NotEqual(TipoSala.Portaria, sala?.Tipo);
            }
        }

        [Fact]
        public void NenhumRecipienteNoPatio()
        {
            var p = Nova();
            foreach (var r in p.Recipientes)
                Assert.NotEqual(TipoSala.Patio, p.Predio.SalaEm(r.Pos)?.Tipo);
        }

        [Fact]
        public void DaParaTerminarOJogoRevistandoTudo()
        {
            var p = Nova();

            // Abre todo recipiente. Em passadas repetidas de propósito: móveis
            // vizinhos cabem dentro do mesmo raio de interação, então parar em
            // frente a um pode abrir o do lado. Uma passada só deixaria alguns
            // fechados — e isso é do jogo, não do teste.
            for (int passada = 0; passada < 6 && p.Recipientes.Exists(r => !r.Aberto); passada++)
                foreach (var r in p.Recipientes)
                {
                    p.Jogador.Pos = r.Pos;
                    p.Interagir();
                }
            Assert.All(p.Recipientes, r => Assert.True(r.Aberto));
            // mesma coisa aqui: a bateria do móvel vizinho pode estar mais perto
            // e roubar a interação, então insiste até a mão encher
            for (int passada = 0; passada < 6 && p.Fusiveis.Exists(f => !f.Recolhido); passada++)
                foreach (var f in p.Fusiveis)
                {
                    p.Jogador.Pos = f.Pos;
                    p.Interagir();
                }
            Assert.Equal(Regras.FusiveisNecessarios, p.Jogador.FusiveisNaMao);

            p.Jogador.Pos = p.Quadro;
            p.Interagir();
            p.Jogador.Pos = p.Portao;
            p.Interagir();
            Assert.Equal(Fase.Escapou, p.Fase);
        }

        [Fact]
        public void TodoRecipienteEhAlcancavelAPe()
        {
            var p = Nova();
            Assert.Empty(p.Validar());
        }
    }
}

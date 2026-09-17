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
            p.IrPara(f.Pos, f.Andar);

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
            // 5 fusíveis + 4 baterias + a planta do prédio: procurar tem de custar
            Assert.Equal(Regras.FusiveisNecessarios + Regras.BateriasNoMapa + 1, comAlgo);
            Assert.True(p.Recipientes.Count > comAlgo * 2,
                $"só {p.Recipientes.Count} recipientes para {comAlgo} itens: acha fácil demais");
        }

        [Fact]
        public void RevistarFazBarulhoEChamaACriatura()
        {
            var p = Nova();
            var r = p.Recipientes.First(x => !x.Aberto);

            // põe a criatura ao lado e tira ela do estado de caça
            p.PorElaEm(new P2(r.Pos.X + 4f, r.Pos.Z), r.Andar);
            p.Ela.Estado = EstadoCriatura.Patrulha;
            p.IrPara(r.Pos, r.Andar);

            p.Interagir();
            Assert.NotEqual(EstadoCriatura.Patrulha, p.Ela.Estado);
        }

        [Fact]
        public void RecipienteVazioAvisaQueEstaVazio()
        {
            var p = Nova();
            var vazio = p.Recipientes.First(r => !r.TemAlgo);
            p.IrPara(vazio.Pos, vazio.Andar);
            p.Interagir();
            Assert.Contains(Evento.RecipienteVazio, p.Eventos);
        }

        [Fact]
        public void RecipienteComAlgoAvisaDiferente()
        {
            var p = Nova();
            var cheio = p.Recipientes.First(r => r.TemAlgo);
            p.IrPara(cheio.Pos, cheio.Andar);
            p.Interagir();
            Assert.Contains(Evento.AbriuRecipiente, p.Eventos);
        }

        [Fact]
        public void NadaFicaNaPortaria()
        {
            var p = Nova();
            foreach (var f in p.Fusiveis)
            {
                var sala = p.Predio.SalaEm(f.Pos, f.Andar);
                Assert.NotEqual(TipoSala.Portaria, sala?.Tipo);
            }
        }

        [Fact]
        public void NenhumRecipienteNoPatio()
        {
            var p = Nova();
            foreach (var r in p.Recipientes)
                Assert.NotEqual(TipoSala.Patio, p.Predio.SalaEm(r.Pos, r.Andar)?.Tipo);
        }

        [Fact]
        public void TemCoisaNosDoisAndares()
        {
            var p = Nova();
            int embaixo = p.Recipientes.Count(r => r.Andar == 0);
            int emCima = p.Recipientes.Count(r => r.Andar == 1);
            Assert.True(embaixo > 0 && emCima > 0,
                $"recipientes só num andar ({embaixo} embaixo, {emCima} em cima)");
        }

        [Fact]
        public void APlantaDoPredioExisteEFicaGuardadaNoTerreo()
        {
            var p = Nova();
            Assert.NotNull(p.MapaItem);
            Assert.True(p.MapaItem.Dentro >= 0, "a planta ficou largada no chão");
            Assert.Equal(0, p.MapaItem.Andar);
            Assert.False(p.Jogador.TemMapa);

            p.Recolher(p.MapaItem.Pos, p.MapaItem.Andar);
            Assert.True(p.Jogador.TemMapa, "revistei o móvel da planta e não peguei nada");
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
                    p.IrPara(r.Pos, r.Andar);
                    p.Interagir();
                }
            Assert.All(p.Recipientes, r => Assert.True(r.Aberto));
            // mesma coisa aqui: a bateria do móvel vizinho pode estar mais perto
            // e roubar a interação, então insiste até a mão encher
            for (int passada = 0; passada < 6 && p.Fusiveis.Exists(f => !f.Recolhido); passada++)
                foreach (var f in p.Fusiveis)
                {
                    p.IrPara(f.Pos, f.Andar);
                    p.Interagir();
                }
            Assert.Equal(Regras.FusiveisNecessarios, p.Jogador.FusiveisNaMao);

            p.IrPara(p.Quadro, p.QuadroAndar);
            p.Interagir();
            p.IrPara(p.Portao, 0);
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

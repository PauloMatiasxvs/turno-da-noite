using System;
using System.Linq;
using TurnoDaNoite.Core;
using Xunit;

namespace TurnoDaNoite.Tests
{
    /// <summary>
    /// O prédio tem mais de uma criatura. Com uma só, decorar a rota dela
    /// resolvia o jogo, e o andar em que ela não estava virava passeio.
    /// Estes testes existem para que o jogo não volte a ter uma sem ninguém
    /// perceber, e para que três não virem três vezes o mesmo barulho.
    /// </summary>
    public class CriaturasTests
    {
        static Partida Nova(int semente = 4242)
        {
            var p = new Partida(semente);
            Assert.True(p.Comecar());
            return p;
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1234)]
        [InlineData(4242)]
        [InlineData(99999)]
        public void NascemTodasELongeDeVoce(int semente)
        {
            var p = Nova(semente);
            Assert.Equal(Regras.QuantidadeDeCriaturas, p.Criaturas.Count);

            foreach (var c in p.Criaturas)
            {
                if (c.Andar != p.Jogador.Andar) continue;
                Assert.True(P2.Distancia(c.Pos, p.Jogador.Pos) >= Regras.DistanciaInicialMinima,
                    $"criatura a {P2.Distancia(c.Pos, p.Jogador.Pos):0} m do jogador");
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1234)]
        [InlineData(4242)]
        public void NascemLongeUmaDaOutra(int semente)
        {
            // duas no mesmo cômodo andam juntas o jogo inteiro e valem por uma
            var p = Nova(semente);
            for (int i = 0; i < p.Criaturas.Count; i++)
                for (int j = i + 1; j < p.Criaturas.Count; j++)
                {
                    var a = p.Criaturas[i];
                    var b = p.Criaturas[j];
                    if (a.Andar != b.Andar) continue;
                    Assert.True(P2.Distancia(a.Pos, b.Pos) >= Regras.DistanciaEntreCriaturas,
                        $"duas nasceram a {P2.Distancia(a.Pos, b.Pos):0} m uma da outra");
                }
        }

        [Fact]
        public void TodasOuvemOBarulhoQueVoceFaz()
        {
            var p = Nova();
            // põe as três em volta do jogador, todas patrulhando
            foreach (var c in p.Criaturas)
            {
                c.Pos = new P2(p.Jogador.Pos.X + 10f, p.Jogador.Pos.Z);
                c.Andar = p.Jogador.Andar;
                c.Estado = EstadoCriatura.Patrulha;
            }

            p.Rodar(2f, Ajuda.Andando(correr: true));

            Assert.All(p.Criaturas,
                c => Assert.NotEqual(EstadoCriatura.Patrulha, c.Estado));
        }

        [Fact]
        public void QuemTeVeChamaAsOutras()
        {
            // É o que transforma três bichos soltos num cerco: você é visto num
            // corredor e o prédio inteiro converge.
            var p = Nova();
            var vista = p.Criaturas[0];
            vista.Pos = new P2(p.Jogador.Pos.X + 3f, p.Jogador.Pos.Z);
            vista.Andar = p.Jogador.Andar;

            // as outras longe, mas dentro do alcance do chamado
            for (int i = 1; i < p.Criaturas.Count; i++)
            {
                p.Criaturas[i].Pos = new P2(p.Jogador.Pos.X + 34f, p.Jogador.Pos.Z);
                p.Criaturas[i].Andar = p.Jogador.Andar;
                p.Criaturas[i].Estado = EstadoCriatura.Patrulha;
            }

            p.Passo(1f / 60f, Ajuda.Parado);

            Assert.Equal(EstadoCriatura.Caca, vista.Estado);
            for (int i = 1; i < p.Criaturas.Count; i++)
                Assert.NotEqual(EstadoCriatura.Patrulha, p.Criaturas[i].Estado);
        }

        [Fact]
        public void QualquerUmaPodeTePegar()
        {
            var p = Nova();
            // a ÚLTIMA da lista em cima do jogador: se só a primeira matasse,
            // este teste passaria por acidente
            var ultima = p.Criaturas[^1];
            ultima.Pos = p.Jogador.Pos;
            ultima.Andar = p.Jogador.Andar;

            p.Passo(1f / 60f, Ajuda.Parado);
            Assert.Equal(Fase.Morto, p.Fase);
        }

        [Fact]
        public void OMedoEhDaMaisPertoENaoASomaDeTodas()
        {
            // somar o medo de três faria você andar apavorado o tempo todo só
            // porque o prédio tem três bichos, e medo constante deixa de ser medo
            var p = Nova();
            var longe = Ajuda.LongeDoJogador(p);

            foreach (var c in p.Criaturas)
            {
                c.Pos = longe;
                c.Andar = p.Jogador.Andar;
            }
            p.Rodar(2f);
            float comTodasLonge = p.Jogador.Medo;

            // agora UMA perto, as outras continuam longe
            p.Criaturas[1].Pos = new P2(p.Jogador.Pos.X + 4f, p.Jogador.Pos.Z);
            p.Rodar(2f);

            Assert.True(comTodasLonge < 0.2f, $"medo {comTodasLonge:0.00} com todas longe");
            Assert.True(p.Jogador.Medo > comTodasLonge, "uma delas chegou perto e o medo não subiu");
        }

        [Fact]
        public void CadaUmaTemSuaPropriaTravaDeEscada()
        {
            // com um contador só, a primeira que subisse travava as outras
            var p = Nova();
            Assert.Equal(Regras.QuantidadeDeCriaturas, p.Criaturas.Count);

            p.Criaturas[0].TravaEscada = Regras.EsperaDaEscada;
            for (int i = 1; i < p.Criaturas.Count; i++)
                Assert.Equal(0f, p.Criaturas[i].TravaEscada);
        }

        [Fact]
        public void InstalarFusivelAceleraTodas()
        {
            var p = Nova();
            p.EstacionarTodasLonge();
            foreach (var f in p.Fusiveis) p.Recolher(f.Pos, f.Andar);

            p.IrPara(p.Quadro, p.QuadroAndar);
            p.Interagir();

            Assert.All(p.Criaturas, c => Assert.True(c.Agressao > 0,
                "uma delas não acelerou quando a energia voltou"));
        }

        [Fact]
        public void OSomSoTocaDeQuemEstaPerto()
        {
            // três bichos andando pelo prédio inteiro somariam um chiado
            // constante, e voltaríamos ao "som chato"
            var p = Nova();
            var longe = Ajuda.LongeDoJogador(p);
            foreach (var c in p.Criaturas)
            {
                c.Pos = longe;
                c.Andar = p.Jogador.Andar;
                c.Velocidade = new P2(3f, 0);
                c.TempoPasso = 0;
            }

            int passos = 0;
            for (int i = 0; i < 60 * 5; i++)
            {
                foreach (var c in p.Criaturas) { c.Pos = longe; c.Velocidade = new P2(3f, 0); }
                p.Passo(1f / 60f, Ajuda.Parado);
                passos += p.Eventos.Count(e => e == Evento.PassoDela);
            }
            Assert.Equal(0, passos);
        }
    }
}

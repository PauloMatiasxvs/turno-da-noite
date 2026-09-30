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
            int moveis = p.Recipientes.Count
                       + p.Armarios.Count
                       + p.Adornos.Count(a => a.Solido)
                       + 1;                                    // o quadro elétrico

            // Por vão: dois batentes e a folha da porta. Escada: uma fileira
            // de postes de cada lado, nos dois andares que ela liga.
            int vaos = 0;
            for (int a = 0; a < p.Predio.Andares; a++)
                vaos += p.Predio.Vaos(a).Count(v => p.Predio.EscadaEm(v.celula) == null);
            int esperado = moveis
                         + vaos * (2 + Regras.PostesDaFolha)
                         + p.Predio.Escadas.Count * 2 * 2 * Regras.PostesDoLance;

            Assert.Equal(esperado, p.Solidos.Count);
            Assert.All(p.Solidos, s => Assert.True(s.Raio > 0.01f));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1234)]
        [InlineData(4242)]
        public void NaoDaParaAtravessarOLanceDeEscadaDeLado(int semente)
        {
            // A lateral do lance era UM círculo, no meio da célula: meio metro
            // de viga colidia e dois e meio não. Dava para andar de lado através
            // da escada, por qualquer ponta que não fosse o centro.
            //
            // O meio do poço continua livre de propósito — é por ali que se
            // sobe —, então o que se mede é a linha da viga, de ponta a ponta.
            var p = Nova(semente);
            var e = p.Predio.Escadas[0];
            var centro = p.Predio.ParaMundo(new Celula(e.Cx, e.Cz, e.De));

            for (int i = 0; i <= 6; i++)
            {
                float z = centro.Z + (i / 6f - 0.5f) * Predio.Celula * 0.86f;
                foreach (float lado in new[] { -1f, 1f })
                {
                    var naViga = new P2(centro.X + lado * Regras.MeioLanceDaEscada, z);
                    var depois = Colisao.Empurrar(naViga, Regras.RaioJogador, p.Solidos, e.De);
                    Assert.True(P2.Distancia(depois, naViga) > 0.05f,
                        $"z={z:0.00} lado={lado}: a viga do lance não empurrou ninguém");
                }
            }

            // e o meio segue livre, senão o andar de cima fica inalcançável
            var meio = new P2(centro.X, centro.Z);
            Assert.True(P2.Distancia(Colisao.Empurrar(meio, Regras.RaioJogador, p.Solidos, e.De), meio) < 0.01f);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1234)]
        [InlineData(4242)]
        public void OBatenteDaPortaTemCorpo(int semente)
        {
            // Eram dezenas, um por vão, e eram desenho puro: dava para
            // atravessar a ombreira de todas as portas do prédio.
            var p = Nova(semente);
            var vaos = p.Predio.Vaos(0).Where(v => p.Predio.EscadaEm(v.celula) == null).Take(8);

            foreach (var (celula, noEixoX) in vaos)
            {
                var m = p.Predio.ParaMundo(celula);
                // um ponto em cima da ombreira, de um lado
                var dentro = noEixoX
                    ? new P2(m.X + Regras.MeioVaoDaPorta, m.Z)
                    : new P2(m.X, m.Z + Regras.MeioVaoDaPorta);

                var fora = Colisao.Empurrar(dentro, Regras.RaioJogador, p.Solidos, 0);
                Assert.True(P2.Distancia(fora, dentro) > 0.1f,
                    $"atravessei o batente do vão em {celula}");
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1234)]
        [InlineData(4242)]
        public void OVaoDaPortaContinuaPassavel(int semente)
        {
            // o perigo do conserto: fechar a porta que se queria emoldurar
            var p = Nova(semente);
            foreach (var (celula, _) in p.Predio.Vaos(0))
            {
                var m = p.Predio.ParaMundo(celula);
                Assert.True(Colisao.Livre(m, Regras.RaioJogador, p.Solidos, 0),
                    $"o meio do vão em {celula} ficou bloqueado");
            }
        }

        [Fact]
        public void OLanceDaEscadaTemCorpoMasOPocoFicaLivre()
        {
            var p = Nova();
            var e = p.Predio.Escadas[0];
            var m = p.Predio.ParaMundo(new Celula(e.Cx, e.Cz, e.De));

            // o montante empurra
            var noMontante = new P2(m.X + Regras.MeioLanceDaEscada, m.Z);
            Assert.True(P2.Distancia(
                Colisao.Empurrar(noMontante, Regras.RaioJogador, p.Solidos, e.De), noMontante) > 0.1f,
                "atravessei o lance de escada de lado");

            // mas o meio continua livre: pisar nele é o que troca de andar
            Assert.True(Colisao.Livre(m, Regras.RaioJogador, p.Solidos, e.De),
                "bloqueei o poço e o andar de cima ficou inalcançável");
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

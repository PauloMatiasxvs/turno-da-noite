using System;
using System.Collections.Generic;

namespace TurnoDaNoite.Core
{
    public enum TipoAdorno
    {
        Caixote, Barril, Bancada, Entulho, Pilha,
        CanoParede, CanoTeto
    }

    /// <summary>
    /// Um objeto de cenário: não se pega, não se revista, só ocupa espaço e
    /// quebra a linha reta da parede.
    ///
    /// Mora no núcleo, e não no Godot, por um motivo prático: se o caixote for
    /// só um desenho, o jogador atravessa ele, e atravessar móvel é a coisa que
    /// mais rápido denuncia que aquilo é uma maquete. Aqui ele tem raio, entra
    /// na colisão e é testável — dá para provar que nenhum deles nasceu em cima
    /// de uma porta.
    /// </summary>
    public sealed class Adorno
    {
        public P2 Pos;
        public int Andar;
        public float Giro;
        public TipoAdorno Tipo;

        /// <summary>Raio de colisão. Zero para o que fica no teto ou é fino demais para atrapalhar.</summary>
        public float Raio;

        public bool Solido => Raio > 0.01f;
    }

    /// <summary>
    /// Enche os cômodos. Sem isto cada sala é uma caixa vazia com quatro paredes
    /// iguais, e o prédio inteiro parece o mesmo corredor repetido dez vezes.
    /// </summary>
    public static class Cenario
    {
        /// <summary>
        /// O que encosta na parede, e o raio que cada um ocupa.
        ///
        /// Nenhum passa de <see cref="RaioMaximo"/>, e isso não é estética: o
        /// móvel é empurrado contra a parede e ainda precisa sobrar corredor
        /// pelo meio da célula. Bancada larga demais fecha o cômodo, e fechar
        /// cômodo com enfeite é a pior forma de travar uma partida.
        /// </summary>
        static readonly (TipoAdorno tipo, float raio)[] DeParede =
        {
            (TipoAdorno.Caixote, 0.45f),
            (TipoAdorno.Barril, 0.38f),
            (TipoAdorno.Bancada, 0.44f),
            (TipoAdorno.Pilha, 0.40f),
            (TipoAdorno.CanoParede, 0f),
            (TipoAdorno.Entulho, 0f)
        };

        public const float RaioMaximo = 0.45f;
        /// <summary>Folga mínima entre o adorno e o meio da célula por onde se anda.</summary>
        public const float CorredorLivre = 0.5f;

        /// <summary>
        /// Espalha adornos pelas bordas dos cômodos. Só nas bordas de propósito:
        /// móvel no meio da sala vira obstáculo de labirinto, encostado na parede
        /// vira cenário. <paramref name="ocupado"/> são os pontos que já têm algo
        /// (recipiente, armário, quadro) e onde nada mais pode nascer.
        /// </summary>
        public static List<Adorno> Montar(Predio predio, Random rng, IReadOnlyList<(P2 pos, int andar)> ocupado)
        {
            var lista = new List<Adorno>();

            foreach (var sala in predio.Salas)
            {
                if (sala.Tipo == TipoSala.Patio) continue;

                var bordas = CelulasDeBorda(predio, sala);
                Embaralhar(bordas, rng);

                int quantos = Math.Max(3, bordas.Count / 3);
                int postosNaSala = 0;

                foreach (var (celula, normal) in bordas)
                {
                    if (lista.Count >= 600) break;
                    // Cômodo pequeno tem poucas bordas boas, e a cota acabava
                    // antes de entrar qualquer coisa: sala vazia de novo. Enquanto
                    // não houver nada aqui dentro, insiste além da cota.
                    if (quantos-- <= 0 && postosNaSala >= 2) break;

                    var (tipo, raio) = DeParede[rng.Next(DeParede.Length)];

                    // encosta na parede: sai do centro da célula na direção dela
                    var centro = predio.ParaMundo(celula);
                    float recuo = Predio.Celula / 2f - Math.Max(raio, 0.25f) - 0.05f;
                    var pos = new P2(centro.X + normal.X * recuo, centro.Z + normal.Z * recuo);

                    // e ainda tem de sobrar por onde passar pelo meio da célula
                    if (recuo - raio < CorredorLivre) continue;

                    if (PertoDeAlgo(pos, sala.Andar, ocupado, 1.6f)) continue;
                    if (PertoDeAdorno(pos, sala.Andar, lista, 1.1f)) continue;
                    if (predio.EscadaEm(celula) != null) continue;   // o poço da escada fica livre

                    lista.Add(new Adorno
                    {
                        Pos = pos,
                        Andar = sala.Andar,
                        Giro = MathF.Atan2(normal.X, normal.Z) + (float)(rng.NextDouble() - 0.5) * 0.5f,
                        Tipo = tipo,
                        Raio = raio
                    });
                    postosNaSala++;
                }

                // um cano atravessando o teto da sala, no eixo mais comprido
                lista.Add(new Adorno
                {
                    Pos = predio.ParaMundo(sala.Centro),
                    Andar = sala.Andar,
                    Giro = sala.Larg >= sala.Alt ? 0f : MathF.PI / 2f,
                    Tipo = TipoAdorno.CanoTeto,
                    Raio = 0f
                });
            }

            return lista;
        }

        /// <summary>
        /// Células de chão que fazem divisa com parede, junto com a direção que
        /// aponta para essa parede. A direção é o que permite encostar o móvel
        /// nela em vez de largar no meio do caminho.
        /// </summary>
        static List<(Celula celula, P2 normal)> CelulasDeBorda(Predio predio, Sala sala)
        {
            var achadas = new List<(Celula, P2)>();
            int a = sala.Andar;
            for (int z = sala.Z; z < sala.Z + sala.Alt; z++)
                for (int x = sala.X; x < sala.X + sala.Larg; x++)
                {
                    if (predio.EhParede(x, z, a)) continue;

                    // porta é um vão na parede: encostar móvel ao lado dela
                    // acaba estreitando a passagem, então pulo as diagonais
                    if (predio.EhParede(x - 1, z, a) && !predio.EhParede(x + 1, z, a))
                        achadas.Add((new Celula(x, z, a), new P2(-1, 0)));
                    else if (predio.EhParede(x + 1, z, a) && !predio.EhParede(x - 1, z, a))
                        achadas.Add((new Celula(x, z, a), new P2(1, 0)));
                    else if (predio.EhParede(x, z - 1, a) && !predio.EhParede(x, z + 1, a))
                        achadas.Add((new Celula(x, z, a), new P2(0, -1)));
                    else if (predio.EhParede(x, z + 1, a) && !predio.EhParede(x, z - 1, a))
                        achadas.Add((new Celula(x, z, a), new P2(0, 1)));
                }
            return achadas;
        }

        static bool PertoDeAlgo(P2 p, int andar, IReadOnlyList<(P2 pos, int andar)> pontos, float limite)
        {
            for (int i = 0; i < pontos.Count; i++)
                if (pontos[i].andar == andar && P2.Distancia(p, pontos[i].pos) < limite) return true;
            return false;
        }

        static bool PertoDeAdorno(P2 p, int andar, List<Adorno> lista, float limite)
        {
            for (int i = 0; i < lista.Count; i++)
                if (lista[i].Andar == andar && P2.Distancia(p, lista[i].Pos) < limite) return true;
            return false;
        }

        static void Embaralhar<T>(List<T> lista, Random rng)
        {
            for (int i = lista.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (lista[i], lista[j]) = (lista[j], lista[i]);
            }
        }

        /// <summary>
        /// Empurra quem esbarrar num adorno sólido. Fica separado do empurrão
        /// das paredes porque adorno é círculo e parede é caixa — juntar os dois
        /// numa função só só esconderia qual deles prendeu você no canto.
        /// </summary>
        public static P2 Empurrar(P2 p, float raio, List<Adorno> adornos, int andar = 0)
        {
            for (int i = 0; i < adornos.Count; i++)
            {
                var a = adornos[i];
                if (!a.Solido || a.Andar != andar) continue;

                float soma = raio + a.Raio;
                float dx = p.X - a.Pos.X, dz = p.Z - a.Pos.Z;
                float d2 = dx * dx + dz * dz;
                if (d2 >= soma * soma) continue;

                float d = MathF.Sqrt(d2);
                if (d < 1e-5f) { p.X += soma; continue; }

                float f = (soma - d) / d;
                p.X += dx * f;
                p.Z += dz * f;
            }
            return p;
        }
    }
}

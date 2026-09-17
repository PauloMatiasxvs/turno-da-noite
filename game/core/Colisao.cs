using System;
using System.Collections.Generic;

namespace TurnoDaNoite.Core
{
    /// <summary>
    /// Uma coisa que ocupa chão. Círculo, e não caixa, de propósito: quem
    /// esbarra num círculo desliza em volta dele, e quem esbarra numa quina
    /// de caixa engancha. Num jogo em que você foge correndo de costas por
    /// um corredor, enganchar é pior do que impreciso.
    /// </summary>
    public struct Solido
    {
        public P2 Pos;
        public int Andar;
        public float Raio;

        public Solido(P2 pos, int andar, float raio) { Pos = pos; Andar = andar; Raio = raio; }
    }

    /// <summary>
    /// Empurra para fora dos móveis.
    ///
    /// Existe porque durante um bom tempo só o cenário solto — caixote,
    /// tambor — tinha corpo. Gaveteiro, prateleira, armário e quadro elétrico
    /// eram desenho: você atravessava os móveis que o jogo inteiro pede para
    /// você usar. Nada denuncia mais rápido que aquilo é uma maquete.
    /// </summary>
    public static class Colisao
    {
        public static P2 Empurrar(P2 p, float raio, IReadOnlyList<Solido> solidos, int andar)
        {
            for (int i = 0; i < solidos.Count; i++)
            {
                var s = solidos[i];
                if (s.Andar != andar || s.Raio <= 0.01f) continue;

                float soma = raio + s.Raio;
                float dx = p.X - s.Pos.X, dz = p.Z - s.Pos.Z;
                float d2 = dx * dx + dz * dz;
                if (d2 >= soma * soma) continue;

                float d = MathF.Sqrt(d2);
                // exatamente no centro: escolhe uma direção qualquer, senão
                // a normalização divide por zero e a posição vira NaN
                if (d < 1e-5f) { p.X += soma; continue; }

                float f = (soma - d) / d;
                p.X += dx * f;
                p.Z += dz * f;
            }
            return p;
        }

        /// <summary>
        /// Dá para um círculo deste raio ficar parado aqui sem estar dentro de
        /// um móvel? É a pergunta que a validação da planta precisa responder
        /// para garantir que nada ficou trancado atrás da mobília.
        /// </summary>
        public static bool Livre(P2 p, float raio, IReadOnlyList<Solido> solidos, int andar)
        {
            for (int i = 0; i < solidos.Count; i++)
            {
                var s = solidos[i];
                if (s.Andar != andar || s.Raio <= 0.01f) continue;
                if (P2.Distancia(p, s.Pos) < raio + s.Raio) return false;
            }
            return true;
        }

        /// <summary>
        /// Busca em largura que RESPEITA os móveis, ao contrário da do Predio,
        /// que só conhece parede. É esta que prova que dá para chegar num
        /// lugar andando, e não só no papel.
        /// </summary>
        public static bool AlcancavelAPe(Predio predio, IReadOnlyList<Solido> solidos,
                                         float raio, Celula origem, Celula destino)
        {
            if (!Passa(predio, solidos, raio, origem) || !Passa(predio, solidos, raio, destino))
                return false;

            int porAndar = predio.Largura * predio.Profundidade;
            var visto = new bool[porAndar * predio.Andares];

            int Indice(Celula c) => (c.Andar * predio.Profundidade + c.Cz) * predio.Largura + c.Cx;

            var fila = new List<Celula> { origem };
            visto[Indice(origem)] = true;

            for (int i = 0; i < fila.Count; i++)
            {
                var at = fila[i];
                if (at.Cx == destino.Cx && at.Cz == destino.Cz && at.Andar == destino.Andar) return true;

                Ver(new Celula(at.Cx + 1, at.Cz, at.Andar));
                Ver(new Celula(at.Cx - 1, at.Cz, at.Andar));
                Ver(new Celula(at.Cx, at.Cz + 1, at.Andar));
                Ver(new Celula(at.Cx, at.Cz - 1, at.Andar));

                var esc = predio.EscadaEm(at);
                if (esc != null) Ver(new Celula(at.Cx, at.Cz, predio.OutroAndar(esc, at.Andar)));

                void Ver(Celula c)
                {
                    if (!predio.NaGrade(c.Cx, c.Cz, c.Andar)) return;
                    int k = Indice(c);
                    if (visto[k]) return;
                    if (!Passa(predio, solidos, raio, c)) return;
                    visto[k] = true;
                    fila.Add(c);
                }
            }
            return false;
        }

        /// <summary>
        /// Dá para atravessar esta célula? Olha o centro e os quatro meios das
        /// bordas: móvel encostado num canto deixa passar pelo outro lado, e
        /// reprovar a célula inteira por causa dele fecharia corredores que
        /// na prática estão abertos.
        /// </summary>
        static bool Passa(Predio predio, IReadOnlyList<Solido> solidos, float raio, Celula c)
        {
            if (predio.EhParede(c)) return false;

            var m = predio.ParaMundo(c);
            float meio = Predio.Celula * 0.30f;
            Span<P2> pontos = stackalloc P2[]
            {
                m,
                new P2(m.X + meio, m.Z), new P2(m.X - meio, m.Z),
                new P2(m.X, m.Z + meio), new P2(m.X, m.Z - meio)
            };

            foreach (var p in pontos)
                if (Livre(p, raio, solidos, c.Andar) &&
                    P2.Distancia(predio.EmpurrarFora(p, raio, c.Andar), p) < 0.01f)
                    return true;

            return false;
        }
    }
}

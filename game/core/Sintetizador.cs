using System;

namespace TurnoDaNoite.Core
{
    /// <summary>
    /// Gera as ondas dos efeitos sonoros. Vive no núcleo, longe do motor, porque
    /// estalo em alto-falante é um defeito MENSURÁVEL: é um salto grande entre
    /// duas amostras vizinhas. Com a síntese aqui, existe teste para isso —
    /// tentei consertar o estalo no olho uma vez e não resolveu.
    /// </summary>
    public static class Sintetizador
    {
        // 44,1 kHz e nao 22 kHz: a 22 kHz uma senoide de 400 Hz ja anda 11% por
        // amostra, o que por si so soa aspero, e o ruido branco serrilha.
        public const int Taxa = 44100;

        /// <summary>Receita de um efeito: varredura de frequência misturada a ruído.</summary>
        public struct Receita
        {
            public float Segundos, DeHz, ParaHz, Volume, Ruido;

            /// <summary>
            /// Tempo de subida. Dez milissegundos servem para sopro e rangido,
            /// mas apagam a batida de um impacto: pisada precisa começar quase
            /// de uma vez, senão vira um bipe macio em vez de um pé no chão.
            /// </summary>
            public float Ataque;

            /// <summary>Expoente da queda. 2 é o normal; 4 é seco, 1 é arrastado.</summary>
            public float Queda;

            /// <summary>
            /// Abertura do filtro do ruído. Baixo abafa, alto deixa a areia
            /// aparecer. É a diferença entre um baque surdo e um pé raspando
            /// no concreto.
            /// </summary>
            public float Corte;

            public Receita(float segundos, float deHz, float paraHz, float volume, float ruido,
                           float ataque = 0.010f, float queda = 2.0f, float corte = 0.25f)
            {
                Segundos = segundos; DeHz = deHz; ParaHz = paraHz; Volume = volume; Ruido = ruido;
                Ataque = ataque; Queda = queda; Corte = corte;
            }
        }

        public static Receita Para(Evento ev) => ev switch
        {
            // Pisada: quase só ruído. A senoide grave fica de corpo, não de
            // melodia — com 0,70 de ruído e filtro fechado sobrava tom suficiente
            // para o ouvido escutar uma nota, e nota repetida a cada meio segundo
            // vira bipe de aparelho, não passo.
            //
            // Suavizada depois de ouvir no jogo: estava seca e arranhada demais
            // para alguém andando de sapato num corredor. Volume menor, filtro
            // mais fechado (menos areia) e subida de 6 ms em vez de 2 — ainda é
            // impacto, mas é o baque abafado de sola, não um estalo.
            Evento.Passo           => new Receita(0.15f, 135,  55,   0.13f, 0.95f, 0.006f, 2.6f, 0.26f),
            Evento.PassoDela       => new Receita(0.26f, 110,  44,   0.50f, 0.88f, 0.004f, 2.6f, 0.38f),
            Evento.Respiracao      => new Receita(0.45f, 420,  190,  0.10f, 0.75f),
            Evento.Ofegante        => new Receita(0.60f, 520,  180,  0.26f, 0.80f),
            Evento.Batida          => new Receita(0.28f, 95,   48,   0.16f, 0f),
            Evento.PegouFusivel    => new Receita(0.16f, 880,  1320, 0.26f, 0f),
            Evento.PegouBateria    => new Receita(0.12f, 520,  760,  0.20f, 0.05f),
            Evento.InstalouFusivel => new Receita(0.30f, 160,  60,   0.40f, 0.40f),
            Evento.EnergiaVoltou   => new Receita(1.40f, 60,   240,  0.32f, 0.20f),
            Evento.AbriuArmario    => new Receita(0.48f, 700,  220,  0.30f, 0.60f),
            Evento.FechouArmario   => new Receita(0.38f, 560,  180,  0.28f, 0.60f),
            Evento.LanternaLigou   => new Receita(0.09f, 1200, 800,  0.15f, 0.45f),
            Evento.LanternaApagou  => new Receita(0.11f, 800,  360,  0.15f, 0.45f),
            Evento.ElaViuVoce      => new Receita(1.00f, 380,  85,   0.70f, 0.40f),
            Evento.ElaRugiu        => new Receita(0.90f, 270,  65,   0.55f, 0.45f),
            Evento.ElaArranhou     => new Receita(0.60f, 1400, 350,  0.30f, 0.80f),
            // volume 0,90 era o som mais alto do jogo: no limite do estouro e com
            // salto entre amostras no limite do estalo. 0,72 resolve os dois.
            Evento.VocePegou       => new Receita(1.60f, 780,  40,   0.72f, 0.45f),
            Evento.VoceEscapou     => new Receita(1.30f, 220,  440,  0.38f, 0f),
            Evento.PortaoTrancado  => new Receita(0.32f, 300,  110,  0.32f, 0.50f),
            Evento.PortaoBateu     => new Receita(0.80f, 140,  38,   0.52f, 0.45f),
            Evento.AbriuRecipiente => new Receita(0.40f, 520,  180,  0.28f, 0.70f),
            Evento.RecipienteVazio => new Receita(0.34f, 380,  150,  0.22f, 0.75f),
            // degrau de metal: impacto seco e metálico, e mais alto que a pisada
            Evento.Escada          => new Receita(0.34f, 260,  95,   0.30f, 0.90f, 0.003f, 2.4f, 0.50f),
            Evento.PegouMapa       => new Receita(0.30f, 900,  1400, 0.24f, 0.55f, 0.006f, 2.0f, 0.40f),
            _ => default
        };

        public static bool Existe(Evento ev) => Para(ev).Segundos > 0f;

        /// <summary>
        /// Quantas gravações diferentes do mesmo evento existem. Passo precisa
        /// de várias: dois pés nunca batem igual, e a mesma onda repetida a cada
        /// meio segundo deixa de soar como pisada e passa a soar como aparelho
        /// apitando. Os sons que tocam uma vez por partida não precisam disso.
        /// </summary>
        public static int Variacoes(Evento ev) => ev switch
        {
            Evento.Passo or Evento.PassoDela => 6,
            Evento.Respiracao or Evento.Ofegante => 4,
            Evento.Batida or Evento.AbriuRecipiente or Evento.RecipienteVazio => 3,
            _ => 1
        };

        /// <summary>
        /// A receita do evento com o tempero da variação: passada um pouco mais
        /// curta, um pouco mais grave, um pouco mais fraca. Nada grande — se a
        /// diferença for grande você escuta dois sons distintos em vez de um som
        /// que respira.
        /// </summary>
        public static Receita Para(Evento ev, int variacao)
        {
            var r = Para(ev);
            if (variacao <= 0 || Variacoes(ev) <= 1) return r;

            // desvios fixos, sem sorteio: som tem de ser reproduzível para o
            // teste conseguir medir estalo nele
            float[] tom = { 1f, 0.94f, 1.07f, 0.90f, 1.03f, 0.97f };
            float[] forca = { 1f, 0.88f, 1.05f, 0.93f, 0.97f, 1.02f };
            float[] duracao = { 1f, 0.92f, 1.06f, 0.88f, 1.01f, 0.95f };

            int i = variacao % tom.Length;
            r.DeHz *= tom[i];
            r.ParaHz *= tom[i];
            r.Volume *= forca[i];
            r.Segundos *= duracao[i];
            return r;
        }

        /// <summary>
        /// Gera as amostras de 16 bits. O que evita estalo:
        ///   - rampa de subida e de descida, para a onda sair do zero e voltar a ele
        ///   - o ruído passa por um filtro de três polos, senão a energia no agudo
        ///     chia e, com taxa de 22 kHz, ainda produz serrilhado
        ///   - o resultado é limitado com margem, para soma de sons não estourar
        /// </summary>
        public static short[] Gerar(Receita r, int semente = 12345)
        {
            int n = Math.Max(2, (int)(Taxa * r.Segundos));
            var saida = new short[n];

            float ataque = r.Ataque > 0 ? r.Ataque : 0.010f;
            float corte = r.Corte > 0 ? Math.Clamp(r.Corte, 0.05f, 0.55f) : 0.25f;
            float queda = r.Queda > 0 ? r.Queda : 2.0f;

            int subida = Math.Max(1, (int)(Taxa * ataque));
            // a descida continua com 10 ms: é ela que impede o corte seco no fim,
            // e ninguém escuta "ataque" no fim de um som
            int descida = Math.Max(1, (int)(Taxa * 0.010f));

            // Ganho que devolve o volume que o filtro tirou. Depende do corte:
            // filtro mais aberto já deixa passar mais energia, e um ganho fixo
            // faria o som brilhante sair muito mais alto que o abafado.
            float porPolo = MathF.Sqrt(corte / (2f - corte));
            float ganho = 0.162f / MathF.Pow(porPolo, 3f);

            uint estado = (uint)(semente == 0 ? 1 : semente);
            float f1 = 0, f2 = 0, f3 = 0;
            float fase = 0;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                float freq = r.DeHz + (r.ParaHz - r.DeHz) * t;
                fase += freq / Taxa * MathF.PI * 2f;

                // xorshift: previsível, para o teste medir sempre a mesma onda
                estado ^= estado << 13; estado ^= estado >> 17; estado ^= estado << 5;
                float bruto = (estado & 0xFFFFFF) / (float)0x800000 - 1f;

                // três polos de suavização derrubam o agudo áspero do ruído branco
                // Coeficiente baixo = filtro forte. A 0,35 o ruido ainda saltava mais
                // do que a onda exigia, e era isso que raspava no ouvido a cada passo.
                f1 += (bruto - f1) * corte;
                f2 += (f1 - f2) * corte;
                f3 += (f2 - f3) * corte;
                float chiado = f3 * ganho;

                float tom = MathF.Sin(fase);
                float corpo = tom + (chiado - tom) * r.Ruido;

                float sobe = MathF.Min(1f, i / (float)subida);
                float desce = MathF.Min(1f, (n - 1 - i) / (float)descida);
                float decai = MathF.Pow(1f - t, queda);

                float amostra = corpo * decai * sobe * desce * r.Volume;
                amostra = Math.Clamp(amostra, -0.85f, 0.85f);   // margem para somas
                saida[i] = (short)(amostra * short.MaxValue);
            }

            // garante zero absoluto nas pontas, custe o que custar
            saida[0] = 0;
            saida[n - 1] = 0;
            return saida;
        }

        /// <summary>
        /// O salto que a propria onda exige entre duas amostras: uma senoide de
        /// frequencia f nao tem como andar menos que isso. Serve de piso para
        /// julgar estalo — salto acima disso e defeito, abaixo e a onda.
        /// </summary>
        public static float SaltoEsperado(Receita r)
        {
            // duas contribuicoes legitimas: a inclinacao da senoide na frequencia
            // mais alta da varredura, e o passo do ruido depois de filtrado
            float daOnda = MathF.Max(r.DeHz, r.ParaHz) / Taxa * MathF.PI * 2f * r.Volume;
            // 0,18 medido. Passou por 0,08 (chute) e 0,11 (ajustado a UMA semente,
            // que e o erro classico): o pico de ruido e sorteado, e medir um unico
            // sorteio de 35 mil amostras da um numero baixo demais. Nas sementes
            // que o jogo realmente toca, 0,11 subestimava em ate 60%.
            //
            // Escala com o corte porque som brilhante SOBE mais rapido entre duas
            // amostras por definicao: cobrar dele o mesmo limite do som abafado
            // seria chamar de defeito o que e a propria natureza do som.
            float corte = r.Corte > 0 ? Math.Clamp(r.Corte, 0.05f, 0.55f) : 0.25f;
            float doRuido = r.Ruido * r.Volume * 0.18f * (corte / 0.25f);
            return daOnda + doRuido;
        }

        /// <summary>
        /// O maior salto entre duas amostras vizinhas, normalizado de 0 a 1.
        /// É a medida de estalo: quanto maior, mais o alto-falante estala.
        /// </summary>
        public static float MaiorSalto(short[] amostras)
        {
            float pior = 0;
            for (int i = 1; i < amostras.Length; i++)
            {
                float salto = MathF.Abs(amostras[i] - amostras[i - 1]) / (float)short.MaxValue;
                if (salto > pior) pior = salto;
            }
            return pior;
        }
    }
}

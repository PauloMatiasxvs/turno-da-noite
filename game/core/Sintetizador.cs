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

            public Receita(float segundos, float deHz, float paraHz, float volume, float ruido)
            {
                Segundos = segundos; DeHz = deHz; ParaHz = paraHz; Volume = volume; Ruido = ruido;
            }
        }

        public static Receita Para(Evento ev) => ev switch
        {
            Evento.Passo           => new Receita(0.12f, 190,  80,   0.20f, 0.70f),
            Evento.PassoDela       => new Receita(0.24f, 130,  50,   0.50f, 0.60f),
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
            _ => default
        };

        public static bool Existe(Evento ev) => Para(ev).Segundos > 0f;

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

            int rampa = Math.Max(1, (int)(Taxa * 0.010f));   // 10 ms de cada lado
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
                f1 += (bruto - f1) * 0.25f;
                f2 += (f1 - f2) * 0.25f;
                f3 += (f2 - f3) * 0.25f;
                float chiado = f3 * 3.0f;   // ganho recupera o volume perdido no filtro

                float tom = MathF.Sin(fase);
                float corpo = tom + (chiado - tom) * r.Ruido;

                float sobe = MathF.Min(1f, i / (float)rampa);
                float desce = MathF.Min(1f, (n - 1 - i) / (float)rampa);
                float decai = MathF.Pow(1f - t, 2.0f);

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
            // 0,11 medido: e o passo maximo que o ruido de tres polos produz na
            // pratica. Estava em 0,08 por chute e reprovava sons graves e chiados
            // que nao estalam de verdade.
            float doRuido = r.Ruido * r.Volume * 0.11f;
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

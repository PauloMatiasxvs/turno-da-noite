using System.Collections.Generic;
using Godot;
using TurnoDaNoite.Core;

namespace TurnoDaNoite.Jogo
{
    /// <summary>
    /// Áudio sintetizado na hora, sem nenhum arquivo no repositório.
    ///
    /// Os sons dela tocam POSICIONADOS no mundo (AudioStreamPlayer3D), e é isso
    /// que dá ao jogador a única informação de defesa que existe: de que lado
    /// vieram os passos. Os sons do próprio jogador tocam sem posição, porque
    /// vêm de dentro da cabeça dele.
    ///
    /// Quando você tiver sons gravados, ponha os .ogg em assets/sons com o nome
    /// do evento (ex.: PassoDela.ogg) que eles passam a ser usados no lugar.
    /// </summary>
    public static class Som
    {
        const int Taxa = 22050;
        static readonly Dictionary<Evento, AudioStream> Cache = new();

        /// <summary>Eventos que vêm dela e precisam de posição no mundo.</summary>
        static readonly HashSet<Evento> Posicionados = new()
        {
            Evento.PassoDela, Evento.ElaRugiu, Evento.ElaArranhou, Evento.ElaViuVoce
        };

        public static void Tocar(Node pai, Evento ev, Partida partida)
        {
            var fluxo = Obter(ev);
            if (fluxo == null) return;

            float db = Mathf.LinearToDb(Mathf.Max(Opcoes.Volume, 0.0001f)) - 6f;

            if (Posicionados.Contains(ev))
            {
                var p = new AudioStreamPlayer3D
                {
                    Stream = fluxo,
                    VolumeDb = db,
                    UnitSize = 6f,
                    MaxDistance = 60f,
                    Autoplay = true,
                    Position = new Vector3(partida.UltimoSomDela.X, 1.5f, partida.UltimoSomDela.Z)
                };
                pai.AddChild(p);
                p.Finished += () => p.QueueFree();
            }
            else
            {
                var p = new AudioStreamPlayer { Stream = fluxo, VolumeDb = db, Autoplay = true };
                pai.AddChild(p);
                p.Finished += () => p.QueueFree();
            }
        }

        static AudioStream Obter(Evento ev)
        {
            if (Cache.TryGetValue(ev, out var pronto)) return pronto;

            // som gravado, se existir, tem prioridade sobre o sintetizado
            string caminho = $"res://assets/sons/{ev}.ogg";
            if (ResourceLoader.Exists(caminho))
            {
                var arquivo = GD.Load<AudioStream>(caminho);
                if (arquivo != null) { Cache[ev] = arquivo; return arquivo; }
            }

            AudioStream feito = ev switch
            {
                Evento.Passo          => Onda(0.10f, 240, 90, 0.22f, 0.75f),
                Evento.PassoDela      => Onda(0.22f, 150, 55, 0.55f, 0.65f),
                Evento.Respiracao     => Onda(0.42f, 620, 240, 0.16f, 0.85f),
                Evento.Ofegante       => Onda(0.55f, 700, 200, 0.30f, 0.9f),
                Evento.Batida         => Onda(0.18f, 56, 30, 0.35f, 0f),
                Evento.PegouFusivel   => Onda(0.14f, 880, 1400, 0.30f, 0f),
                Evento.PegouBateria   => Onda(0.10f, 520, 780, 0.22f, 0.1f),
                Evento.InstalouFusivel=> Onda(0.28f, 160, 60, 0.45f, 0.5f),
                Evento.EnergiaVoltou  => Onda(1.40f, 60, 240, 0.35f, 0.25f),
                Evento.AbriuArmario   => Onda(0.45f, 900, 260, 0.35f, 0.7f),
                Evento.FechouArmario  => Onda(0.35f, 700, 200, 0.32f, 0.7f),
                Evento.LanternaLigou  => Onda(0.07f, 1400, 900, 0.18f, 0.6f),
                Evento.LanternaApagou => Onda(0.09f, 900, 400, 0.18f, 0.6f),
                Evento.ElaViuVoce     => Onda(0.95f, 420, 90, 0.75f, 0.45f),
                Evento.ElaRugiu       => Onda(0.85f, 300, 70, 0.60f, 0.5f),
                Evento.ElaArranhou    => Onda(0.55f, 1800, 400, 0.35f, 0.85f),
                Evento.VocePegou      => Onda(1.60f, 900, 40, 0.95f, 0.5f),
                Evento.VoceEscapou    => Onda(1.30f, 220, 440, 0.40f, 0f),
                Evento.PortaoTrancado => Onda(0.30f, 320, 120, 0.35f, 0.55f),
                _ => null
            };

            if (feito != null) Cache[ev] = feito;
            return feito;
        }

        /// <summary>
        /// Varredura de frequência com decaimento, misturada a ruído.
        /// ruido = 0 dá tom limpo (batida do coração); 1 dá só chiado (arranhão).
        /// </summary>
        static AudioStreamWav Onda(float segundos, float deHz, float paraHz, float volume, float ruido)
        {
            int n = (int)(Taxa * segundos);
            var pcm = new byte[n * 2];
            var rng = new RandomNumberGenerator();
            rng.Randomize();

            float fase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float freq = Mathf.Lerp(deHz, paraHz, t);
                fase += freq / Taxa * Mathf.Tau;

                float tom = Mathf.Sin(fase);
                float chiado = rng.Randf() * 2f - 1f;
                float envelope = Mathf.Pow(1f - t, 2.0f);
                float amostra = Mathf.Lerp(tom, chiado, ruido) * envelope * volume;

                short s = (short)(Mathf.Clamp(amostra, -1f, 1f) * short.MaxValue);
                pcm[i * 2] = (byte)(s & 0xFF);
                pcm[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
            }

            return new AudioStreamWav
            {
                Data = pcm,
                Format = AudioStreamWav.FormatEnum.Format16Bits,
                MixRate = Taxa,
                Stereo = false
            };
        }
    }
}

using Godot;
using NeonArena.Core;

namespace NeonArena.GodotGame
{
    /// <summary>
    /// Som sintetizado em tempo de execução — nenhum arquivo de áudio no repositório.
    /// Cada efeito é uma onda gerada por matemática e escrita num AudioStreamWav,
    /// o equivalente ao que a versão web faz com osciladores do WebAudio.
    /// </summary>
    public static class Sfx
    {
        const int SampleRate = 22050;
        static readonly System.Collections.Generic.Dictionary<SimEvent, AudioStream> Cache
            = new System.Collections.Generic.Dictionary<SimEvent, AudioStream>();

        public static void Play(Node parent, SimEvent ev)
        {
            AudioStream stream = Get(ev);
            if (stream == null) return;

            var player = new AudioStreamPlayer { Stream = stream, VolumeDb = -8f, Autoplay = true };
            parent.AddChild(player);
            player.Finished += () => player.QueueFree();
        }

        static AudioStream Get(SimEvent ev)
        {
            if (Cache.TryGetValue(ev, out var cached)) return cached;

            AudioStream made = null;
            switch (ev)
            {
                case SimEvent.Shot:       made = Make(0.09f, 240f, 70f, 0.55f, 0.30f); break;
                case SimEvent.HitEnemy:   made = Make(0.06f, 1500f, 900f, 0.30f, 0f); break;
                case SimEvent.CoreHit:    made = Make(0.10f, 2200f, 1200f, 0.42f, 0f); break;
                case SimEvent.Killed:     made = Make(0.32f, 420f, 60f, 0.45f, 0.45f); break;
                case SimEvent.PlayerHurt: made = Make(0.24f, 180f, 60f, 0.55f, 0.35f); break;
                case SimEvent.Reloaded:   made = Make(0.10f, 1600f, 700f, 0.25f, 0.60f); break;
                case SimEvent.DryFire:    made = Make(0.05f, 3200f, 2200f, 0.22f, 0.80f); break;
                case SimEvent.PickupTaken:made = Make(0.16f, 680f, 1020f, 0.35f, 0f); break;
                case SimEvent.WaveStarted:made = Make(0.50f, 330f, 550f, 0.35f, 0f); break;
                case SimEvent.EnemySpawned:made = Make(0.28f, 90f, 300f, 0.25f, 0f); break;
                case SimEvent.GameOver:   made = Make(1.10f, 300f, 40f, 0.50f, 0.30f); break;
                default: return null;
            }

            Cache[ev] = made;
            return made;
        }

        /// <summary>
        /// Varredura de frequência com decaimento exponencial, misturada a ruído.
        /// noise = 0 dá tom limpo; noise = 1 dá só chiado (explosões, impactos).
        /// </summary>
        static AudioStreamWav Make(float seconds, float fromHz, float toHz, float volume, float noise)
        {
            int count = (int)(SampleRate * seconds);
            var pcm = new byte[count * 2];
            var rng = new RandomNumberGenerator();
            rng.Seed = 1234;

            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float freq = Mathf.Lerp(fromHz, toHz, t);
                phase += freq / SampleRate * Mathf.Tau;

                float tone = Mathf.Sin(phase);
                float hiss = rng.Randf() * 2f - 1f;
                float envelope = Mathf.Pow(1f - t, 2.2f);
                float sample = Mathf.Lerp(tone, hiss, noise) * envelope * volume;

                short s = (short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue);
                pcm[i * 2] = (byte)(s & 0xFF);
                pcm[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
            }

            return new AudioStreamWav
            {
                Data = pcm,
                Format = AudioStreamWav.FormatEnum.Format16Bits,
                MixRate = SampleRate,
                Stereo = false
            };
        }
    }
}

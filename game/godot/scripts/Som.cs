using System.Collections.Generic;
using Godot;
using TurnoDaNoite.Core;

namespace TurnoDaNoite.Jogo
{
    /// <summary>
    /// Toca os efeitos. A síntese em si NÃO mora aqui — mora em
    /// Core/Sintetizador.cs, coberta por testes que medem estalo, estouro e
    /// silêncio nas pontas. Aqui só se embrulha a onda num recurso do Godot
    /// e se decide se ela toca posicionada no mundo ou na cabeça do jogador.
    ///
    /// Sons gravados têm prioridade: ponha um .ogg em assets/sons com o nome
    /// do evento (PassoDela.ogg, VocePegou.ogg) e ele substitui o sintetizado.
    /// </summary>
    public static class Som
    {
        /// <summary>
        /// Um evento pode ter várias gravações. Passo tem seis: enquanto tinha
        /// uma só, o jogo repetia a mesma onda idêntica duas vezes por segundo,
        /// e onda idêntica repetida o ouvido reconhece na hora como máquina.
        /// </summary>
        static readonly Dictionary<Evento, AudioStream[]> Cache = new();
        static readonly RandomNumberGenerator Sorte = new();

        /// <summary>Eventos que vêm dela e precisam de posição no mundo.</summary>
        static readonly HashSet<Evento> Posicionados = new()
        {
            Evento.PassoDela, Evento.ElaRugiu, Evento.ElaArranhou, Evento.ElaViuVoce
        };

        /// <summary>
        /// Teto de sons simultâneos. Sem isto, vários efeitos somados estouram
        /// a saída e o que se ouve é distorção, que soa igual a estalo.
        /// </summary>
        const int MaximoSimultaneo = 8;
        static int _tocando;

        public static void Tocar(Node pai, Evento ev, Partida partida)
        {
            var fluxo = Obter(ev);
            if (fluxo == null) return;
            if (_tocando >= MaximoSimultaneo) return;

            float db = Mathf.LinearToDb(Mathf.Max(Opcoes.Volume, 0.0001f)) - 6f;
            _tocando++;

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
                p.Finished += () => { _tocando--; p.QueueFree(); };
            }
            else
            {
                var p = new AudioStreamPlayer { Stream = fluxo, VolumeDb = db, Autoplay = true };
                pai.AddChild(p);
                p.Finished += () => { _tocando--; p.QueueFree(); };
            }
        }

        static AudioStream Obter(Evento ev)
        {
            var lista = Montar(ev);
            if (lista == null || lista.Length == 0) return null;
            return lista.Length == 1 ? lista[0] : lista[Sorte.RandiRange(0, lista.Length - 1)];
        }

        static AudioStream[] Montar(Evento ev)
        {
            if (Cache.TryGetValue(ev, out var pronto)) return pronto;

            // som gravado, se existir, ganha do sintetizado
            string caminho = $"res://assets/sons/{ev}.ogg";
            if (ResourceLoader.Exists(caminho))
            {
                var arquivo = GD.Load<AudioStream>(caminho);
                if (arquivo != null) { Cache[ev] = new[] { arquivo }; return Cache[ev]; }
            }

            if (!Sintetizador.Existe(ev)) return null;

            int quantas = Sintetizador.Variacoes(ev);
            var feitas = new AudioStream[quantas];
            for (int v = 0; v < quantas; v++)
                feitas[v] = ParaWav(Sintetizador.Gerar(Sintetizador.Para(ev, v),
                                                       (int)ev * 977 + 13 + v * 7919));

            Cache[ev] = feitas;
            return feitas;
        }

        static AudioStreamWav ParaWav(short[] amostras)
        {
            var pcm = new byte[amostras.Length * 2];
            for (int i = 0; i < amostras.Length; i++)
            {
                pcm[i * 2] = (byte)(amostras[i] & 0xFF);
                pcm[i * 2 + 1] = (byte)((amostras[i] >> 8) & 0xFF);
            }

            return new AudioStreamWav
            {
                Data = pcm,
                Format = AudioStreamWav.FormatEnum.Format16Bits,
                MixRate = Sintetizador.Taxa,
                Stereo = false
            };
        }
    }
}

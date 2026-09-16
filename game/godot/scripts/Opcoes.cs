using Godot;

namespace TurnoDaNoite.Jogo
{
    /// <summary>
    /// Ajustes do jogador, salvos em user:// — que no Windows cai em
    /// %APPDATA%\Godot\app_userdata\Turno da Noite. Sensibilidade de mouse é
    /// a primeira reclamação de todo jogo em primeira pessoa, então ela é
    /// ajustável em jogo e persiste entre sessões.
    /// </summary>
    public static class Opcoes
    {
        const string Caminho = "user://opcoes.cfg";

        public static float Sensibilidade { get; private set; } = 0.0022f;
        public static bool InverterY { get; private set; }
        public static float Volume { get; private set; } = 0.85f;

        public const float SensMin = 0.0006f;
        public const float SensMax = 0.008f;
        const float SensPadrao = 0.0022f;

        public static void Carregar()
        {
            var cfg = new ConfigFile();
            if (cfg.Load(Caminho) != Error.Ok) return;

            Sensibilidade = Mathf.Clamp((float)cfg.GetValue("mouse", "sensibilidade", SensPadrao), SensMin, SensMax);
            InverterY = (bool)cfg.GetValue("mouse", "inverter_y", false);
            Volume = Mathf.Clamp((float)cfg.GetValue("audio", "volume", 0.85f), 0f, 1f);
        }

        public static void Salvar()
        {
            var cfg = new ConfigFile();
            cfg.SetValue("mouse", "sensibilidade", Sensibilidade);
            cfg.SetValue("mouse", "inverter_y", InverterY);
            cfg.SetValue("audio", "volume", Volume);
            cfg.Save(Caminho);
        }

        /// <summary>Um passo para cada lado. Devolve a porcentagem para mostrar na tela.</summary>
        public static int AjustarSensibilidade(int direcao)
        {
            Sensibilidade = Mathf.Clamp(Sensibilidade + direcao * 0.0003f, SensMin, SensMax);
            Salvar();
            return Porcentagem;
        }

        public static int Porcentagem => Mathf.RoundToInt(Sensibilidade / SensPadrao * 100f);

        public static void DefinirVolume(float v)
        {
            Volume = Mathf.Clamp(v, 0f, 1f);
            Salvar();
        }

        public static void AlternarInverterY()
        {
            InverterY = !InverterY;
            Salvar();
        }
    }
}

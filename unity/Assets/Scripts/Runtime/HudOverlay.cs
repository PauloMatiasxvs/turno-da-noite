using UnityEngine;
using NeonArena.Core;

namespace NeonArena.Unity
{
    /// <summary>
    /// HUD desenhado com IMGUI (OnGUI). Escolha deliberada: não exige Canvas,
    /// TextMeshPro nem nenhum asset importado — o projeto abre e funciona numa
    /// cena vazia. Para um jogo de verdade, troque por uGUI.
    /// </summary>
    public class HudOverlay : MonoBehaviour
    {
        Sim _sim;
        Texture2D _white;
        GUIStyle _big, _small, _center;

        static readonly Color Cyan = new Color(0.20f, 0.85f, 1f);
        static readonly Color Ice = new Color(0.86f, 0.92f, 0.96f);
        static readonly Color Dim = new Color(0.44f, 0.51f, 0.60f);
        static readonly Color Red = new Color(1f, 0.23f, 0.32f);
        static readonly Color Amber = new Color(1f, 0.69f, 0.23f);
        static readonly Color Green = new Color(0.30f, 1f, 0.62f);

        public void Bind(Sim sim) => _sim = sim;

        void Awake()
        {
            _white = new Texture2D(1, 1);
            _white.SetPixel(0, 0, Color.white);
            _white.Apply();
        }

        void EnsureStyles()
        {
            if (_big != null) return;
            _big = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            _center = new GUIStyle(GUI.skin.label)
            {
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }

        void Rect01(float x, float y, float w, float h, Color c)
        {
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(new Rect(x, y, w, h), _white);
            GUI.color = old;
        }

        void Label(float x, float y, string text, GUIStyle style, Color color)
        {
            Color old = style.normal.textColor;
            style.normal.textColor = color;
            GUI.Label(new Rect(x, y, 420, style.fontSize + 12), text, style);
            style.normal.textColor = old;
        }

        void OnGUI()
        {
            if (_sim == null) return;
            EnsureStyles();

            float w = Screen.width, h = Screen.height;

            // canto superior esquerdo: onda e combo
            Label(24, 10, "ONDA", _small, Dim);
            Label(24, 24, _sim.Wave.ToString(), _big, Cyan);
            if (_sim.Combo > 1) Label(24, 64, $"COMBO x{_sim.Combo}", _small, Amber);

            // canto superior direito: pontos
            Label(w - 220, 10, "PONTOS", _small, Dim);
            Label(w - 220, 24, _sim.Score.ToString("N0"), _big, Ice);

            // canto inferior esquerdo: barra de vida
            float pct = _sim.Player.MaxHp <= 0 ? 0f : Mathf.Clamp01(_sim.Player.Hp / _sim.Player.MaxHp);
            Label(24, h - 82, "INTEGRIDADE", _small, Dim);
            Rect01(24, h - 62, 230, 12, new Color(1, 1, 1, 0.10f));
            Rect01(24, h - 62, 230 * pct, 12, pct < 0.35f ? Red : Green);

            // canto inferior direito: arma e munição
            int wi = _sim.Player.Weapon;
            string name = Rules.Weapons[wi].Name.ToUpper();
            string ammo = _sim.Player.ReloadTimer > 0f
                ? "RECARREGANDO"
                : $"{_sim.Player.Ammo[wi]} / {_sim.Player.Reserve[wi]}";
            Label(w - 220, h - 84, name, _small, Amber);
            Label(w - 220, h - 66, ammo, _big, _sim.Player.Ammo[wi] == 0 ? Red : Ice);

            // mira
            if (_sim.Phase == SimPhase.Playing)
            {
                float cx = w / 2f, cy = h / 2f;
                Rect01(cx - 14, cy - 1, 9, 2, Ice);
                Rect01(cx + 5, cy - 1, 9, 2, Ice);
                Rect01(cx - 1, cy - 14, 2, 9, Ice);
                Rect01(cx - 1, cy + 5, 2, 9, Ice);
            }
            else if (_sim.Phase == SimPhase.Dead)
            {
                GUI.Label(new Rect(0, h * 0.38f, w, 120),
                    $"SISTEMA OFFLINE\nonda {_sim.Wave} · {_sim.Score:N0} pontos · {_sim.Kills} abates\nF5 para recomeçar",
                    _center);
            }
        }
    }
}

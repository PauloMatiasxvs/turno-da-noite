using Godot;
using NeonArena.Core;

namespace NeonArena.GodotGame
{
    /// <summary>HUD montado por código: mira, vida, munição, onda e pontos.</summary>
    public partial class Hud : CanvasLayer
    {
        Label _wave, _score, _ammo, _hp, _center, _combo;
        ColorRect _hpBar, _hpFill;
        Control _crosshair;

        static readonly Color Cyan = new Color(0.20f, 0.85f, 1f);
        static readonly Color Ice = new Color(0.86f, 0.92f, 0.96f);
        static readonly Color Dim = new Color(0.44f, 0.51f, 0.60f);
        static readonly Color Red = new Color(1f, 0.23f, 0.32f);
        static readonly Color Amber = new Color(1f, 0.69f, 0.23f);

        public override void _Ready()
        {
            _wave = MakeLabel(new Vector2(28, 22), 30, Cyan);
            MakeLabel(new Vector2(28, 8), 12, Dim).Text = "ONDA";

            _score = MakeLabel(new Vector2(-170, 22), 30, Ice);
            _score.HorizontalAlignment = HorizontalAlignment.Right;
            _score.AnchorLeft = 1; _score.AnchorRight = 1;
            _score.Size = new Vector2(150, 40);

            _combo = MakeLabel(new Vector2(28, 62), 16, Amber);

            _hpBar = new ColorRect
            {
                Color = new Color(1, 1, 1, 0.08f),
                Position = new Vector2(28, -58),
                Size = new Vector2(230, 12),
                AnchorTop = 1, AnchorBottom = 1
            };
            AddChildControl(_hpBar);

            _hpFill = new ColorRect
            {
                Color = new Color(0.30f, 1f, 0.62f),
                Position = new Vector2(0, 0),
                Size = new Vector2(230, 12)
            };
            _hpBar.AddChild(_hpFill);

            _hp = MakeLabel(new Vector2(28, -84), 13, Dim);
            _hp.AnchorTop = 1; _hp.AnchorBottom = 1;

            _ammo = MakeLabel(new Vector2(-210, -70), 34, Ice);
            _ammo.HorizontalAlignment = HorizontalAlignment.Right;
            _ammo.AnchorLeft = 1; _ammo.AnchorRight = 1;
            _ammo.AnchorTop = 1; _ammo.AnchorBottom = 1;
            _ammo.Size = new Vector2(180, 44);

            _center = MakeLabel(Vector2.Zero, 34, Cyan);
            _center.HorizontalAlignment = HorizontalAlignment.Center;
            _center.VerticalAlignment = VerticalAlignment.Center;
            _center.AnchorRight = 1; _center.AnchorBottom = 1;

            BuildCrosshair();
        }

        void AddChildControl(Control c) => AddChild(c);

        Label MakeLabel(Vector2 pos, int size, Color color)
        {
            var label = new Label
            {
                Position = pos,
                Size = new Vector2(320, size + 10)
            };
            label.AddThemeFontSizeOverride("font_size", size);
            label.AddThemeColorOverride("font_color", color);
            AddChild(label);
            return label;
        }

        void BuildCrosshair()
        {
            _crosshair = new Control { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f };
            AddChild(_crosshair);

            void Bar(Vector2 pos, Vector2 size)
            {
                _crosshair.AddChild(new ColorRect { Color = Ice, Position = pos, Size = size });
            }
            Bar(new Vector2(-14, -1), new Vector2(9, 2));
            Bar(new Vector2(5, -1), new Vector2(9, 2));
            Bar(new Vector2(-1, -14), new Vector2(2, 9));
            Bar(new Vector2(-1, 5), new Vector2(2, 9));
        }

        public void Refresh(Sim sim)
        {
            _wave.Text = sim.Wave.ToString();
            _score.Text = sim.Score.ToString("N0");
            _combo.Text = sim.Combo > 1 ? $"COMBO x{sim.Combo}" : "";

            float pct = sim.Player.MaxHp <= 0 ? 0 : sim.Player.Hp / sim.Player.MaxHp;
            _hpFill.Size = new Vector2(230f * Mathf.Clamp(pct, 0f, 1f), 12);
            _hpFill.Color = pct < 0.35f ? Red : new Color(0.30f, 1f, 0.62f);
            _hp.Text = $"INTEGRIDADE  {Mathf.RoundToInt(sim.Player.Hp)}";

            int w = sim.Player.Weapon;
            string name = Rules.Weapons[w].Name.ToUpper();
            _ammo.Text = sim.Player.ReloadTimer > 0f
                ? $"{name}  RECARREGANDO"
                : $"{name}  {sim.Player.Ammo[w]} / {sim.Player.Reserve[w]}";

            if (sim.Phase == SimPhase.Dead)
            {
                _center.Text = $"SISTEMA OFFLINE\nonda {sim.Wave} · {sim.Score:N0} pontos\nF5 para recomeçar";
                _center.AddThemeColorOverride("font_color", Red);
                _crosshair.Visible = false;
            }
            else
            {
                _center.Text = sim.Wave == 0 ? "PREPARE-SE" : "";
                _crosshair.Visible = true;
            }
        }
    }
}

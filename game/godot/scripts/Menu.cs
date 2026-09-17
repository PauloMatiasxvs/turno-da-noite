using System;
using Godot;

namespace TurnoDaNoite.Jogo
{
    /// <summary>
    /// Tela de título, opções e a história de abertura.
    ///
    /// Antes o jogo começava direto, no escuro, sem nada: a primeira pergunta
    /// de quem jogou foi "como que ganha?". Aqui o jogador entende onde está,
    /// o que quer, e ajusta o mouse antes de descer.
    /// </summary>
    public partial class Menu : CanvasLayer
    {
        public event Action AoComecar;

        static readonly Color Osso = new(0.82f, 0.80f, 0.75f);
        static readonly Color Fraco = new(0.42f, 0.42f, 0.45f);
        static readonly Color Sangue = new(0.55f, 0.09f, 0.14f);
        static readonly Color Ambar = new(0.85f, 0.60f, 0.24f);

        Control _raizTitulo, _raizOpcoes, _raizHistoria;
        Label _titulo, _historiaTexto, _avisoSens, _avisoVol;
        ColorRect _fundo;

        float _piscaEm, _tempo;
        int _carta = -1;
        float _tempoDaCarta;

        /// <summary>
        /// As cartas da história. Curtas de propósito: ninguém lê parágrafo
        /// antes de jogar, mas quatro linhas dão contexto e criam expectativa.
        /// </summary>
        static readonly string[] Historia =
        {
            "Subestação 7 foi desativada em 1998.\nA companhia nunca explicou por quê.",
            "Você faz o turno da noite há três semanas.\nSempre do lado de fora, sempre no portão.",
            "Hoje a energia caiu às 2h14.\nO manual diz: cinco fusíveis, quadro do subsolo.",
            "O manual não diz o que fazer\nse alguma coisa lá dentro estiver acordada."
        };

        public override void _Ready()
        {
            _fundo = new ColorRect
            {
                Color = new Color(0.015f, 0.015f, 0.02f),
                AnchorRight = 1, AnchorBottom = 1
            };
            AddChild(_fundo);

            MontarTitulo();
            MontarOpcoes();
            MontarHistoria();

            _raizOpcoes.Visible = false;
            _raizHistoria.Visible = false;
        }

        // ------------------------------------------------------------ título

        void MontarTitulo()
        {
            _raizTitulo = NovoPainel();

            _titulo = Rotulo("TURNO DA NOITE", 58, Osso);
            _titulo.Position = new Vector2(0, -170);
            _raizTitulo.AddChild(_titulo);

            var sub = Rotulo("SUBESTAÇÃO 7   ·   DESATIVADA EM 1998", 13, Fraco);
            sub.Position = new Vector2(0, -100);
            _raizTitulo.AddChild(sub);

            var caixa = new VBoxContainer
            {
                AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f,
                Position = new Vector2(-130, -30),
                CustomMinimumSize = new Vector2(260, 0)
            };
            caixa.AddThemeConstantOverride("separation", 10);
            _raizTitulo.AddChild(caixa);

            caixa.AddChild(Botao("DESCER", () => ComecarHistoria()));
            caixa.AddChild(Botao("OPÇÕES", () => { _raizTitulo.Visible = false; _raizOpcoes.Visible = true; }));
            caixa.AddChild(Botao("SAIR", () => GetTree().Quit()));

            var rodape = Rotulo("use fone de ouvido — a direção do som é a sua defesa", 12, Fraco);
            rodape.Position = new Vector2(0, 175);
            _raizTitulo.AddChild(rodape);
        }

        // ------------------------------------------------------------ opções

        void MontarOpcoes()
        {
            _raizOpcoes = NovoPainel();

            var t = Rotulo("OPÇÕES", 34, Osso);
            t.Position = new Vector2(0, -150);
            _raizOpcoes.AddChild(t);

            _avisoSens = Rotulo("", 14, Ambar);
            _avisoSens.Position = new Vector2(0, -80);
            _raizOpcoes.AddChild(_avisoSens);

            var sens = Deslizante(Opcoes.SensMin, Opcoes.SensMax, Opcoes.Sensibilidade, -50,
                v => { Opcoes.DefinirSensibilidade((float)v); AtualizarAvisos(); });
            _raizOpcoes.AddChild(sens);

            _avisoVol = Rotulo("", 14, Ambar);
            _avisoVol.Position = new Vector2(0, 10);
            _raizOpcoes.AddChild(_avisoVol);

            var vol = Deslizante(0, 1, Opcoes.Volume, 40,
                v => { Opcoes.DefinirVolume((float)v); AtualizarAvisos(); });
            _raizOpcoes.AddChild(vol);

            var inverter = Botao(TextoInverter(), null);
            inverter.Pressed += () =>
            {
                Opcoes.AlternarInverterY();
                inverter.Text = TextoInverter();
            };
            inverter.AnchorLeft = 0.5f; inverter.AnchorRight = 0.5f;
            inverter.Position = new Vector2(-130, 90);
            inverter.CustomMinimumSize = new Vector2(260, 0);
            _raizOpcoes.AddChild(inverter);

            var voltar = Botao("VOLTAR", () => { _raizOpcoes.Visible = false; _raizTitulo.Visible = true; });
            voltar.AnchorLeft = 0.5f; voltar.AnchorRight = 0.5f;
            voltar.Position = new Vector2(-130, 145);
            voltar.CustomMinimumSize = new Vector2(260, 0);
            _raizOpcoes.AddChild(voltar);

            AtualizarAvisos();
        }

        static string TextoInverter() => Opcoes.InverterY ? "EIXO Y: INVERTIDO" : "EIXO Y: NORMAL";

        void AtualizarAvisos()
        {
            _avisoSens.Text = $"sensibilidade do mouse — {Opcoes.Porcentagem}%";
            _avisoVol.Text = $"volume — {Mathf.RoundToInt(Opcoes.Volume * 100)}%";
        }

        HSlider Deslizante(double min, double max, double valor, float y, Action<double> aoMudar)
        {
            var s = new HSlider
            {
                MinValue = min, MaxValue = max, Value = valor,
                Step = (max - min) / 100.0,
                AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f,
                Position = new Vector2(-130, y),
                CustomMinimumSize = new Vector2(260, 18)
            };
            s.ValueChanged += v => aoMudar(v);
            return s;
        }

        // ------------------------------------------------------------ história

        void MontarHistoria()
        {
            _raizHistoria = NovoPainel();

            _historiaTexto = Rotulo("", 22, Osso);
            _historiaTexto.Position = new Vector2(0, -40);
            _raizHistoria.AddChild(_historiaTexto);

            var pular = Rotulo("clique ou aperte qualquer tecla", 12, Fraco);
            pular.Position = new Vector2(0, 170);
            _raizHistoria.AddChild(pular);
        }

        void ComecarHistoria()
        {
            _raizTitulo.Visible = false;
            _raizHistoria.Visible = true;
            _carta = 0;
            _tempoDaCarta = 0;
            _historiaTexto.Text = Historia[0];
        }

        /// <summary>Avança a história. Devolve true quando ela acabou.</summary>
        public bool Avancar()
        {
            if (_raizOpcoes.Visible) { _raizOpcoes.Visible = false; _raizTitulo.Visible = true; return false; }
            if (_carta < 0) return false;

            _carta++;
            _tempoDaCarta = 0;
            if (_carta < Historia.Length)
            {
                _historiaTexto.Text = Historia[_carta];
                return false;
            }

            Visible = false;
            AoComecar?.Invoke();
            return true;
        }

        public bool NaHistoria => _carta >= 0;

        /// <summary>Vai direto ao jogo. Usado pelo modo de captura de tela.</summary>
        public void PularTudo()
        {
            Visible = false;
            AoComecar?.Invoke();
        }

        public override void _Process(double delta)
        {
            _tempo += (float)delta;

            // o título tem mau contato, como as lâmpadas do prédio
            if (_titulo != null && _raizTitulo.Visible)
            {
                if (_tempo > _piscaEm)
                {
                    _piscaEm = _tempo + (float)GD.RandRange(0.4, 3.5);
                    _titulo.Modulate = GD.Randf() < 0.35f
                        ? new Color(1, 1, 1, 0.45f)
                        : Colors.White;
                }
            }

            // cada carta fica um tempo e passa sozinha, para quem não clica
            if (_carta >= 0 && _raizHistoria.Visible)
            {
                _tempoDaCarta += (float)delta;
                float fade = Mathf.Min(1f, _tempoDaCarta * 1.6f);
                _historiaTexto.Modulate = new Color(1, 1, 1, fade);
                if (_tempoDaCarta > 5.5f) Avancar();
            }
        }

        // ------------------------------------------------------------ peças

        Control NovoPainel()
        {
            var c = new Control { AnchorRight = 1, AnchorBottom = 1, MouseFilter = Control.MouseFilterEnum.Pass };
            AddChild(c);
            return c;
        }

        Label Rotulo(string texto, int tamanho, Color cor)
        {
            var l = new Label
            {
                Text = texto,
                AnchorLeft = 0, AnchorRight = 1,
                AnchorTop = 0.5f, AnchorBottom = 0.5f,
                HorizontalAlignment = HorizontalAlignment.Center,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            l.AddThemeFontSizeOverride("font_size", tamanho);
            l.AddThemeColorOverride("font_color", cor);
            l.AddThemeConstantOverride("line_spacing", 8);
            return l;
        }

        Button Botao(string texto, Action aoClicar)
        {
            var b = new Button { Text = texto, CustomMinimumSize = new Vector2(0, 42) };
            b.AddThemeFontSizeOverride("font_size", 15);
            b.AddThemeColorOverride("font_color", Osso);
            b.AddThemeColorOverride("font_hover_color", Colors.White);

            b.AddThemeStyleboxOverride("normal", Caixa(new Color(0, 0, 0, 0), new Color(0.35f, 0.35f, 0.38f)));
            b.AddThemeStyleboxOverride("hover", Caixa(new Color(0.12f, 0.12f, 0.14f), Osso));
            b.AddThemeStyleboxOverride("pressed", Caixa(new Color(0.06f, 0.06f, 0.07f), Sangue));
            b.AddThemeStyleboxOverride("focus", Caixa(new Color(0, 0, 0, 0), new Color(0.5f, 0.5f, 0.52f)));

            if (aoClicar != null) b.Pressed += () => aoClicar();
            return b;
        }

        static StyleBoxFlat Caixa(Color fundo, Color borda) => new()
        {
            BgColor = fundo,
            BorderColor = borda,
            BorderWidthLeft = 1, BorderWidthRight = 1,
            BorderWidthTop = 1, BorderWidthBottom = 1,
            ContentMarginLeft = 12, ContentMarginRight = 12,
            ContentMarginTop = 8, ContentMarginBottom = 8
        };
    }
}

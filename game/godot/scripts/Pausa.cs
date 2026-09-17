using System;
using Godot;

namespace TurnoDaNoite.Jogo
{
    /// <summary>
    /// A tela de pausa, com botões de verdade.
    ///
    /// Existe porque "aperto Esc e não consigo sair" apareceu duas vezes. A
    /// primeira resposta foi imprimir um texto no meio da tela — e texto no
    /// meio da tela, sobre um cenário escuro, some. Botão que você clica com
    /// o mouse não depende de o Esc ter chegado, nem de você adivinhar que a
    /// tecla de sair é Q, nem de o texto estar legível sobre o chão de
    /// concreto. É a saída que funciona mesmo quando o resto falhou.
    /// </summary>
    public partial class Pausa : CanvasLayer
    {
        public event Action AoVoltar;
        public event Action AoRecomecar;
        public event Action AoSair;

        static readonly Color Osso = new(0.82f, 0.80f, 0.75f);
        static readonly Color Ambar = new(0.85f, 0.60f, 0.24f);

        Label _sensibilidade;
        int _sensAnterior = -1;

        public override void _Ready()
        {
            Layer = 20;          // por cima da HUD, sempre
            Visible = false;

            var fundo = new ColorRect
            {
                Color = new Color(0.02f, 0.02f, 0.03f, 0.88f),
                AnchorRight = 1, AnchorBottom = 1,
                MouseFilter = Control.MouseFilterEnum.Stop
            };
            AddChild(fundo);

            var caixa = new VBoxContainer
            {
                AnchorLeft = 0.5f, AnchorRight = 0.5f,
                AnchorTop = 0.5f, AnchorBottom = 0.5f,
                GrowHorizontal = Control.GrowDirection.Both,
                GrowVertical = Control.GrowDirection.Both,
                CustomMinimumSize = new Vector2(360, 0)
            };
            caixa.AddThemeConstantOverride("separation", 14);
            fundo.AddChild(caixa);

            var titulo = new Label { Text = "PAUSADO", HorizontalAlignment = HorizontalAlignment.Center };
            titulo.AddThemeFontSizeOverride("font_size", 34);
            titulo.AddThemeColorOverride("font_color", Osso);
            caixa.AddChild(titulo);

            caixa.AddChild(Botao("CONTINUAR      (Esc)", () => AoVoltar?.Invoke()));
            caixa.AddChild(Botao("RECOMEÇAR      (F5)", () => AoRecomecar?.Invoke()));
            caixa.AddChild(Botao("SAIR DO JOGO   (Q)", () => AoSair?.Invoke()));

            var ajuda = new Label
            {
                Text = "- e =  ajustam a sensibilidade do mouse",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            ajuda.AddThemeFontSizeOverride("font_size", 13);
            ajuda.AddThemeColorOverride("font_color", new Color(0.45f, 0.45f, 0.47f));
            caixa.AddChild(ajuda);

            _sensibilidade = new Label { HorizontalAlignment = HorizontalAlignment.Center };
            _sensibilidade.AddThemeFontSizeOverride("font_size", 14);
            _sensibilidade.AddThemeColorOverride("font_color", Ambar);
            caixa.AddChild(_sensibilidade);
        }

        Button Botao(string texto, Action aoClicar)
        {
            var b = new Button { Text = texto, CustomMinimumSize = new Vector2(320, 46) };
            b.AddThemeFontSizeOverride("font_size", 17);
            b.Pressed += () => aoClicar();
            return b;
        }

        public void Abrir()
        {
            Visible = true;
            _sensAnterior = -1;
            Atualizar();
        }

        public void Fechar() => Visible = false;

        public void Atualizar()
        {
            if (Opcoes.Porcentagem == _sensAnterior) return;
            _sensAnterior = Opcoes.Porcentagem;
            _sensibilidade.Text = $"sensibilidade do mouse: {Opcoes.Porcentagem}%";
        }
    }
}

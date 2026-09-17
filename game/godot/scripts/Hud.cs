using Godot;
using TurnoDaNoite.Core;

namespace TurnoDaNoite.Jogo
{
    /// <summary>
    /// HUD mínima. Num jogo de terror a interface tem de sumir: só bateria,
    /// fôlego, fusíveis e a dica do que está ao alcance da mão.
    /// </summary>
    public partial class Hud : CanvasLayer
    {
        Label _objetivo, _dica, _fusiveis, _centro, _sensibilidade, _bussola;
        ColorRect _bateriaFundo, _bateriaBarra, _folegoFundo, _folegoBarra, _escuro;

        static readonly Color Osso = new(0.79f, 0.77f, 0.72f);
        static readonly Color Fraco = new(0.36f, 0.37f, 0.40f);
        static readonly Color Ambar = new(0.85f, 0.60f, 0.24f);
        static readonly Color Sangue = new(0.49f, 0.08f, 0.13f);

        float _tempoDica;
        int _sensAnterior = -1;
        float _tempoSens;

        public override void _Ready()
        {
            _escuro = new ColorRect
            {
                Color = new Color(0, 0, 0, 1),
                AnchorRight = 1, AnchorBottom = 1,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            AddChild(_escuro);

            _objetivo = Texto(12, Fraco);
            _objetivo.AnchorRight = 1;
            _objetivo.Position = new Vector2(0, 70);
            _objetivo.HorizontalAlignment = HorizontalAlignment.Center;

            _dica = Texto(14, Osso);
            _dica.AnchorRight = 1; _dica.AnchorTop = 1; _dica.AnchorBottom = 1;
            _dica.Position = new Vector2(0, -170);
            _dica.HorizontalAlignment = HorizontalAlignment.Center;

            _fusiveis = Texto(26, Osso);
            _fusiveis.AnchorLeft = 1; _fusiveis.AnchorRight = 1;
            _fusiveis.AnchorTop = 1; _fusiveis.AnchorBottom = 1;
            _fusiveis.Position = new Vector2(-190, -70);
            _fusiveis.HorizontalAlignment = HorizontalAlignment.Right;
            _fusiveis.Size = new Vector2(150, 40);

            _bussola = Texto(15, Ambar);
            _bussola.AnchorRight = 1;
            _bussola.Position = new Vector2(0, 130);
            _bussola.HorizontalAlignment = HorizontalAlignment.Center;

            _sensibilidade = Texto(12, Ambar);
            _sensibilidade.AnchorRight = 1;
            _sensibilidade.Position = new Vector2(0, 100);
            _sensibilidade.HorizontalAlignment = HorizontalAlignment.Center;

            _centro = Texto(28, Osso);
            _centro.AnchorRight = 1; _centro.AnchorBottom = 1;
            _centro.HorizontalAlignment = HorizontalAlignment.Center;
            _centro.VerticalAlignment = VerticalAlignment.Center;

            (_folegoFundo, _folegoBarra) = Barra(new Vector2(28, -92), 150, 4, Osso);
            (_bateriaFundo, _bateriaBarra) = Barra(new Vector2(28, -70), 150, 9, Ambar);
        }

        Label Texto(int tamanho, Color cor)
        {
            var l = new Label { Size = new Vector2(420, tamanho + 14) };
            l.AddThemeFontSizeOverride("font_size", tamanho);
            l.AddThemeColorOverride("font_color", cor);
            l.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.9f));
            l.AddThemeConstantOverride("shadow_offset_y", 2);
            AddChild(l);
            return l;
        }

        (ColorRect, ColorRect) Barra(Vector2 pos, float larg, float alt, Color cor)
        {
            var fundo = new ColorRect
            {
                Color = new Color(1, 1, 1, 0.08f),
                Position = pos, Size = new Vector2(larg, alt),
                AnchorTop = 1, AnchorBottom = 1
            };
            AddChild(fundo);
            var barra = new ColorRect { Color = cor, Size = new Vector2(larg, alt) };
            fundo.AddChild(barra);
            return (fundo, barra);
        }

        /// <summary>
        /// Tela de abertura. O jogo largava a pessoa no escuro sem dizer o que
        /// fazer — a primeira reação de quem jogou foi "como que ganha?".
        /// </summary>
        public void MostrarAbertura()
        {
            _centro.Text =
                "TURNO DA NOITE\n\n" +
                "Você é o vigia. A energia caiu às 2h14.\n" +
                "Ache CINCO FUSÍVEIS espalhados pelo prédio,\n" +
                "leve ao QUADRO ELÉTRICO e volte ao PORTÃO.\n\n" +
                "Você não tem arma. Tem algo aqui embaixo.\n\n" +
                "W A S D andar   ·   Mouse olhar   ·   F lanterna\n" +
                "E pegar e usar   ·   Shift correr   ·   Ctrl agachar\n" +
                "TAB mostra para onde ir   ·   ESC pausa\n\n" +
                "clique para começar";
            _centro.AddThemeColorOverride("font_color", Osso);
        }

        public void Atualizar(Partida p, bool pausado = false, string bussola = null)
        {
            if (pausado)
            {
                // Sem este aviso o Esc parecia não fazer nada: o mouse era
                // liberado e mais nada mudava na tela.
                _centro.Text = "PAUSADO\n\nESC — voltar ao jogo\nQ — sair do jogo\nF5 — recomeçar\n\n- e =  ajustam a sensibilidade do mouse";
                _centro.AddThemeColorOverride("font_color", Osso);
                return;
            }

            // abertura: o preto some devagar, o jogo nasce do escuro
            if (_escuro.Color.A > 0)
                _escuro.Color = new Color(0, 0, 0, Mathf.Max(0, _escuro.Color.A - 0.006f));

            var j = p.Jogador;

            _bateriaBarra.Size = new Vector2(150 * Mathf.Clamp(j.Bateria, 0, 1), 9);
            _bateriaBarra.Color = j.Bateria < 0.22f ? Sangue : Ambar;
            _folegoBarra.Size = new Vector2(150 * Mathf.Clamp(j.Folego, 0, 1), 4);
            _fusiveis.Text = $"{j.FusiveisNaMao} / {Regras.FusiveisNecessarios}";

            // Objetivo explícito, e em duas linhas: o que fazer agora e onde você está.
            // "ACHE CINCO FUSÍVEIS" sozinho não diz o que fazer depois de achá-los.
            string tarefa;
            if (p.PortaoAberto)
                tarefa = "ENERGIA RESTABELECIDA — VOLTE AO PORTÃO, NA PORTARIA";
            else if (j.FusiveisNaMao > 0)
                tarefa = $"LEVE OS {j.FusiveisNaMao} FUSÍVEIS AO QUADRO ELÉTRICO";
            else if (j.FusiveisInstalados > 0)
                tarefa = $"FALTAM {Regras.FusiveisNecessarios - j.FusiveisInstalados} FUSÍVEIS — PROCURE NAS SALAS";
            else
                tarefa = "ACHE CINCO FUSÍVEIS PELO PRÉDIO";

            var sala = p.Predio.SalaEm(j.Pos);
            _objetivo.Text = tarefa + (sala != null ? $"\nvocê está: {sala.Nome}" : "\nvocê está: corredor");

            _bussola.Text = bussola ?? "";

            // aviso de sensibilidade aparece só quando você acabou de mexer
            if (Opcoes.Porcentagem != _sensAnterior)
            {
                _sensAnterior = Opcoes.Porcentagem;
                _tempoSens = 1.6f;
            }
            _tempoSens = Mathf.Max(0, _tempoSens - 0.016f);
            _sensibilidade.Text = _tempoSens > 0 ? $"sensibilidade do mouse: {Opcoes.Porcentagem}%" : "";

            _dica.Text = j.Escondido
                ? "E sai · ESPAÇO prende a respiração"
                : DicaDoAlvo(p);

            _centro.Text = p.Fase switch
            {
                Fase.Morto => $"ELE TE PEGOU\n{j.FusiveisInstalados} de {Regras.FusiveisNecessarios} fusíveis instalados\nF5 para tentar de novo",
                Fase.Escapou => "VOCÊ SAIU\nO portão fechou atrás de você.\nF5 para descer de novo",
                _ => ""
            };
            _centro.AddThemeColorOverride("font_color", p.Fase == Fase.Morto ? Sangue : Osso);
        }

        static string DicaDoAlvo(Partida p)
        {
            return p.AlvoMaisPerto(out _) switch
            {
                Partida.Alvo.Fusivel => "E — pegar fusível",
                Partida.Alvo.Bateria => "E — pegar bateria",
                Partida.Alvo.Armario => "E — se esconder",
                Partida.Alvo.Quadro => p.Jogador.FusiveisNaMao > 0
                    ? "E — instalar fusíveis"
                    : "o quadro está vazio",
                Partida.Alvo.Portao => p.PortaoAberto ? "E — sair" : "trancado — falta energia",
                _ => ""
            };
        }
    }
}

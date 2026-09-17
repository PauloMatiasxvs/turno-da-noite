using Godot;
using TurnoDaNoite.Core;

namespace TurnoDaNoite.Jogo
{
    /// <summary>
    /// A planta do prédio, desenhada na tela.
    ///
    /// Só aparece depois que você acha a planta, e só desenha os cômodos em
    /// que já pôs o pé. Isso é a coisa toda: mapa que mostra o prédio inteiro
    /// de graça resolve o jogo, porque o jogo é justamente estar perdido. Um
    /// mapa que enche conforme você anda faz o contrário — dá a medida de
    /// quanto ainda falta, que é uma informação assustadora.
    ///
    /// Desenha com _Draw em vez de nós: são dezenas de retângulos que mudam a
    /// cada passo, e um nó por sala seria lixo de alocação a cada quadro.
    /// </summary>
    public partial class Mapa : Control
    {
        Partida _p;
        int _andarMostrado;

        static readonly Color Papel = new(0.09f, 0.09f, 0.10f, 0.93f);
        static readonly Color Traco = new(0.72f, 0.68f, 0.58f);
        static readonly Color TracoFraco = new(0.34f, 0.33f, 0.31f);
        static readonly Color Voce = new(0.95f, 0.82f, 0.35f);
        static readonly Color Alvo = new(0.40f, 0.85f, 0.95f);
        static readonly Color Saida = new(0.55f, 0.90f, 0.55f);
        static readonly Color Escadinha = new(0.80f, 0.55f, 0.95f);

        public override void _Ready()
        {
            AnchorRight = 1; AnchorBottom = 1;
            MouseFilter = MouseFilterEnum.Ignore;
            Visible = false;
        }

        public void Mostrar(Partida p, int andar)
        {
            _p = p;
            _andarMostrado = andar;
            Visible = true;
            QueueRedraw();
        }

        public void Esconder() { Visible = false; }

        public override void _Draw()
        {
            if (_p == null) return;

            var tela = GetViewportRect().Size;
            float margem = 70f;
            var area = new Rect2(margem, margem, tela.X - margem * 2, tela.Y - margem * 2 - 40);

            DrawRect(new Rect2(Vector2.Zero, tela), new Color(0, 0, 0, 0.82f));
            DrawRect(area, Papel);
            DrawRect(area, TracoFraco, false, 2f);

            var predio = _p.Predio;
            float escala = Mathf.Min(area.Size.X / (predio.Largura * Predio.Celula),
                                     area.Size.Y / (predio.Profundidade * Predio.Celula));
            var centro = area.Position + area.Size / 2;

            Vector2 Projetar(P2 m) => centro + new Vector2(m.X, m.Z) * escala;

            // O rastro: cada célula por onde você passou. É isto que desenha os
            // corredores. Sem eles a planta era uma dúzia de caixas boiando no
            // escuro, sem nada dizendo como uma leva à outra — que é justamente
            // a pergunta que faz alguém abrir um mapa.
            float lado = Predio.Celula * escala;
            for (int z = 0; z < predio.Profundidade; z++)
                for (int x = 0; x < predio.Largura; x++)
                {
                    if (!_p.JaViu(x, z, _andarMostrado)) continue;
                    var m = predio.ParaMundo(new Celula(x, z, _andarMostrado));
                    var canto = Projetar(new P2(m.X - Predio.Celula / 2, m.Z - Predio.Celula / 2));
                    DrawRect(new Rect2(canto, new Vector2(lado + 0.5f, lado + 0.5f)),
                             new Color(Traco, 0.16f));
                }

            // os cômodos já visitados
            foreach (var s in predio.Salas)
            {
                if (s.Andar != _andarMostrado) continue;
                if (!_p.JaViu(s)) continue;

                var canto = predio.ParaMundo(new Celula(s.X, s.Z, s.Andar));
                var fim = predio.ParaMundo(new Celula(s.X + s.Larg, s.Z + s.Alt, s.Andar));
                var a = Projetar(new P2(canto.X - Predio.Celula / 2, canto.Z - Predio.Celula / 2));
                var b = Projetar(new P2(fim.X - Predio.Celula / 2, fim.Z - Predio.Celula / 2));
                var r = new Rect2(a, b - a);

                DrawRect(r, new Color(Traco, 0.10f));
                DrawRect(r, Traco, false, 1.5f);

                if (r.Size.X > 46 && r.Size.Y > 22)
                    DrawString(ThemeDB.FallbackFont, r.Position + new Vector2(5, 14),
                        s.Nome, HorizontalAlignment.Left, r.Size.X - 8, 10, TracoFraco);
            }

            // escadas: aparecem nos dois andares, e é como você sabe por onde subir
            foreach (var e in predio.Escadas)
            {
                if (e.De != _andarMostrado && e.Para != _andarMostrado) continue;
                var m = predio.ParaMundo(new Celula(e.Cx, e.Cz, _andarMostrado));
                var sala = predio.SalaEm(m, _andarMostrado);
                if (sala != null && !_p.JaViu(sala)) continue;

                var q = Projetar(m);
                DrawRect(new Rect2(q - new Vector2(7, 7), new Vector2(14, 14)), Escadinha, false, 2f);
                DrawString(ThemeDB.FallbackFont, q + new Vector2(10, 4), "escada",
                    HorizontalAlignment.Left, -1, 10, Escadinha);
            }

            // o quadro elétrico, se você já esteve na sala dele
            var salaQuadro = predio.SalaEm(_p.Quadro, _p.QuadroAndar);
            if (_p.QuadroAndar == _andarMostrado && salaQuadro != null && _p.JaViu(salaQuadro))
                Marcar(Projetar(_p.Quadro), Alvo, "quadro");

            // a saída fica marcada desde o começo: você entrou por ela
            if (_andarMostrado == 0) Marcar(Projetar(_p.Portao), Saida, "saída");

            // você
            if (_p.Jogador.Andar == _andarMostrado)
            {
                var v = Projetar(_p.Jogador.Pos);
                DrawCircle(v, 6f, Voce);
                var frente = _p.Jogador.Frente;
                DrawLine(v, v + new Vector2(frente.X, frente.Z) * 18f, Voce, 2.5f);
            }

            string andarTexto = _andarMostrado == 0 ? "TÉRREO" : $"{_andarMostrado}º ANDAR";
            DrawString(ThemeDB.FallbackFont, area.Position + new Vector2(8, -12),
                $"PLANTA DO PRÉDIO — {andarTexto}", HorizontalAlignment.Left, -1, 15, Traco);

            string rodape = _p.Jogador.Andar == _andarMostrado
                ? "TAB fecha   ·   Q alterna o andar"
                : "TAB fecha   ·   Q volta ao seu andar";
            DrawString(ThemeDB.FallbackFont, new Vector2(area.Position.X + 8, area.End.Y + 22),
                rodape, HorizontalAlignment.Left, -1, 13, TracoFraco);
        }

        void Marcar(Vector2 onde, Color cor, string nome)
        {
            DrawCircle(onde, 5f, cor);
            DrawString(ThemeDB.FallbackFont, onde + new Vector2(9, 4), nome,
                HorizontalAlignment.Left, -1, 11, cor);
        }
    }
}

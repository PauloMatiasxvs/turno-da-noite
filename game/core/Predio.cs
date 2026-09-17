using System;
using System.Collections.Generic;

namespace TurnoDaNoite.Core
{
    /// <summary>Ponto no plano. O prédio é plano; só a câmera usa altura.</summary>
    public struct P2
    {
        public float X, Z;
        public P2(float x, float z) { X = x; Z = z; }

        public static P2 operator +(P2 a, P2 b) => new P2(a.X + b.X, a.Z + b.Z);
        public static P2 operator -(P2 a, P2 b) => new P2(a.X - b.X, a.Z - b.Z);
        public static P2 operator *(P2 a, float s) => new P2(a.X * s, a.Z * s);

        public float Comprimento => MathF.Sqrt(X * X + Z * Z);
        public P2 Normalizado { get { float l = Comprimento; return l > 1e-6f ? new P2(X / l, Z / l) : default; } }
        public static float Distancia(P2 a, P2 b) => (a - b).Comprimento;
        public static float Escalar(P2 a, P2 b) => a.X * b.X + a.Z * b.Z;
        public override string ToString() => $"({X:0.##}, {Z:0.##})";
    }

    public struct Celula
    {
        public int Cx, Cz;
        public Celula(int cx, int cz) { Cx = cx; Cz = cz; }
        public override string ToString() => $"[{Cx},{Cz}]";
        public bool Equals(Celula o) => Cx == o.Cx && Cz == o.Cz;
    }

    public enum TipoSala { Portaria, Maquinas, Deposito, Escritorio, Vestiario, Galpao, Quadro, Tunel, Camara }

    public sealed class Sala
    {
        public TipoSala Tipo;
        public string Nome;
        public int X, Z, Larg, Alt;

        public Celula Centro => new Celula(X + Larg / 2, Z + Alt / 2);
        public bool Contem(Celula c) => c.Cx >= X && c.Cx < X + Larg && c.Cz >= Z && c.Cz < Z + Alt;
    }

    /// <summary>
    /// A planta do prédio: grade de células onde 1 é parede e 0 é chão.
    /// Cômodos são retângulos escavados de um bloco maciço e corredores ligam
    /// os centros. A busca em largura mora aqui porque é ela que prova que a
    /// planta é jogável — item inalcançável trava a partida sem avisar.
    /// </summary>
    public sealed class Predio
    {
        public const float Celula = 3.0f;
        public const float PeDireito = 3.4f;
        public const float AlturaPasso = 0.4f;

        public int Largura { get; }
        public int Profundidade { get; }
        public IReadOnlyList<Sala> Salas => _salas;

        readonly byte[] _grade;
        readonly List<Sala> _salas = new();

        public Predio(int largura = 44, int profundidade = 36)
        {
            Largura = largura;
            Profundidade = profundidade;
            _grade = new byte[largura * profundidade];
        }

        public bool NaGrade(int cx, int cz) => cx >= 0 && cz >= 0 && cx < Largura && cz < Profundidade;
        public bool EhParede(int cx, int cz) => !NaGrade(cx, cz) || _grade[cz * Largura + cx] == 1;
        public bool EhParede(Celula c) => EhParede(c.Cx, c.Cz);

        public P2 ParaMundo(Celula c) =>
            new P2((c.Cx - Largura / 2f + .5f) * Celula, (c.Cz - Profundidade / 2f + .5f) * Celula);

        public Celula ParaCelula(P2 p) =>
            new Celula((int)MathF.Floor(p.X / Celula + Largura / 2f),
                       (int)MathF.Floor(p.Z / Celula + Profundidade / 2f));

        // ---------------------------------------------------------- construção

        public void Construir()
        {
            Array.Fill(_grade, (byte)1);
            _salas.Clear();

            void Add(TipoSala tipo, string nome, int x, int z, int w, int h)
                => _salas.Add(new Sala { Tipo = tipo, Nome = nome, X = x, Z = z, Larg = w, Alt = h });

            Add(TipoSala.Portaria,   "portaria",        2,  29, 9,  5);
            Add(TipoSala.Maquinas,   "casa de bombas",  16, 28, 10, 6);
            Add(TipoSala.Deposito,   "depósito",        29, 27, 12, 7);
            Add(TipoSala.Escritorio, "escritório",      32, 17, 9,  7);
            Add(TipoSala.Vestiario,  "vestiário",       19, 17, 9,  7);
            Add(TipoSala.Galpao,     "galpão",          4,  16, 12, 8);
            Add(TipoSala.Quadro,     "quadro elétrico", 17, 5,  12, 7);
            Add(TipoSala.Tunel,      "túnel norte",     32, 5,  8,  6);
            Add(TipoSala.Camara,     "câmara fria",     3,  4,  10, 7);

            foreach (var s in _salas) Escavar(s.X, s.Z, s.Larg, s.Alt);

            // Ligações com ciclos de propósito: numa perseguição o jogador precisa
            // de rota alternativa, senão todo encontro vira beco sem saída.
            var ligacoes = new (TipoSala, TipoSala)[]
            {
                (TipoSala.Portaria, TipoSala.Maquinas), (TipoSala.Maquinas, TipoSala.Deposito),
                (TipoSala.Deposito, TipoSala.Escritorio), (TipoSala.Escritorio, TipoSala.Tunel),
                (TipoSala.Tunel, TipoSala.Quadro), (TipoSala.Quadro, TipoSala.Camara),
                (TipoSala.Camara, TipoSala.Galpao), (TipoSala.Galpao, TipoSala.Portaria),
                (TipoSala.Vestiario, TipoSala.Maquinas), (TipoSala.Vestiario, TipoSala.Quadro),
                (TipoSala.Galpao, TipoSala.Vestiario), (TipoSala.Deposito, TipoSala.Tunel)
            };
            foreach (var (a, b) in ligacoes)
                Corredor(Sala(a).Centro, Sala(b).Centro, 3);

            // moldura sólida: ninguém escapa pela borda
            for (int i = 0; i < Largura; i++) { _grade[i] = 1; _grade[(Profundidade - 1) * Largura + i] = 1; }
            for (int j = 0; j < Profundidade; j++) { _grade[j * Largura] = 1; _grade[j * Largura + Largura - 1] = 1; }
        }

        public Sala Sala(TipoSala tipo) => _salas.Find(s => s.Tipo == tipo);

        void Escavar(int x, int z, int w, int h)
        {
            for (int cz = z; cz < z + h; cz++)
                for (int cx = x; cx < x + w; cx++)
                    if (NaGrade(cx, cz)) _grade[cz * Largura + cx] = 0;
        }

        void Corredor(Celula a, Celula b, int larg)
        {
            int r = larg / 2;
            void Horizontal(int x0, int x1, int z)
            {
                for (int x = Math.Min(x0, x1); x <= Math.Max(x0, x1); x++)
                    for (int d = -r; d <= r; d++)
                        if (NaGrade(x, z + d)) _grade[(z + d) * Largura + x] = 0;
            }
            void Vertical(int z0, int z1, int x)
            {
                for (int z = Math.Min(z0, z1); z <= Math.Max(z0, z1); z++)
                    for (int d = -r; d <= r; d++)
                        if (NaGrade(x + d, z)) _grade[z * Largura + (x + d)] = 0;
            }
            // cotovelo alternado para a planta não ficar toda com o mesmo formato
            if ((a.Cx + a.Cz) % 2 == 0) { Horizontal(a.Cx, b.Cx, a.Cz); Vertical(a.Cz, b.Cz, b.Cx); }
            else { Vertical(a.Cz, b.Cz, a.Cx); Horizontal(a.Cx, b.Cx, b.Cz); }
        }

        public int CelulasLivres()
        {
            int n = 0;
            for (int i = 0; i < _grade.Length; i++) if (_grade[i] == 0) n++;
            return n;
        }

        // ---------------------------------------------------------- navegação

        /// <summary>Caminho célula a célula, ou null se não houver. É busca em largura: a grade é pequena e o custo é uniforme, então A* não pagaria a complexidade.</summary>
        public List<Celula> Caminho(Celula origem, Celula destino)
        {
            if (EhParede(origem) || EhParede(destino)) return null;

            var veio = new int[Largura * Profundidade];
            Array.Fill(veio, -1);

            int inicio = origem.Cz * Largura + origem.Cx;
            int alvo = destino.Cz * Largura + destino.Cx;
            var fila = new List<int> { inicio };
            veio[inicio] = inicio;

            for (int i = 0; i < fila.Count; i++)
            {
                int at = fila[i];
                if (at == alvo)
                {
                    var caminho = new List<Celula>();
                    int n = at;
                    while (n != veio[n]) { caminho.Add(new Celula(n % Largura, n / Largura)); n = veio[n]; }
                    caminho.Add(origem);
                    caminho.Reverse();
                    return caminho;
                }

                int cx = at % Largura, cz = at / Largura;
                Span<(int, int)> vizinhos = stackalloc (int, int)[]
                    { (cx + 1, cz), (cx - 1, cz), (cx, cz + 1), (cx, cz - 1) };

                foreach (var (nx, nz) in vizinhos)
                {
                    if (EhParede(nx, nz)) continue;
                    int k = nz * Largura + nx;
                    if (veio[k] != -1) continue;
                    veio[k] = at;
                    fila.Add(k);
                }
            }
            return null;
        }

        public bool Alcancavel(Celula a, Celula b) => Caminho(a, b) != null;

        /// <summary>Linha de visão andando pela grade. Sem isto a criatura enxerga através de parede.</summary>
        public bool Visivel(P2 a, P2 b)
        {
            var delta = b - a;
            float dist = delta.Comprimento;
            if (dist < .001f) return true;

            int passos = (int)MathF.Ceiling(dist / (Celula * 0.4f));
            for (int i = 1; i < passos; i++)
            {
                float t = i / (float)passos;
                if (EhParede(ParaCelula(a + delta * t))) return false;
            }
            return true;
        }

        /// <summary>Empurra um círculo para fora das paredes. Só checa as 9 células em volta.</summary>
        public P2 EmpurrarFora(P2 p, float raio)
        {
            var c = ParaCelula(p);
            for (int dz = -1; dz <= 1; dz++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = c.Cx + dx, nz = c.Cz + dz;
                    if (!EhParede(nx, nz)) continue;

                    var centro = ParaMundo(new Celula(nx, nz));
                    float minx = centro.X - Celula / 2, maxx = centro.X + Celula / 2;
                    float minz = centro.Z - Celula / 2, maxz = centro.Z + Celula / 2;

                    float px = Math.Clamp(p.X, minx, maxx);
                    float pz = Math.Clamp(p.Z, minz, maxz);
                    float ddx = p.X - px, ddz = p.Z - pz;
                    float d2 = ddx * ddx + ddz * ddz;
                    if (d2 >= raio * raio) continue;

                    float d = MathF.Sqrt(d2);
                    if (d < 1e-5f)
                    {
                        float ex = Math.Min(p.X - minx, maxx - p.X);
                        float ez = Math.Min(p.Z - minz, maxz - p.Z);
                        if (ex < ez) p.X += p.X < centro.X ? -(ex + raio) : (ex + raio);
                        else p.Z += p.Z < centro.Z ? -(ez + raio) : (ez + raio);
                    }
                    else
                    {
                        float f = (raio - d) / d;
                        p.X += ddx * f;
                        p.Z += ddz * f;
                    }
                }
            return p;
        }

        /// <summary>Junta corridas horizontais de parede numa caixa só, para o renderizador não instanciar mil cubos.</summary>
        public List<(P2 centro, float largura)> BlocosDeParede()
        {
            var blocos = new List<(P2, float)>();
            var usado = new bool[Largura * Profundidade];

            for (int cz = 0; cz < Profundidade; cz++)
                for (int cx = 0; cx < Largura; cx++)
                {
                    if (_grade[cz * Largura + cx] != 1 || usado[cz * Largura + cx]) continue;

                    int fim = cx;
                    while (fim + 1 < Largura && _grade[cz * Largura + fim + 1] == 1 && !usado[cz * Largura + fim + 1]) fim++;
                    for (int k = cx; k <= fim; k++) usado[cz * Largura + k] = true;

                    float larg = (fim - cx + 1) * Celula;
                    var m = ParaMundo(new Celula(cx, cz));
                    blocos.Add((new P2(m.X + (larg - Celula) / 2, m.Z), larg));
                }
            return blocos;
        }

        /// <summary>Em que cômodo está este ponto, ou null se for corredor. Serve para o jogador se situar.</summary>
        public Sala SalaEm(P2 ponto)
        {
            var c = ParaCelula(ponto);
            foreach (var s in _salas) if (s.Contem(c)) return s;
            return null;
        }

        /// <summary>Uma célula livre dentro da sala, ou o centro se a sala estiver cheia.</summary>
        public Celula PontoLivre(Sala s, Random rng, int margem = 1)
        {
            for (int t = 0; t < 60; t++)
            {
                int cx = rng.Next(s.X + margem, s.X + s.Larg - margem);
                int cz = rng.Next(s.Z + margem, s.Z + s.Alt - margem);
                if (!EhParede(cx, cz)) return new Celula(cx, cz);
            }
            return s.Centro;
        }
    }
}

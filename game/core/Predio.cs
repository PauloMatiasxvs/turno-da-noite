using System;
using System.Collections.Generic;

namespace TurnoDaNoite.Core
{
    /// <summary>Ponto no plano de um andar. A altura vem do andar, não daqui.</summary>
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

    /// <summary>
    /// Célula da grade. Carrega o andar junto porque, com o prédio em dois
    /// pisos, "célula 12,7" deixou de identificar um lugar: existe uma no
    /// térreo e outra em cima, e confundir as duas põe a criatura caçando você
    /// pelo andar errado.
    /// </summary>
    public struct Celula
    {
        public int Cx, Cz, Andar;
        public Celula(int cx, int cz, int andar = 0) { Cx = cx; Cz = cz; Andar = andar; }
        public override string ToString() => $"[{Cx},{Cz}@{Andar}]";
        public bool Equals(Celula o) => Cx == o.Cx && Cz == o.Cz && Andar == o.Andar;
    }

    /// <summary>
    /// Só os tipos que mudam alguma regra. Os outros cômodos são
    /// <see cref="TipoSala.Comum"/> e se distinguem pelo nome — inventar um
    /// enum por cômodo obrigaria a mexer em código toda vez que a planta
    /// ganhasse uma sala nova, e ela ganha muitas.
    /// </summary>
    public enum TipoSala { Comum, Portaria, Quadro, Camara, Patio, Escada }

    public sealed class Sala
    {
        public TipoSala Tipo;
        public string Nome;
        public int X, Z, Larg, Alt, Andar;

        public Celula Centro => new Celula(X + Larg / 2, Z + Alt / 2, Andar);
        public bool Contem(Celula c) =>
            c.Andar == Andar && c.Cx >= X && c.Cx < X + Larg && c.Cz >= Z && c.Cz < Z + Alt;

        public int Area => Larg * Alt;
    }

    /// <summary>Ligação entre dois andares, na mesma coluna da grade.</summary>
    public sealed class Escada
    {
        public int Cx, Cz;
        /// <summary>Andar de baixo. A escada liga este com o de cima.</summary>
        public int De;
        public int Para => De + 1;
    }

    /// <summary>
    /// A planta do prédio: uma grade por andar, onde 1 é parede e 0 é chão.
    ///
    /// Os cômodos saem de uma divisão recursiva do retângulo do andar — corta
    /// no meio, corta de novo, até cada pedaço ter o tamanho de uma sala — e
    /// cada corte vira um corredor. Foi assim, e não com dez retângulos
    /// escritos à mão, porque à mão as salas saíam do tamanho de um campo de
    /// futebol e os corredores com nove metros de largura: o prédio inteiro
    /// lia como um galpão só, sem cômodo nenhum.
    ///
    /// A busca em largura mora aqui porque é ela que prova que a planta é
    /// jogável — item inalcançável trava a partida sem avisar.
    /// </summary>
    public sealed class Predio
    {
        public const float Celula = 3.0f;
        public const float PeDireito = 3.4f;
        public const float EspessuraLaje = 0.45f;
        public const float AlturaPasso = 0.4f;

        /// <summary>Onde fica o vão da porta de entrada, em células.</summary>
        public const int PortaX = 5;
        public const int PortaLargura = 3;

        /// <summary>
        /// Tamanho de uma sala, em células — três células são nove metros.
        ///
        /// O mínimo baixo não é para fazer salas minúsculas: é o que permite
        /// CORTAR. Um pedaço só se divide se couberem dois mínimos nele, então
        /// mínimo alto deixa pedaços grandes indivisíveis, e foi assim que
        /// apareceu uma "oficina" de vinte e sete metros de lado.
        /// </summary>
        const int SalaMinima = 3;
        const int SalaMaxima = 7;

        public int Largura { get; }
        public int Profundidade { get; }
        public int Andares { get; }

        /// <summary>Linha da grade onde fica a parede da frente, com o vão da porta.</summary>
        public int PortaZ { get; }

        public IReadOnlyList<Sala> Salas => _salas;
        public IReadOnlyList<Escada> Escadas => _escadas;

        readonly byte[] _grade;
        readonly List<Sala> _salas = new();
        readonly List<Escada> _escadas = new();
        readonly Random _rng;

        /// <summary>
        /// As últimas fileiras do térreo são o pátio externo, onde o jogador
        /// começa. Entrar no prédio é parte do jogo, então o pátio precisa
        /// existir na planta e não ser só cenário.
        /// </summary>
        const int FileirasDoPatio = 5;

        /// <summary>
        /// 40 por 34 células são 120 por 102 metros por andar, e dão mais ou
        /// menos vinte e cinco cômodos em cada um. Já experimentei 52 por 46:
        /// saíram noventa e uma salas e duzentos e quarenta e oito móveis para
        /// cinco fusíveis. Prédio daquele tamanho não é ambicioso, é garimpo.
        /// </summary>
        public Predio(int semente = 0, int largura = 40, int profundidade = 34, int andares = 2)
        {
            Largura = largura;
            Profundidade = profundidade;
            Andares = Math.Max(1, andares);
            PortaZ = profundidade - FileirasDoPatio - 1;
            _grade = new byte[largura * profundidade * Andares];
            _rng = new Random(semente == 0 ? Environment.TickCount : semente);
        }

        int Indice(int cx, int cz, int andar) => (andar * Profundidade + cz) * Largura + cx;

        public bool NaGrade(int cx, int cz, int andar = 0) =>
            cx >= 0 && cz >= 0 && andar >= 0 && cx < Largura && cz < Profundidade && andar < Andares;

        public bool EhParede(int cx, int cz, int andar = 0) =>
            !NaGrade(cx, cz, andar) || _grade[Indice(cx, cz, andar)] == 1;

        public bool EhParede(Celula c) => EhParede(c.Cx, c.Cz, c.Andar);

        /// <summary>Altura do piso deste andar, em metros.</summary>
        public static float AlturaDoAndar(int andar) => andar * (PeDireito + EspessuraLaje);

        public P2 ParaMundo(Celula c) =>
            new P2((c.Cx - Largura / 2f + .5f) * Celula, (c.Cz - Profundidade / 2f + .5f) * Celula);

        public Celula ParaCelula(P2 p, int andar = 0) =>
            new Celula((int)MathF.Floor(p.X / Celula + Largura / 2f),
                       (int)MathF.Floor(p.Z / Celula + Profundidade / 2f), andar);

        // ---------------------------------------------------------- construção

        /// <summary>Nomes de cômodo de prédio industrial desativado, sorteados sem repetir.</summary>
        static readonly string[] NomesPossiveis =
        {
            "almoxarifado", "refeitório", "vestiário", "oficina", "sala de bombas",
            "arquivo morto", "expedição", "caldeiras", "sala de controle", "depósito",
            "garagem", "lavanderia", "enfermaria", "sala dos motores", "estoque frio",
            "ala administrativa", "sala de juntas", "copa", "compressores", "triagem",
            "carga e descarga", "manutenção", "central telefônica", "laboratório",
            "sala de exaustores", "dormitório", "rouparia", "necrotério", "subestação",
            "sala de painéis", "poço de ventilação", "câmara de reagentes"
        };

        public void Construir()
        {
            Array.Fill(_grade, (byte)1);
            _salas.Clear();
            _escadas.Clear();

            // A lista de nomes acaba antes das salas num prédio deste tamanho.
            // Quando acaba, a segunda volta ganha número — "depósito 2" ainda
            // situa o jogador, e "sala sem placa" repetida seis vezes não.
            var nomes = new List<string>(NomesPossiveis);
            Embaralhar(nomes);
            int proximoNome = 0;
            string Nome()
            {
                int i = proximoNome++;
                int volta = i / nomes.Count;
                string baseNome = nomes[i % nomes.Count];
                return volta == 0 ? baseNome : $"{baseNome} {volta + 1}";
            }

            for (int andar = 0; andar < Andares; andar++)
            {
                // O pátio é ao ar livre, então o prédio para antes dele — em
                // TODOS os andares. Enquanto o de cima ia até o fim da grade,
                // havia laje e cômodo pairando sobre o quintal.
                int fundo = PortaZ;
                var folhas = new List<(int x, int z, int w, int h)>();
                Dividir(1, 1, Largura - 2, fundo - 1, folhas, 0);

                foreach (var f in folhas)
                {
                    // Recua uma célula de cada lado: é esse recuo que vira a
                    // parede entre um cômodo e o vizinho. Sem ele os cômodos
                    // encostam e a planta volta a ser um salão.
                    var s = new Sala
                    {
                        Tipo = TipoSala.Comum,
                        Nome = Nome(),
                        X = f.x + 1, Z = f.z + 1,
                        Larg = f.w - 2, Alt = f.h - 2,
                        Andar = andar
                    };
                    if (s.Larg < 2 || s.Alt < 2) continue;
                    _salas.Add(s);
                    Escavar(s.X, s.Z, s.Larg, s.Alt, andar);
                }

                LigarSalas(andar);
            }

            MarcarSalasEspeciais();
            AbrirPatioEPorta();
            FurarEscadas();
            Moldura();
        }

        /// <summary>
        /// Corta o retângulo ao meio enquanto ele for grande demais para ser um
        /// cômodo. Corta no lado mais comprido, com o ponto de corte sorteado
        /// no terço do meio — cortar sempre na metade exata produz um prédio
        /// simétrico, e simetria em planta de terror mata a desorientação.
        /// </summary>
        void Dividir(int x, int z, int w, int h, List<(int, int, int, int)> folhas, int profundidadeDoCorte)
        {
            // +2 porque duas células do pedaço viram parede no recuo
            int limite = SalaMaxima + 2;
            bool grandeDemais = w > limite || h > limite;
            bool podeCortar = w >= (SalaMinima + 2) * 2 || h >= (SalaMinima + 2) * 2;

            if (!grandeDemais || !podeCortar || profundidadeDoCorte > 8)
            {
                folhas.Add((x, z, w, h));
                return;
            }

            int minimo = SalaMinima + 2;

            bool naVertical = w > h;
            if (Math.Abs(w - h) < 3) naVertical = _rng.Next(2) == 0;

            // Se o eixo sorteado não couber, tenta o OUTRO antes de desistir.
            // Enquanto desistia direto, um pedaço de 11 por 9 que sorteasse o
            // corte horizontal virava sala inteira: nove metros por vinte e
            // sete, que é o galpão que este código existe para não produzir.
            if ((naVertical ? w : h) < minimo * 2) naVertical = !naVertical;
            int comprimento = naVertical ? w : h;
            if (comprimento < minimo * 2) { folhas.Add((x, z, w, h)); return; }

            int corte = _rng.Next(minimo, comprimento - minimo + 1);

            if (naVertical)
            {
                Dividir(x, z, corte, h, folhas, profundidadeDoCorte + 1);
                Dividir(x + corte, z, w - corte, h, folhas, profundidadeDoCorte + 1);
            }
            else
            {
                Dividir(x, z, w, corte, folhas, profundidadeDoCorte + 1);
                Dividir(x, z + corte, w, h - corte, folhas, profundidadeDoCorte + 1);
            }
        }

        /// <summary>
        /// Abre corredores entre os cômodos do andar. Liga cada sala à mais
        /// próxima ainda desligada — o que garante que dá para chegar em todas
        /// — e depois abre alguns atalhos a mais, de propósito: numa
        /// perseguição o jogador precisa de rota alternativa, senão todo
        /// encontro vira beco sem saída.
        /// </summary>
        void LigarSalas(int andar)
        {
            var doAndar = _salas.FindAll(s => s.Andar == andar);
            if (doAndar.Count < 2) return;

            var ligadas = new List<Sala> { doAndar[0] };
            var soltas = new List<Sala>(doAndar.GetRange(1, doAndar.Count - 1));

            while (soltas.Count > 0)
            {
                Sala melhorA = null, melhorB = null;
                float menor = float.MaxValue;

                foreach (var a in ligadas)
                    foreach (var b in soltas)
                    {
                        float d = DistanciaDeCentros(a, b);
                        if (d >= menor) continue;
                        menor = d; melhorA = a; melhorB = b;
                    }

                Corredor(melhorA.Centro, melhorB.Centro, andar);
                ligadas.Add(melhorB);
                soltas.Remove(melhorB);
            }

            int atalhos = Math.Max(2, doAndar.Count / 3);
            for (int i = 0; i < atalhos; i++)
            {
                var a = doAndar[_rng.Next(doAndar.Count)];
                var b = doAndar[_rng.Next(doAndar.Count)];
                if (a != b) Corredor(a.Centro, b.Centro, andar);
            }
        }

        static float DistanciaDeCentros(Sala a, Sala b)
        {
            float dx = (a.X + a.Larg / 2f) - (b.X + b.Larg / 2f);
            float dz = (a.Z + a.Alt / 2f) - (b.Z + b.Alt / 2f);
            return MathF.Sqrt(dx * dx + dz * dz);
        }

        void Escavar(int x, int z, int w, int h, int andar)
        {
            for (int cz = z; cz < z + h; cz++)
                for (int cx = x; cx < x + w; cx++)
                    if (NaGrade(cx, cz, andar)) _grade[Indice(cx, cz, andar)] = 0;
        }

        /// <summary>
        /// Corredor de UMA célula, ou seja, três metros. Antes eram três
        /// células — nove metros de largura — e era esse número, mais que
        /// qualquer outra coisa, que fazia o prédio parecer não ter cômodos:
        /// um corredor de nove metros não é corredor, é sala.
        /// </summary>
        void Corredor(Celula a, Celula b, int andar)
        {
            void Horizontal(int x0, int x1, int z)
            {
                for (int x = Math.Min(x0, x1); x <= Math.Max(x0, x1); x++)
                    if (NaGrade(x, z, andar)) _grade[Indice(x, z, andar)] = 0;
            }
            void Vertical(int z0, int z1, int x)
            {
                for (int z = Math.Min(z0, z1); z <= Math.Max(z0, z1); z++)
                    if (NaGrade(x, z, andar)) _grade[Indice(x, z, andar)] = 0;
            }

            // cotovelo alternado para a planta não ficar toda com o mesmo formato
            if ((a.Cx + a.Cz) % 2 == 0) { Horizontal(a.Cx, b.Cx, a.Cz); Vertical(a.Cz, b.Cz, b.Cx); }
            else { Vertical(a.Cz, b.Cz, a.Cx); Horizontal(a.Cx, b.Cx, b.Cz); }
        }

        /// <summary>
        /// Escolhe quem é a portaria, o quadro elétrico e a toca dela.
        ///
        /// O quadro vai para o andar de cima sempre que houver um: obrigar a
        /// subir com os fusíveis na mão é o que faz a escada valer alguma
        /// coisa, em vez de ser um enfeite que você atravessa uma vez.
        /// </summary>
        void MarcarSalasEspeciais()
        {
            var terreo = _salas.FindAll(s => s.Andar == 0);
            if (terreo.Count == 0) return;

            // portaria: a sala do térreo mais perto do vão da porta
            Sala portaria = null;
            float menor = float.MaxValue;
            foreach (var s in terreo)
            {
                float dx = (s.X + s.Larg / 2f) - (PortaX + PortaLargura / 2f);
                float dz = (s.Z + s.Alt / 2f) - PortaZ;
                float d = dx * dx + dz * dz;
                if (d >= menor) continue;
                menor = d; portaria = s;
            }
            if (portaria != null) { portaria.Tipo = TipoSala.Portaria; portaria.Nome = "portaria"; }

            // quadro elétrico: lá em cima, o mais longe possível da portaria
            var candidatasQuadro = _salas.FindAll(s => s.Tipo == TipoSala.Comum &&
                                                       s.Andar == Math.Min(1, Andares - 1));
            if (candidatasQuadro.Count == 0) candidatasQuadro = _salas.FindAll(s => s.Tipo == TipoSala.Comum);
            var quadro = MaisLongeDe(candidatasQuadro, portaria);
            if (quadro != null) { quadro.Tipo = TipoSala.Quadro; quadro.Nome = "sala do quadro elétrico"; }

            // a toca dela: longe da portaria também, e nunca a mesma do quadro
            var candidatasCamara = _salas.FindAll(s => s.Tipo == TipoSala.Comum);
            var camara = MaisLongeDe(candidatasCamara, portaria);
            if (camara != null) { camara.Tipo = TipoSala.Camara; camara.Nome = "câmara fria"; }
        }

        static Sala MaisLongeDe(List<Sala> candidatas, Sala referencia)
        {
            Sala melhor = null;
            float maior = -1;
            foreach (var s in candidatas)
            {
                float d = referencia == null ? s.Area : DistanciaDeCentros(s, referencia)
                                                        + Math.Abs(s.Andar - referencia.Andar) * 20f;
                if (d <= maior) continue;
                maior = d; melhor = s;
            }
            return melhor;
        }

        void AbrirPatioEPorta()
        {
            var patio = new Sala
            {
                Tipo = TipoSala.Patio,
                Nome = "pátio externo",
                X = PortaX - 2, Z = PortaZ + 1,
                Larg = PortaLargura + 4, Alt = FileirasDoPatio - 1,
                Andar = 0
            };
            _salas.Add(patio);
            Escavar(patio.X, patio.Z, patio.Larg, patio.Alt, 0);

            // o vão da porta, e um pedaço de corredor que garante que ele dá em
            // algum lugar: sem isso a entrada pode abrir contra uma parede
            for (int cx = PortaX; cx < PortaX + PortaLargura; cx++)
                if (NaGrade(cx, PortaZ, 0)) _grade[Indice(cx, PortaZ, 0)] = 0;

            var portaria = _salas.Find(s => s.Tipo == TipoSala.Portaria);
            if (portaria != null)
                Corredor(new Celula(PortaX + 1, PortaZ, 0), portaria.Centro, 0);
        }

        /// <summary>
        /// Abre os poços de escada. Duas de propósito: com uma só, quem
        /// conhece o mapa espera você no único caminho, e o andar de cima
        /// vira uma ratoeira sem saída.
        /// </summary>
        void FurarEscadas()
        {
            for (int andar = 0; andar + 1 < Andares; andar++)
            {
                int postas = 0;
                for (int tentativa = 0; tentativa < 4000 && postas < 2; tentativa++)
                {
                    int cx = _rng.Next(2, Largura - 2);
                    int cz = _rng.Next(2, (andar == 0 ? PortaZ : Profundidade - 2) - 2);

                    if (EhParede(cx, cz, andar) || EhParede(cx, cz, andar + 1)) continue;
                    if (DentroDoPatio(cx, cz)) continue;

                    // longe uma da outra, senão as duas viram a mesma escada
                    bool pertoDeOutra = false;
                    foreach (var e in _escadas)
                        if (Math.Abs(e.Cx - cx) + Math.Abs(e.Cz - cz) < 14) pertoDeOutra = true;
                    if (pertoDeOutra) continue;

                    _escadas.Add(new Escada { Cx = cx, Cz = cz, De = andar });
                    postas++;
                }
            }
        }

        bool DentroDoPatio(int cx, int cz)
        {
            var p = _salas.Find(s => s.Tipo == TipoSala.Patio);
            return p != null && p.Contem(new Celula(cx, cz, 0));
        }

        void Moldura()
        {
            for (int andar = 0; andar < Andares; andar++)
            {
                for (int i = 0; i < Largura; i++)
                {
                    _grade[Indice(i, 0, andar)] = 1;
                    _grade[Indice(i, Profundidade - 1, andar)] = 1;
                }
                for (int j = 0; j < Profundidade; j++)
                {
                    _grade[Indice(0, j, andar)] = 1;
                    _grade[Indice(Largura - 1, j, andar)] = 1;
                }
            }
        }

        void Embaralhar<T>(List<T> lista)
        {
            for (int i = lista.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (lista[i], lista[j]) = (lista[j], lista[i]);
            }
        }

        // ---------------------------------------------------------- escadas

        /// <summary>A escada nesta célula, ou null. O andar importa: a mesma coluna tem uma em cada piso.</summary>
        public Escada EscadaEm(Celula c)
        {
            foreach (var e in _escadas)
            {
                if (e.Cx != c.Cx || e.Cz != c.Cz) continue;
                if (e.De == c.Andar || e.Para == c.Andar) return e;
            }
            return null;
        }

        /// <summary>Para onde esta escada leva, saindo do andar informado.</summary>
        public int OutroAndar(Escada e, int andarAtual) => andarAtual == e.De ? e.Para : e.De;

        public int CelulasLivres()
        {
            int n = 0;
            for (int i = 0; i < _grade.Length; i++) if (_grade[i] == 0) n++;
            return n;
        }

        // ---------------------------------------------------------- navegação

        /// <summary>
        /// Caminho célula a célula, atravessando andares pelas escadas, ou null
        /// se não houver. É busca em largura: a grade é pequena e o custo é
        /// uniforme, então A* não pagaria a complexidade.
        /// </summary>
        public List<Celula> Caminho(Celula origem, Celula destino)
        {
            if (EhParede(origem) || EhParede(destino)) return null;

            int total = Largura * Profundidade * Andares;
            var veio = new int[total];
            Array.Fill(veio, -1);

            int inicio = Indice(origem.Cx, origem.Cz, origem.Andar);
            int alvo = Indice(destino.Cx, destino.Cz, destino.Andar);
            var fila = new List<int> { inicio };
            veio[inicio] = inicio;

            for (int i = 0; i < fila.Count; i++)
            {
                int at = fila[i];
                if (at == alvo) return Refazer(veio, at, origem);

                int porAndar = Largura * Profundidade;
                int andar = at / porAndar;
                int resto = at % porAndar;
                int cx = resto % Largura, cz = resto / Largura;

                Visitar(cx + 1, cz, andar);
                Visitar(cx - 1, cz, andar);
                Visitar(cx, cz + 1, andar);
                Visitar(cx, cz - 1, andar);

                // a escada é uma aresta a mais, ligando a mesma coluna nos dois pisos
                var esc = EscadaEm(new Celula(cx, cz, andar));
                if (esc != null) Visitar(cx, cz, OutroAndar(esc, andar));

                void Visitar(int nx, int nz, int na)
                {
                    if (EhParede(nx, nz, na)) return;
                    int k = Indice(nx, nz, na);
                    if (veio[k] != -1) return;
                    veio[k] = at;
                    fila.Add(k);
                }
            }
            return null;
        }

        List<Celula> Refazer(int[] veio, int fim, Celula origem)
        {
            int porAndar = Largura * Profundidade;
            var caminho = new List<Celula>();
            int n = fim;
            while (n != veio[n])
            {
                int a = n / porAndar, r = n % porAndar;
                caminho.Add(new Celula(r % Largura, r / Largura, a));
                n = veio[n];
            }
            caminho.Add(origem);
            caminho.Reverse();
            return caminho;
        }

        public bool Alcancavel(Celula a, Celula b) => Caminho(a, b) != null;

        /// <summary>
        /// Linha de visão andando pela grade, dentro de um andar só. Laje é
        /// opaca: ninguém enxerga o piso de cima.
        /// </summary>
        public bool Visivel(P2 a, P2 b, int andar = 0)
        {
            var delta = b - a;
            float dist = delta.Comprimento;
            if (dist < .001f) return true;

            int passos = (int)MathF.Ceiling(dist / (Celula * 0.4f));
            for (int i = 1; i < passos; i++)
            {
                float t = i / (float)passos;
                if (EhParede(ParaCelula(a + delta * t, andar))) return false;
            }
            return true;
        }

        /// <summary>Empurra um círculo para fora das paredes. Só checa as 9 células em volta.</summary>
        public P2 EmpurrarFora(P2 p, float raio, int andar = 0)
        {
            var c = ParaCelula(p, andar);
            for (int dz = -1; dz <= 1; dz++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = c.Cx + dx, nz = c.Cz + dz;
                    if (!EhParede(nx, nz, andar)) continue;

                    var centro = ParaMundo(new Celula(nx, nz, andar));
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
        public List<(P2 centro, float largura)> BlocosDeParede(int andar = 0)
        {
            var blocos = new List<(P2, float)>();
            var usado = new bool[Largura * Profundidade];

            for (int cz = 0; cz < Profundidade; cz++)
                for (int cx = 0; cx < Largura; cx++)
                {
                    if (_grade[Indice(cx, cz, andar)] != 1 || usado[cz * Largura + cx]) continue;

                    int fim = cx;
                    while (fim + 1 < Largura && _grade[Indice(fim + 1, cz, andar)] == 1
                           && !usado[cz * Largura + fim + 1]) fim++;
                    for (int k = cx; k <= fim; k++) usado[cz * Largura + k] = true;

                    float larg = (fim - cx + 1) * Celula;
                    var m = ParaMundo(new Celula(cx, cz, andar));
                    blocos.Add((new P2(m.X + (larg - Celula) / 2, m.Z), larg));
                }
            return blocos;
        }

        /// <summary>Em que cômodo está este ponto, ou null se for corredor. Serve para o jogador se situar.</summary>
        public Sala SalaEm(P2 ponto, int andar = 0)
        {
            var c = ParaCelula(ponto, andar);
            foreach (var s in _salas) if (s.Contem(c)) return s;
            return null;
        }

        public Sala Sala(TipoSala tipo) => _salas.Find(s => s.Tipo == tipo);

        /// <summary>Uma célula livre dentro da sala, ou o centro se a sala estiver cheia.</summary>
        public Celula PontoLivre(Sala s, Random rng, int margem = 1)
        {
            int m = Math.Min(margem, Math.Min(s.Larg, s.Alt) / 3);
            for (int t = 0; t < 60; t++)
            {
                int cx = rng.Next(s.X + m, Math.Max(s.X + m + 1, s.X + s.Larg - m));
                int cz = rng.Next(s.Z + m, Math.Max(s.Z + m + 1, s.Z + s.Alt - m));
                if (!EhParede(cx, cz, s.Andar)) return new Celula(cx, cz, s.Andar);
            }
            return s.Centro;
        }
    }
}

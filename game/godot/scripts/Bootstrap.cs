using System.Collections.Generic;
using Godot;
using TurnoDaNoite.Core;

namespace TurnoDaNoite.Jogo
{
    /// <summary>
    /// Ponte entre o Godot e a simulação. Lê teclado e mouse, entrega um Comando
    /// para a Partida e depois copia o estado resultante para os nós 3D.
    /// Nenhuma regra de jogo mora aqui: se algo parece errado, o bug está no
    /// núcleo — e lá existe um teste para escrever.
    /// </summary>
    public partial class Bootstrap : Node3D
    {
        Partida _partida;
        Camera3D _camera;
        SpotLight3D _lanterna;
        OmniLight3D _lampiao;
        Hud _hud;
        Node3D _raizItens, _raizArmarios;
        Node3D _criatura;
        Node3D _mao;
        MeshInstance3D _vidroDaLanterna;
        StandardMaterial3D _matVidro;
        Vector2 _balancoDaMao;
        AnimationPlayer _animCriatura;
        string _clipeAtual = "";
        /// <summary>Se o modelo do artista olha para +Z em vez de -Z, isto vira Pi.</summary>
        const float GiroDoModelo = Mathf.Pi;

        /// <summary>
        /// Força do facho com a bateria cheia. Fica numa constante só porque já
        /// esteve em dois lugares: o valor calibrado aqui era 6,5 e a linha que
        /// roda todo quadro reescrevia 32 por cima, desfazendo a calibração e
        /// estourando o chão de branco a cada passo.
        /// </summary>
        const float EnergiaDaLanterna = 7.5f;

        readonly List<Node3D> _fusiveis = new();
        readonly List<Node3D> _baterias = new();

        /// <summary>
        /// Um recipiente na cena. Guardo a peça móvel separada porque abrir tem
        /// de ser visível de longe: sem isso você revista o mesmo gaveteiro três
        /// vezes sem notar, e o mapa vira um labirinto de móveis iguais.
        /// </summary>
        sealed class NoRecipiente
        {
            public Node3D Raiz, Tampa;
            public TipoRecipiente Tipo;
            public float Abertura;      // 0 fechado, 1 escancarado
        }
        readonly List<NoRecipiente> _recipientes = new();

        Mapa _mapa;
        Pausa _telaPausa;
        bool _mapaAberto;
        int _andarNoMapa;

        /// <summary>Um nó por andar do prédio, e um por andar para móveis e cenário.</summary>
        readonly List<Node3D> _andares = new();
        readonly Dictionary<int, Node3D> _raizPorAndar = new();
        int _andarDesenhado = -1;

        StandardMaterial3D _matParede, _matPiso, _matTeto, _matArmario,
                           _matQuadro, _matFusivel, _matBateria, _matCriatura, _matPortao, _matMapa;
        Node3D _mapaNo;

        float _giro, _inclinacao;
        bool _mouseCapturado;
        bool _pausado;
        bool _naAbertura;
        Menu _menu;
        bool _interagirPedido, _lanternaPedida;
        float _balanco;

        bool _autoTeste;
        int _quadrosDeTeste;

        // modo de captura: roda alguns segundos e salva um PNG, para dar para
        // conferir o visual sem precisar sentar e jogar
        bool _tirarFoto;
        int _quadrosDeFoto;
        // modo de diagnostico: fundo berrante e luz forte, para separar
        // "nao ha geometria" de "ha geometria sem luz"
        bool _diagnostico;
        bool _semSombra;
        bool _forcarLuz;
        bool _semNevoa;
        bool _semAmbiente;
        // poe a camera na frente do primeiro fusivel, para conferir o modelo de perto
        bool _verItem;
        // encosta no recipiente que guarda o primeiro fusivel e abre ele no meio
        // da captura: a segunda foto mostra o estado aberto, que e o que interessa
        bool _verRecipiente;
        // modos de captura do prédio de dois andares: sala do quadro lá em cima,
        // pé da escada, e a planta aberta na tela
        bool _verAndarDeCima, _verMapa, _verEscada;

        public override void _Ready()
        {
            foreach (string arg in OS.GetCmdlineUserArgs())
            {
                if (arg == "--selftest") _autoTeste = true;
                if (arg == "--screenshot") _tirarFoto = true;
                if (arg == "--diag") _diagnostico = true;
                if (arg == "--semsombra") _semSombra = true;
                if (arg == "--forcaluz") _forcarLuz = true;
                if (arg == "--semnevoa") _semNevoa = true;
                if (arg == "--semnormal") SemNormal = true;
                if (arg == "--semambiente") _semAmbiente = true;
                if (arg == "--verfusivel") _verItem = true;
                if (arg == "--verrecipiente") _verRecipiente = true;
                if (arg == "--vercima") _verAndarDeCima = true;
                if (arg == "--vermapa") _verMapa = true;
                if (arg == "--verescada") _verEscada = true;
            }

            Opcoes.Carregar();
            CriarMateriais();

            _partida = new Partida(_autoTeste ? 4242 : 0);
            if (!_partida.Comecar())
            {
                GD.PrintErr("não consegui gerar uma planta jogável");
                return;
            }

            MontarAmbiente();
            MontarPredio();
            MontarItens();

            _hud = new Hud();
            AddChild(_hud);

            _mapa = new Mapa();
            AddChild(_mapa);

            _telaPausa = new Pausa();
            _telaPausa.AoVoltar += () => { if (_pausado) AlternarPausa(); };
            _telaPausa.AoRecomecar += () => GetTree().ReloadCurrentScene();
            _telaPausa.AoSair += () => GetTree().Quit();
            AddChild(_telaPausa);

            // Abre no menu, com historia e opcoes. Sem isto a pessoa cai no escuro
            // sem saber o que fazer, que foi exatamente o que aconteceu.
            // no modo captura o menu tambem aparece: e preciso poder olhar para ele
            _naAbertura = !_autoTeste;
            if (_naAbertura)
            {
                _menu = new Menu();
                _menu.AoComecar += () => { _naAbertura = false; CapturarMouse(true); };
                AddChild(_menu);
            }

            CapturarMouse(!_autoTeste && !_naAbertura);
        }

        // ------------------------------------------------------------- cenário

        void CriarMateriais()
        {
            StandardMaterial3D Fosco(Color c, float aspereza = 0.85f) => new()
            {
                AlbedoColor = c,
                Roughness = aspereza,
                Metallic = 0.05f
            };
            StandardMaterial3D Brilho(Color c, float forca) => new()
            {
                AlbedoColor = c,
                EmissionEnabled = true,
                Emission = c,
                EmissionEnergyMultiplier = forca,
                Roughness = 0.5f
            };

            _matParede = Texturizado("parede", new Color(0.55f, 0.53f, 0.50f), 0.28f)
                         ?? Fosco(new Color(0.26f, 0.25f, 0.23f));
            _matPiso = Texturizado("piso", new Color(0.52f, 0.51f, 0.49f), 0.22f)
                       ?? Fosco(new Color(0.19f, 0.18f, 0.17f), 0.95f);
            _matTeto = Fosco(new Color(0.13f, 0.13f, 0.14f));
            _matArmario = Fosco(new Color(0.22f, 0.24f, 0.26f), 0.6f);
            _matQuadro = Fosco(new Color(0.30f, 0.26f, 0.18f), 0.7f);
            _matCriatura = Fosco(new Color(0.045f, 0.04f, 0.05f), 1f);
            _matFusivel = Brilho(new Color(1f, 0.72f, 0.20f), 1.8f);
            _matBateria = Brilho(new Color(0.35f, 0.9f, 0.55f), 1.4f);
            _matPortao = Fosco(new Color(0.24f, 0.11f, 0.11f), 0.8f);
            _matMapa = Brilho(new Color(0.86f, 0.78f, 0.55f), 0.7f);
        }

        /// <summary>
        /// Monta um material PBR a partir das texturas em assets/texturas, ou
        /// devolve null se elas não estiverem lá — aí quem chama usa cor chapada.
        ///
        /// Usa projeção triplanar de propósito: as paredes são caixas geradas em
        /// tamanhos diferentes e não têm coordenadas de textura. Triplanar projeta
        /// do espaço do mundo, então a textura fica no tamanho certo em qualquer
        /// parede sem ninguém precisar abrir UV no Blender.
        /// </summary>
        /// <summary>Desliga o mapa de normal, para testar se ele esta quebrando a luz.</summary>
        public static bool SemNormal;

        static StandardMaterial3D Texturizado(string nome, Color tinta, float escala)
        {
            string cor = $"res://assets/texturas/{nome}_cor.jpg";
            if (!ResourceLoader.Exists(cor)) return null;

            var mat = new StandardMaterial3D
            {
                AlbedoTexture = GD.Load<Texture2D>(cor),
                AlbedoColor = tinta,
                Uv1Triplanar = true,
                Uv1Scale = new Vector3(escala, escala, escala),
                Roughness = 1f,
                Metallic = 0f
            };

            string normal = "res://assets/texturas/" + nome + "_normal.jpg";
            if (!SemNormal && ResourceLoader.Exists(normal))
            {
                mat.NormalEnabled = true;
                mat.NormalTexture = GD.Load<Texture2D>(normal);
                mat.NormalScale = 1.2f;   // realça o relevo: no escuro é o que dá textura à parede
            }

            string aspereza = $"res://assets/texturas/{nome}_aspereza.jpg";
            if (ResourceLoader.Exists(aspereza))
            {
                mat.RoughnessTexture = GD.Load<Texture2D>(aspereza);
                mat.RoughnessTextureChannel = BaseMaterial3D.TextureChannel.Red;
            }

            return mat;
        }

        void MontarAmbiente()
        {
            var env = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color,
                BackgroundColor = new Color(0.004f, 0.005f, 0.009f),
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = new Color(0.10f, 0.11f, 0.15f),
                AmbientLightEnergy = 0.22f,       // quase nada, mas o suficiente para ler a silhueta
                FogEnabled = true,
                FogLightColor = new Color(0.03f, 0.032f, 0.045f),
                FogDensity = 0.008f,
                GlowEnabled = true,
                GlowIntensity = 0.35f,

                // Calibrado com captura de tela. A combinação anterior — ACES com
                // exposição 1 mais AdjustmentContrast/Saturation ligados — comia
                // praticamente toda a luz da lanterna: a cena ficava preta mesmo
                // com energia 500 no holofote. Exposição acima de 1 e ajustes
                // desligados devolvem a imagem.
                // 1,8 vinha do tempo em que a cena estava preta e era preciso
                // forçar. Com a lanterna calibrada isso passou a estourar tudo
                // que estivesse a menos de dois metros: um gaveteiro cinza-chumbo
                // aparecia branco na tela. 1,15 devolve o cinza sem escurecer o
                // fundo, porque em Godot o holofote quase não perde força com a
                // distância — quem estava errado era a exposição, não o alcance.
                TonemapMode = Godot.Environment.ToneMapper.Filmic,
                TonemapExposure = 1.15f,
                AdjustmentEnabled = false
            };
            if (_semNevoa) env.FogEnabled = false;
            if (_diagnostico)
            {
                env.BackgroundColor = new Color(1f, 0f, 1f);
                env.AmbientLightColor = new Color(1f, 1f, 1f);
                env.AmbientLightEnergy = 0.0f;   // so a lanterna ilumina
                env.FogEnabled = false;
            }
            if (!_semAmbiente) AddChild(new WorldEnvironment { Environment = env });

            _camera = new Camera3D { Fov = 74, Current = true, Near = 0.04f, Far = 200f };
            AddChild(_camera);

            // A lanterna é o jogo: cone estreito, quente, com sombra.
            _lanterna = new SpotLight3D
            {
                LightColor = new Color(1f, 0.93f, 0.78f),
                // Calibrado olhando captura de tela: com energia 9 a luz morria em
                // dois metros. Em Godot a queda do holofote e agressiva, entao o
                // valor util fica bem acima do que a intuicao sugere.
                // Recalibrado depois que o Environment parou de comer a luz: com 32
                // o facho virava um circulo branco estourado. Cone mais aberto e
                // com borda macia parece lanterna, e nao holofote de estadio.
                LightEnergy = EnergiaDaLanterna,
                SpotRange = 26f,
                SpotAngle = 43f,
                SpotAngleAttenuation = 2.4f,
                // queda mais macia: com 1.1 o chao a um metro estourava branco
                // enquanto a parede do fundo sumia. 0.75 espalha o facho.
                SpotAttenuation = 0.75f,
                ShadowEnabled = !_semSombra,
                ShadowBias = 0.06f,
                ShadowNormalBias = 2.0f
            };
            _camera.AddChild(_lanterna);

            MontarMaoComLanterna();

            // lampião fraco preso ao jogador, para o escuro total não virar tela preta
            _lampiao = new OmniLight3D
            {
                LightColor = new Color(0.35f, 0.40f, 0.55f),
                LightEnergy = 1.6f,
                OmniRange = 6f,
                ShadowEnabled = false
            };
            _camera.AddChild(_lampiao);
        }

        /// <summary>
        /// Braço e lanterna em primeira pessoa, presos à câmera.
        ///
        /// Duas armadilhas que este código evita de propósito:
        /// a lanterna fica ADIANTE do holofote, senão o próprio corpo dela
        /// bloqueia a luz e projeta uma sombra gigante na cena; e todas as
        /// peças têm sombra desligada, pela mesma razão.
        /// </summary>
        void MontarMaoComLanterna()
        {
            _mao = new Node3D { Name = "MaoComLanterna" };
            _camera.AddChild(_mao);
            _mao.Position = new Vector3(0.21f, -0.17f, -0.46f);

            // A mão inteira é uma peça só, montada em Modelos: dedos em volta do
            // tubo, cabeça cônica e manga. Antes eram dois cilindros soltos, e
            // o braço lia como um cano escuro atravessando o canto da tela.
            var mao = Modelos.MaoComLanterna();
            mao.Name = "Lanterna";
            _mao.AddChild(mao);

            // vidro da frente: acende junto com a luz e some quando ela apaga
            _vidroDaLanterna = mao.GetNode<MeshInstance3D>("Vidro");

            _matVidro = new StandardMaterial3D
            {
                AlbedoColor = new Color(1f, 0.93f, 0.78f),
                EmissionEnabled = true,
                Emission = new Color(1f, 0.93f, 0.78f),
                EmissionEnergyMultiplier = 1.4f
            };
            _vidroDaLanterna.MaterialOverride = _matVidro;

            // nada na mão projeta sombra: o holofote está atrás dela
            foreach (var n in TodosOsNos(_mao))
                if (n is GeometryInstance3D g)
                    g.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        }

        static System.Collections.Generic.IEnumerable<Node> TodosOsNos(Node raiz)
        {
            yield return raiz;
            foreach (var f in raiz.GetChildren())
                foreach (var n in TodosOsNos(f)) yield return n;
        }

        /// <summary>
        /// Monta os dois andares. Cada um vira um nó próprio, deslocado na
        /// altura, e o andar em que o jogador não está fica ESCONDIDO —
        /// não por performance, mas porque a laje é opaca de verdade: deixar
        /// os dois visíveis faria você enxergar móveis flutuando pelo teto.
        /// </summary>
        void MontarPredio()
        {
            var raiz = new Node3D { Name = "Predio" };
            AddChild(raiz);

            var predio = _partida.Predio;
            float lado = Mathf.Max(predio.Largura, predio.Profundidade) * Predio.Celula + 12f;

            for (int andar = 0; andar < predio.Andares; andar++)
            {
                var noDoAndar = new Node3D
                {
                    Name = $"Andar{andar}",
                    Position = new Vector3(0, Predio.AlturaDoAndar(andar), 0)
                };
                raiz.AddChild(noDoAndar);
                _andares.Add(noDoAndar);

                MontarUmAndar(noDoAndar, andar, lado);
            }

            MontarEscadas(raiz);
        }

        void MontarUmAndar(Node3D raiz, int andar, float lado)
        {
            var predio = _partida.Predio;

            var piso = Props.Criar(Peca.Piso, new Vector3(lado, 0.1f, lado), _matPiso);
            raiz.AddChild(piso);

            MontarTeto(raiz, andar, lado);

            foreach (var (centro, largura) in predio.BlocosDeParede(andar))
            {
                var parede = Props.Criar(Peca.Parede,
                    new Vector3(largura, Predio.PeDireito, Predio.Celula), _matParede);
                parede.Position = new Vector3(centro.X, 0, centro.Z);
                raiz.AddChild(parede);
            }

            // luminárias mortas por sala; uma em cada quatro ainda pisca
            int n = 0;
            foreach (var sala in predio.Salas)
            {
                if (sala.Andar != andar) continue;

                var m = predio.ParaMundo(sala.Centro);
                var lum = Props.Criar(Peca.Luminaria, new Vector3(1.2f, 0.12f, 0.3f), _matTeto);
                lum.Position = new Vector3(m.X, Predio.PeDireito - 0.2f, m.Z);
                raiz.AddChild(lum);

                if (n++ % 4 == 0)
                {
                    var luz = new OmniLight3D
                    {
                        LightColor = new Color(1f, 0.55f, 0.25f),
                        LightEnergy = 0.9f,
                        OmniRange = 9f,
                        ShadowEnabled = false,
                        Position = new Vector3(m.X, Predio.PeDireito - 0.35f, m.Z)
                    };
                    luz.AddChild(new Piscar());
                    raiz.AddChild(luz);
                }
            }
        }

        /// <summary>
        /// O teto de um andar, com BURACO em cima de cada escada.
        ///
        /// Vai em faixas, uma por fileira da grade, em vez de um plano só. A
        /// razão é o buraco: com o teto inteiriço, a escada subia e batia numa
        /// laje fechada — lia como escada de mentira encostada na parede, e não
        /// como passagem para o andar de cima. Uma faixa por fileira custa umas
        /// trinta e poucas malhas por andar, que é barato pelo que resolve.
        ///
        /// O teto cobre só o prédio: o pátio externo fica sob o céu, senão
        /// "entrar no prédio" não se distingue de andar num corredor.
        /// </summary>
        void MontarTeto(Node3D raiz, int andar, float lado)
        {
            var predio = _partida.Predio;

            for (int cz = 0; cz <= predio.PortaZ; cz++)
            {
                // onde este fileira tem poço de escada
                var buracos = new List<int>();
                foreach (var e in predio.Escadas)
                    if (e.Cz == cz && (e.De == andar || e.Para == andar)) buracos.Add(e.Cx);
                buracos.Sort();

                var m = predio.ParaMundo(new Celula(0, cz, andar));
                float z = m.Z;
                float x0 = -predio.Largura / 2f * Predio.Celula;

                int inicio = 0;
                foreach (int furo in buracos)
                {
                    if (furo > inicio) Faixa(raiz, x0, inicio, furo, z);
                    inicio = furo + 1;
                }
                if (inicio < predio.Largura) Faixa(raiz, x0, inicio, predio.Largura, z);
            }

            // uma borda de teto além da porta, para o beiral aparecer de fora
            var beiral = Props.Criar(Peca.Teto, new Vector3(lado, 0.1f, Predio.Celula), _matTeto);
            beiral.Position = new Vector3(0, Predio.PeDireito,
                (predio.PortaZ - predio.Profundidade / 2f + 1.5f) * Predio.Celula);
            beiral.RotateZ(Mathf.Pi);
            raiz.AddChild(beiral);
        }

        void Faixa(Node3D raiz, float x0, int de, int ate, float z)
        {
            float largura = (ate - de) * Predio.Celula;
            var no = Props.Criar(Peca.Teto, new Vector3(largura, 0.1f, Predio.Celula), _matTeto);
            no.Position = new Vector3(x0 + (de * Predio.Celula) + largura / 2, Predio.PeDireito, z);
            no.RotateZ(Mathf.Pi);   // vira a face para baixo
            raiz.AddChild(no);
        }

        /// <summary>
        /// Desenha os lances de escada. Ficam fora dos nós de andar porque
        /// atravessam os dois: escondendo com o andar, você chegaria em cima e
        /// veria o vão por onde subiu desaparecer atrás de você.
        /// </summary>
        void MontarEscadas(Node3D raiz)
        {
            foreach (var e in _partida.Predio.Escadas)
            {
                var m = _partida.Predio.ParaMundo(new Celula(e.Cx, e.Cz, e.De));
                var lance = Modelos.Escada(Predio.PeDireito + Predio.EspessuraLaje, Predio.Celula);
                lance.Position = new Vector3(m.X, Predio.AlturaDoAndar(e.De), m.Z);
                raiz.AddChild(lance);
            }
        }

        /// <summary>
        /// Mostra só o andar em que o jogador está. O de cima tem laje opaca,
        /// mas a escada é um buraco nela: sem esconder, dava para ver os móveis
        /// do outro piso pelo vão e, pior, o facho da lanterna iluminava lá.
        /// </summary>
        void MostrarApenasOAndar(int andar)
        {
            if (andar == _andarDesenhado) return;
            _andarDesenhado = andar;

            for (int i = 0; i < _andares.Count; i++) _andares[i].Visible = i == andar;
            if (_raizPorAndar.TryGetValue(0, out var baixo)) baixo.Visible = andar == 0;
            if (_raizPorAndar.TryGetValue(1, out var cima)) cima.Visible = andar == 1;
        }

        /// <summary>
        /// Onde pendurar uma coisa que está num andar. Tudo o que o jogo
        /// coloca no mundo passa por aqui: assim ninguém esquece de somar a
        /// altura do piso, que é o erro que faz o móvel do primeiro andar
        /// nascer enterrado no térreo.
        /// </summary>
        Node3D RaizDoAndar(int andar)
        {
            if (_raizPorAndar.TryGetValue(andar, out var pronta)) return pronta;

            var no = new Node3D
            {
                Name = $"Coisas{andar}",
                Position = new Vector3(0, Predio.AlturaDoAndar(andar), 0)
            };
            AddChild(no);
            _raizPorAndar[andar] = no;
            return no;
        }

        void MontarItens()
        {
            _raizItens = new Node3D { Name = "Itens" };
            _raizArmarios = new Node3D { Name = "Armarios" };
            AddChild(_raizItens);
            AddChild(_raizArmarios);

            foreach (var a in _partida.Armarios)
            {
                var no = Props.Criar(Peca.Armario, new Vector3(1.0f, 2.0f, 0.62f), _matArmario);
                no.Position = new Vector3(a.Pos.X, 0, a.Pos.Z);
                RaizDoAndar(a.Andar).AddChild(no);
            }

            var quadro = Props.Criar(Peca.QuadroEletrico, new Vector3(1.5f, 1.9f, 0.42f), _matQuadro);
            quadro.Position = new Vector3(_partida.Quadro.X, 0.35f, _partida.Quadro.Z);
            RaizDoAndar(_partida.QuadroAndar).AddChild(quadro);

            var portao = Props.Criar(Peca.Portao, new Vector3(2.4f, 3.2f, 0.3f), _matPortao);
            portao.Position = new Vector3(_partida.Portao.X, 0, _partida.Portao.Z);
            portao.Name = "Portao";
            RaizDoAndar(0).AddChild(portao);

            MontarRecipientes();
            MontarCenario();

            foreach (var f in _partida.Fusiveis)
            {
                var no = Props.Criar(Peca.Fusivel, new Vector3(0.2f, 0.34f, 0.2f), _matFusivel);
                no.Position = PosicaoDoItem(f);
                RaizDoAndar(f.Andar).AddChild(no);
                _fusiveis.Add(no);
            }
            foreach (var b in _partida.Baterias)
            {
                var no = Props.Criar(Peca.Bateria, new Vector3(0.16f, 0.26f, 0.16f), _matBateria);
                no.Position = PosicaoDoItem(b);
                RaizDoAndar(b.Andar).AddChild(no);
                _baterias.Add(no);
            }

            if (_partida.MapaItem != null)
            {
                _mapaNo = Props.Criar(Peca.Planta, new Vector3(0.3f, 0.02f, 0.22f), _matMapa);
                _mapaNo.Position = PosicaoDoItem(_partida.MapaItem);
                RaizDoAndar(_partida.MapaItem.Andar).AddChild(_mapaNo);
            }

            _criatura = Props.Criar(Peca.Criatura, new Vector3(0.7f, 2.35f, 0.5f), _matCriatura);
            AddChild(_criatura);
            _animCriatura = AcharAnimador(_criatura);
        }

        static Peca PecaDo(TipoRecipiente t) => t switch
        {
            TipoRecipiente.CaixaDeFerramentas => Peca.CaixaFerramentas,
            TipoRecipiente.Gaveteiro => Peca.Gaveteiro,
            _ => Peca.Prateleira
        };

        /// <summary>
        /// Onde o item aparece depois que o recipiente abre, no espaço do móvel.
        /// São alturas de prateleira e de gaveta, não números redondos: item
        /// pairando entre dois níveis denuncia na hora que ele não está apoiado
        /// em nada. O Z tira ele de dentro do corpo do móvel e põe na gaveta
        /// aberta, que é o lugar de onde você o pegaria.
        /// </summary>
        static Vector3 LugarNoRecipiente(TipoRecipiente t) => t switch
        {
            TipoRecipiente.CaixaDeFerramentas => new Vector3(0, 0.30f, 0.02f),
            TipoRecipiente.Gaveteiro => new Vector3(0, 0.42f, 0.26f),
            _ => new Vector3(-0.22f, 1.19f, 0.02f)
        };

        /// <summary>Posição de mundo do item, já girada junto com o móvel que o guarda.</summary>
        Vector3 PosicaoDoItem(Item it)
        {
            if (it.Dentro < 0) return new Vector3(it.Pos.X, 0.45f, it.Pos.Z);

            var no = _recipientes[it.Dentro];
            var local = LugarNoRecipiente(no.Tipo);
            var girado = local.Rotated(Vector3.Up, no.Raiz.Rotation.Y);
            return new Vector3(it.Pos.X + girado.X, girado.Y, it.Pos.Z + girado.Z);
        }

        void MontarRecipientes()
        {
            var raiz = new Node3D { Name = "Recipientes" };
            AddChild(raiz);

            foreach (var r in _partida.Recipientes)
            {
                var no = Props.Criar(PecaDo(r.Tipo), Modelos.TamanhoGaveteiro, _matArmario);
                no.Position = new Vector3(r.Pos.X, 0, r.Pos.Z);
                // gira cada um um pouco: fileira de móveis alinhados denuncia grade
                no.RotateY(Mathf.Tau * (Mathf.Abs(r.Pos.X * 7.13f + r.Pos.Z * 3.31f) % 1f));
                RaizDoAndar(r.Andar).AddChild(no);

                _recipientes.Add(new NoRecipiente
                {
                    Raiz = no,
                    Tampa = no.GetNodeOrNull<Node3D>("Tampa") ?? AcharPorNome(no, "Tampa"),
                    Tipo = r.Tipo
                });
            }
        }

        /// <summary>
        /// Põe na tela o cenário que o núcleo já decidiu onde fica. Aqui não se
        /// escolhe nada: se a posição fosse sorteada de novo neste lado, o que
        /// você vê e o que te empurra seriam coisas diferentes.
        /// </summary>
        void MontarCenario()
        {
            var raiz = new Node3D { Name = "Cenario" };
            AddChild(raiz);

            foreach (var a in _partida.Adornos)
            {
                Node3D no = a.Tipo switch
                {
                    TipoAdorno.Caixote => Props.Criar(Peca.Caixote, new Vector3(0.85f, 0.8f, 0.85f), _matArmario),
                    TipoAdorno.Barril => Props.Criar(Peca.Barril, new Vector3(0.72f, 0.95f, 0.72f), _matArmario),
                    TipoAdorno.Bancada => Props.Criar(Peca.Bancada, Vector3.One, _matArmario),
                    TipoAdorno.Pilha => Props.Criar(Peca.Pilha, Vector3.One, _matArmario),
                    TipoAdorno.Entulho => Props.Criar(Peca.Entulho, Vector3.One, _matArmario),
                    TipoAdorno.CanoParede => Modelos.CanoParede(),
                    _ => Modelos.CanoTeto(ComprimentoDoCano(a.Pos))
                };

                no.Position = new Vector3(a.Pos.X, 0, a.Pos.Z);
                no.RotateY(a.Giro);
                RaizDoAndar(a.Andar).AddChild(no);
            }
        }

        /// <summary>O cano de teto atravessa a sala inteira, então precisa do tamanho dela.</summary>
        float ComprimentoDoCano(P2 pos)
        {
            var sala = _partida.Predio.SalaEm(pos);
            if (sala == null) return Predio.Celula * 3f;
            return Mathf.Max(sala.Larg, sala.Alt) * Predio.Celula * 0.9f;
        }

        static Node3D AcharPorNome(Node raiz, string nome)
        {
            foreach (var filho in raiz.GetChildren())
            {
                if (filho.Name == nome && filho is Node3D n) return n;
                var achado = AcharPorNome(filho, nome);
                if (achado != null) return achado;
            }
            return null;
        }

        /// <summary>
        /// Procura o AnimationPlayer que veio dentro do modelo importado.
        /// Quando a peça ainda é primitiva não existe nenhum, e o jogo segue
        /// rodando com a criatura deslizando — feio, mas não quebra.
        /// </summary>
        static AnimationPlayer AcharAnimador(Node raiz)
        {
            if (raiz is AnimationPlayer ap) return ap;
            foreach (var filho in raiz.GetChildren())
            {
                var achado = AcharAnimador(filho);
                if (achado != null) return achado;
            }
            return null;
        }

        /// <summary>Toca a animação só quando ela muda, senão reinicia a cada quadro.</summary>
        void Animar(string clipe)
        {
            if (_animCriatura == null || _clipeAtual == clipe) return;
            if (!_animCriatura.HasAnimation(clipe)) return;
            _animCriatura.Play(clipe, 0.25f);   // 0,25 s de transição entre estados
            _clipeAtual = clipe;
        }

        // ------------------------------------------------------------- entrada

        void CapturarMouse(bool capturar)
        {
            _mouseCapturado = capturar;
            Input.MouseMode = capturar ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
        }

        /// <summary>
        /// Em _Input, e não em _UnhandledInput: aqui a tecla chega sempre, antes
        /// de qualquer nó da interface ter chance de engolir o evento. Foi por
        /// isso que o Esc "não fazia nada" e o jogador ficava preso no jogo.
        /// </summary>
        public override void _Input(InputEvent e)
        {
            if (e is InputEventMouseMotion mm && _mouseCapturado && !_pausado)
            {
                _giro -= mm.Relative.X * Opcoes.Sensibilidade;
                _inclinacao = Mathf.Clamp(
                    _inclinacao - mm.Relative.Y * Opcoes.Sensibilidade * (Opcoes.InverterY ? -1 : 1),
                    -1.4f, 1.4f);
                return;
            }

            if (e is InputEventKey k && k.Pressed && !k.Echo)
            {
                switch (k.Keycode)
                {
                    case Key.Escape:
                        if (_naAbertura) _menu?.Avancar();
                        else if (_mapaAberto) AlternarMapa();   // mapa aberto: Esc fecha o mapa
                        else AlternarPausa();
                        break;

                    // sair de verdade. Sem isto só restava Alt+F4, e ninguém
                    // deveria precisar descobrir isso sozinho. Com o mapa aberto
                    // o Q troca de andar, que é o outro uso natural da tecla.
                    case Key.Q:
                        if (_pausado) GetTree().Quit();
                        else if (_mapaAberto) TrocarAndarDoMapa();
                        break;

                    case Key.Tab: if (!_pausado && !_naAbertura) AlternarMapa(); break;
                    case Key.E: if (!_pausado && !_mapaAberto) _interagirPedido = true; break;
                    case Key.F: if (!_pausado) _lanternaPedida = true; break;
                    case Key.Minus: Opcoes.AjustarSensibilidade(-1); break;
                    case Key.Equal: Opcoes.AjustarSensibilidade(1); break;
                    case Key.F5: GetTree().ReloadCurrentScene(); break;
                }
                return;
            }

            if (e is InputEventMouseButton mb && mb.Pressed)
            {
                if (_naAbertura) { if (_menu != null && _menu.NaHistoria) _menu.Avancar(); }
                else if (_pausado) AlternarPausa();
            }
        }

        /// <summary>
        /// Bússola sob demanda (TAB). Não fica ligada porque saber sempre onde
        /// ir mata a tensão — mas ficar perdido no escuro sem nenhuma saída é
        /// pior. Segurar a tecla é o meio-termo.
        /// </summary>
        string TextoDaBussola()
        {
            if (!Input.IsKeyPressed(Key.Tab)) return null;

            P2 alvo;
            string oQue;
            if (_partida.PortaoAberto) { alvo = _partida.Portao; oQue = "portão"; }
            else if (_partida.Jogador.FusiveisNaMao > 0) { alvo = _partida.Quadro; oQue = "quadro elétrico"; }
            else
            {
                // fusível mais próximo que ainda está no chão
                alvo = _partida.Quadro;
                oQue = "quadro elétrico";
                float melhor = float.MaxValue;
                foreach (var f in _partida.Fusiveis)
                {
                    if (f.Recolhido) continue;
                    float d = P2.Distancia(f.Pos, _partida.Jogador.Pos);
                    if (d < melhor) { melhor = d; alvo = f.Pos; oQue = "fusível mais próximo"; }
                }
            }

            var para = alvo - _partida.Jogador.Pos;
            float dist = para.Comprimento;

            // ângulo entre para onde você olha e onde está o alvo
            var frente = Direcao.Frente(_giro);
            var direita = Direcao.Direita(_giro);
            float aFrente = P2.Escalar(para.Normalizado, frente);
            float aoLado = P2.Escalar(para.Normalizado, direita);

            string seta = aFrente > 0.7f ? "em frente"
                        : aFrente < -0.7f ? "atrás de você"
                        : aoLado > 0 ? "à sua direita" : "à sua esquerda";

            return $"{oQue}: {seta}, {dist:0} m";
        }

        void AlternarPausa()
        {
            _pausado = !_pausado;
            CapturarMouse(!_pausado);

            // fecha o mapa junto: pausar com o mapa aberto deixava dois
            // painéis empilhados e o clique não chegava em nenhum botão
            if (_pausado) { _mapaAberto = false; _mapa?.Esconder(); _telaPausa?.Abrir(); }
            else _telaPausa?.Fechar();
        }

        /// <summary>
        /// Abre e fecha a planta. Só funciona depois de achar o item — apertar
        /// TAB sem ele tem de dizer por que não abriu, senão parece defeito.
        /// </summary>
        void AlternarMapa()
        {
            if (!_partida.Jogador.TemMapa) { _hud?.Avisar("você não tem a planta do prédio"); return; }

            _mapaAberto = !_mapaAberto;
            if (_mapaAberto)
            {
                _andarNoMapa = _partida.Jogador.Andar;
                _mapa.Mostrar(_partida, _andarNoMapa);
            }
            else _mapa.Esconder();
        }

        void TrocarAndarDoMapa()
        {
            _andarNoMapa = (_andarNoMapa + 1) % _partida.Predio.Andares;
            _mapa.Mostrar(_partida, _andarNoMapa);
        }

        /// <summary>
        /// Lê teclado e monta o comando. Com a planta aberta você para de andar
        /// mas o mundo não para: é o preço de consultar o mapa no meio do turno.
        /// </summary>
        Comando LerComando(bool olhandoOMapa = false)
        {
            float fx = 0, fz = 0;
            if (!olhandoOMapa)
            {
                if (Input.IsKeyPressed(Key.W)) fz -= 1;
                if (Input.IsKeyPressed(Key.S)) fz += 1;
                if (Input.IsKeyPressed(Key.D)) fx += 1;
                if (Input.IsKeyPressed(Key.A)) fx -= 1;
            }

            // A conversão mora no núcleo e tem teste: escrita à mão aqui, um
            // sinal trocado fazia o W andar para trás em metade das direções.
            var mundo = Direcao.LocalParaMundo(fx, fz, _giro);
            var cmd = new Comando
            {
                FrenteX = mundo.X,
                FrenteZ = mundo.Z,
                Giro = _giro,
                Inclinacao = _inclinacao,
                Correr = Input.IsKeyPressed(Key.Shift),
                Agachar = Input.IsKeyPressed(Key.Ctrl),
                PrenderAr = Input.IsKeyPressed(Key.Space),
                Interagir = _interagirPedido,
                AlternarLanterna = _lanternaPedida
            };
            _interagirPedido = false;
            _lanternaPedida = false;
            return cmd;
        }

        // ------------------------------------------------------------- quadro

        public override void _Process(double delta)
        {
            float dt = (float)delta;

            if (_autoTeste) { AutoTeste(); return; }

            if (_tirarFoto)
            {
                // deixa a partida andar um pouco para a criatura sair do lugar,
                // e gira a câmera devagar para não fotografar sempre a mesma parede
                if (!_naAbertura)
                {
                    if (_verAndarDeCima)
                    {
                        _partida.Jogador.Pos = new P2(_partida.Quadro.X, _partida.Quadro.Z + 4.5f);
                        _partida.Jogador.Andar = _partida.QuadroAndar;
                        _giro = 0; _inclinacao = -0.10f;
                    }
                    else if (_verEscada)
                    {
                        var e = _partida.Predio.Escadas[0];
                        var m = _partida.Predio.ParaMundo(new Celula(e.Cx, e.Cz, e.De));
                        _partida.Jogador.Pos = new P2(m.X, m.Z + 5.5f);
                        _partida.Jogador.Andar = e.De;
                        _giro = 0; _inclinacao = 0.06f;
                    }
                    else if (_verRecipiente)
                    {
                        var rec = _partida.Recipientes[_partida.Fusiveis[0].Dentro];
                        _partida.Jogador.Pos = new P2(rec.Pos.X, rec.Pos.Z + 1.7f);
                        _giro = 0; _inclinacao = -0.35f;
                        SincronizarRecipientes(dt);
                    }
                    else if (_verItem)
                    {
                        // encosta no item para julgar o modelo, em vez de adivinhar
                        var alvo = _partida.Fusiveis[0].Pos;
                        _partida.Jogador.Pos = new P2(alvo.X, alvo.Z + 0.85f);
                        _giro = 0; _inclinacao = -0.95f;
                    }
                    else { _partida.Passo(1f / 60f, new Comando()); _giro += dt * 0.35f; }
                }
                if (!_verRecipiente && !_verAndarDeCima && !_verEscada)
                    _inclinacao = -0.14f;   // olha um pouco para baixo: mostra chao e parede
                _quadrosDeFoto++;
                SincronizarCamera(dt);
                SincronizarItens(dt);
                SincronizarCriatura(dt);
                _hud.Atualizar(_partida);

                if (_quadrosDeFoto == 148)
                {
                    var predio = GetNodeOrNull<Node3D>("Predio");
                    GD.Print("camera em " + _camera.GlobalPosition + ", lanterna visivel=" + _lanterna.Visible + " energia=" + _lanterna.LightEnergy);
                    GD.Print("lanterna global=" + _lanterna.GlobalPosition + " frente=" + (-_lanterna.GlobalTransform.Basis.Z));
                    GD.Print("camera frente=" + (-_camera.GlobalTransform.Basis.Z) + " rot=" + _camera.Rotation);
                    if (_forcarLuz) _lanterna.LightEnergy = 500f;
                    GD.Print($"filhos do predio: {predio?.GetChildCount()}");
                    if (predio != null)
                        for (int i = 1; i < Mathf.Min(5, predio.GetChildCount()); i++)
                            if (predio.GetChild(i) is Node3D nd)
                                GD.Print($"  peca {i}: {nd.Name} em {nd.GlobalPosition}, visivel={nd.Visible}");
                    GD.Print($"material parede: albedo={_matParede.AlbedoColor} textura={(_matParede.AlbedoTexture != null ? "sim" : "nao")}");
                }
                if (_quadrosDeFoto == 150) TirarFoto("res://captura_1.png");
                // depois da foto do menu, dispensa ele e fotografa o jogo
                if (_quadrosDeFoto == 160 && _menu != null) _menu.PularTudo();
                // abre o recipiente entre uma foto e outra: dá para comparar
                if (_quadrosDeFoto == 240 && _verRecipiente) _partida.Interagir();
                if (_quadrosDeFoto == 200 && _verMapa)
                {
                    // finge que a planta foi achada e marca algumas salas como
                    // vistas, senão a captura sai com o mapa em branco
                    _partida.Jogador.TemMapa = true;
                    _partida.RevelarTudoParaCaptura();
                    _andarNoMapa = _partida.Jogador.Andar;
                    _mapaAberto = true;
                    _mapa.Mostrar(_partida, _andarNoMapa);
                }
                if (_quadrosDeFoto == 330) TirarFoto("res://captura_2.png");
                if (_quadrosDeFoto >= 340) GetTree().Quit();
                return;
            }

            // O mapa NÃO pausa o jogo: olhar a planta tem de custar tempo real,
            // senão vira um botão de "pensar de graça" no meio da perseguição.
            if (_partida.Fase == Fase.Jogando && !_pausado && !_naAbertura)
            {
                _partida.Passo(dt, LerComando(_mapaAberto));
                foreach (var ev in _partida.Eventos) Som.Tocar(this, ev, _partida);
            }

            if (_mapaAberto) _mapa.QueueRedraw();
            if (_pausado) _telaPausa?.Atualizar();
            // HUD some com o mapa aberto: os dois na tela ao mesmo tempo
            // escreviam um por cima do outro
            if (_hud != null) _hud.Visible = !_mapaAberto;

            SincronizarCamera(dt);
            SincronizarItens(dt);
            SincronizarCriatura(dt);
            if (_naAbertura) return;
            _hud.Atualizar(_partida, _pausado, TextoDaBussola());
        }

        void SincronizarCamera(float dt)
        {
            var j = _partida.Jogador;

            float alvo = j.Correndo ? 0.055f : 0.028f;
            _balanco += dt * (j.Correndo ? 9f : 5f);
            float sobe = Mathf.Sin(_balanco) * alvo;
            float tremor = j.Medo > 0.55f ? (GD.Randf() - 0.5f) * (j.Medo - 0.55f) * 0.05f : 0f;

            // a altura do andar entra aqui: sem ela, subir a escada deixava a
            // câmera no térreo olhando para dentro da laje
            float pisoDoAndar = Predio.AlturaDoAndar(j.Andar);
            _camera.Position = new Vector3(j.Pos.X, pisoDoAndar + j.AlturaOlho + sobe + tremor, j.Pos.Z);
            _camera.Rotation = new Vector3(_inclinacao, _giro, Mathf.Cos(_balanco * 0.5f) * 0.006f);
            MostrarApenasOAndar(j.Andar);

            // o holofote fica um pouco a frente da mao, para o corpo da lanterna
            // nao tapar a propria luz
            _lanterna.Position = new Vector3(0.26f, -0.20f, -0.5f);
            _lanterna.Visible = j.LuzAcesa;

            // braco e lanterna acompanham o passo e demoram a seguir a mira,
            // que e o que faz parecer peso na mao em vez de adesivo na tela
            if (_mao != null)
            {
                float alvoX = Mathf.Sin(_balanco) * 0.012f;
                float alvoY = Mathf.Abs(Mathf.Cos(_balanco)) * 0.010f;
                _balancoDaMao = _balancoDaMao.Lerp(new Vector2(alvoX, alvoY), Mathf.Min(1f, dt * 8f));
                _mao.Position = new Vector3(0.21f + _balancoDaMao.X, -0.17f + _balancoDaMao.Y, -0.46f);
                _mao.Rotation = new Vector3(_balancoDaMao.Y * 2.5f, -_balancoDaMao.X * 3f, 0);
                _mao.Visible = !j.Escondido;
                if (_matVidro != null)
                    _matVidro.EmissionEnergyMultiplier = j.LuzAcesa ? 1.4f * Mathf.Clamp(j.Bateria * 3f, 0.3f, 1f) : 0f;
            }
            // a luz fraqueja junto com a bateria: avisa antes de apagar
            if (!_forcarLuz)
                _lanterna.LightEnergy = EnergiaDaLanterna * Mathf.Clamp(j.Bateria * 3f, 0.25f, 1f);
            _camera.Fov = j.Correndo ? 80 : 74;
        }

        void SincronizarItens(float dt)
        {
            float t = _partida.Tempo;

            SincronizarRecipientes(dt);

            for (int i = 0; i < _fusiveis.Count; i++)
            {
                var f = _partida.Fusiveis[i];
                _fusiveis[i].Visible = !f.Recolhido && Revelado(f);
                var onde = PosicaoDoItem(f);
                _fusiveis[i].Position = onde + new Vector3(0, Mathf.Sin(t * 1.6f + i) * 0.03f, 0);
                _fusiveis[i].RotateY(dt * 0.9f);
            }
            for (int i = 0; i < _baterias.Count; i++)
            {
                var b = _partida.Baterias[i];
                _baterias[i].Visible = !b.Recolhido && Revelado(b);
                _baterias[i].RotateY(dt * 0.6f);
            }

            if (_mapaNo != null && _partida.MapaItem != null)
            {
                _mapaNo.Visible = !_partida.MapaItem.Recolhido && Revelado(_partida.MapaItem);
                _mapaNo.RotateY(dt * 0.4f);
            }

            var portao = GetNodeOrNull<Node3D>("Portao");
            if (portao != null && _partida.PortaoAberto)
                portao.Position = new Vector3(_partida.Portao.X, -2.6f, _partida.Portao.Z);  // sobe a grade
        }

        /// <summary>O item só existe para os olhos depois que a tampa saiu da frente.</summary>
        bool Revelado(Item it) =>
            it.Dentro < 0 || _recipientes[it.Dentro].Abertura > 0.45f;

        /// <summary>
        /// A abertura é interpolada, não instantânea: gaveta que salta para fora
        /// num quadro parece bug. Meio segundo de movimento é o que transforma
        /// "o estado mudou" em "eu abri isso agora".
        /// </summary>
        void SincronizarRecipientes(float dt)
        {
            for (int i = 0; i < _recipientes.Count; i++)
            {
                var no = _recipientes[i];
                float alvo = _partida.Recipientes[i].Aberto ? 1f : 0f;
                if (Mathf.IsEqualApprox(no.Abertura, alvo)) continue;

                no.Abertura = Mathf.MoveToward(no.Abertura, alvo, dt * 2.2f);
                if (no.Tampa == null) continue;

                float a = no.Abertura;
                switch (no.Tipo)
                {
                    case TipoRecipiente.CaixaDeFerramentas:
                        no.Tampa.Rotation = new Vector3(-a * 1.9f, 0, 0);   // tampa cai para trás
                        break;
                    case TipoRecipiente.Gaveteiro:
                        // 22 cm: com 34 a bandeja saia quase inteira do movel e
                        // ficava pendurada no ar, sem nada segurando
                        no.Tampa.Position = new Vector3(0, 0, a * 0.22f);
                        break;
                    default:
                        no.Tampa.Rotation = new Vector3(0, 0, a * 1.15f);   // a caixa tomba
                        break;
                }
            }
        }

        void SincronizarCriatura(float dt)
        {
            var ela = _partida.Ela;
            _criatura.Position = new Vector3(ela.Pos.X, Predio.AlturaDoAndar(ela.Andar), ela.Pos.Z);
            // não desenha a criatura que está no outro piso: sem isto ela
            // aparece atravessando o chão quando passa embaixo de você
            _criatura.Visible = ela.Andar == _partida.Jogador.Andar;
            var dir = ela.Direcao;
            _criatura.Rotation = new Vector3(0, Mathf.Atan2(dir.X, dir.Z) + GiroDoModelo, 0);

            // a animacao segue o estado: parada, andando ou correndo atras de voce
            float ritmo = ela.Velocidade.Comprimento;
            Animar(ela.Estado == EstadoCriatura.Caca || ritmo > 4f ? "Run"
                 : ritmo > 0.4f ? "Walk"
                 : "Idle");
            // só existe quando está por perto: economiza e evita vê-la de longe sem querer
            _criatura.Visible = P2.Distancia(ela.Pos, _partida.Jogador.Pos) < 45f;
        }

        void TirarFoto(string caminho)
        {
            var img = GetViewport().GetTexture().GetImage();
            string real = ProjectSettings.GlobalizePath(caminho);
            img.SavePng(real);
            GD.Print($"captura salva: {real}");
        }

        // ------------------------------------------------------------- auto-teste

        void AutoTeste()
        {
            _quadrosDeTeste++;
            if (_quadrosDeTeste < 2) return;

            bool ok = true;
            void Checar(string o_que, bool cond)
            {
                GD.Print($"  [{(cond ? "ok  " : "FALHA")}] {o_que}");
                if (!cond) ok = false;
            }

            GD.Print("auto-teste do Godot:");
            Checar("planta validada", _partida.Validar().Count == 0);
            Checar($"paredes instanciadas ({_partida.Predio.BlocosDeParede().Count})",
                   _partida.Predio.BlocosDeParede().Count > 20);
            Checar($"fusíveis na cena ({_fusiveis.Count})", _fusiveis.Count == Regras.FusiveisNecessarios);
            Checar($"armários na cena ({_partida.Armarios.Count})", _partida.Armarios.Count >= 8);
            Checar($"recipientes na cena ({_recipientes.Count})",
                   _recipientes.Count == _partida.Recipientes.Count && _recipientes.Count >= 20);
            Checar("todo recipiente tem peça móvel",
                   _recipientes.TrueForAll(r => r.Tampa != null));

            int salas0 = 0, salas1 = 0;
            foreach (var s in _partida.Predio.Salas) { if (s.Andar == 0) salas0++; else salas1++; }
            Checar($"cômodos: {salas0} no térreo, {salas1} em cima", salas0 >= 8 && salas1 >= 8);
            Checar($"nós de andar montados ({_andares.Count})", _andares.Count == _partida.Predio.Andares);
            Checar($"escadas ({_partida.Predio.Escadas.Count})", _partida.Predio.Escadas.Count >= 2);
            Checar($"quadro no andar {_partida.QuadroAndar}", _partida.QuadroAndar >= 0);
            Checar("planta do prédio existe e está guardada",
                   _partida.MapaItem != null && _partida.MapaItem.Dentro >= 0);
            Checar("câmera criada", _camera != null);
            Checar("lanterna criada", _lanterna != null);
            Checar("criatura na cena", _criatura != null);
            Checar(_animCriatura != null
                ? "animações do modelo: " + string.Join(", ", _animCriatura.GetAnimationList())
                : "modelo da criatura sem AnimationPlayer (ainda em primitiva?)",
                _animCriatura != null && _animCriatura.HasAnimation("Walk") && _animCriatura.HasAnimation("Run"));

            // roda 20 s de partida sem jogador para ver a simulação andar
            var antes = _partida.Ela.Pos;
            for (int i = 0; i < 60 * 20; i++) _partida.Passo(1f / 60f, new Comando());
            Checar($"criatura andou {P2.Distancia(antes, _partida.Ela.Pos):0.0} m",
                   P2.Distancia(antes, _partida.Ela.Pos) > 5f);
            Checar("posições finitas",
                   !float.IsNaN(_partida.Ela.Pos.X) && !float.IsNaN(_partida.Jogador.Pos.X));

            GD.Print(Props.Relatorio());
            GD.Print(ok ? "auto-teste: TUDO CERTO" : "auto-teste: FALHOU");
            GetTree().Quit(ok ? 0 : 1);
        }
    }

    /// <summary>Faz uma luz oscilar como lâmpada com mau contato.</summary>
    public partial class Piscar : Node
    {
        float _t, _proxima = 0.4f;
        float _base = -1f;

        public override void _Process(double delta)
        {
            if (GetParent() is not OmniLight3D luz) return;
            if (_base < 0) _base = luz.LightEnergy;

            _t += (float)delta;
            if (_t < _proxima) return;
            _t = 0;
            _proxima = (float)GD.RandRange(0.05, 2.2);
            luz.LightEnergy = GD.Randf() < 0.3f ? _base * 0.05f : _base * (float)GD.RandRange(0.6, 1.1);
        }
    }
}

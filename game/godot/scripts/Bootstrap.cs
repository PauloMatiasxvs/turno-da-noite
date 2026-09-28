using System.Collections.Generic;
using System.Linq;
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
        /// <summary>Onde a mão descansa na frente da câmera. Muda conforme ela seja o modelo importado ou a de código.</summary>
        Vector3 _poseDaMao = new Vector3(0.185f, -0.215f, -0.60f);
        /// <summary>Inclinação de descanso da mão, em graus. O balanço do passo soma em cima disto.</summary>
        Vector3 _giroDaMao = Vector3.Zero;
        Skeleton3D _ossosDasMaos;
        MeshInstance3D _vidroDaLanterna;
        DirectionalLight3D _luzDaMao;
        StandardMaterial3D _matVidro;
        Vector2 _balancoDaMao;
        AnimationPlayer _animCriatura;
        string _clipeAtual = "";
        /// <summary>Pi porque o modelo olha para -Z, que e a frente padrao. Modelo que olha para +Z quer 0.</summary>
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

        /// <summary>Um armario na cena, com a porta separada para poder abrir.</summary>
        sealed class NoArmario
        {
            public Node3D Raiz, Porta;
            public float Abertura;
        }
        readonly List<NoArmario> _armarios = new();

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
        bool _verAndarDeCima, _verMapa, _verEscada, _verCriatura, _verMapaDeCima, _verArmario;

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
                if (arg == "--vercriatura") _verCriatura = true;
                if (arg == "--vermapacima") { _verMapa = true; _verMapaDeCima = true; }
                if (arg == "--verarmario") _verArmario = true;
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

            MontarTratamentoDeImagem();

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

        /// <summary>
        /// Grão, vinheta, preto esmagado e um fio de aberração cromática.
        ///
        /// É a camada que separa "render de motor" de "cena de filme", e custa
        /// um quad de tela cheia. Vai numa CanvasLayer ABAIXO da HUD — com a
        /// interface dentro, o texto pegaria grão e vinheta e ficaria sujo.
        ///
        /// Se o arquivo do shader sumir, o jogo roda sem ele: o tratamento é
        /// melhoria de imagem, não regra de jogo.
        /// </summary>
        void MontarTratamentoDeImagem()
        {
            const string caminho = "res://shaders/filme.gdshader";
            if (!ResourceLoader.Exists(caminho)) return;

            var shader = GD.Load<Shader>(caminho);
            if (shader == null) return;

            // camada -1: desenha depois do 3D e ANTES da HUD, que fica na 0.
            // Com o filme por cima, o texto da interface pegava grao e vinheta.
            var camada = new CanvasLayer { Name = "Filme", Layer = -1 };
            camada.AddChild(new ColorRect
            {
                Material = new ShaderMaterial { Shader = shader },
                AnchorRight = 1,
                AnchorBottom = 1,
                MouseFilter = Control.MouseFilterEnum.Ignore
            });
            AddChild(camada);
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

            // madeira em ripas verticais, como na referencia; escala menor para
            // a ripa aparecer em vez de virar borrao
            _matParede = Texturizado("parede", new Color(0.62f, 0.50f, 0.36f), 0.42f)
                         ?? Fosco(new Color(0.26f, 0.25f, 0.23f));
            // escala baixa: com 0,22 o carpete repetia tanto que dava moire —
            // aquele padrao de interferencia que so acontece em textura de jogo
            _matPiso = Texturizado("piso", new Color(0.46f, 0.45f, 0.44f), 0.085f)
                       ?? Fosco(new Color(0.19f, 0.18f, 0.17f), 0.95f);
            // Texturado sempre que houver arquivo. Cor chapada num armário de
            // metal a um metro do nariz é a coisa que mais rápido entrega que
            // aquilo é uma maquete: sem grão, sem risco, sem ferrugem, a peça
            // vira plástico. Todas as texturas são CC0 do ambientCG.
            _matTeto = Texturizado("teto", new Color(0.52f, 0.52f, 0.50f), 0.38f)
                       ?? Fosco(new Color(0.13f, 0.13f, 0.14f));
            _matArmario = Texturizado("metal", new Color(0.38f, 0.41f, 0.44f), 1.3f)
                          ?? Fosco(new Color(0.22f, 0.24f, 0.26f), 0.6f);
            _matQuadro = Texturizado("ferrugem", new Color(0.52f, 0.45f, 0.34f), 1.7f)
                         ?? Fosco(new Color(0.30f, 0.26f, 0.18f), 0.7f);
            _matCriatura = Fosco(new Color(0.045f, 0.04f, 0.05f), 1f);
            _matFusivel = Brilho(new Color(1f, 0.72f, 0.20f), 1.8f);
            _matBateria = Brilho(new Color(0.35f, 0.9f, 0.55f), 1.4f);
            _matPortao = Texturizado("metal", new Color(0.34f, 0.20f, 0.18f), 1.1f)
                         ?? Fosco(new Color(0.24f, 0.11f, 0.11f), 0.8f);
            _matMapa = Brilho(new Color(0.86f, 0.78f, 0.55f), 0.7f);

            // as texturas que os móveis montados em código usam
            Modelos.Texturas(
                metal: Texturizado("metal", new Color(0.30f, 0.32f, 0.35f), 2.2f),
                madeira: Texturizado("madeira", new Color(0.42f, 0.32f, 0.22f), 1.6f),
                ferrugem: Texturizado("ferrugem", new Color(0.46f, 0.30f, 0.22f), 2.0f),
                pele: Texturizado("criatura", new Color(0.115f, 0.105f, 0.115f), 0.55f),
                // escala ALTA aqui: a mao tem dez centimetros, entao a textura
                // precisa repetir muito para o grao aparecer no tamanho certo
                metalDaMao: Texturizado("metal", new Color(0.24f, 0.25f, 0.27f), 9f),
                couroDaMao: Texturizado("couro", new Color(0.115f, 0.095f, 0.085f), 14f));
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

        /// <summary>
        /// Acha o arquivo da textura, em .jpg ou .png.
        ///
        /// Aceita os dois porque quem gera textura fora do jogo costuma
        /// entregar PNG, e exigir conversão só para soltar um arquivo na pasta
        /// anulava a ideia do registro de assets.
        /// </summary>
        static string AcharTextura(string nome, string mapa)
        {
            foreach (string ext in new[] { ".png", ".jpg" })
            {
                string caminho = $"res://assets/texturas/{nome}_{mapa}{ext}";
                if (ResourceLoader.Exists(caminho)) return caminho;
            }
            return null;
        }

        static StandardMaterial3D Texturizado(string nome, Color tinta, float escala)
        {
            string cor = AcharTextura(nome, "cor");
            if (cor == null) return null;

            var mat = new StandardMaterial3D
            {
                AlbedoTexture = GD.Load<Texture2D>(cor),
                AlbedoColor = tinta,
                Uv1Triplanar = true,
                Uv1Scale = new Vector3(escala, escala, escala),
                Roughness = 1f,
                Metallic = 0f
            };

            string normal = AcharTextura(nome, "normal");
            if (!SemNormal && normal != null)
            {
                mat.NormalEnabled = true;
                mat.NormalTexture = GD.Load<Texture2D>(normal);
                mat.NormalScale = 1.2f;   // realça o relevo: no escuro é o que dá textura à parede
            }

            string aspereza = AcharTextura(nome, "aspereza");
            if (aspereza != null)
            {
                mat.RoughnessTexture = GD.Load<Texture2D>(aspereza);
                mat.RoughnessTextureChannel = BaseMaterial3D.TextureChannel.Red;
            }

            // Mapa de metalicidade, quando vier um. Sem ele uma chapa de aço
            // fica com o brilho de papelão: é o metalness que faz o reflexo
            // acompanhar a luz em vez de espalhar por igual.
            string metal = AcharTextura(nome, "metal");
            if (metal != null)
            {
                mat.MetallicTexture = GD.Load<Texture2D>(metal);
                mat.MetallicTextureChannel = BaseMaterial3D.TextureChannel.Red;
                mat.Metallic = 1f;
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
                LightCullMask = CamadaDoMundo,
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
                LightCullMask = CamadaDoMundo,
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

            var bracos = Props.CriarSemAjuste(Peca.Mao);
            if (bracos != null) MontarBracos(bracos);
            else MontarMaoDeCodigo();

            _mao.Position = _poseDaMao;
            _mao.RotationDegrees = _giroDaMao;

            _matVidro = new StandardMaterial3D
            {
                AlbedoColor = new Color(1f, 0.93f, 0.78f),
                EmissionEnabled = true,
                Emission = new Color(1f, 0.93f, 0.78f),
                EmissionEnergyMultiplier = 1.4f
            };
            _vidroDaLanterna.MaterialOverride = _matVidro;

            // Nada na mao projeta sombra, e tudo nela sai da camada do mundo e vai
            // para a CAMADA 2, sozinha. Isso desliga TODA a luz do jogo sobre a
            // mao — inclusive o proprio facho, que ao ser afastada a mao passou
            // a bater nela a dez centimetros e a deixava branca. Quem ilumina a
            // mao agora sao so as duas luzes de preenchimento abaixo.
            foreach (var n in TodosOsNos(_mao))
                if (n is GeometryInstance3D g)
                {
                    g.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
                    g.Layers = CamadaDaMao;
                }

            CriarLuzDaMao();
        }

        /// <summary>
        /// A mão montada em código, que é o que aparece quando não há arquivo
        /// de modelo na pasta. Fica no canto: o que precisa estar em quadro é a
        /// lanterna e um pedaço da luva, não o antebraço inteiro.
        /// </summary>
        void MontarMaoDeCodigo()
        {
            _poseDaMao = new Vector3(0.185f, -0.215f, -0.60f);
            var mao = Modelos.MaoComLanterna();
            mao.Name = "Lanterna";
            _mao.AddChild(mao);
            _vidroDaLanterna = mao.GetNode<MeshInstance3D>("Vidro");
        }

        /// <summary>
        /// Os braços de verdade: modelo com esqueleto, duas mãos, unhas.
        ///
        /// Três coisas precisam acontecer aqui, e nenhuma delas o arquivo traz
        /// pronta:
        ///
        /// 1. **Tirar o cotovelo de trás da câmera.** O modelo nasce com a
        ///    origem no olho e os braços indo até 22 cm ATRÁS dele. O que fica
        ///    atrás do plano de corte não some: aparece fatiado, mostrando o
        ///    avesso do tubo do braço. Por isso o conjunto inteiro é empurrado
        ///    para a frente, até o toco do braço passar do plano — lá embaixo,
        ///    fora de quadro.
        ///
        /// 2. **Fechar a mão.** As quatro animações que vieram no arquivo
        ///    (idle, grab, punch, fist_guard) mexem o PULSO um grau e meio e
        ///    mais nada: as pistas dos dedos têm todas o mesmo valor do começo
        ///    ao fim. Ou seja, o rig existe mas ninguém posou ele. Os dedos são
        ///    dobrados aqui, osso por osso.
        ///
        /// 3. **Pôr a lanterna DENTRO da mão.** Ela é pendurada no osso
        ///    `hand_R`, então segue o punho em qualquer pose ou balanço, sem
        ///    ninguém sincronizar nada.
        /// </summary>
        void MontarBracos(Node3D bracos)
        {
            _poseDaMao = PoseDosBracos;
            _giroDaMao = GiroDosBracos;
            _mao.Scale = new Vector3(EscalaDosBracos, EscalaDosBracos, EscalaDosBracos);
            _mao.AddChild(bracos);

            VestirMaos(bracos);

            var esqueleto = PrimeiroDoTipo<Skeleton3D>(bracos);
            _ossosDasMaos = esqueleto;
            if (esqueleto == null)
            {
                // modelo sem rig: ainda dá para mostrar as mãos, só não dá para
                // fechar a garra — a lanterna vai presa à raiz mesmo
                var solta = Modelos.Lanterna();
                solta.Position = PoseDaLanterna;
                solta.RotationDegrees = GiroDaLanterna;
                bracos.AddChild(solta);
                _vidroDaLanterna = solta.GetNode<MeshInstance3D>("Vidro");
                return;
            }

            FecharAMao(esqueleto, "_R", CurvaDaGarra, CurvaDoPolegarNaGarra, GiroDoPunho);
            // a esquerda fica solta, só não esticada: dedo reto e imóvel é a
            // coisa que mais denuncia manequim
            FecharAMao(esqueleto, "_L", CurvaDaMaoLivre, CurvaDoPolegarLivre, GiroDoPunhoLivre);

            // os braços. O modelo vem com os dois estendidos para a frente, na
            // mesma altura, feito quem pede esmola: quem carrega uma lanterna
            // sobe a direita e deixa a esquerda cair.
            Girar(esqueleto, "upperarm_R", GiroDoBracoDireito);
            Girar(esqueleto, "forearm_R", GiroDoAntebracoDireito);
            Girar(esqueleto, "upperarm_L", GiroDoBracoEsquerdo);
            Girar(esqueleto, "forearm_L", GiroDoAntebracoEsquerdo);

            PorAsLuvas(esqueleto, "_R");
            PorAsLuvas(esqueleto, "_L");

            var preso = new BoneAttachment3D { Name = "PunhoDireito", BoneName = "hand_R" };
            esqueleto.AddChild(preso);

            var lanterna = Modelos.Lanterna();
            preso.AddChild(lanterna);
            lanterna.Transform = EncaixarNaGarra(esqueleto);
            _vidroDaLanterna = lanterna.GetNode<MeshInstance3D>("Vidro");
        }

        /// <summary>
        /// Põe a lanterna dentro do punho fechado, apontada para onde a câmera
        /// olha, seja qual for a pose do braço.
        ///
        /// Calcular em vez de acertar no olho é o que torna as poses acima
        /// livres: mexer no cotovelo deixaria a lanterna para trás se o lugar
        /// dela fosse um número fixo. Aqui ela vai no MEIO dos quatro dedos —
        /// que é onde o buraco do punho está, por definição — e mira desfazendo
        /// a inclinação do conjunto, porque o facho sai da câmera e tem de
        /// combinar com o que ela ilumina.
        /// </summary>
        Transform3D EncaixarNaGarra(Skeleton3D esqueleto)
        {
            var centro = Vector3.Zero;
            int quantos = 0;
            foreach (string dedo in new[] { "index", "middle", "ring", "pinky" })
            {
                int osso = esqueleto.FindBone(dedo + "2_R");
                if (osso < 0) continue;
                centro += esqueleto.GetBoneGlobalPose(osso).Origin;
                quantos++;
            }
            if (quantos > 0) centro /= quantos;

            var mira = (Basis.FromEuler(_giroDaMao * (Mathf.Pi / 180f)).Inverse()
                      * Basis.FromEuler(DesvioDoFacho * (Mathf.Pi / 180f))
                      * new Vector3(0, 0, -1)).Normalized();

            // a pega fica ATRÁS da origem do modelo da lanterna, então a origem
            // anda para a frente do punho na mesma medida
            var alvo = new Transform3D(
                Basis.LookingAt(mira, Vector3.Up).Scaled(Vector3.One * EscalaDaLanterna),
                centro + mira * (RecuoDaPega * EscalaDaLanterna));
            int punho = esqueleto.FindBone("hand_R");
            return punho < 0 ? alvo : esqueleto.GetBoneGlobalPose(punho).AffineInverse() * alvo;
        }

        // ---- como os braços ficam na tela. Tudo aqui foi acertado no olho,
        // ---- comparando capturas: são os números que o modelo não traz.

        /// <summary>
        /// Empurrão para a frente. Não é enquadramento, é necessidade: o modelo
        /// tem geometria de braço atrás da câmera, e sem isto ela aparece
        /// cortada pelo plano de corte, pelo avesso.
        /// </summary>
        static readonly Vector3 PoseDosBracos = new Vector3(0.15f, -0.07f, -0.32f);
        /// <summary>Inclina os dois para baixo: o modelo vem com os braços na horizontal, e mão na altura do olho lê como zumbi.</summary>
        static readonly Vector3 GiroDosBracos = new Vector3(-18f, 0f, 0f);
        const float EscalaDosBracos = 1f;

        /// <summary>
        /// Giro do punho direito. Os noventa graus em Y são o que transforma
        /// "mão espalmada para baixo" em "mão fechada em volta de um cano
        /// apontado para a frente": o túnel que os dedos dobrados formam corre
        /// no eixo X do osso, então é o X que tem de virar para onde o facho vai.
        /// </summary>
        /// Sessenta e não noventa porque a pele paga o preço: o pulso é um osso
        /// só, sem osso de torção, e a guinada inteira amassava o dorso da mão
        /// num nó. O túnel do punho deixa de apontar exatamente para a frente,
        /// e não faz falta: quem mira a lanterna é EncaixarNaGarra, não o dedo.
        static readonly Vector3 GiroDoPunho = new Vector3(0f, 24f, 0f);
        static readonly Vector3 GiroDoPunhoLivre = new Vector3(-10f, 0f, 0f);

        /// <summary>Quanto cada falange dobra, em graus. Negativo é para dentro da palma.</summary>
        static readonly float[] CurvaDaGarra = { -52f, -68f, -46f };
        static readonly float[] CurvaDaMaoLivre = { -26f, -34f, -22f };
        const float CurvaDoPolegarNaGarra = -34f;
        const float CurvaDoPolegarLivre = -14f;

        /// <summary>
        /// Onde a lanterna fica, no espaço do osso `hand_R`. O X positivo vira
        /// a frente depois do giro do punho, então o 0,04 é o que põe a pega
        /// dentro da mão e deixa a cabeça 23 cm adiante dela.
        /// </summary>
        static readonly Vector3 PoseDaLanterna = new Vector3(0.040f, 0.005f, -0.095f);
        static readonly Vector3 GiroDaLanterna = new Vector3(0f, -90f, 0f);

        /// <summary>Ombro e cotovelo de cada lado. É o que tira os dois braços da mesma altura.</summary>
        static readonly Vector3 GiroDoBracoDireito = new Vector3(16f, -14f, 0f);
        static readonly Vector3 GiroDoAntebracoDireito = new Vector3(12f, 0f, 0f);
        // a esquerda cai para fora do quadro: mão que não faz nada e aparece
        // pela metade na borda de baixo lê como falha de desenho, não como braço
        static readonly Vector3 GiroDoBracoEsquerdo = new Vector3(-26f, 20f, 0f);
        static readonly Vector3 GiroDoAntebracoEsquerdo = new Vector3(6f, 0f, 0f);

        /// <summary>
        /// Quanto o facho sai torto em relação ao que a câmera mira, em graus.
        /// Não é zero de propósito: apontada exatamente para a frente, a
        /// lanterna aparece de topo — dois discos pretos e nada mais. Uns
        /// poucos graus de lado mostram o corpo dela e não mudam onde a luz cai.
        /// </summary>
        static readonly Vector3 DesvioDoFacho = new Vector3(-2f, 9f, 0f);
        /// <summary>Do meio da pega até a origem do modelo da lanterna.</summary>
        const float RecuoDaPega = 0.055f;
        /// <summary>
        /// A lanterna foi desenhada para aparecer sozinha num canto da tela.
        /// Fechada dentro de uma mão de tamanho real ela vira porrete: o aro
        /// da cabeça tem oito centímetros e o punho tem nove de largura.
        /// </summary>
        const float EscalaDaLanterna = 0.80f;

        /// <summary>
        /// Dobra os dedos de uma das mãos. Vai osso por osso porque o rig não
        /// traz pose nenhuma — só a hierarquia.
        ///
        /// Cada falange gira em torno do X local dela, que é o eixo do nó do
        /// dedo. Como os ossos vêm sem rotação de repouso, a dobra se acumula
        /// sozinha descendo a cadeia: a ponta do dedo fecha somando as três.
        /// </summary>
        static void FecharAMao(Skeleton3D esqueleto, string lado, float[] curva,
                               float polegar, Vector3 giroDoPunho)
        {
            Girar(esqueleto, "hand" + lado, giroDoPunho);

            foreach (string dedo in new[] { "index", "middle", "ring", "pinky" })
                for (int f = 0; f < 3; f++)
                    Girar(esqueleto, dedo + (f + 1) + lado, new Vector3(curva[f], 0, 0));

            // o polegar se opõe aos outros: fecha girando em Z, por cima do cano
            float ang = polegar * (lado == "_R" ? 1f : -1f);
            for (int f = 0; f < 3; f++)
                Girar(esqueleto, "thumb" + (f + 1) + lado, new Vector3(0, 0, ang));
        }

        /// <summary>
        /// Gira um osso, em graus, mantendo onde ele nasce.
        ///
        /// Vai por <c>SetBonePose</c> e não pelo óbvio <c>SetBonePoseRotation</c>
        /// porque neste Godot o segundo GUARDA o valor e não move nada: o
        /// getter devolve a rotação certinha, os filhos do osso continuam no
        /// lugar, e a tela mostra a mão aberta. Levei uma captura inteira para
        /// descobrir que o problema não eram os ângulos.
        /// </summary>
        static void Girar(Skeleton3D esqueleto, string nome, Vector3 graus)
        {
            int osso = esqueleto.FindBone(nome);
            if (osso < 0) return;
            var t = esqueleto.GetBonePose(osso);
            t.Basis = Basis.FromEuler(graus * (Mathf.Pi / 180f));
            esqueleto.SetBonePose(osso, t);
        }

        /// <summary>
        /// Enfia o antebraço dentro de um punho de couro.
        ///
        /// Fica pendurado no osso do ANTEBRAÇO, e não no da mão, porque é onde
        /// uma luva fica: a mão gira dentro dela. Preso na mão, o punho girava
        /// junto e o caroço do pulso reaparecia por baixo.
        ///
        /// A posição sai do próprio esqueleto — o repouso do osso da mão É o
        /// vetor cotovelo-pulso, no quadro do antebraço — então mexer nas poses
        /// do braço não desencaixa nada.
        /// </summary>
        void PorAsLuvas(Skeleton3D esqueleto, string lado)
        {
            int antebraco = esqueleto.FindBone("forearm" + lado);
            int punho = esqueleto.FindBone("hand" + lado);
            if (antebraco < 0 || punho < 0) return;

            var ate = esqueleto.GetBoneRest(punho).Origin;
            if (ate.LengthSquared() < 1e-6f) return;

            var preso = new BoneAttachment3D { Name = "Luva" + lado, BoneName = "forearm" + lado };
            esqueleto.AddChild(preso);

            var luva = Modelos.PunhoDaLuva(ate.Length() * FatiaDoAntebraco,
                                           RaioDaLuvaNoPulso, RaioDaLuvaNoCotovelo);
            luva.Transform = new Transform3D(Basis.LookingAt(ate.Normalized(), Vector3.Up),
                                             ate * BocaDaLuva);
            preso.AddChild(luva);

            foreach (var n in TodosOsNos(luva))
                if (n is GeometryInstance3D g)
                {
                    g.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
                    g.Layers = CamadaDaMao;
                }
        }

        /// <summary>Onde fica a boca da luva, em fração do antebraço. Passa de 1 para engolir o pulso.</summary>
        // Passa de 1 para ENGOLIR o pulso. Parada em cima da junta, a boca da
        // luva deixava o caroço do pulso aparecendo logo à frente dela, e o
        // couro em volta só servia de moldura para o defeito.
        const float BocaDaLuva = 1.13f;
        /// <summary>Quanto do antebraço a luva cobre. O resto, do cotovelo para trás, sai de quadro.</summary>
        const float FatiaDoAntebraco = 0.56f;
        const float RaioDaLuvaNoPulso = 0.047f;
        // A luva aponta para a câmera, então a ponta grossa fica a meio metro
        // do olho e a perspectiva engorda ela três vezes. Catorze centímetros
        // de diâmetro, que é uma luva de solda normal, enchiam um terço da tela.
        const float RaioDaLuvaNoCotovelo = 0.056f;

        /// <summary>
        /// Acerta a pele das mãos.
        ///
        /// O modelo pinta a pele nas CORES DE VÉRTICE, e deixa o difuso do
        /// material em branco puro — quem não ligar `VertexColorUseAsAlbedo`
        /// recebe duas mãos brancas chapadas. É o mesmo defeito que a criatura
        /// tinha, com outro nome.
        ///
        /// O material importado é um recurso compartilhado, então é duplicado
        /// antes: mexer nele direto mexeria em qualquer outro uso do arquivo.
        /// </summary>
        void VestirMaos(Node3D raiz)
        {
            foreach (var n in TodosOsNos(raiz))
            {
                if (n is not MeshInstance3D mi || mi.Mesh == null) continue;
                for (int i = 0; i < mi.Mesh.GetSurfaceCount(); i++)
                {
                    var m = mi.Mesh.SurfaceGetMaterial(i) is StandardMaterial3D vindo
                        ? (StandardMaterial3D)vindo.Duplicate()
                        : new StandardMaterial3D();
                    m.VertexColorUseAsAlbedo = true;
                    // as cores de vértice já trazem sombreado pintado; o difuso
                    // aqui só tira o excesso de brilho que a luz de mão daria
                    m.AlbedoColor = new Color(TomDaPele, TomDaPele, TomDaPele);
                    m.Roughness = 0.80f;
                    m.Metallic = 0f;
                    mi.SetSurfaceOverrideMaterial(i, m);
                }
            }
        }

        /// <summary>
        /// Escurece a pele. As cores de vértice do modelo foram pintadas para
        /// luz de dia; num prédio às escuras a mão saltava rosada no meio de
        /// uma cena que é toda marrom e preta — a coisa mais clara da tela era
        /// o braço do jogador.
        /// </summary>
        const float TomDaPele = 0.58f;

        static T PrimeiroDoTipo<T>(Node raiz) where T : Node
        {
            foreach (var n in TodosOsNos(raiz))
                if (n is T achado) return achado;
            return null;
        }

        /// <summary>
        /// Camadas de renderizacao. O mundo inteiro vive na 1; a mao, sozinha,
        /// na 2. Toda luz do jogo enxerga so a 1, e as duas luzes de
        /// preenchimento da mao enxergam so a 2.
        ///
        /// Nao basta mudar a camada da mao: luz em Godot nasce com mascara
        /// "todas as camadas", entao o facho continuava batendo nela mesmo
        /// depois de separada — foi por isso que baixar a energia da luz de
        /// preenchimento nao mudou nada na tela.
        /// </summary>
        const uint CamadaDoMundo = 1 << 0;
        const uint CamadaDaMao = 1 << 1;

        /// <summary>
        /// A luz que dá forma à mão, e só a ela.
        ///
        /// O problema: o holofote da lanterna aponta para a FRENTE, então a mão
        /// que o segura fica nas costas dele, recebendo só a luz ambiente — que
        /// é azulada e não vem de direção nenhuma. Sem direção não há sombra
        /// própria, e sem sombra própria a mão vira um borrão chapado onde não
        /// se distingue dedo de cano.
        ///
        /// A tentativa óbvia — uma luz pontual na cabeça da lanterna — é pior:
        /// a cinco centímetros dos dedos qualquer energia estoura, e eles saem
        /// brancos. Luz direcional não tem queda com a distância, então não
        /// estoura nada; o que faltava era impedir que ela iluminasse o prédio
        /// inteiro. Daí a máscara: a mão está na camada 2, esta luz enxerga só
        /// a camada 2, e nada mais no jogo está lá.
        /// </summary>
        void CriarLuzDaMao()
        {
            var fill = new DirectionalLight3D
            {
                Name = "LuzDaMao",
                LightColor = new Color(1f, 0.94f, 0.86f),
                // 1,35 dava cinza medio num albedo de 0,10: com exposicao filmica
                // a conta e mais generosa do que a intuicao sugere
                LightEnergy = 0.58f,
                ShadowEnabled = false,
                LightCullMask = CamadaDaMao
            };
            // de cima, da esquerda e um pouco de trás: é a direção que separa
            // o dorso dos dedos e deixa o lado de baixo na sombra
            fill.RotationDegrees = new Vector3(-38f, 28f, 0f);
            _camera.AddChild(fill);
            _luzDaMao = fill;

            // um respingo bem fraco por baixo, para o lado escuro não sumir
            var contra = new DirectionalLight3D
            {
                Name = "ContraLuzDaMao",
                LightColor = new Color(0.62f, 0.68f, 0.85f),
                LightEnergy = 0.15f,
                ShadowEnabled = false,
                LightCullMask = CamadaDaMao
            };
            contra.RotationDegrees = new Vector3(34f, -140f, 0f);
            _camera.AddChild(contra);
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
            MontarFachada(raiz);
        }

        /// <summary>
        /// O lado de fora: chão de concreto, degraus, marquise e a luz sobre a
        /// porta.
        ///
        /// Existe porque "entrar no prédio" não significava nada. O pátio
        /// recebia o mesmo carpete, a mesma madeira e o mesmo forro dos
        /// cômodos, então atravessar a porta era mudar de sala. Um limiar só
        /// existe quando os dois lados são diferentes: fora é concreto sob o
        /// céu, dentro é carpete sob o forro.
        /// </summary>
        void MontarFachada(Node3D raiz)
        {
            var predio = _partida.Predio;
            var patio = predio.Sala(TipoSala.Patio);
            if (patio == null) return;

            var fora = new Node3D { Name = "Fachada" };
            raiz.AddChild(fora);

            var concreto = Texturizado("teto", new Color(0.30f, 0.30f, 0.29f), 0.22f)
                           ?? new StandardMaterial3D
                           { AlbedoColor = new Color(0.17f, 0.17f, 0.16f), Roughness = 0.95f };
            var chapa = Texturizado("metal", new Color(0.22f, 0.23f, 0.24f), 0.9f)
                        ?? new StandardMaterial3D
                        { AlbedoColor = new Color(0.16f, 0.17f, 0.18f), Roughness = 0.7f };

            // ---- o chão do pátio, por cima do carpete do térreo
            float largura = (patio.Larg + 4) * Predio.Celula;
            float fundo = (patio.Alt + 3) * Predio.Celula;
            var centro = predio.ParaMundo(patio.Centro);

            var piso = new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(largura, 0.12f, fundo) },
                MaterialOverride = concreto,
                Position = new Vector3(centro.X, 0.05f, centro.Z + Predio.Celula),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
            };
            fora.AddChild(piso);

            // ---- os degraus até a porta: o corpo sente que subiu
            var porta = _partida.Portao;
            for (int i = 0; i < 2; i++)
            {
                float z = porta.Z + Predio.Celula * 0.55f + i * 0.34f;
                fora.AddChild(new MeshInstance3D
                {
                    Mesh = new BoxMesh { Size = new Vector3(Predio.Celula * 2.2f, 0.11f, 0.34f) },
                    MaterialOverride = concreto,
                    Position = new Vector3(porta.X, 0.055f + (1 - i) * 0.11f, z),
                    CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
                });
            }

            // ---- a marquise sobre a entrada, com dois montantes
            fora.AddChild(new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(Predio.Celula * 2.6f, 0.16f, 1.9f) },
                MaterialOverride = chapa,
                Position = new Vector3(porta.X, 3.05f, porta.Z + 0.95f)
            });
            foreach (float lado in new[] { -1f, 1f })
            {
                var x = porta.X + lado * Predio.Celula * 1.2f;
                float z = porta.Z + 1.75f;

                fora.AddChild(new MeshInstance3D
                {
                    Mesh = new BoxMesh { Size = new Vector3(0.12f, 3.0f, 0.12f) },
                    MaterialOverride = chapa,
                    Position = new Vector3(x, 1.5f, z)
                });

                // o montante tem corpo: poste que se atravessa e pior do que
                // poste nenhum, porque a tela mostra uma coisa e o corpo outra
                _partida.Solidos.Add(new Solido(new P2(x, z), 0, 0.16f));
            }

            // ---- a luz sobre a porta: é o farol que diz "a entrada é aqui"
            var luminaria = new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(0.5f, 0.14f, 0.22f) },
                MaterialOverride = new StandardMaterial3D
                {
                    AlbedoColor = new Color(1f, 0.86f, 0.62f),
                    EmissionEnabled = true,
                    Emission = new Color(1f, 0.84f, 0.58f),
                    EmissionEnergyMultiplier = 2.4f
                },
                Position = new Vector3(porta.X, 2.82f, porta.Z + 0.42f),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
            };
            fora.AddChild(luminaria);

            fora.AddChild(new OmniLight3D
            {
                LightCullMask = CamadaDoMundo,
                LightColor = new Color(1f, 0.80f, 0.54f),
                LightEnergy = 4.2f,
                OmniRange = 13f,
                ShadowEnabled = true,
                Position = new Vector3(porta.X, 2.72f, porta.Z + 0.55f)
            });

            // ---- a placa ao lado da porta
            fora.AddChild(new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(1.15f, 0.42f, 0.05f) },
                MaterialOverride = new StandardMaterial3D
                {
                    AlbedoColor = new Color(0.14f, 0.16f, 0.15f), Roughness = 0.6f
                },
                Position = new Vector3(porta.X + Predio.Celula * 1.05f, 1.9f, porta.Z + 0.36f),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
            });
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

            MontarAcabamentoDasParedes(raiz, andar);
            MontarPortasInternas(raiz, andar);

            // luminárias mortas por sala; uma em cada quatro ainda pisca
            int n = 0;
            foreach (var sala in predio.Salas)
            {
                if (sala.Andar != andar) continue;

                var m = predio.ParaMundo(sala.Centro);
                var lum = Props.Criar(Peca.Luminaria, new Vector3(1.2f, 0.12f, 0.3f), _matTeto);
                lum.Position = new Vector3(m.X, Predio.PeDireito - 0.2f, m.Z);
                raiz.AddChild(lum);

                // Metade dos cômodos tem luz funcionando, e ela ILUMINA de
                // verdade. Estava em um a cada quatro, com energia 0,9: o
                // prédio inteiro era preto e só o facho existia, e nada do
                // acabamento que se pôs nas paredes aparecia. Um prédio sem
                // energia ainda tem luz de emergência acesa em parte dele —
                // e o escuro assusta mais quando há luz perto para comparar.
                if (n++ % 2 == 0)
                {
                    var luz = new OmniLight3D
                    {
                        LightCullMask = CamadaDoMundo,
                        LightColor = new Color(1f, 0.72f, 0.42f),
                        LightEnergy = 3.2f,
                        OmniRange = 16f,
                        OmniAttenuation = 1.1f,
                        ShadowEnabled = true,
                        DistanceFadeBegin = 26f,
                        DistanceFadeLength = 8f,
                        Position = new Vector3(m.X, Predio.PeDireito - 0.35f, m.Z)
                    };
                    // só uma em cada três pisca: piscar em todas vira discoteca
                    if (n % 3 == 0) luz.AddChild(new Piscar());
                    raiz.AddChild(luz);
                }
            }
        }

        /// <summary>
        /// Rodapé e meia-parede nas faces de parede voltadas para o cômodo.
        ///
        /// É a diferença entre um corredor de concreto e o corredor de um
        /// prédio. Parede lisa do chão ao teto não existe em lugar nenhum: há
        /// sempre rodapé embaixo, e quase sempre um friso na altura do peito
        /// separando dois acabamentos. Custa duas caixas por face e é o que
        /// mais rápido faz o lugar parecer construído por alguém.
        /// </summary>
        void MontarAcabamentoDasParedes(Node3D raiz, int andar)
        {
            var predio = _partida.Predio;
            float meio = Predio.Celula / 2f;

            // Alturas de marcenaria de verdade. O erro da primeira tentativa foi
            // pôr só uma LISTRINHA na parede: na referência o terço de baixo é
            // um painel inteiro, de outro material e outra cor, e é o contraste
            // entre os dois que faz o corredor parecer construído por alguém.
            const float alturaPainel = 1.10f;
            const float alturaRodape = 0.16f;

            var painelMat = Texturizado("reboco", new Color(0.66f, 0.66f, 0.63f), 0.65f)
                            ?? new StandardMaterial3D
                            {
                                AlbedoColor = new Color(0.30f, 0.29f, 0.27f), Roughness = 0.82f
                            };
            var rodapeMat = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.085f, 0.082f, 0.078f), Roughness = 0.86f
            };
            var frisoMat = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.20f, 0.19f, 0.175f), Roughness = 0.72f
            };

            for (int cz = 1; cz < predio.Profundidade - 1; cz++)
                for (int cx = 1; cx < predio.Largura - 1; cx++)
                {
                    if (!predio.EhParede(cx, cz, andar)) continue;

                    // uma face para cada vizinho que é chão: é a face que se vê
                    foreach (var (dx, dz) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                    {
                        if (predio.EhParede(cx + dx, cz + dz, andar)) continue;

                        // A face que dá para o pátio é FACHADA, não parede de
                        // cômodo: madeira e rodapé do lado de fora acabariam
                        // com a diferença entre entrar e não entrar.
                        var vizinha = predio.ParaMundo(new Celula(cx + dx, cz + dz, andar));
                        if (predio.SalaEm(vizinha, andar)?.Tipo == TipoSala.Patio) continue;

                        var m = predio.ParaMundo(new Celula(cx, cz, andar));
                        bool noEixoX = dx != 0;

                        Vector3 Chapa(float espessura, float altura) => noEixoX
                            ? new Vector3(espessura, altura, Predio.Celula)
                            : new Vector3(Predio.Celula, altura, espessura);

                        void Por(float espessura, float altura, float y, Material mat)
                        {
                            float fora = meio + espessura / 2f + 0.005f;
                            raiz.AddChild(new MeshInstance3D
                            {
                                Mesh = new BoxMesh { Size = Chapa(espessura, altura) },
                                MaterialOverride = mat,
                                Position = new Vector3(m.X + dx * fora, y, m.Z + dz * fora),
                                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
                            });
                        }

                        // o painel de baixo, que é a peça grande
                        Por(0.035f, alturaPainel, alturaPainel / 2f, painelMat);
                        // o friso que remata o painel, saliente
                        Por(0.075f, 0.075f, alturaPainel + 0.02f, frisoMat);
                        Por(0.055f, 0.035f, alturaPainel + 0.075f, frisoMat);
                        // e o rodapé, mais saliente ainda
                        Por(0.070f, alturaRodape, alturaRodape / 2f, rodapeMat);
                    }
                }
        }

        /// <summary>
        /// Batente e porta em cada vão entre cômodos.
        ///
        /// Sem isto os cômodos se ligam por buracos secos no concreto, e nenhuma
        /// quantidade de textura faz um buraco seco parecer a porta de um
        /// prédio. A folha fica encostada e aberta para um dos lados — porta
        /// fechada precisaria de colisão e de abrir, e o jogo não pede isso.
        /// </summary>
        void MontarPortasInternas(Node3D raiz, int andar)
        {
            var predio = _partida.Predio;

            var batenteMat = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.175f, 0.165f, 0.150f), Roughness = 0.78f
            };
            var folhaMat = Texturizado("metal", new Color(0.30f, 0.31f, 0.32f), 1.2f)
                           ?? new StandardMaterial3D
                           {
                               AlbedoColor = new Color(0.20f, 0.21f, 0.22f), Roughness = 0.6f
                           };

            const float alturaVao = 2.25f;
            const float larguraVao = 1.35f;

            foreach (var (celula, noEixoX) in predio.Vaos(andar))
            {
                // o poço da escada não é porta
                if (predio.EscadaEm(celula) != null) continue;

                var m = predio.ParaMundo(celula);
                var no = new Node3D { Position = new Vector3(m.X, 0, m.Z) };
                if (!noEixoX) no.RotateY(Mathf.Pi / 2);

                // Batente: duas ombreiras e a verga. As ombreiras ficam nas
                // pontas da célula, deixando o vão de 1,35 m no meio — largo o
                // bastante para você passar correndo sem enganchar.
                foreach (float lado in new[] { -1f, 1f })
                    no.AddChild(new MeshInstance3D
                    {
                        Mesh = new BoxMesh { Size = new Vector3(0.16f, alturaVao, 0.34f) },
                        MaterialOverride = batenteMat,
                        Position = new Vector3(lado * (larguraVao / 2 + 0.08f), alturaVao / 2, 0),
                        CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
                    });

                no.AddChild(new MeshInstance3D
                {
                    Mesh = new BoxMesh { Size = new Vector3(larguraVao + 0.32f, 0.18f, 0.34f) },
                    MaterialOverride = batenteMat,
                    Position = new Vector3(0, alturaVao + 0.09f, 0),
                    CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
                });

                // a folha, escancarada contra a parede, e a maçaneta
                var folha = new Node3D
                {
                    Position = new Vector3(-(larguraVao / 2 + 0.08f), 0, 0.14f)
                };
                folha.RotateY(-1.42f);
                folha.AddChild(new MeshInstance3D
                {
                    Mesh = new BoxMesh { Size = new Vector3(larguraVao * 0.92f, alturaVao - 0.06f, 0.05f) },
                    MaterialOverride = folhaMat,
                    Position = new Vector3(larguraVao * 0.46f, (alturaVao - 0.06f) / 2, 0),
                    CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
                });
                folha.AddChild(new MeshInstance3D
                {
                    Mesh = new CylinderMesh { TopRadius = 0.022f, BottomRadius = 0.022f, Height = 0.12f, RadialSegments = 8 },
                    MaterialOverride = batenteMat,
                    Position = new Vector3(larguraVao * 0.84f, 1.02f, 0.05f),
                    CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
                });
                no.AddChild(folha);

                raiz.AddChild(no);
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
                // a porta olha para dentro do comodo, e nao todas para o mesmo
                // lado: havia armario encostado de costas, com a porta no concreto
                no.RotateY(a.Giro);
                RaizDoAndar(a.Andar).AddChild(no);
                _armarios.Add(new NoArmario { Raiz = no, Porta = AcharPorNome(no, "Porta") });
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

            // pela altura, nao pela caixa: a envergadura dela esmagava a escala
            // e o bicho de 2,35 m aparecia com setenta centimetros
            // Uma cena por criatura. Eram uma só quando o jogo tinha uma só;
            // com três, desenhar uma e teleportá-la faria duas delas serem
            // invisíveis — e criatura invisível que te pega é bug, não susto.
            foreach (var bicho in _partida.Criaturas)
            {
                var no = Props.CriarComAltura(Peca.Criatura, Regras.AlturaDaCriatura, _matCriatura);
                AddChild(no);
                VestirCriatura(no);
                _criaturas.Add(new NoCriatura
                {
                    Raiz = no,
                    CoxaE = AcharPorNome(no, "CoxaE"),
                    CoxaD = AcharPorNome(no, "CoxaD"),
                    BracoE = AcharPorNome(no, "BracoE"),
                    BracoD = AcharPorNome(no, "BracoD"),
                    Tronco = AcharPorNome(no, "Tronco"),
                    Cabeca = AcharPorNome(no, "Cabeca"),
                    // fase de caminhada diferente em cada uma: três andando no
                    // mesmo compasso parecem um desfile
                    Passo = (float)GD.RandRange(0, 6.28)
                });
            }

            _criatura = _criaturas.Count > 0 ? _criaturas[0].Raiz : null;
            _animCriatura = _criatura != null ? AcharAnimador(_criatura) : null;
        }

        /// <summary>Uma criatura na cena, com os membros guardados para animar na mão.</summary>
        sealed class NoCriatura
        {
            public Node3D Raiz, CoxaE, CoxaD, BracoE, BracoD, Tronco, Cabeca;
            public float Passo;
        }
        readonly List<NoCriatura> _criaturas = new();

        /// <summary>
        /// Guarda os membros para animar na mão. O modelo montado em código não
        /// tem esqueleto nem AnimationPlayer, então a caminhada é feita aqui.
        /// Quando um modelo com rig aparecer em assets/modelos, estes ficam
        /// nulos e o AnimationPlayer dele assume.
        /// </summary>
        void GuardarMembros() { }   // os membros agora vêm por criatura, em NoCriatura

        /// <summary>
        /// A caminhada dela: pernas e braços em oposição, com o tronco
        /// balançando junto. É uma senoide, mas basta — o que faz o bicho
        /// parecer vivo é o passo bater com o quanto ele anda, e o ritmo vem
        /// da velocidade de verdade, não de um relógio solto.
        /// </summary>
        void AnimarNaMao(NoCriatura no, Criatura c, float dt)
        {
            float velocidade = c.Velocidade.Comprimento;
            no.Passo += dt * (0.9f + velocidade * 1.15f);

            // Modelo importado sem esqueleto: não há perna para mexer, então o
            // corpo inteiro rasteja — sobe e desce, rola de um lado para o
            // outro, e afunda um pouco quando acelera. É pouco, mas é a
            // diferença entre um bicho e um adesivo deslizando pelo chão.
            if (no.CoxaE == null)
            {
                float ritmo = no.Passo * 1.6f;
                no.Raiz.Position += new Vector3(0, Mathf.Sin(ritmo) * 0.045f, 0);
                no.Raiz.Rotation = new Vector3(
                    Mathf.Sin(ritmo * 0.5f) * 0.035f - velocidade * 0.02f,
                    no.Raiz.Rotation.Y,
                    Mathf.Sin(ritmo) * 0.06f);
                return;
            }

            float vel = velocidade;

            float balanco = Mathf.Sin(no.Passo) * Mathf.Min(0.85f, 0.14f + vel * 0.13f);
            float contra = -balanco;

            no.CoxaE.Rotation = new Vector3(balanco, 0, 0);
            no.CoxaD.Rotation = new Vector3(contra, 0, 0);
            // braços na contramão das pernas, e mais soltos
            no.BracoE.Rotation = new Vector3(contra * 1.25f, 0, 0.10f);
            no.BracoD.Rotation = new Vector3(balanco * 1.25f, 0, -0.10f);

            if (no.Tronco != null)
                no.Tronco.Rotation = new Vector3(0.18f + Mathf.Abs(balanco) * 0.10f,
                                                 Mathf.Sin(no.Passo) * 0.05f, 0);
            if (no.Cabeca != null)
                no.Cabeca.Rotation = new Vector3(0, Mathf.Sin(no.Passo * 0.5f) * 0.12f, 0);
        }

        /// <summary>
        /// Troca os materiais do modelo importado da criatura.
        ///
        /// O .mtl que veio com ele dá difuso BRANCO PURO na pele. Com o facho
        /// em cima, ela saía estourada, um vulto branco chapado no meio da
        /// sala — o oposto do que um bicho no escuro tem de ser. Aqui a pele
        /// vira escura (com a textura de pele, se houver) e só o que o artista
        /// marcou como olho continua aceso.
        ///
        /// Vai superfície por superfície, e não com MaterialOverride, porque o
        /// override substitui TODAS: apagaria os olhos junto com a pele.
        /// </summary>
        void VestirCriatura(Node3D raiz)
        {
            var pele = Texturizado("criatura", new Color(0.115f, 0.105f, 0.115f), 2.4f)
                       ?? new StandardMaterial3D
                       {
                           AlbedoColor = new Color(0.085f, 0.078f, 0.085f),
                           Roughness = 0.95f,
                           SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled
                       };

            var olho = new StandardMaterial3D
            {
                AlbedoColor = new Color(1f, 0.46f, 0.08f),
                EmissionEnabled = true,
                Emission = new Color(1f, 0.42f, 0.06f),
                EmissionEnergyMultiplier = 3.2f,
                Roughness = 0.35f
            };

            foreach (var n in TodosOsNos(raiz))
            {
                if (n is not MeshInstance3D mi || mi.Mesh == null) continue;

                for (int s = 0; s < mi.Mesh.GetSurfaceCount(); s++)
                {
                    string nome = mi.Mesh.SurfaceGetMaterial(s)?.ResourceName ?? "";
                    bool ehOlho = nome.Contains("eye", System.StringComparison.OrdinalIgnoreCase)
                               || nome.Contains("glow", System.StringComparison.OrdinalIgnoreCase)
                               || nome.Contains("olho", System.StringComparison.OrdinalIgnoreCase);
                    mi.SetSurfaceOverrideMaterial(s, ehOlho ? olho : pele);
                }
            }
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

        /// <summary>O nó de cena de um adorno, achado pela posição. Só o diagnóstico usa.</summary>
        Node3D AcharNoDoAdorno(Adorno a)
        {
            if (!_raizPorAndar.TryGetValue(a.Andar, out var raiz)) return null;
            foreach (var filho in raiz.GetChildren())
                if (filho is Node3D n &&
                    Mathf.Abs(n.Position.X - a.Pos.X) < 0.02f &&
                    Mathf.Abs(n.Position.Z - a.Pos.Z) < 0.02f) return n;
            return null;
        }

        static Aabb MedirNo(Node3D raiz)
        {
            var total = new Aabb();
            bool primeiro = true;
            foreach (var n in TodosOsNos(raiz))
            {
                if (n is not VisualInstance3D v) continue;
                var c = v.GetAabb();
                // leva a escala do nó em conta, senão mede o modelo cru
                c = new Aabb(c.Position * v.Scale, c.Size * v.Scale);
                if (primeiro) { total = c; primeiro = false; } else total = total.Merge(c);
            }
            return total;
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
                    if (_verArmario)
                    {
                        // entra num armario e fica la: e de dentro dele que a
                        // tela estava quebrada
                        // entra no armario e fica la: e de dentro dele que a tela
                        // estava quebrada
                        var arm = _partida.Armarios[0];
                        if (!_partida.Jogador.Escondido)
                        {
                            _partida.Jogador.Pos = arm.Pos;
                            _partida.Jogador.Andar = arm.Andar;
                            _partida.Interagir();
                        }
                        _giro = arm.Giro + Mathf.Pi;
                        _inclinacao = -0.02f;
                    }
                    else if (_verCriatura)
                    {
                        // Encara ela de frente numa sala, os dois parados. Sem
                        // fixar os dois, a simulação a levava embora e a foto
                        // saía de uma parede vazia.
                        var sala = _partida.Predio.Salas[0];
                        foreach (var s in _partida.Predio.Salas)
                            if (s.Tipo == TipoSala.Comum && s.Larg * s.Alt > sala.Larg * sala.Alt) sala = s;

                        var centro = _partida.Predio.ParaMundo(sala.Centro);
                        _partida.Jogador.Andar = sala.Andar;
                        _partida.Jogador.Pos = new P2(centro.X, centro.Z + 5.5f);
                        _partida.Ela.Andar = sala.Andar;
                        _partida.Ela.Pos = centro;
                        _giro = 0; _inclinacao = -0.02f;
                    }
                    else if (_verAndarDeCima)
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
            SincronizarArmarios(dt);
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
                if (!_verRecipiente && !_verAndarDeCima && !_verEscada && !_verCriatura && !_verArmario)
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
                if (_quadrosDeFoto == 320 && _verArmario)
                {
                    var a0 = _partida.Armarios[0];
                    GD.Print($"armario0 pos={a0.Pos} andar={a0.Andar} giro={a0.Giro:0.00}");
                    GD.Print($"jogador pos={_partida.Jogador.Pos} andar={_partida.Jogador.Andar} escondido={_partida.Jogador.Escondido}");
                    GD.Print($"camera={_camera.GlobalPosition} giro={_giro:0.00} frenteCam={-_camera.GlobalTransform.Basis.Z}");
                    GD.Print($"armarios na cena={_armarios.Count} porta0={(_armarios.Count > 0 && _armarios[0].Porta != null)}");
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
                    _andarNoMapa = _verMapaDeCima ? 1 : _partida.Jogador.Andar;
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
            var olho = new Vector3(j.Pos.X, pisoDoAndar + j.AlturaOlho + sobe + tremor, j.Pos.Z);

            // Escondido, a camera encosta ATRAS DA PORTA, não no centro da
            // caixa. No centro você ficava dentro da chapa do fundo e do
            // corpo do móvel; encostado na porta você espia pelas venezianas,
            // que é o que se faz dentro de um armário.
            if (j.Escondido)
            {
                var arm = ArmarioMaisPerto(j);
                if (arm != null)
                {
                    var frente = arm.Frente;
                    olho = new Vector3(arm.Pos.X + frente.X * 0.09f,
                                       Predio.AlturaDoAndar(arm.Andar) + j.AlturaOlho,
                                       arm.Pos.Z + frente.Z * 0.09f);
                }
            }
            _camera.Position = olho;
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
                _mao.Position = _poseDaMao + new Vector3(_balancoDaMao.X, _balancoDaMao.Y, 0f);
                _mao.RotationDegrees = _giroDaMao
                                     + new Vector3(_balancoDaMao.Y * 143f, -_balancoDaMao.X * 172f, 0f);
                _mao.Visible = !j.Escondido;
                if (_matVidro != null)
                    _matVidro.EmissionEnergyMultiplier = j.LuzAcesa ? 1.4f * Mathf.Clamp(j.Bateria * 3f, 0.3f, 1f) : 0f;
                // a luz de preenchimento da mao cai junto com a lanterna, mas
                // nunca zera: no escuro voce ainda ve a propria mao, de relance
                if (_luzDaMao != null)
                    _luzDaMao.LightEnergy = j.LuzAcesa ? 0.58f : 0.14f;
            }
            // a luz fraqueja junto com a bateria: avisa antes de apagar
            if (!_forcarLuz)
                _lanterna.LightEnergy = EnergiaDaLanterna * Mathf.Clamp(j.Bateria * 3f, 0.25f, 1f);
            _camera.Fov = j.Correndo ? 80 : 74;
        }

        /// <summary>O armário em que o jogador está enfiado, ou null.</summary>
        Armario ArmarioMaisPerto(Jogador j)
        {
            Armario melhor = null;
            float md = 1.2f;
            foreach (var a in _partida.Armarios)
            {
                if (a.Andar != j.Andar) continue;
                float d = P2.Distancia(a.Pos, j.Pos);
                if (d >= md) continue;
                md = d; melhor = a;
            }
            return melhor;
        }

        /// <summary>
        /// Abre a porta do armário em que você entrou, e fecha as outras.
        /// A porta gira até quase encostada: escondido, o que se vê é a fresta
        /// e as venezianas — porta escancarada não esconde ninguém.
        /// </summary>
        void SincronizarArmarios(float dt)
        {
            var j = _partida.Jogador;
            var dentro = j.Escondido ? ArmarioMaisPerto(j) : null;

            for (int i = 0; i < _armarios.Count; i++)
            {
                var no = _armarios[i];
                if (no.Porta == null) continue;

                bool esteAberto = dentro != null && i < _partida.Armarios.Count
                                  && ReferenceEquals(_partida.Armarios[i], dentro);

                float alvo = esteAberto ? 1f : 0f;
                if (Mathf.IsEqualApprox(no.Abertura, alvo)) continue;

                no.Abertura = Mathf.MoveToward(no.Abertura, alvo, dt * 3.2f);
                // mal encostada, nao escancarada: a porta aberta nao esconde
                // ninguem, e o que deixa voce ver e a grelha, nao o vao dela
                no.Porta.Rotation = new Vector3(0, -no.Abertura * 0.13f, 0);
            }
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
            int quantas = Mathf.Min(_criaturas.Count, _partida.Criaturas.Count);
            for (int i = 0; i < quantas; i++)
                SincronizarUma(_criaturas[i], _partida.Criaturas[i], dt);

            // a animação importada, quando houver, segue a primeira
            if (_animCriatura != null && _partida.Criaturas.Count > 0)
            {
                var ela = _partida.Ela;
                float ritmo = ela.Velocidade.Comprimento;
                Animar(ela.Estado == EstadoCriatura.Caca || ritmo > 4f ? "Run"
                     : ritmo > 0.4f ? "Walk"
                     : "Idle");
            }
        }

        void SincronizarUma(NoCriatura no, Criatura c, float dt)
        {
            no.Raiz.Position = new Vector3(c.Pos.X, Predio.AlturaDoAndar(c.Andar), c.Pos.Z);

            var dir = c.Direcao;
            no.Raiz.Rotation = new Vector3(0, Mathf.Atan2(dir.X, dir.Z) + GiroDoModelo, 0);

            // As DUAS condições, numa linha só. Estavam em duas atribuições
            // separadas e a segunda apagava a primeira: a criatura voltava a
            // ser desenhada através da laje sempre que passasse a menos de
            // quarenta e cinco metros, um andar abaixo de você.
            no.Raiz.Visible = c.Andar == _partida.Jogador.Andar
                           && P2.Distancia(c.Pos, _partida.Jogador.Pos) < 45f;

            // sem esqueleto importado, a caminhada é feita aqui na mão
            AnimarNaMao(no, c, dt);
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
            Checar($"criaturas na cena ({_criaturas.Count})",
                   _criaturas.Count == _partida.Criaturas.Count
                   && _criaturas.Count == Regras.QuantidadeDeCriaturas);
            Checar("cada criatura tem o proprio no",
                   _criaturas.TrueForAll(n => n.Raiz != null)
                   && _criaturas.Select(n => n.Raiz).Distinct().Count() == _criaturas.Count);
            // A criatura anda de dois jeitos: pelo AnimationPlayer de um modelo
            // importado, ou pelos membros que o modelo em código expõe. O que
            // não pode é nenhum dos dois — aí ela desliza pelo chão sem mexer
            // uma perna, que é o que ela fazia antes.
            bool comRig = _animCriatura != null
                          && _animCriatura.HasAnimation("Walk") && _animCriatura.HasAnimation("Run");
            bool comMembros = _criaturas.Count > 0 && _criaturas.TrueForAll(
                n => n.CoxaE != null && n.CoxaD != null && n.BracoE != null && n.BracoD != null);
            // modelo importado sem rig: o corpo inteiro rasteja
            bool comCorpo = _criaturas.Count > 0 && _criaturas.TrueForAll(n => n.Raiz != null);

            Checar(comRig ? "animacao: esqueleto do modelo (" +
                            string.Join(", ", _animCriatura.GetAnimationList()) + ")"
                 : comMembros ? "animacao: membros montados em codigo"
                 : comCorpo ? "animacao: o corpo inteiro rasteja (modelo sem rig)"
                 : "a criatura nao tem como andar",
                comRig || comMembros || comCorpo);

            // roda 20 s de partida sem jogador para ver a simulação andar
            var antes = _partida.Ela.Pos;
            for (int i = 0; i < 60 * 20; i++) _partida.Passo(1f / 60f, new Comando());
            Checar($"criatura andou {P2.Distancia(antes, _partida.Ela.Pos):0.0} m",
                   P2.Distancia(antes, _partida.Ela.Pos) > 5f);
            Checar("posições finitas",
                   !float.IsNaN(_partida.Ela.Pos.X) && !float.IsNaN(_partida.Jogador.Pos.X));

            // Quem e grande na tela e pequeno na colisao: e isso que faz o
            // jogador atravessar a metade de um movel e chamar de bug. Roda
            // sempre, para a proxima peca que entrar torta ser reprovada aqui
            // e nao na mao de quem joga.
            var tortos = new List<string>();
            foreach (var a in _partida.Adornos)
            {
                if (!a.Solido) continue;
                var no = AcharNoDoAdorno(a);
                if (no == null) continue;
                float meiaLargura = Mathf.Max(MedirNo(no).Size.X, MedirNo(no).Size.Z) / 2f;
                if (meiaLargura > a.Raio + 0.25f) tortos.Add(a.Tipo.ToString());
            }
            Checar(tortos.Count == 0
                ? "todo movel colide do tamanho que aparece"
                : "movel maior na tela do que na colisao: " + string.Join(", ", tortos.Distinct()),
                tortos.Count == 0);

            // A mão. Ela fica em quadro do primeiro ao último segundo de
            // partida, e já passou meia hora aberta e espalmada porque
            // `SetBonePoseRotation` guarda o ângulo sem mover osso nenhum: os
            // getters respondiam certo e a tela mostrava o contrário. Daqui em
            // diante quem responde é a distância entre a ponta do dedo posada
            // e a ponta do dedo em repouso — que é o que a tela mostra.
            if (_ossosDasMaos != null)
            {
                int ponta = _ossosDasMaos.FindBone("index3_R");
                int punho = _ossosDasMaos.FindBone("hand_R");
                // medido DENTRO da mão: contra o repouso do esqueleto inteiro,
                // girar o ombro já passava no teste com os dedos esticados
                float fechou = ponta < 0 || punho < 0 ? 0f
                    : (_ossosDasMaos.GetBoneGlobalPose(punho).AffineInverse()
                       * _ossosDasMaos.GetBoneGlobalPose(ponta).Origin)
                      .DistanceTo(_ossosDasMaos.GetBoneGlobalRest(punho).AffineInverse()
                       * _ossosDasMaos.GetBoneGlobalRest(ponta).Origin);
                Checar($"maos: dedos fecharam ({fechou * 100f:0} cm da pose de repouso)", fechou > 0.03f);
                Checar("maos: lanterna pendurada no punho",
                       _vidroDaLanterna != null
                       && PrimeiroDoTipo<BoneAttachment3D>(_mao) != null);
            }
            else Checar("maos: modelo em codigo (sem esqueleto)", _vidroDaLanterna != null);

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

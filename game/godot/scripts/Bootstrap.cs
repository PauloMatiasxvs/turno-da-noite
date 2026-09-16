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
        AnimationPlayer _animCriatura;
        string _clipeAtual = "";
        /// <summary>Se o modelo do artista olha para +Z em vez de -Z, isto vira Pi.</summary>
        const float GiroDoModelo = Mathf.Pi;

        readonly List<Node3D> _fusiveis = new();
        readonly List<Node3D> _baterias = new();

        StandardMaterial3D _matParede, _matPiso, _matTeto, _matArmario,
                           _matQuadro, _matFusivel, _matBateria, _matCriatura, _matPortao;

        float _giro, _inclinacao;
        bool _mouseCapturado;
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

            CapturarMouse(!_autoTeste);
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
                TonemapMode = Godot.Environment.ToneMapper.Filmic,
                TonemapExposure = 1.8f,
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
                LightEnergy = 32f,
                SpotRange = 30f,
                SpotAngle = 34f,
                SpotAngleAttenuation = 1.4f,
                SpotAttenuation = 0.55f,
                ShadowEnabled = !_semSombra,
                ShadowBias = 0.06f,
                ShadowNormalBias = 2.0f
            };
            _camera.AddChild(_lanterna);

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

        void MontarPredio()
        {
            var raiz = new Node3D { Name = "Predio" };
            AddChild(raiz);

            float lado = Mathf.Max(_partida.Predio.Largura, _partida.Predio.Profundidade) * Predio.Celula + 12f;

            var piso = Props.Criar(Peca.Piso, new Vector3(lado, 0.1f, lado), _matPiso);
            raiz.AddChild(piso);

            var teto = Props.Criar(Peca.Teto, new Vector3(lado, 0.1f, lado), _matTeto);
            teto.Position = new Vector3(0, Predio.PeDireito, 0);
            teto.RotateZ(Mathf.Pi);   // vira a face para baixo
            raiz.AddChild(teto);

            foreach (var (centro, largura) in _partida.Predio.BlocosDeParede())
            {
                var parede = Props.Criar(Peca.Parede,
                    new Vector3(largura, Predio.PeDireito, Predio.Celula), _matParede);
                parede.Position = new Vector3(centro.X, 0, centro.Z);
                raiz.AddChild(parede);
            }

            // luminárias mortas por sala; uma em cada quatro ainda pisca
            int n = 0;
            foreach (var sala in _partida.Predio.Salas)
            {
                var m = _partida.Predio.ParaMundo(sala.Centro);
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
                _raizArmarios.AddChild(no);
            }

            var quadro = Props.Criar(Peca.QuadroEletrico, new Vector3(1.5f, 1.9f, 0.42f), _matQuadro);
            quadro.Position = new Vector3(_partida.Quadro.X, 0.35f, _partida.Quadro.Z);
            AddChild(quadro);

            var portao = Props.Criar(Peca.Portao, new Vector3(2.4f, 3.2f, 0.3f), _matPortao);
            portao.Position = new Vector3(_partida.Portao.X, 0, _partida.Portao.Z);
            portao.Name = "Portao";
            AddChild(portao);

            foreach (var f in _partida.Fusiveis)
            {
                var no = Props.Criar(Peca.Fusivel, new Vector3(0.2f, 0.34f, 0.2f), _matFusivel);
                no.Position = new Vector3(f.Pos.X, 0.45f, f.Pos.Z);
                _raizItens.AddChild(no);
                _fusiveis.Add(no);
            }
            foreach (var b in _partida.Baterias)
            {
                var no = Props.Criar(Peca.Bateria, new Vector3(0.16f, 0.26f, 0.16f), _matBateria);
                no.Position = new Vector3(b.Pos.X, 0.35f, b.Pos.Z);
                _raizItens.AddChild(no);
                _baterias.Add(no);
            }

            _criatura = Props.Criar(Peca.Criatura, new Vector3(0.7f, 2.35f, 0.5f), _matCriatura);
            AddChild(_criatura);
            _animCriatura = AcharAnimador(_criatura);
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

        public override void _UnhandledInput(InputEvent e)
        {
            if (e is InputEventMouseMotion mm && _mouseCapturado)
            {
                _giro -= mm.Relative.X * Opcoes.Sensibilidade;
                _inclinacao = Mathf.Clamp(_inclinacao - mm.Relative.Y * Opcoes.Sensibilidade * (Opcoes.InverterY ? -1 : 1), -1.4f, 1.4f);
            }
            else if (e is InputEventKey k && k.Pressed && !k.Echo)
            {
                switch (k.Keycode)
                {
                    case Key.Escape: CapturarMouse(!_mouseCapturado); break;
                    case Key.E: _interagirPedido = true; break;
                    case Key.F: _lanternaPedida = true; break;
                    case Key.Minus: Opcoes.AjustarSensibilidade(-1); break;
                    case Key.Equal: Opcoes.AjustarSensibilidade(1); break;
                    case Key.F5: if (_partida.Fase != Fase.Jogando) GetTree().ReloadCurrentScene(); break;
                }
            }
            else if (e is InputEventMouseButton mb && mb.Pressed && !_mouseCapturado)
                CapturarMouse(true);
        }

        Comando LerComando()
        {
            float fx = 0, fz = 0;
            if (Input.IsKeyPressed(Key.W)) fz -= 1;
            if (Input.IsKeyPressed(Key.S)) fz += 1;
            if (Input.IsKeyPressed(Key.D)) fx += 1;
            if (Input.IsKeyPressed(Key.A)) fx -= 1;

            // leva o eixo local para o mundo girando pelo yaw
            float sin = Mathf.Sin(_giro), cos = Mathf.Cos(_giro);
            var cmd = new Comando
            {
                FrenteX = fx * cos - fz * sin,
                FrenteZ = -fx * sin + fz * cos,
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
                _partida.Passo(1f / 60f, new Comando());
                _giro += dt * 0.35f;
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
                if (_quadrosDeFoto == 330) TirarFoto("res://captura_2.png");
                if (_quadrosDeFoto >= 340) GetTree().Quit();
                return;
            }

            if (_partida.Fase == Fase.Jogando)
            {
                _partida.Passo(dt, LerComando());
                foreach (var ev in _partida.Eventos) Som.Tocar(this, ev, _partida);
            }

            SincronizarCamera(dt);
            SincronizarItens(dt);
            SincronizarCriatura(dt);
            _hud.Atualizar(_partida);
        }

        void SincronizarCamera(float dt)
        {
            var j = _partida.Jogador;

            float alvo = j.Correndo ? 0.055f : 0.028f;
            _balanco += dt * (j.Correndo ? 9f : 5f);
            float sobe = Mathf.Sin(_balanco) * alvo;
            float tremor = j.Medo > 0.55f ? (GD.Randf() - 0.5f) * (j.Medo - 0.55f) * 0.05f : 0f;

            _camera.Position = new Vector3(j.Pos.X, j.AlturaOlho + sobe + tremor, j.Pos.Z);
            _camera.Rotation = new Vector3(_inclinacao, _giro, Mathf.Cos(_balanco * 0.5f) * 0.006f);

            _lanterna.Visible = j.LuzAcesa;
            // a luz fraqueja junto com a bateria: avisa antes de apagar
            if (!_forcarLuz) _lanterna.LightEnergy = 32f * Mathf.Clamp(j.Bateria * 3f, 0.25f, 1f);
            _camera.Fov = j.Correndo ? 80 : 74;
        }

        void SincronizarItens(float dt)
        {
            float t = _partida.Tempo;
            for (int i = 0; i < _fusiveis.Count; i++)
            {
                _fusiveis[i].Visible = !_partida.Fusiveis[i].Recolhido;
                _fusiveis[i].Position = new Vector3(_partida.Fusiveis[i].Pos.X,
                    0.45f + Mathf.Sin(t * 1.6f + i) * 0.06f, _partida.Fusiveis[i].Pos.Z);
                _fusiveis[i].RotateY(dt * 0.9f);
            }
            for (int i = 0; i < _baterias.Count; i++)
            {
                _baterias[i].Visible = !_partida.Baterias[i].Recolhido;
                _baterias[i].RotateY(dt * 0.6f);
            }

            var portao = GetNodeOrNull<Node3D>("Portao");
            if (portao != null && _partida.PortaoAberto)
                portao.Position = new Vector3(_partida.Portao.X, -2.6f, _partida.Portao.Z);  // sobe a grade
        }

        void SincronizarCriatura(float dt)
        {
            var ela = _partida.Ela;
            _criatura.Position = new Vector3(ela.Pos.X, 0, ela.Pos.Z);
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

using System.Collections.Generic;
using Godot;
using NeonArena.Core;

namespace NeonArena.GodotGame
{
    /// <summary>
    /// Ponte entre o Godot e a simulação. Cada quadro: lê o teclado, entrega um
    /// InputFrame para o Sim, e depois copia o estado resultante para os nós 3D.
    /// Nenhuma regra de jogo mora aqui — se algo parece errado, o bug está no Core
    /// (e aí existe um teste para escrever).
    /// </summary>
    public partial class Bootstrap : Node3D
    {
        Sim _sim;
        Camera3D _camera;
        Hud _hud;
        Node3D _enemyRoot, _boltRoot, _pickupRoot;

        readonly List<MeshInstance3D> _enemyPool = new List<MeshInstance3D>();
        readonly List<MeshInstance3D> _boltPool = new List<MeshInstance3D>();
        readonly List<MeshInstance3D> _pickupPool = new List<MeshInstance3D>();

        StandardMaterial3D _droneMat, _runnerMat, _bruteMat, _boltMat, _healthMat, _ammoMat, _tracerMat;

        float _yaw, _pitch;
        bool _mouseCaptured;
        int _switchRequest = -1;
        double _tracerTimeout;
        MeshInstance3D _tracer;

        // modo de verificação: godot --headless --path godot -- --selftest
        bool _selfTest;
        int _selfTestFrames;

        const float MouseSensitivity = 0.0022f;

        public override void _Ready()
        {
            foreach (string arg in OS.GetCmdlineUserArgs())
                if (arg == "--selftest") _selfTest = true;

            SceneBuilder.BuildEnvironment(this);
            SceneBuilder.BuildFloor(this);
            SceneBuilder.BuildArena(this);

            _droneMat = SceneBuilder.Glow(SceneBuilder.Cyan, 1.6f);
            _runnerMat = SceneBuilder.Glow(SceneBuilder.Magenta, 1.6f);
            _bruteMat = SceneBuilder.Glow(SceneBuilder.Amber, 1.4f);
            _boltMat = SceneBuilder.Glow(new Color(0.55f, 1f, 1f), 3.0f);
            _healthMat = SceneBuilder.Glow(new Color(0.25f, 1f, 0.6f), 2.0f);
            _ammoMat = SceneBuilder.Glow(SceneBuilder.Amber, 2.0f);
            _tracerMat = SceneBuilder.Glow(new Color(1f, 0.85f, 0.45f), 3.5f);

            _enemyRoot = new Node3D(); AddChild(_enemyRoot);
            _boltRoot = new Node3D(); AddChild(_boltRoot);
            _pickupRoot = new Node3D(); AddChild(_pickupRoot);

            _camera = new Camera3D { Fov = 72f, Current = true, Near = 0.05f, Far = 400f };
            AddChild(_camera);

            _tracer = new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(0.04f, 0.04f, 1f) },
                MaterialOverride = _tracerMat,
                Visible = false
            };
            AddChild(_tracer);

            _hud = new Hud();
            AddChild(_hud);

            _sim = new Sim(_selfTest ? 4242 : (int)(Time.GetUnixTimeFromSystem() % 100000));
            _sim.Start();

            CaptureMouse(!_selfTest);
        }

        /// <summary>
        /// Roda alguns segundos sem jogador e confere que a partida realmente andou:
        /// a onda começou, inimigos nasceram e o dano chegou. Sai com código 1 se
        /// algo não bateu, para servir de teste de fumaça em automação.
        /// </summary>
        void RunSelfTest(float dt)
        {
            _selfTestFrames++;
            if (_selfTestFrames < 900) return;   // ~15 s a 60 fps

            bool ok = true;
            void Check(string what, bool condition)
            {
                GD.Print($"  [{(condition ? "ok  " : "FALHA")}] {what}");
                if (!condition) ok = false;
            }

            GD.Print("auto-teste do Godot:");
            Check($"onda começou (wave = {_sim.Wave})", _sim.Wave >= 1);
            Check($"inimigos nasceram ({_sim.Enemies.Count} vivos)", _sim.Enemies.Count > 0);
            Check($"jogador tomou dano (hp = {_sim.Player.Hp:0})", _sim.Player.Hp < _sim.Player.MaxHp);
            Check($"nós de inimigo criados ({_enemyPool.Count})", _enemyPool.Count > 0);
            Check("posição do jogador é finita", !float.IsNaN(_sim.Player.Pos.X) && !float.IsNaN(_sim.Player.Pos.Z));
            Check($"tempo avançou ({_sim.Time:0.0} s)", _sim.Time > 10f);

            GD.Print(ok ? "auto-teste: TUDO CERTO" : "auto-teste: FALHOU");
            GetTree().Quit(ok ? 0 : 1);
        }

        void CaptureMouse(bool capture)
        {
            _mouseCaptured = capture;
            Input.MouseMode = capture ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is InputEventMouseMotion motion && _mouseCaptured)
            {
                _yaw -= motion.Relative.X * MouseSensitivity;
                _pitch -= motion.Relative.Y * MouseSensitivity;
                _pitch = Mathf.Clamp(_pitch, -1.45f, 1.45f);
            }
            else if (@event is InputEventKey key && key.Pressed && !key.Echo)
            {
                if (key.Keycode == Key.Escape) CaptureMouse(!_mouseCaptured);
                if (key.Keycode == Key.Key1) _switchRequest = 0;
                if (key.Keycode == Key.Key2) _switchRequest = 1;
                if (key.Keycode == Key.R) _sim.StartReload();
                if (key.Keycode == Key.F5 && _sim.Phase == SimPhase.Dead) _sim.Start();
            }
            else if (@event is InputEventMouseButton click && click.Pressed && !_mouseCaptured)
            {
                CaptureMouse(true);
            }
        }

        public override void _Process(double delta)
        {
            float dt = (float)delta;

            if (_sim.Phase == SimPhase.Playing)
            {
                // no auto-teste o passo é fixo: o resultado não pode depender da máquina
                _sim.Step(_selfTest ? 1f / 60f : dt, _selfTest ? new InputFrame { SwitchWeapon = -1 } : ReadInput());
                if (!_selfTest)
                {
                    foreach (var ev in _sim.Events) Sfx.Play(this, ev);
                    if (_sim.Events.Contains(SimEvent.Shot)) ShowTracer();
                }
            }

            if (_selfTest) { SyncEnemies(); RunSelfTest(dt); return; }

            SyncCamera();
            SyncEnemies();
            SyncBolts();
            SyncPickups();

            _tracerTimeout -= delta;
            if (_tracerTimeout <= 0) _tracer.Visible = false;

            _hud.Refresh(_sim);
        }

        InputFrame ReadInput()
        {
            // eixos no referencial do jogador: frente é -Z girado por yaw
            float fx = 0f, fz = 0f;
            if (Input.IsKeyPressed(Key.W)) fz -= 1f;
            if (Input.IsKeyPressed(Key.S)) fz += 1f;
            if (Input.IsKeyPressed(Key.D)) fx += 1f;
            if (Input.IsKeyPressed(Key.A)) fx -= 1f;

            float sin = Mathf.Sin(_yaw), cos = Mathf.Cos(_yaw);
            // sinal do segundo termo estava trocado e espelhava o movimento:
            // W andava para tras olhando para leste ou oeste. A versao testada
            // desta conta esta em game/core/Direcao.cs
            float worldX = fx * cos + fz * sin;
            float worldZ = -fx * sin + fz * cos;

            var input = new InputFrame
            {
                MoveX = worldX,
                MoveZ = worldZ,
                Yaw = _yaw,
                Pitch = _pitch,
                Jump = Input.IsKeyPressed(Key.Space),
                Sprint = Input.IsKeyPressed(Key.Shift),
                Fire = _mouseCaptured && Input.IsMouseButtonPressed(MouseButton.Left),
                Reload = false,
                SwitchWeapon = _switchRequest
            };
            _switchRequest = -1;
            return input;
        }

        void SyncCamera()
        {
            Vec3 eye = _sim.Player.EyePos;
            _camera.Position = new Vector3(eye.X, eye.Y, eye.Z);
            _camera.Rotation = new Vector3(_pitch, _yaw, 0f);
        }

        void ShowTracer()
        {
            Vec3 from = _sim.Player.EyePos, dir = _sim.Player.LookDir;
            Vec3 to = from + dir * 60f;
            var a = new Vector3(from.X, from.Y, from.Z);
            var b = new Vector3(to.X, to.Y, to.Z);

            _tracer.Position = (a + b) * 0.5f;
            _tracer.LookAt(b, Vector3.Up);
            _tracer.Scale = new Vector3(1, 1, a.DistanceTo(b));
            _tracer.Visible = true;
            _tracerTimeout = 0.04;
        }

        /// <summary>
        /// Reaproveita nós em vez de criar e destruir: instanciar por quadro é o
        /// caminho mais curto para engasgar o coletor de lixo.
        /// </summary>
        static MeshInstance3D Grow(List<MeshInstance3D> pool, Node3D parent, Mesh mesh, Material mat)
        {
            var node = new MeshInstance3D { Mesh = mesh, MaterialOverride = mat };
            parent.AddChild(node);
            pool.Add(node);
            return node;
        }

        void SyncEnemies()
        {
            var list = _sim.Enemies;
            for (int i = 0; i < list.Count; i++)
            {
                if (i >= _enemyPool.Count)
                    Grow(_enemyPool, _enemyRoot, new SphereMesh { Radius = 0.5f, Height = 1f }, _droneMat);

                var node = _enemyPool[i];
                var e = list[i];
                float scale = e.Radius * 2f * Mathf.Min(1f, e.Birth * 1.4f + 0.3f);

                node.Position = new Vector3(e.Pos.X, e.Pos.Y, e.Pos.Z);
                node.Scale = new Vector3(scale, scale, scale);
                node.MaterialOverride = e.Kind == EnemyKind.Drone ? _droneMat
                                      : e.Kind == EnemyKind.Runner ? _runnerMat : _bruteMat;
                node.Visible = true;
            }
            for (int i = list.Count; i < _enemyPool.Count; i++) _enemyPool[i].Visible = false;
        }

        void SyncBolts()
        {
            var list = _sim.Bolts;
            for (int i = 0; i < list.Count; i++)
            {
                if (i >= _boltPool.Count)
                    Grow(_boltPool, _boltRoot, new SphereMesh { Radius = 0.17f, Height = 0.34f }, _boltMat);

                _boltPool[i].Position = new Vector3(list[i].Pos.X, list[i].Pos.Y, list[i].Pos.Z);
                _boltPool[i].Visible = true;
            }
            for (int i = list.Count; i < _boltPool.Count; i++) _boltPool[i].Visible = false;
        }

        void SyncPickups()
        {
            var list = _sim.Pickups;
            for (int i = 0; i < list.Count; i++)
            {
                if (i >= _pickupPool.Count)
                    Grow(_pickupPool, _pickupRoot, new BoxMesh { Size = new Vector3(0.42f, 0.42f, 0.42f) }, _healthMat);

                var node = _pickupPool[i];
                var p = list[i];
                float bob = Mathf.Sin((float)_sim.Time * 2.6f + i) * 0.18f;
                node.Position = new Vector3(p.Pos.X, p.Pos.Y + bob, p.Pos.Z);
                node.Rotation = new Vector3(0, (float)_sim.Time * 1.7f, 0);
                node.MaterialOverride = p.IsHealth ? _healthMat : _ammoMat;
                node.Visible = true;
            }
            for (int i = list.Count; i < _pickupPool.Count; i++) _pickupPool[i].Visible = false;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using NeonArena.Core;

namespace NeonArena.Unity
{
    /// <summary>
    /// Único script que você precisa colocar na cena.
    /// Monte assim: cena vazia -> GameObject vazio -> adicione este componente -> Play.
    /// Ele cria câmera, luzes, arena, HUD e roda a simulação. Não há prefab
    /// nem nada para arrastar no inspetor, de propósito: cena montada à mão
    /// quebra em silêncio, código não.
    /// </summary>
    [AddComponentMenu("Neon Arena/Game Bootstrap")]
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Mira")]
        [Tooltip("Sensibilidade do mouse, em radianos por pixel.")]
        public float mouseSensitivity = 0.0022f;

        [Tooltip("Semente do sorteio. Deixe 0 para partida diferente a cada vez.")]
        public int seed = 0;

        Sim _sim;
        Camera _camera;
        HudOverlay _hud;
        Transform _enemyRoot, _boltRoot, _pickupRoot;

        readonly List<Transform> _enemyPool = new List<Transform>();
        readonly List<Transform> _boltPool = new List<Transform>();
        readonly List<Transform> _pickupPool = new List<Transform>();

        Material _wallMat, _pillarMat, _crateMat;
        Material _droneMat, _runnerMat, _bruteMat, _boltMat, _healthMat, _ammoMat;

        float _yaw, _pitch;
        int _switchRequest = -1;

        static readonly Color Cyan = new Color(0.20f, 0.85f, 1.00f);
        static readonly Color Magenta = new Color(1.00f, 0.18f, 0.53f);
        static readonly Color Amber = new Color(1.00f, 0.69f, 0.23f);
        static readonly Color FogColor = new Color(0.035f, 0.05f, 0.09f);

        void Start()
        {
            BuildMaterials();
            BuildEnvironment();
            BuildArena();

            _enemyRoot = new GameObject("Enemies").transform;
            _boltRoot = new GameObject("Bolts").transform;
            _pickupRoot = new GameObject("Pickups").transform;

            _hud = gameObject.AddComponent<HudOverlay>();

            _sim = new Sim(seed != 0 ? seed : Random.Range(1, 999999));
            _sim.Start();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void BuildMaterials()
        {
            _wallMat = Solid(new Color(0.17f, 0.21f, 0.31f));
            _pillarMat = Solid(new Color(0.21f, 0.25f, 0.36f));
            _crateMat = Solid(new Color(0.26f, 0.30f, 0.40f));
            _droneMat = Glow(Cyan);
            _runnerMat = Glow(Magenta);
            _bruteMat = Glow(Amber);
            _boltMat = Glow(new Color(0.55f, 1f, 1f));
            _healthMat = Glow(new Color(0.25f, 1f, 0.60f));
            _ammoMat = Glow(Amber);
        }

        /// <summary>
        /// Shader escolhido em tempo de execução: o projeto funciona tanto no
        /// pipeline padrão quanto no URP, sem você configurar nada.
        /// </summary>
        static Shader PickShader()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Diffuse");
            return s;
        }

        static Material Solid(Color color)
        {
            var m = new Material(PickShader());
            m.color = color;
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.35f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.35f);
            return m;
        }

        static Material Glow(Color color)
        {
            var m = Solid(color);
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", color * 2.2f);
            return m;
        }

        void BuildEnvironment()
        {
            var camGo = new GameObject("Player Camera");
            _camera = camGo.AddComponent<Camera>();
            _camera.fieldOfView = 72f;
            _camera.nearClipPlane = 0.05f;
            _camera.farClipPlane = 400f;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = FogColor;
            camGo.AddComponent<AudioListener>();

            RenderSettings.fog = true;
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.014f;
            RenderSettings.ambientLight = new Color(0.30f, 0.36f, 0.50f);

            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.94f, 0.84f);
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(52f, 35f, 0f);

            var fillGo = new GameObject("Fill Light");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.32f, 0.55f, 1f);
            fill.intensity = 0.35f;
            fill.shadows = LightShadows.None;
            fillGo.transform.rotation = Quaternion.Euler(28f, 215f, 0f);

            float span = ArenaLayout.Half * 2f + ArenaLayout.WallThickness * 2f;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(span / 10f, 1f, span / 10f);
            floor.GetComponent<Renderer>().sharedMaterial = Solid(new Color(0.085f, 0.11f, 0.17f));

            // linhas da grade a cada 4 unidades
            var lineMat = Glow(new Color(0.10f, 0.45f, 0.62f));
            for (float i = -ArenaLayout.Half; i <= ArenaLayout.Half; i += 4f)
            {
                MakeBox(new Vector3(span, 0.02f, 0.05f), new Vector3(0, 0.011f, i), lineMat, "GridLine");
                MakeBox(new Vector3(0.05f, 0.02f, span), new Vector3(i, 0.011f, 0), lineMat, "GridLine");
            }
        }

        static GameObject MakeBox(Vector3 size, Vector3 position, Material material, string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.localScale = size;
            go.transform.position = position;
            go.GetComponent<Renderer>().sharedMaterial = material;
            // a colisão é nossa, do Core — o collider do Unity só atrapalharia
            Destroy(go.GetComponent<Collider>());
            return go;
        }

        void BuildArena()
        {
            var root = new GameObject("Arena").transform;

            foreach (var piece in ArenaLayout.Build())
            {
                Vec3 c = piece.Box.Center, s = piece.Box.Size;
                Material body;
                Material neon;
                switch (piece.Surface)
                {
                    case Surface.Wall: body = _wallMat; neon = _droneMat; break;
                    case Surface.Pillar: body = _pillarMat; neon = _runnerMat; break;
                    default: body = _crateMat; neon = _bruteMat; break;
                }

                var box = MakeBox(new Vector3(s.X, s.Y, s.Z), new Vector3(c.X, c.Y, c.Z), body, "Piece");
                box.transform.SetParent(root, true);

                if (piece.Neon)
                {
                    var strip = MakeBox(
                        new Vector3(s.X * 1.01f, 0.09f, s.Z * 1.01f),
                        new Vector3(c.X, c.Y + s.Y / 2f + 0.05f, c.Z),
                        neon, "Neon");
                    strip.transform.SetParent(root, true);
                }
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) _switchRequest = 0;
            if (Input.GetKeyDown(KeyCode.Alpha2)) _switchRequest = 1;
            if (Input.GetKeyDown(KeyCode.R)) _sim.StartReload();
            if (Input.GetKeyDown(KeyCode.F5) && _sim.Phase == SimPhase.Dead) _sim.Start();
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                bool locked = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = locked;
            }

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                _yaw -= Input.GetAxisRaw("Mouse X") * mouseSensitivity * 60f;
                _pitch -= Input.GetAxisRaw("Mouse Y") * mouseSensitivity * 60f;
                _pitch = Mathf.Clamp(_pitch, -1.45f, 1.45f);
            }

            if (_sim.Phase == SimPhase.Playing)
                _sim.Step(Time.deltaTime, ReadInput());

            SyncCamera();
            SyncEnemies();
            SyncBolts();
            SyncPickups();
            _hud.Bind(_sim);
        }

        InputFrame ReadInput()
        {
            float fx = 0f, fz = 0f;
            if (Input.GetKey(KeyCode.W)) fz -= 1f;
            if (Input.GetKey(KeyCode.S)) fz += 1f;
            if (Input.GetKey(KeyCode.D)) fx += 1f;
            if (Input.GetKey(KeyCode.A)) fx -= 1f;

            // leva o eixo local para o mundo girando por yaw
            float sin = Mathf.Sin(_yaw), cos = Mathf.Cos(_yaw);
            var input = new InputFrame
            {
                MoveX = fx * cos - fz * sin,
                MoveZ = -fx * sin + fz * cos,
                Yaw = _yaw,
                Pitch = _pitch,
                Jump = Input.GetKey(KeyCode.Space),
                Sprint = Input.GetKey(KeyCode.LeftShift),
                Fire = Cursor.lockState == CursorLockMode.Locked && Input.GetMouseButton(0),
                Reload = false,
                SwitchWeapon = _switchRequest
            };
            _switchRequest = -1;
            return input;
        }

        void SyncCamera()
        {
            Vec3 eye = _sim.Player.EyePos;
            _camera.transform.position = new Vector3(eye.X, eye.Y, eye.Z);
            _camera.transform.rotation = Quaternion.Euler(-_pitch * Mathf.Rad2Deg, _yaw * Mathf.Rad2Deg, 0f);
        }

        Transform Grow(List<Transform> pool, Transform parent, PrimitiveType shape, Material material)
        {
            var go = GameObject.CreatePrimitive(shape);
            Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = material;
            go.transform.SetParent(parent, false);
            pool.Add(go.transform);
            return go.transform;
        }

        void SyncEnemies()
        {
            var list = _sim.Enemies;
            for (int i = 0; i < list.Count; i++)
            {
                if (i >= _enemyPool.Count) Grow(_enemyPool, _enemyRoot, PrimitiveType.Sphere, _droneMat);

                var t = _enemyPool[i];
                var e = list[i];
                float scale = e.Radius * 2f * Mathf.Min(1f, e.Birth * 1.4f + 0.3f);

                t.position = new Vector3(e.Pos.X, e.Pos.Y, e.Pos.Z);
                t.localScale = Vector3.one * scale;
                t.GetComponent<Renderer>().sharedMaterial =
                    e.Kind == EnemyKind.Drone ? _droneMat :
                    e.Kind == EnemyKind.Runner ? _runnerMat : _bruteMat;
                t.gameObject.SetActive(true);
            }
            for (int i = list.Count; i < _enemyPool.Count; i++) _enemyPool[i].gameObject.SetActive(false);
        }

        void SyncBolts()
        {
            var list = _sim.Bolts;
            for (int i = 0; i < list.Count; i++)
            {
                if (i >= _boltPool.Count) Grow(_boltPool, _boltRoot, PrimitiveType.Sphere, _boltMat);
                _boltPool[i].position = new Vector3(list[i].Pos.X, list[i].Pos.Y, list[i].Pos.Z);
                _boltPool[i].localScale = Vector3.one * 0.34f;
                _boltPool[i].gameObject.SetActive(true);
            }
            for (int i = list.Count; i < _boltPool.Count; i++) _boltPool[i].gameObject.SetActive(false);
        }

        void SyncPickups()
        {
            var list = _sim.Pickups;
            for (int i = 0; i < list.Count; i++)
            {
                if (i >= _pickupPool.Count) Grow(_pickupPool, _pickupRoot, PrimitiveType.Cube, _healthMat);

                var t = _pickupPool[i];
                var p = list[i];
                float bob = Mathf.Sin(_sim.Time * 2.6f + i) * 0.18f;
                t.position = new Vector3(p.Pos.X, p.Pos.Y + bob, p.Pos.Z);
                t.rotation = Quaternion.Euler(0f, _sim.Time * 97f, 0f);
                t.localScale = Vector3.one * 0.42f;
                t.GetComponent<Renderer>().sharedMaterial = p.IsHealth ? _healthMat : _ammoMat;
                t.gameObject.SetActive(true);
            }
            for (int i = list.Count; i < _pickupPool.Count; i++) _pickupPool[i].gameObject.SetActive(false);
        }
    }
}

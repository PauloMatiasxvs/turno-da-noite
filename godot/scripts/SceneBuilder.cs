using Godot;
using NeonArena.Core;

namespace NeonArena.GodotGame
{
    /// <summary>
    /// Monta a arena, as luzes e o clima por código a partir de ArenaLayout.
    /// Não existe cena montada à mão: mudar a planta no C# muda o jogo nos dois motores.
    /// </summary>
    public static class SceneBuilder
    {
        public static readonly Color Cyan = new Color(0.20f, 0.85f, 1.00f);
        public static readonly Color Magenta = new Color(1.00f, 0.18f, 0.53f);
        public static readonly Color Amber = new Color(1.00f, 0.69f, 0.23f);
        public static readonly Color Fog = new Color(0.035f, 0.05f, 0.09f);

        public static StandardMaterial3D Solid(Color color)
        {
            return new StandardMaterial3D
            {
                AlbedoColor = color,
                Roughness = 0.65f,
                Metallic = 0.10f
            };
        }

        public static StandardMaterial3D Glow(Color color, float strength = 2.2f)
        {
            var m = new StandardMaterial3D
            {
                AlbedoColor = color,
                EmissionEnabled = true,
                Emission = color,
                EmissionEnergyMultiplier = strength,
                Roughness = 0.4f
            };
            return m;
        }

        public static MeshInstance3D BoxNode(Vector3 size, Material material, Vector3 position)
        {
            var node = new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = size },
                MaterialOverride = material,
                Position = position
            };
            return node;
        }

        public static void BuildEnvironment(Node3D root)
        {
            var env = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color,
                BackgroundColor = Fog,
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = new Color(0.30f, 0.36f, 0.50f),
                AmbientLightEnergy = 0.55f,
                FogEnabled = true,
                FogLightColor = Fog,
                FogDensity = 0.012f,
                GlowEnabled = true,
                GlowIntensity = 0.9f,
                GlowBloom = 0.25f,
                TonemapMode = Godot.Environment.ToneMapper.Aces
            };
            root.AddChild(new WorldEnvironment { Environment = env });

            var sun = new DirectionalLight3D
            {
                LightColor = new Color(1.0f, 0.94f, 0.84f),
                LightEnergy = 1.1f,
                ShadowEnabled = true
            };
            sun.LookAtFromPosition(new Vector3(20, 30, 14), Vector3.Zero, Vector3.Up);
            root.AddChild(sun);

            // preenchimento frio do lado oposto — o mesmo truque do shader da versão web
            var fill = new DirectionalLight3D
            {
                LightColor = new Color(0.32f, 0.55f, 1.0f),
                LightEnergy = 0.35f,
                ShadowEnabled = false
            };
            fill.LookAtFromPosition(new Vector3(-22, 12, -16), Vector3.Zero, Vector3.Up);
            root.AddChild(fill);
        }

        public static void BuildFloor(Node3D root)
        {
            float span = ArenaLayout.Half * 2f + ArenaLayout.WallThickness * 2f;
            var floor = new MeshInstance3D
            {
                Mesh = new PlaneMesh { Size = new Vector2(span, span) },
                MaterialOverride = Solid(new Color(0.085f, 0.11f, 0.17f)),
                Position = Vector3.Zero
            };
            root.AddChild(floor);

            // linhas da grade: barras finas e luminosas a cada 4 unidades
            var lineMat = Glow(new Color(0.10f, 0.45f, 0.62f), 1.4f);
            for (float i = -ArenaLayout.Half; i <= ArenaLayout.Half; i += 4f)
            {
                root.AddChild(BoxNode(new Vector3(span, 0.02f, 0.05f), lineMat, new Vector3(0, 0.011f, i)));
                root.AddChild(BoxNode(new Vector3(0.05f, 0.02f, span), lineMat, new Vector3(i, 0.011f, 0)));
            }
        }

        public static void BuildArena(Node3D root)
        {
            var wallMat = Solid(new Color(0.17f, 0.21f, 0.31f));
            var pillarMat = Solid(new Color(0.21f, 0.25f, 0.36f));
            var crateMat = Solid(new Color(0.26f, 0.30f, 0.40f));

            var neonCyan = Glow(Cyan);
            var neonMagenta = Glow(Magenta);
            var neonAmber = Glow(Amber);

            foreach (var piece in ArenaLayout.Build())
            {
                Vec3 c = piece.Box.Center, s = piece.Box.Size;
                var size = new Vector3(s.X, s.Y, s.Z);
                var pos = new Vector3(c.X, c.Y, c.Z);

                Material body;
                Material neon;
                switch (piece.Surface)
                {
                    case Surface.Wall: body = wallMat; neon = neonCyan; break;
                    case Surface.Pillar: body = pillarMat; neon = neonMagenta; break;
                    default: body = crateMat; neon = neonAmber; break;
                }

                root.AddChild(BoxNode(size, body, pos));

                if (piece.Neon)
                {
                    // faixa luminosa no topo: é o que dá a leitura de silhueta no escuro
                    var stripSize = new Vector3(size.X * 1.01f, 0.09f, size.Z * 1.01f);
                    var stripPos = new Vector3(pos.X, pos.Y + size.Y / 2f + 0.05f, pos.Z);
                    root.AddChild(BoxNode(stripSize, neon, stripPos));
                }
            }
        }
    }
}

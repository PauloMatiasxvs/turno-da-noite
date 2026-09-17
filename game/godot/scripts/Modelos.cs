using Godot;

namespace TurnoDaNoite.Jogo
{
    /// <summary>
    /// Peças montadas com formas simples combinadas, para as coisas que o
    /// jogador olha de perto.
    ///
    /// Por que não baixar em vez de montar: pacote de asset gratuito tem
    /// caixote e barril aos montes, mas ninguém modela fusível de porcelana
    /// nem armário de vestiário. Um cilindro cerâmico com dois terminais de
    /// metal e o filamento aceso por dentro lê como fusível na hora — e um
    /// cubo brilhante lê como cubo brilhante.
    ///
    /// Quando um modelo de verdade aparecer em assets/modelos, o registro em
    /// Props usa ele e nada disto roda.
    /// </summary>
    public static class Modelos
    {
        // ------------------------------------------------------------ materiais

        static StandardMaterial3D _ceramica, _metal, _metalEscuro, _borracha, _vidroAmbar, _rotuloVerde;

        static StandardMaterial3D Ceramica => _ceramica ??= new StandardMaterial3D
        {
            AlbedoColor = new Color(0.78f, 0.74f, 0.66f), Roughness = 0.75f
        };
        static StandardMaterial3D Metal => _metal ??= new StandardMaterial3D
        {
            AlbedoColor = new Color(0.62f, 0.60f, 0.56f), Metallic = 0.9f, Roughness = 0.3f
        };
        static StandardMaterial3D MetalEscuro => _metalEscuro ??= new StandardMaterial3D
        {
            AlbedoColor = new Color(0.20f, 0.21f, 0.23f), Metallic = 0.7f, Roughness = 0.45f
        };
        static StandardMaterial3D Borracha => _borracha ??= new StandardMaterial3D
        {
            AlbedoColor = new Color(0.09f, 0.09f, 0.10f), Roughness = 0.95f
        };
        static StandardMaterial3D VidroAmbar => _vidroAmbar ??= Aceso(new Color(1f, 0.70f, 0.22f), 2.6f);
        static StandardMaterial3D RotuloVerde => _rotuloVerde ??= Aceso(new Color(0.30f, 0.95f, 0.50f), 1.5f);

        static StandardMaterial3D Aceso(Color c, float forca) => new()
        {
            AlbedoColor = c, EmissionEnabled = true, Emission = c,
            EmissionEnergyMultiplier = forca, Roughness = 0.4f
        };

        // ------------------------------------------------------------ auxiliares

        static MeshInstance3D Cilindro(float raio, float altura, Material mat, Vector3 pos, int lados = 14)
            => new()
            {
                Mesh = new CylinderMesh
                {
                    TopRadius = raio, BottomRadius = raio, Height = altura, RadialSegments = lados
                },
                MaterialOverride = mat,
                Position = pos
            };

        static MeshInstance3D Caixa(Vector3 tamanho, Material mat, Vector3 pos)
            => new()
            {
                Mesh = new BoxMesh { Size = tamanho },
                MaterialOverride = mat,
                Position = pos
            };

        // ------------------------------------------------------------ peças

        /// <summary>
        /// Fusível cartucho: corpo de porcelana, dois terminais de metal e o
        /// filamento aceso por dentro, que é o que faz ele ser achável no escuro.
        /// </summary>
        public static Node3D Fusivel()
        {
            var raiz = new Node3D();
            const float corpo = 0.20f, raio = 0.045f;

            raiz.AddChild(Cilindro(raio, corpo, Ceramica, new Vector3(0, corpo / 2 + 0.03f, 0)));
            raiz.AddChild(Cilindro(raio * 1.18f, 0.035f, Metal, new Vector3(0, 0.045f, 0)));
            raiz.AddChild(Cilindro(raio * 1.18f, 0.035f, Metal, new Vector3(0, corpo + 0.015f, 0)));

            // janelinha acesa no meio: o filamento
            raiz.AddChild(Cilindro(raio * 0.55f, corpo * 0.55f, VidroAmbar,
                new Vector3(0, corpo / 2 + 0.03f, 0), 10));

            return raiz;
        }

        /// <summary>Pilha grande: corpo, polo positivo e rótulo. Sem o polo, vira só um cilindro.</summary>
        public static Node3D Bateria()
        {
            var raiz = new Node3D();
            const float corpo = 0.20f, raio = 0.055f;

            raiz.AddChild(Cilindro(raio, corpo, MetalEscuro, new Vector3(0, corpo / 2 + 0.01f, 0)));
            raiz.AddChild(Cilindro(raio * 1.02f, 0.07f, RotuloVerde, new Vector3(0, corpo * 0.55f, 0)));
            raiz.AddChild(Cilindro(raio * 0.35f, 0.025f, Metal, new Vector3(0, corpo + 0.02f, 0), 10));

            return raiz;
        }

        /// <summary>
        /// Armário de vestiário: corpo, porta rebaixada, dobradiças, maçaneta e
        /// venezianas. As venezianas são o detalhe que faz ler como armário.
        /// </summary>
        public static Node3D Armario(Vector3 t)
        {
            var raiz = new Node3D();
            float l = t.X, a = t.Y, p = t.Z;

            raiz.AddChild(Caixa(new Vector3(l, a, p), MetalEscuro, new Vector3(0, a / 2, 0)));
            // porta um pouco menor e adiantada, para criar o vinco da fresta
            raiz.AddChild(Caixa(new Vector3(l * 0.88f, a * 0.93f, 0.03f),
                new StandardMaterial3D { AlbedoColor = new Color(0.26f, 0.28f, 0.30f), Metallic = 0.5f, Roughness = 0.6f },
                new Vector3(0, a / 2, p / 2 + 0.015f)));

            for (int i = 0; i < 4; i++)
                raiz.AddChild(Caixa(new Vector3(l * 0.55f, 0.018f, 0.02f), Borracha,
                    new Vector3(0, a * 0.80f - i * 0.055f, p / 2 + 0.035f)));

            raiz.AddChild(Caixa(new Vector3(0.05f, 0.14f, 0.035f), Metal,
                new Vector3(l * 0.33f, a * 0.5f, p / 2 + 0.04f)));
            raiz.AddChild(Caixa(new Vector3(0.03f, 0.06f, 0.03f), MetalEscuro,
                new Vector3(-l * 0.42f, a * 0.80f, p / 2 + 0.02f)));
            raiz.AddChild(Caixa(new Vector3(0.03f, 0.06f, 0.03f), MetalEscuro,
                new Vector3(-l * 0.42f, a * 0.22f, p / 2 + 0.02f)));

            return raiz;
        }

        /// <summary>Quadro elétrico: caixa, tampa, dobradiças, alavanca e conduíte saindo por cima.</summary>
        public static Node3D QuadroEletrico(Vector3 t)
        {
            var raiz = new Node3D();
            float l = t.X, a = t.Y, p = t.Z;

            raiz.AddChild(Caixa(new Vector3(l, a, p), MetalEscuro, new Vector3(0, a / 2, 0)));
            raiz.AddChild(Caixa(new Vector3(l * 0.94f, a * 0.9f, 0.025f),
                new StandardMaterial3D { AlbedoColor = new Color(0.34f, 0.30f, 0.20f), Metallic = 0.4f, Roughness = 0.7f },
                new Vector3(0, a / 2, p / 2 + 0.013f)));

            raiz.AddChild(Caixa(new Vector3(0.04f, 0.16f, 0.04f), Metal,
                new Vector3(l * 0.40f, a * 0.5f, p / 2 + 0.04f)));
            raiz.AddChild(Cilindro(0.045f, 0.5f, MetalEscuro, new Vector3(-l * 0.25f, a + 0.22f, 0), 10));
            raiz.AddChild(Cilindro(0.045f, 0.5f, MetalEscuro, new Vector3(l * 0.25f, a + 0.22f, 0), 10));

            return raiz;
        }

        /// <summary>Porta da frente: chapa com dois painéis rebaixados e barra antipânico.</summary>
        public static Node3D Porta(Vector3 t)
        {
            var raiz = new Node3D();
            float l = t.X, a = t.Y, p = t.Z;

            raiz.AddChild(Caixa(new Vector3(l, a, p), MetalEscuro, new Vector3(0, a / 2, 0)));
            var chapa = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.28f, 0.27f, 0.26f), Metallic = 0.6f, Roughness = 0.55f
            };
            raiz.AddChild(Caixa(new Vector3(l * 0.8f, a * 0.36f, 0.03f), chapa,
                new Vector3(0, a * 0.70f, p / 2 + 0.02f)));
            raiz.AddChild(Caixa(new Vector3(l * 0.8f, a * 0.36f, 0.03f), chapa,
                new Vector3(0, a * 0.28f, p / 2 + 0.02f)));
            raiz.AddChild(Caixa(new Vector3(l * 0.86f, 0.06f, 0.06f), Metal,
                new Vector3(0, a * 0.48f, p / 2 + 0.05f)));

            return raiz;
        }

        /// <summary>Caixote de madeira com ripas nas quinas.</summary>
        public static Node3D Caixote(Vector3 t)
        {
            var raiz = new Node3D();
            var madeira = new StandardMaterial3D { AlbedoColor = new Color(0.34f, 0.26f, 0.17f), Roughness = 0.9f };
            var ripa = new StandardMaterial3D { AlbedoColor = new Color(0.26f, 0.19f, 0.12f), Roughness = 0.95f };

            raiz.AddChild(Caixa(t, madeira, new Vector3(0, t.Y / 2, 0)));
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                    raiz.AddChild(Caixa(new Vector3(0.07f, t.Y * 1.02f, 0.07f), ripa,
                        new Vector3(sx * t.X * 0.46f, t.Y / 2, sz * t.Z * 0.46f)));
            return raiz;
        }

        /// <summary>Tambor de 200 litros, com as cintas salientes.</summary>
        public static Node3D Barril(Vector3 t)
        {
            var raiz = new Node3D();
            float raio = t.X / 2, a = t.Y;
            var ferrugem = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.36f, 0.20f, 0.12f), Metallic = 0.35f, Roughness = 0.85f
            };

            raiz.AddChild(Cilindro(raio, a, ferrugem, new Vector3(0, a / 2, 0), 16));
            raiz.AddChild(Cilindro(raio * 1.06f, 0.05f, MetalEscuro, new Vector3(0, a * 0.28f, 0), 16));
            raiz.AddChild(Cilindro(raio * 1.06f, 0.05f, MetalEscuro, new Vector3(0, a * 0.72f, 0), 16));
            raiz.AddChild(Cilindro(raio * 0.25f, 0.03f, Metal, new Vector3(raio * 0.4f, a + 0.01f, 0), 10));
            return raiz;
        }
    }
}

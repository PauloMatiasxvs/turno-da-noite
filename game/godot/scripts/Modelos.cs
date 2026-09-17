using Godot;
using TurnoDaNoite.Core;

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

        // ------------------------------------------------ recipientes que se abrem
        //
        // Os três têm uma peça móvel chamada "Tampa". Quem monta a cena guarda
        // esse nó e gira/desliza ele quando o jogador revista: abrir precisa ter
        // consequência visível, senão você revista a mesma gaveta três vezes sem
        // perceber. O tamanho é decidido aqui, e não por quem chama, porque a
        // proporção é o que faz a coisa ser reconhecível.

        public static Vector3 TamanhoCaixaFerramentas => new(0.58f, 0.26f, 0.30f);
        public static Vector3 TamanhoGaveteiro => new(0.66f, 1.02f, 0.52f);
        public static Vector3 TamanhoPrateleira => new(1.30f, 1.75f, 0.42f);

        /// <summary>Caixa de ferramentas de chapa, com tampa de abrir e alça em arco.</summary>
        public static Node3D CaixaFerramentas()
        {
            var raiz = new Node3D();
            var t = TamanhoCaixaFerramentas;
            var chapa = new StandardMaterial3D
            {
                // vermelho de caixa velha, nao laranja de plastico novo
                AlbedoColor = new Color(0.30f, 0.12f, 0.09f), Metallic = 0.45f, Roughness = 0.72f
            };

            raiz.AddChild(Caixa(new Vector3(t.X, t.Y, t.Z), chapa, new Vector3(0, t.Y / 2, 0)));

            // A tampa é filha de um pivô na dobradiça de trás. Girar o pivô abre;
            // girar a tampa em si a faria atravessar a caixa.
            var pivo = new Node3D { Name = "Tampa", Position = new Vector3(0, t.Y, -t.Z / 2) };
            pivo.AddChild(Caixa(new Vector3(t.X, 0.05f, t.Z), chapa, new Vector3(0, 0.025f, t.Z / 2)));
            var alca = Cilindro(0.012f, t.X * 0.55f, Metal, new Vector3(0, 0.10f, t.Z / 2), 8);
            alca.RotateZ(Mathf.Pi / 2);   // deita a alça no eixo X
            pivo.AddChild(alca);
            raiz.AddChild(pivo);

            raiz.AddChild(Caixa(new Vector3(0.05f, 0.05f, 0.02f), Metal,
                new Vector3(0, t.Y * 0.55f, t.Z / 2 + 0.01f)));
            return raiz;
        }

        /// <summary>Gaveteiro de três gavetas. Revistar puxa as três de uma vez.</summary>
        public static Node3D Gaveteiro()
        {
            var raiz = new Node3D();
            var t = TamanhoGaveteiro;
            // Cinza escuro de armário de repartição. Estava bem mais claro e,
            // com a lanterna a um metro, saía branco estourado na tela.
            var carcaca = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.17f, 0.19f, 0.20f), Metallic = 0.5f, Roughness = 0.68f
            };
            var frente = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.23f, 0.25f, 0.25f), Metallic = 0.45f, Roughness = 0.60f
            };

            // O corpo é OCO: cinco chapas em vez de um cubo maciço. Enquanto era
            // maciço, a gaveta saía de dentro de uma parede fechada, sem buraco
            // nenhum de onde ela pudesse ter vindo.
            const float chapa = 0.025f;
            raiz.AddChild(Caixa(new Vector3(t.X, t.Y, chapa), carcaca,
                new Vector3(0, t.Y / 2, -t.Z / 2 + chapa / 2)));                    // costas
            raiz.AddChild(Caixa(new Vector3(t.X, chapa, t.Z), carcaca,
                new Vector3(0, t.Y - chapa / 2, 0)));                               // tampo
            raiz.AddChild(Caixa(new Vector3(t.X, chapa, t.Z), carcaca,
                new Vector3(0, chapa / 2, 0)));                                     // base
            foreach (float sx in new[] { -1f, 1f })
                raiz.AddChild(Caixa(new Vector3(chapa, t.Y, t.Z), carcaca,
                    new Vector3(sx * (t.X / 2 - chapa / 2), t.Y / 2, 0)));          // laterais

            // As gavetas vão juntas num pivô que desliza em +Z quando abre. Cada
            // uma é uma bandeja de verdade — frente, piso, dois lados e costas —
            // e não um painel chapado: painel puxado para fora lê como porta
            // solta boiando no ar, que foi exatamente o que apareceu na tela.
            var gavetas = new Node3D { Name = "Tampa" };
            float alturaGaveta = t.Y * 0.27f;
            float fundoGaveta = t.Z * 0.78f;
            float zFrente = t.Z / 2 + 0.015f;
            float zMeio = zFrente - fundoGaveta / 2;

            for (int i = 0; i < 3; i++)
            {
                float y = t.Y * (0.20f + i * 0.30f);
                float piso = y - alturaGaveta / 2;

                gavetas.AddChild(Caixa(new Vector3(t.X * 0.94f, alturaGaveta, 0.03f), frente,
                    new Vector3(0, y, zFrente)));                                   // frente
                gavetas.AddChild(Caixa(new Vector3(t.X * 0.86f, 0.018f, fundoGaveta), carcaca,
                    new Vector3(0, piso + 0.009f, zMeio)));                         // piso
                foreach (float sx in new[] { -1f, 1f })
                    gavetas.AddChild(Caixa(new Vector3(0.018f, alturaGaveta * 0.78f, fundoGaveta),
                        carcaca, new Vector3(sx * t.X * 0.43f, y, zMeio)));         // lados
                gavetas.AddChild(Caixa(new Vector3(t.X * 0.86f, alturaGaveta * 0.78f, 0.018f),
                    carcaca, new Vector3(0, y, zFrente - fundoGaveta)));            // costas

                gavetas.AddChild(Caixa(new Vector3(t.X * 0.32f, 0.030f, 0.028f), Metal,
                    new Vector3(0, y, zFrente + 0.028f)));                          // puxador
            }
            raiz.AddChild(gavetas);

            // rodapé recuado: tira o ar de caixa pousada no chão
            raiz.AddChild(Caixa(new Vector3(t.X * 0.9f, 0.06f, t.Z * 0.9f), Borracha,
                new Vector3(0, 0.03f, 0)));
            return raiz;
        }

        /// <summary>
        /// Prateleira de aço com caixas e um tambor pequeno. Não tem porta: o que
        /// muda ao revistar é a caixa de cima, que fica tombada para o lado.
        /// </summary>
        public static Node3D Prateleira()
        {
            var raiz = new Node3D();
            var t = TamanhoPrateleira;
            var aco = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.33f, 0.31f, 0.28f), Metallic = 0.6f, Roughness = 0.6f
            };
            var papelao = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.45f, 0.35f, 0.24f), Roughness = 0.95f
            };

            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                    raiz.AddChild(Caixa(new Vector3(0.05f, t.Y, 0.05f), aco,
                        new Vector3(sx * t.X * 0.46f, t.Y / 2, sz * t.Z * 0.42f)));

            for (int i = 0; i < 4; i++)
                raiz.AddChild(Caixa(new Vector3(t.X, 0.035f, t.Z), aco,
                    new Vector3(0, 0.12f + i * (t.Y - 0.2f) / 3f, 0)));

            raiz.AddChild(Caixa(new Vector3(0.34f, 0.26f, 0.30f), papelao,
                new Vector3(-t.X * 0.24f, 0.12f + 0.035f + 0.13f, 0)));
            raiz.AddChild(Cilindro(0.13f, 0.34f, MetalEscuro,
                new Vector3(t.X * 0.27f, 0.12f + (t.Y - 0.2f) / 3f + 0.19f, 0), 12));

            // A caixa que tomba fica no próprio pivô, com o mesh na origem local:
            // assim girar o nó a vira no lugar em vez de arremessá-la pela sala.
            var tombar = new Node3D
            {
                Name = "Tampa",
                Position = new Vector3(t.X * 0.18f, 0.12f + 2 * (t.Y - 0.2f) / 3f + 0.14f, 0)
            };
            tombar.AddChild(Caixa(new Vector3(0.30f, 0.24f, 0.28f), papelao, Vector3.Zero));
            raiz.AddChild(tombar);

            return raiz;
        }

        // ---------------------------------------------------------- cenário solto
        //
        // O que enche as salas. Cada um é pequeno de propósito: o raio de colisão
        // vive em Core/Cenario.cs e as formas daqui têm de caber nele, senão você
        // esbarra no ar ou atravessa metade de uma bancada.

        static StandardMaterial3D _madeira, _sujo;
        static StandardMaterial3D Madeira => _madeira ??= new StandardMaterial3D
        {
            AlbedoColor = new Color(0.32f, 0.24f, 0.16f), Roughness = 0.92f
        };
        static StandardMaterial3D Sujo => _sujo ??= new StandardMaterial3D
        {
            AlbedoColor = new Color(0.24f, 0.23f, 0.21f), Roughness = 0.95f
        };

        /// <summary>Bancada de oficina: tampo, pés, prateleira embaixo e um torninho.</summary>
        public static Node3D Bancada()
        {
            var raiz = new Node3D();
            const float l = 0.98f, p = 0.82f, a = 0.88f;

            raiz.AddChild(Caixa(new Vector3(l, 0.06f, p), Madeira, new Vector3(0, a, 0)));
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                    raiz.AddChild(Caixa(new Vector3(0.07f, a, 0.07f), MetalEscuro,
                        new Vector3(sx * l * 0.42f, a / 2, sz * p * 0.40f)));

            raiz.AddChild(Caixa(new Vector3(l * 0.9f, 0.04f, p * 0.8f), Sujo,
                new Vector3(0, a * 0.32f, 0)));
            raiz.AddChild(Caixa(new Vector3(0.16f, 0.14f, 0.12f), Metal,
                new Vector3(l * 0.28f, a + 0.09f, 0)));
            return raiz;
        }

        /// <summary>Pilha de caixas de papelão encostadas, tortas umas sobre as outras.</summary>
        public static Node3D Pilha()
        {
            var raiz = new Node3D();
            var papelao = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.42f, 0.33f, 0.22f), Roughness = 0.95f
            };

            float y = 0;
            float[] larguras = { 0.72f, 0.62f, 0.50f };
            float[] tortos = { 0.06f, -0.13f, 0.21f };
            for (int i = 0; i < 3; i++)
            {
                float alt = 0.30f - i * 0.04f;
                var c = Caixa(new Vector3(larguras[i], alt, larguras[i] * 0.85f), papelao,
                              new Vector3(0, y + alt / 2, 0));
                c.RotateY(tortos[i]);
                raiz.AddChild(c);
                y += alt;
            }
            return raiz;
        }

        /// <summary>Entulho: tábuas e cacos no chão. Não colide — é para pisar por cima.</summary>
        public static Node3D Entulho()
        {
            var raiz = new Node3D();
            float[,] pecas =
            {
                { 0.80f, 0.03f, 0.11f,  0.10f, 0.4f },
                { 0.62f, 0.03f, 0.09f, -0.22f, 1.1f },
                { 0.30f, 0.05f, 0.24f,  0.28f, 2.2f },
                { 0.18f, 0.08f, 0.16f, -0.10f, 0.7f }
            };
            for (int i = 0; i < pecas.GetLength(0); i++)
            {
                var c = Caixa(new Vector3(pecas[i, 0], pecas[i, 1], pecas[i, 2]),
                              i < 2 ? Madeira : Sujo,
                              new Vector3(pecas[i, 3], pecas[i, 1] / 2, pecas[i, 3] * 0.6f));
                c.RotateY(pecas[i, 4]);
                raiz.AddChild(c);
            }
            return raiz;
        }

        /// <summary>Cano descendo pela parede, com duas abraçadeiras. Só passa raspando.</summary>
        public static Node3D CanoParede()
        {
            var raiz = new Node3D();
            float h = Predio.PeDireito;

            raiz.AddChild(Cilindro(0.06f, h, MetalEscuro, new Vector3(0, h / 2, 0), 10));
            raiz.AddChild(Cilindro(0.075f, 0.06f, Metal, new Vector3(0, h * 0.25f, 0), 10));
            raiz.AddChild(Cilindro(0.075f, 0.06f, Metal, new Vector3(0, h * 0.75f, 0), 10));
            // joelho na altura do peito: quebra a linha reta, que é o que cansa a vista
            var joelho = Cilindro(0.06f, 0.42f, MetalEscuro, new Vector3(0.20f, h * 0.55f, 0), 10);
            joelho.RotateZ(Mathf.Pi / 2);
            raiz.AddChild(joelho);
            return raiz;
        }

        /// <summary>
        /// Cano atravessando o teto da sala, com tirantes. É o que mais faz o
        /// lugar parecer um prédio de verdade e não uma caixa de papelão.
        /// </summary>
        public static Node3D CanoTeto(float comprimento)
        {
            var raiz = new Node3D();
            float y = Predio.PeDireito - 0.32f;

            var cano = Cilindro(0.075f, comprimento, MetalEscuro, new Vector3(0, y, 0), 10);
            cano.RotateX(Mathf.Pi / 2);            // deita no eixo Z
            raiz.AddChild(cano);

            var fino = Cilindro(0.035f, comprimento * 0.92f, Sujo, new Vector3(0.22f, y - 0.07f, 0), 8);
            fino.RotateX(Mathf.Pi / 2);
            raiz.AddChild(fino);

            int quantos = Mathf.Max(2, (int)(comprimento / 3.2f));
            for (int i = 0; i < quantos; i++)
            {
                float z = -comprimento / 2 + comprimento * (i + 0.5f) / quantos;
                raiz.AddChild(Caixa(new Vector3(0.03f, 0.30f, 0.03f), Metal,
                    new Vector3(0, y + 0.18f, z)));
            }
            return raiz;
        }

        // ------------------------------------------------------------ a mão

        /// <summary>
        /// A mão segurando a lanterna, em primeira pessoa. É a única peça que
        /// fica na tela o tempo todo, então é a que mais paga atenção ao detalhe:
        /// enquanto era um cilindro liso, virava um cano escuro atravessando o
        /// canto do quadro, e nenhum resto de cenário bonito compensava isso.
        ///
        /// Os nós "Vidro" e "Corpo" saem nomeados porque quem chama precisa
        /// acender um e não pode deixar nenhum projetar sombra.
        /// </summary>
        public static Node3D MaoComLanterna()
        {
            var raiz = new Node3D();

            var corpoMat = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.14f, 0.15f, 0.17f), Metallic = 0.75f, Roughness = 0.32f
            };
            var pegaMat = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.07f, 0.07f, 0.08f), Roughness = 0.9f
            };
            var luvaMat = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.13f, 0.12f, 0.12f), Roughness = 0.88f
            };
            var mangaMat = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.09f, 0.10f, 0.12f), Roughness = 0.95f
            };

            // ---- lanterna: tubo, pega emborrachada, cabeça cônica e aro
            var corpo = new Node3D { Name = "Corpo" };

            var tubo = Cilindro(0.026f, 0.21f, corpoMat, new Vector3(0, 0, -0.015f), 16);
            tubo.RotateX(Mathf.Pi / 2);
            corpo.AddChild(tubo);

            var pega = Cilindro(0.029f, 0.075f, pegaMat, new Vector3(0, 0, 0.035f), 16);
            pega.RotateX(Mathf.Pi / 2);
            corpo.AddChild(pega);

            var cabeca = new MeshInstance3D
            {
                Mesh = new CylinderMesh
                {
                    TopRadius = 0.042f, BottomRadius = 0.027f, Height = 0.06f, RadialSegments = 16
                },
                MaterialOverride = corpoMat,
                Position = new Vector3(0, 0, -0.145f)
            };
            cabeca.RotateX(-Mathf.Pi / 2);      // a boca larga vira para a frente
            corpo.AddChild(cabeca);

            var aro = Cilindro(0.044f, 0.012f, pegaMat, new Vector3(0, 0, -0.174f), 16);
            aro.RotateX(Mathf.Pi / 2);
            corpo.AddChild(aro);

            // tampa de trás, para o tubo não terminar num buraco
            var tampa = Cilindro(0.027f, 0.014f, pegaMat, new Vector3(0, 0, 0.082f), 16);
            tampa.RotateX(Mathf.Pi / 2);
            corpo.AddChild(tampa);

            raiz.AddChild(corpo);

            // ---- o vidro, que acende junto com a luz
            var vidro = new MeshInstance3D
            {
                Name = "Vidro",
                Mesh = new CylinderMesh
                {
                    TopRadius = 0.036f, BottomRadius = 0.030f, Height = 0.014f, RadialSegments = 16
                },
                // na frente do aro, e nao atras: escondido atras dele o vidro
                // virava um buraco preto no meio da lanterna
                Position = new Vector3(0, 0, -0.182f)
            };
            vidro.RotateX(Mathf.Pi / 2);
            raiz.AddChild(vidro);

            // ---- a mão: palma, quatro dedos por cima do tubo e o polegar do lado
            var palma = Caixa(new Vector3(0.085f, 0.055f, 0.105f), luvaMat,
                              new Vector3(0.012f, -0.030f, 0.040f));
            palma.RotateY(0.12f);
            raiz.AddChild(palma);

            for (int i = 0; i < 4; i++)
            {
                float z = 0.005f + i * 0.026f;
                float raio = 0.0115f - i * 0.0006f;
                var dedo = Cilindro(raio, 0.072f, luvaMat, new Vector3(-0.004f, -0.006f, z), 8);
                dedo.RotateZ(Mathf.Pi / 2);      // deita o dedo atravessado no tubo
                dedo.RotateX(0.10f);
                raiz.AddChild(dedo);
            }

            var polegar = Cilindro(0.012f, 0.055f, luvaMat, new Vector3(0.030f, -0.022f, 0.015f), 8);
            polegar.RotateX(Mathf.Pi / 2);
            polegar.RotateZ(-0.45f);
            raiz.AddChild(polegar);

            // ---- punho e manga, saindo para baixo e para trás
            var punho = Cilindro(0.040f, 0.040f, mangaMat, new Vector3(0.020f, -0.055f, 0.105f), 12);
            punho.RotateX(Mathf.Pi / 2 - 0.5f);
            raiz.AddChild(punho);

            var manga = new MeshInstance3D
            {
                Mesh = new CylinderMesh
                {
                    TopRadius = 0.040f, BottomRadius = 0.058f, Height = 0.26f, RadialSegments = 12
                },
                MaterialOverride = mangaMat,
                Position = new Vector3(0.036f, -0.120f, 0.215f)
            };
            manga.RotateX(Mathf.Pi / 2 - 0.5f);
            raiz.AddChild(manga);

            return raiz;
        }

        // ------------------------------------------------------------ escada

        /// <summary>
        /// Lance de escada de um andar: degraus, dois montantes e corrimão.
        ///
        /// O jogador não sobe degrau por degrau — trocar de andar é pisar no
        /// poço — mas a escada precisa ESTAR lá, e inteira. Um buraco no chão
        /// sem escada nenhuma lê como bug, não como passagem.
        /// </summary>
        public static Node3D Escada(float altura, float largura)
        {
            var raiz = new Node3D();
            var aco = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.22f, 0.23f, 0.24f), Metallic = 0.65f, Roughness = 0.5f
            };
            var piso = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.30f, 0.30f, 0.31f), Metallic = 0.5f, Roughness = 0.62f
            };

            int degraus = Mathf.Max(6, (int)(altura / 0.19f));
            float passoY = altura / degraus;
            float profundidade = largura * 0.92f;
            float passoZ = profundidade / degraus;
            float larguraDoLance = largura * 0.62f;

            for (int i = 0; i < degraus; i++)
            {
                float y = passoY * (i + 0.5f);
                float z = -profundidade / 2 + passoZ * (i + 0.5f);

                raiz.AddChild(Caixa(new Vector3(larguraDoLance, 0.045f, passoZ * 0.95f), piso,
                    new Vector3(0, y, z)));                                    // piso do degrau
                raiz.AddChild(Caixa(new Vector3(larguraDoLance, passoY * 0.8f, 0.03f), aco,
                    new Vector3(0, y - passoY * 0.4f, z - passoZ * 0.45f)));   // espelho
            }

            // montantes laterais, inclinados junto com o lance
            float inclinacao = Mathf.Atan2(altura, profundidade);
            float comprimento = Mathf.Sqrt(altura * altura + profundidade * profundidade);
            foreach (float sx in new[] { -1f, 1f })
            {
                var viga = Caixa(new Vector3(0.05f, 0.22f, comprimento), aco,
                    new Vector3(sx * larguraDoLance / 2, altura / 2 - 0.12f, 0));
                viga.RotateX(-inclinacao);
                raiz.AddChild(viga);

                var corrimao = Cilindro(0.028f, comprimento, aco,
                    new Vector3(sx * larguraDoLance / 2, altura / 2 + 0.85f, 0), 8);
                corrimao.RotateX(Mathf.Pi / 2 - inclinacao);
                raiz.AddChild(corrimao);

                // três balaústres por lado, para o corrimão não flutuar
                for (int i = 0; i < 3; i++)
                {
                    float t = (i + 0.5f) / 3f;
                    raiz.AddChild(Cilindro(0.016f, 0.9f, aco, new Vector3(
                        sx * larguraDoLance / 2,
                        altura * t + 0.4f,
                        -profundidade / 2 + profundidade * t), 6));
                }
            }

            // patamar em cima, onde você desemboca
            raiz.AddChild(Caixa(new Vector3(largura * 0.8f, 0.06f, largura * 0.35f), piso,
                new Vector3(0, altura - 0.03f, profundidade / 2 + largura * 0.16f)));

            return raiz;
        }

        /// <summary>
        /// A planta do prédio: uma folha dobrada, amarelada, com traços de
        /// tinta. É o item mais importante do jogo depois dos fusíveis, então
        /// não pode ser um retângulo branco no chão.
        /// </summary>
        public static Node3D Planta()
        {
            var raiz = new Node3D();
            var papel = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.86f, 0.81f, 0.64f),
                Roughness = 0.95f,
                EmissionEnabled = true,
                Emission = new Color(0.86f, 0.78f, 0.55f),
                EmissionEnergyMultiplier = 0.55f
            };
            var tinta = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.16f, 0.22f, 0.35f), Roughness = 0.9f
            };

            raiz.AddChild(Caixa(new Vector3(0.30f, 0.006f, 0.22f), papel, new Vector3(0, 0.05f, 0)));

            // a metade dobrada, levantada num ângulo
            var dobra = Caixa(new Vector3(0.30f, 0.005f, 0.20f), papel, new Vector3(0, 0.02f, -0.02f));
            dobra.RotateX(-0.55f);
            dobra.Position = new Vector3(0, 0.10f, -0.08f);
            raiz.AddChild(dobra);

            // riscos de planta baixa: dois traços cruzados, o bastante para ler
            raiz.AddChild(Caixa(new Vector3(0.20f, 0.002f, 0.012f), tinta, new Vector3(0, 0.054f, 0.04f)));
            raiz.AddChild(Caixa(new Vector3(0.012f, 0.002f, 0.14f), tinta, new Vector3(-0.06f, 0.054f, 0)));
            raiz.AddChild(Caixa(new Vector3(0.012f, 0.002f, 0.09f), tinta, new Vector3(0.07f, 0.054f, -0.02f)));

            return raiz;
        }
    }
}

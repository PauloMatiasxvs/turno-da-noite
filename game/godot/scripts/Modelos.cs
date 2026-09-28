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

        /// <summary>
        /// Texturas PBR para as peças montadas aqui, entregues por quem monta a
        /// cena (só ele sabe carregar arquivo do Godot).
        ///
        /// Enquanto tudo era cor chapada, um armário de metal a um metro do
        /// nariz virava plástico: sem grão, sem risco, sem ferrugem, nada que
        /// dissesse de que material aquilo é feito. Se vier null, as peças
        /// continuam com as cores de antes e o jogo roda igual.
        /// </summary>
        public static void Texturas(StandardMaterial3D metal = null,
                                    StandardMaterial3D madeira = null,
                                    StandardMaterial3D ferrugem = null,
                                    StandardMaterial3D pele = null,
                                    StandardMaterial3D metalDaMao = null,
                                    StandardMaterial3D couroDaMao = null)
        {
            if (metal != null) _metalEscuro = metal;
            if (madeira != null) _madeira = madeira;
            if (ferrugem != null) _ferrugem = ferrugem;
            if (pele != null) _peleDela = pele;
            if (metalDaMao != null) _metalDaMao = metalDaMao;
            if (couroDaMao != null) _couroDaMao = couroDaMao;
        }

        /// <summary>Pele da criatura, se alguem entregar uma. Triplanar, porque ela nao tem UV.</summary>
        static StandardMaterial3D _peleDela;
        /// <summary>Materiais da mao em primeira pessoa, entregues por quem monta a cena.</summary>
        static StandardMaterial3D _metalDaMao, _couroDaMao;

        static StandardMaterial3D _ferrugem;
        static StandardMaterial3D Ferrugem => _ferrugem ??= new StandardMaterial3D
        {
            AlbedoColor = new Color(0.36f, 0.20f, 0.12f), Metallic = 0.35f, Roughness = 0.85f
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
            const float chapa = 0.03f;

            var frenteMat = new StandardMaterial3D
            {
// escuro: por dentro o armario nao tem luz, e a ripa iluminada so
                // pelo ambiente azulado virava persiana de escritorio
                AlbedoColor = new Color(0.135f, 0.145f, 0.150f), Metallic = 0.5f, Roughness = 0.65f
            };

            // O corpo é OCO: cinco chapas, não um cubo maciço.
            //
            // Isto não é capricho. O jogador se esconde AQUI DENTRO, e com o
            // corpo maciço a câmera ficava dentro do concreto: o que aparecia
            // na tela eram as faces internas da caixa, umas barras claras
            // atravessando tudo, sem nada parecer armário.
            raiz.AddChild(Caixa(new Vector3(l, a, chapa), MetalEscuro,
                new Vector3(0, a / 2, -p / 2 + chapa / 2)));                    // costas
            raiz.AddChild(Caixa(new Vector3(chapa, a, p), MetalEscuro,
                new Vector3(-l / 2 + chapa / 2, a / 2, 0)));                    // lado
            raiz.AddChild(Caixa(new Vector3(chapa, a, p), MetalEscuro,
                new Vector3(l / 2 - chapa / 2, a / 2, 0)));                     // lado
            raiz.AddChild(Caixa(new Vector3(l, chapa, p), MetalEscuro,
                new Vector3(0, a - chapa / 2, 0)));                             // tampo
            raiz.AddChild(Caixa(new Vector3(l, chapa, p), MetalEscuro,
                new Vector3(0, chapa / 2, 0)));                                 // base
            raiz.AddChild(Caixa(new Vector3(l * 0.86f, 0.02f, p * 0.8f), MetalEscuro,
                new Vector3(0, a * 0.72f, 0)));                                 // prateleira de cima

            // A PORTA, num pivô na dobradiça: quem monta a cena gira este nó
            // para abri-la quando você entra.
            var porta = new Node3D { Name = "Porta", Position = new Vector3(-l / 2, 0, p / 2) };

            // A porta é só a METADE DE BAIXO cheia. A de cima é uma grelha de
            // ripas com VÃO entre elas.
            //
            // Antes as venezianas eram ripas coladas numa porta inteiriça —
            // decoração. Escondido dentro do armário, o que aparecia na tela
            // era a chapa da porta a quinze centímetros do nariz, ou seja,
            // nada. Com vão de verdade você enxerga o cômodo em faixas, que é
            // exatamente o que se vê de dentro de um armário de vestiário.
            const float alturaGrelha = 0.55f;                 // fração da porta
            float baseGrelha = a * alturaGrelha;

            porta.AddChild(Caixa(new Vector3(l * 0.96f, baseGrelha, 0.025f), frenteMat,
                new Vector3(l * 0.48f, baseGrelha / 2, 0)));  // painel de baixo, cheio

            // moldura da grelha: duas colunas e o topo
            float alturaVao = a * 0.95f - baseGrelha;
            foreach (float lado in new[] { 0.06f, 0.90f })
                porta.AddChild(Caixa(new Vector3(l * 0.07f, alturaVao, 0.025f), frenteMat,
                    new Vector3(l * lado, baseGrelha + alturaVao / 2, 0)));
            porta.AddChild(Caixa(new Vector3(l * 0.96f, a * 0.045f, 0.025f), frenteMat,
                new Vector3(l * 0.48f, a * 0.95f - a * 0.022f, 0)));

            // as ripas, inclinadas, com ar entre uma e outra
            const int ripas = 7;
            for (int i = 0; i < ripas; i++)
            {
                float y = baseGrelha + alturaVao * (i + 0.5f) / ripas;
                var ripa = Caixa(new Vector3(l * 0.80f, alturaVao / ripas * 0.42f, 0.014f),
                                 frenteMat, new Vector3(l * 0.48f, y, 0));
                ripa.RotateX(-0.55f);     // viradas para baixo, como veneziana de verdade
                porta.AddChild(ripa);
            }

            porta.AddChild(Caixa(new Vector3(0.05f, 0.14f, 0.035f), Metal,
                new Vector3(l * 0.82f, a * 0.5f, 0.03f)));                      // maçaneta
            raiz.AddChild(porta);

            // dobradiças, do lado do pivô
            raiz.AddChild(Caixa(new Vector3(0.03f, 0.07f, 0.03f), MetalEscuro,
                new Vector3(-l * 0.47f, a * 0.80f, p / 2 + 0.01f)));
            raiz.AddChild(Caixa(new Vector3(0.03f, 0.07f, 0.03f), MetalEscuro,
                new Vector3(-l * 0.47f, a * 0.22f, p / 2 + 0.01f)));

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
            raiz.AddChild(Cilindro(raio, a, Ferrugem, new Vector3(0, a / 2, 0), 16));
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

        // ------------------------------------------------------- a lanterna

        /// <summary>
        /// A lanterna sozinha, sem mão nenhuma: tubo, pega emborrachada com
        /// anéis, cabeça cônica, aro e vidro.
        ///
        /// Ficou separada da mão quando entrou um modelo de braços com
        /// esqueleto: lá a lanterna é pendurada no osso `hand_R` e o modelo
        /// fecha os dedos em volta dela. Aqui ela continua servindo à mão
        /// montada em código, que é o que aparece se o arquivo do modelo
        /// sumir da pasta.
        ///
        /// Ela aponta para −Z, com a pega na origem: é a convenção que o
        /// resto do jogo assume ao encaixar a peça em qualquer lugar.
        ///
        /// O nó "Vidro" sai nomeado porque quem chama acende ele junto com a
        /// luz — é o único pedaço que muda quando a lanterna liga.
        /// </summary>
        public static Node3D Lanterna()
        {
            var raiz = new Node3D { Name = "Lanterna" };

            var aluminio = _metalDaMao ?? new StandardMaterial3D
            {
                AlbedoColor = new Color(0.115f, 0.120f, 0.130f),
                Metallic = 0.85f, Roughness = 0.34f
            };
            var borracha = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.045f, 0.045f, 0.050f), Roughness = 0.93f
            };

            // ---- a lanterna, em peças distintas
            var tubo = Cilindro(0.024f, 0.155f, aluminio, new Vector3(0, 0, -0.045f), 18);
            tubo.RotateX(Mathf.Pi / 2);
            raiz.AddChild(tubo);

            // pega emborrachada com anéis: é o que dá escala ao objeto
            var pega = Cilindro(0.027f, 0.075f, borracha, new Vector3(0, 0, 0.055f), 18);
            pega.RotateX(Mathf.Pi / 2);
            raiz.AddChild(pega);
            for (int i = 0; i < 4; i++)
            {
                var anel = Cilindro(0.0285f, 0.007f, borracha,
                                    new Vector3(0, 0, 0.026f + i * 0.019f), 18);
                anel.RotateX(Mathf.Pi / 2);
                raiz.AddChild(anel);
            }

            var cabeca = new MeshInstance3D
            {
                Mesh = new CylinderMesh
                {
                    TopRadius = 0.040f, BottomRadius = 0.025f, Height = 0.055f, RadialSegments = 18
                },
                MaterialOverride = aluminio,
                Position = new Vector3(0, 0, -0.150f)
            };
            cabeca.RotateX(-Mathf.Pi / 2);
            raiz.AddChild(cabeca);

            var aro = Cilindro(0.042f, 0.010f, borracha, new Vector3(0, 0, -0.180f), 18);
            aro.RotateX(Mathf.Pi / 2);
            raiz.AddChild(aro);

            var tampa = Cilindro(0.026f, 0.012f, borracha, new Vector3(0, 0, 0.096f), 18);
            tampa.RotateX(Mathf.Pi / 2);
            raiz.AddChild(tampa);

            // o vidro fica À FRENTE do aro: atrás dele virava um buraco preto
            var vidro = new MeshInstance3D
            {
                Name = "Vidro",
                Mesh = new CylinderMesh
                {
                    TopRadius = 0.035f, BottomRadius = 0.030f, Height = 0.012f, RadialSegments = 18
                },
                Position = new Vector3(0, 0, -0.188f)
            };
            vidro.RotateX(Mathf.Pi / 2);
            raiz.AddChild(vidro);

            return raiz;
        }

        // ------------------------------------------------------------ a mão

        /// <summary>
        /// A mão segurando a lanterna, em primeira pessoa.
        ///
        /// É a única peça que fica na tela o tempo todo, então é a que mais
        /// paga atenção ao detalhe. Já passou por duas versões ruins:
        ///
        /// 1. Dois cilindros lisos — o braço lia como um cano escuro
        ///    atravessando o canto do quadro.
        /// 2. Tudo da mesma cor e iluminado só pela luz ambiente, que é
        ///    azulada e não vem de lugar nenhum. Sem uma fonte batendo nela,
        ///    a mão não tinha forma: saía um borrão lilás chapado, em que não
        ///    dava para separar dedo de cano nem de manga.
        ///
        /// Desta vez: materiais diferentes para luva, borracha, alumínio e
        /// manga; dedos com duas falanges cada, em vez de argolas no tubo; e
        /// uma luz fraca na cabeça da lanterna, que é o que acontece de
        /// verdade quando se segura uma lanterna acesa.
        ///
        /// O nó "Vidro" sai nomeado porque quem chama acende ele junto com a
        /// lanterna. A luz que dá forma à mão NÃO mora aqui: é uma luz de
        /// preenchimento presa à câmera, em Bootstrap, que enxerga só a mão.
        /// Tentei uma luz pontual na cabeça da lanterna e o resultado foi pior:
        /// a cinco centímetros dos dedos, qualquer energia estoura, e eles
        /// viravam cunhas brancas saltando do cano.
        /// </summary>
        public static Node3D MaoComLanterna()
        {
            var raiz = Lanterna();

            // ---- materiais
            //
            // Texturados quando houver arquivo. Enquanto o predio inteiro
            // ganhou textura e a mao ficou em cor chapada, ela virou a unica
            // coisa lisa na tela — e era POR ISSO que destoava, mais do que
            // pela forma.
            var luva = _couroDaMao ?? new StandardMaterial3D
            {
                // couro escuro puxado para o quente: a luva cinza-azulada
                // sumia dentro da luz ambiente, que também é azulada
                AlbedoColor = new Color(0.105f, 0.082f, 0.068f), Roughness = 0.88f
            };
            var luvaVinco = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.062f, 0.048f, 0.040f), Roughness = 0.92f
            };
            var manga = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.068f, 0.070f, 0.066f), Roughness = 0.96f
            };

            // ---- a mão: dorso, quatro dedos de duas falanges, polegar
            var dorso = Caixa(new Vector3(0.028f, 0.062f, 0.098f), luva,
                              new Vector3(0.026f, 0.006f, 0.030f));
            dorso.RotateZ(-0.22f);
            raiz.AddChild(dorso);

            // Os nós dos dedos, em fileira no alto, são o que faz o olho
            // reconhecer uma mão — sem eles era um tubo com argolas.
            for (int i = 0; i < 4; i++)
            {
                float z = 0.058f - i * 0.024f;
                float escala = 1f - i * 0.07f;

                raiz.AddChild(Esfera(0.0105f * escala, luva, new Vector3(0.019f, 0.016f, z)));

                // Bem coladas no tubo. Afastadas, os dedos viravam cunhas
                // saltando de dentro do cano em vez de agarrarem ele.
                var falange1 = Cilindro(0.0090f * escala, 0.034f, luva,
                                        new Vector3(0.003f, 0.018f, z), 8);
                falange1.RotateZ(Mathf.Pi / 2);
                falange1.RotateX(0.12f);
                raiz.AddChild(falange1);

                // a segunda falange dobra para o outro lado do tubo
                var falange2 = Cilindro(0.0082f * escala, 0.026f, luvaVinco,
                                        new Vector3(-0.016f, 0.005f, z), 8);
                falange2.RotateZ(Mathf.Pi / 2 - 0.95f);
                raiz.AddChild(falange2);
            }

            var polegar1 = Cilindro(0.0125f, 0.040f, luva, new Vector3(0.034f, -0.012f, 0.050f), 8);
            polegar1.RotateX(Mathf.Pi / 2);
            polegar1.RotateY(-0.35f);
            raiz.AddChild(polegar1);

            var polegar2 = Cilindro(0.0105f, 0.038f, luva, new Vector3(0.030f, -0.016f, 0.014f), 8);
            polegar2.RotateX(Mathf.Pi / 2);
            polegar2.RotateY(-0.12f);
            raiz.AddChild(polegar2);

            // ---- punho, e SÓ.
            //
            // O antebraço saiu. Um cilindro de vinte e quatro centímetros
            // atravessando o canto inferior da tela vira salsicha, e nenhuma
            // textura conserta isso — o que conserta é não estar lá. Jogo de
            // terror em primeira pessoa mostra o objeto e um pedaço da luva;
            // o braço fica fora de quadro, que é onde ele estaria mesmo se
            // você olhasse para a frente segurando uma lanterna.
            var punho = Cilindro(0.034f, 0.052f, luvaVinco, new Vector3(0.030f, -0.030f, 0.095f), 14);
            punho.RotateX(Mathf.Pi / 2 - 0.42f);
            raiz.AddChild(punho);

            var canhao = Cilindro(0.040f, 0.026f, manga, new Vector3(0.035f, -0.046f, 0.120f), 14);
            canhao.RotateX(Mathf.Pi / 2 - 0.42f);
            raiz.AddChild(canhao);

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

        // ------------------------------------------------------------ ela

        /// <summary>
        /// A criatura. Montada em código, e não baixada, porque o único modelo
        /// CC0 disponível era um demônio ROSA de desenho, com auréola e
        /// forquilha: pintar de preto não resolve, porque a silhueta continua
        /// sendo a piada. Aqui a silhueta é o produto — alta demais para ser
        /// gente, magra demais, braços que chegam ao chão, sem rosto.
        ///
        /// As peças saem nomeadas porque quem desenha anima elas na mão:
        /// CoxaE, CoxaD, BracoE, BracoD, Tronco e Cabeca.
        /// </summary>
        public static Node3D Criatura(float altura)
        {
            var raiz = new Node3D();

            // proporções em fração da altura, para escalar junto
            float h = altura;
            var pele = _peleDela ?? new StandardMaterial3D
            {
                AlbedoColor = new Color(0.055f, 0.050f, 0.058f),
                Roughness = 0.97f,
                Metallic = 0f,
                SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled
            };
            var olho = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.85f, 0.80f, 0.72f),
                EmissionEnabled = true,
                Emission = new Color(0.9f, 0.82f, 0.70f),
                EmissionEnergyMultiplier = 1.1f
            };

            float alturaQuadril = h * 0.46f;
            float alturaOmbro = h * 0.82f;

            // ---- pernas: dois pivôs no quadril, para a caminhada girar neles
            foreach (int lado in new[] { -1, 1 })
            {
                var coxa = new Node3D
                {
                    Name = lado < 0 ? "CoxaE" : "CoxaD",
                    Position = new Vector3(lado * h * 0.055f, alturaQuadril, 0)
                };
                coxa.AddChild(Capsula(h * 0.032f, alturaQuadril * 0.55f, pele,
                    new Vector3(0, -alturaQuadril * 0.28f, 0)));
                coxa.AddChild(Capsula(h * 0.026f, alturaQuadril * 0.5f, pele,
                    new Vector3(0, -alturaQuadril * 0.74f, 0)));
                coxa.AddChild(Caixa(new Vector3(h * 0.05f, h * 0.018f, h * 0.10f), pele,
                    new Vector3(0, -alturaQuadril + h * 0.012f, h * 0.022f)));   // pé
                raiz.AddChild(coxa);
            }

            // ---- tronco: estreito e curvado para a frente
            var tronco = new Node3D { Name = "Tronco", Position = new Vector3(0, alturaQuadril, 0) };
            tronco.RotateX(0.18f);      // corcunda

            float alturaTronco = alturaOmbro - alturaQuadril;
            tronco.AddChild(Capsula(h * 0.072f, alturaTronco * 0.62f, pele,
                new Vector3(0, alturaTronco * 0.34f, 0)));                        // quadril e barriga
            tronco.AddChild(Capsula(h * 0.060f, alturaTronco * 0.55f, pele,
                new Vector3(0, alturaTronco * 0.80f, -h * 0.008f)));              // peito
            // costelas: quatro vincos que dão fome ao bicho
            for (int i = 0; i < 4; i++)
                tronco.AddChild(Caixa(new Vector3(h * 0.10f, h * 0.006f, h * 0.075f), pele,
                    new Vector3(0, alturaTronco * (0.55f + i * 0.09f), h * 0.030f)));

            raiz.AddChild(tronco);

            // ---- pescoço e cabeça, sem rosto, dois olhos acesos
            var cabeca = new Node3D { Name = "Cabeca", Position = new Vector3(0, alturaOmbro, 0) };
            cabeca.AddChild(Capsula(h * 0.022f, h * 0.075f, pele, new Vector3(0, h * 0.035f, -h * 0.01f)));
            var cranio = Capsula(h * 0.048f, h * 0.075f, pele, new Vector3(0, h * 0.10f, -h * 0.022f));
            cranio.RotateX(0.35f);      // cabeça baixa, olhando de baixo para cima
            cabeca.AddChild(cranio);

            foreach (int lado in new[] { -1, 1 })
                cabeca.AddChild(Esfera(h * 0.0085f, olho,
                    new Vector3(lado * h * 0.020f, h * 0.105f, -h * 0.055f)));
            raiz.AddChild(cabeca);

            // ---- braços: longos demais, quase encostando no chão
            foreach (int lado in new[] { -1, 1 })
            {
                var braco = new Node3D
                {
                    Name = lado < 0 ? "BracoE" : "BracoD",
                    Position = new Vector3(lado * h * 0.075f, alturaOmbro - h * 0.02f, 0)
                };
                braco.AddChild(Capsula(h * 0.026f, h * 0.20f, pele, new Vector3(0, -h * 0.10f, 0)));
                braco.AddChild(Capsula(h * 0.021f, h * 0.22f, pele, new Vector3(0, -h * 0.31f, 0)));

                // mão: uma palma e três dedos compridos
                var mao = new Node3D { Position = new Vector3(0, -h * 0.43f, 0) };
                mao.AddChild(Caixa(new Vector3(h * 0.035f, h * 0.045f, h * 0.018f), pele, Vector3.Zero));
                for (int d = -1; d <= 1; d++)
                    mao.AddChild(Capsula(h * 0.006f, h * 0.055f, pele,
                        new Vector3(d * h * 0.013f, -h * 0.045f, 0)));
                braco.AddChild(mao);

                raiz.AddChild(braco);
            }

            return raiz;
        }

        static MeshInstance3D Capsula(float raio, float altura, Material mat, Vector3 pos)
            => new()
            {
                Mesh = new CapsuleMesh { Radius = raio, Height = Mathf.Max(altura, raio * 2.05f),
                                         RadialSegments = 10, Rings = 4 },
                MaterialOverride = mat,
                Position = pos
            };

        static MeshInstance3D Esfera(float raio, Material mat, Vector3 pos)
            => new()
            {
                Mesh = new SphereMesh { Radius = raio, Height = raio * 2, RadialSegments = 8, Rings = 4 },
                MaterialOverride = mat,
                Position = pos
            };
    }
}

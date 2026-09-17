using System.Collections.Generic;
using Godot;

namespace TurnoDaNoite.Jogo
{
    /// <summary>
    /// O que cada coisa do jogo é, visualmente.
    /// </summary>
    public enum Peca
    {
        Parede, Piso, Teto, Armario, QuadroEletrico, Portao,
        Fusivel, Bateria, Criatura, Luminaria, Caixote, Barril, Cano,
        Mao,
        CaixaFerramentas, Gaveteiro, Prateleira,
        Bancada, Pilha, Entulho
    }

    /// <summary>
    /// Registro de assets — a peça que torna este projeto vendável.
    ///
    /// Cada coisa do jogo pede um modelo por nome. Se o arquivo existir em
    /// assets/modelos, ele é usado; se não existir, entra uma primitiva no lugar
    /// e o jogo continua rodando. Ou seja: dá para jogar hoje com caixas e, no dia
    /// em que você baixar modelos de verdade, é só soltar os arquivos na pasta com
    /// o nome certo — nenhuma linha de código muda.
    ///
    /// Formatos que o Godot importa sozinho: .glb, .gltf, .obj, .fbx, .blend.
    /// Onde achar, com licença para uso comercial, está em docs/ASSETS.md.
    /// </summary>
    public static class Props
    {
        /// <summary>Nome do arquivo (sem extensão) esperado em assets/modelos.</summary>
        static readonly Dictionary<Peca, string> Arquivos = new()
        {
            { Peca.Parede,         "parede" },
            { Peca.Piso,           "piso" },
            { Peca.Teto,           "teto" },
            { Peca.Armario,        "armario" },
            { Peca.QuadroEletrico, "quadro_eletrico" },
            { Peca.Portao,         "portao" },
            { Peca.Fusivel,        "fusivel" },
            { Peca.Bateria,        "bateria" },
            { Peca.Criatura,       "criatura" },
            { Peca.Luminaria,      "luminaria" },
            { Peca.Caixote,        "caixote" },
            { Peca.Barril,         "barril" },
            { Peca.Cano,           "cano" },
            { Peca.Mao,            "mao_com_lanterna" },
            { Peca.CaixaFerramentas, "caixa_ferramentas" },
            { Peca.Gaveteiro,        "gaveteiro" },
            { Peca.Prateleira,       "prateleira" },
            { Peca.Bancada,          "bancada" },
            { Peca.Pilha,            "pilha" },
            { Peca.Entulho,          "entulho" }
        };

        static readonly string[] Extensoes = { ".glb", ".gltf", ".obj", ".fbx", ".tscn" };
        static readonly Dictionary<Peca, PackedScene> Cache = new();
        static readonly HashSet<Peca> SemModelo = new();

        /// <summary>Quantas peças ainda estão usando primitiva. Vira aviso na tela de opções.</summary>
        public static int PecasSemModelo => SemModelo.Count;

        static PackedScene Carregar(Peca peca)
        {
            if (Cache.TryGetValue(peca, out var pronto)) return pronto;
            if (SemModelo.Contains(peca)) return null;

            foreach (string ext in Extensoes)
            {
                string caminho = $"res://assets/modelos/{Arquivos[peca]}{ext}";
                if (!ResourceLoader.Exists(caminho)) continue;

                var cena = GD.Load<PackedScene>(caminho);
                if (cena == null) continue;
                Cache[peca] = cena;
                return cena;
            }

            SemModelo.Add(peca);
            return null;
        }

        /// <summary>
        /// Devolve o nó visual da peça: o modelo importado, se houver, ou a
        /// primitiva equivalente. Quem chama não precisa saber qual dos dois veio.
        /// </summary>
        public static Node3D Criar(Peca peca, Vector3 tamanho, Material fallback)
        {
            // O visual vai SEMPRE dentro de um nó pai vazio. Sem esse invólucro,
            // quem chama define .Position e apaga o deslocamento que apoia a peça
            // no chão — foi assim que as paredes nasceram com o centro na altura 0
            // e o jogador passou a enxergar por cima de todas elas.
            var recipiente = new Node3D();

            var cena = Carregar(peca);
            if (cena != null)
            {
                var no = cena.Instantiate<Node3D>();
                EncaixarNoTamanho(no, tamanho);
                recipiente.AddChild(no);
            }
            else recipiente.AddChild(Primitiva(peca, tamanho, fallback));

            return recipiente;
        }

        /// <summary>
        /// Escala o modelo importado para caber na caixa que o jogo reservou.
        /// Sem isto, um modelo baixado em centímetros vira um monstro de 200 m.
        /// </summary>
        static void EncaixarNoTamanho(Node3D no, Vector3 alvo)
        {
            var aabb = CalcularAabb(no);
            if (aabb.Size.X <= 0.0001f || aabb.Size.Y <= 0.0001f || aabb.Size.Z <= 0.0001f) return;

            float escala = Mathf.Min(alvo.X / aabb.Size.X,
                           Mathf.Min(alvo.Y / aabb.Size.Y, alvo.Z / aabb.Size.Z));
            no.Scale = new Vector3(escala, escala, escala);
            // apoia a base no chão em vez de centralizar no pivô do artista
            no.Position = new Vector3(0, -aabb.Position.Y * escala, 0);
        }

        static Aabb CalcularAabb(Node3D raiz)
        {
            var total = new Aabb();
            bool primeiro = true;
            foreach (var filho in Todos(raiz))
            {
                if (filho is not VisualInstance3D vis) continue;
                var caixa = vis.GetAabb();
                if (primeiro) { total = caixa; primeiro = false; }
                else total = total.Merge(caixa);
            }
            return total;
        }

        static IEnumerable<Node> Todos(Node raiz)
        {
            yield return raiz;
            foreach (var f in raiz.GetChildren())
                foreach (var n in Todos(f)) yield return n;
        }

        /// <summary>Formas provisórias. Feias de propósito: é para dar vontade de trocar.</summary>
        static Node3D Primitiva(Peca peca, Vector3 tamanho, Material material)
        {
            // Peças que o jogador olha de perto são montadas com várias formas
            // em Modelos.cs. Um cubo brilhante lê como cubo brilhante; um
            // cilindro cerâmico com dois terminais lê como fusível.
            switch (peca)
            {
                case Peca.Fusivel:        return Modelos.Fusivel();
                case Peca.Bateria:        return Modelos.Bateria();
                case Peca.Armario:        return Modelos.Armario(tamanho);
                case Peca.QuadroEletrico: return Modelos.QuadroEletrico(tamanho);
                case Peca.Portao:         return Modelos.Porta(tamanho);
                case Peca.Caixote:        return Modelos.Caixote(tamanho);
                case Peca.Barril:         return Modelos.Barril(tamanho);
                case Peca.CaixaFerramentas: return Modelos.CaixaFerramentas();
                case Peca.Gaveteiro:      return Modelos.Gaveteiro();
                case Peca.Prateleira:     return Modelos.Prateleira();
                case Peca.Bancada:        return Modelos.Bancada();
                case Peca.Pilha:          return Modelos.Pilha();
                case Peca.Entulho:        return Modelos.Entulho();
                case Peca.Mao:            return Modelos.MaoComLanterna();
            }

            Mesh malha = peca switch
            {
                Peca.Fusivel or Peca.Bateria => new BoxMesh { Size = tamanho },
                Peca.Barril => new CylinderMesh
                {
                    TopRadius = tamanho.X / 2, BottomRadius = tamanho.X / 2, Height = tamanho.Y
                },
                Peca.Cano => new CylinderMesh
                {
                    TopRadius = tamanho.X / 2, BottomRadius = tamanho.X / 2, Height = tamanho.Y, RadialSegments = 8
                },
                Peca.Piso or Peca.Teto => new PlaneMesh { Size = new Vector2(tamanho.X, tamanho.Z) },
                _ => new BoxMesh { Size = tamanho }
            };

            var mi = new MeshInstance3D { Mesh = malha, MaterialOverride = material };

            if (peca is not (Peca.Piso or Peca.Teto))
                mi.Position = new Vector3(0, tamanho.Y / 2f, 0);

            return mi;
        }

        /// <summary>Pecas que tem modelo montado em codigo quando falta arquivo.</summary>
        static readonly HashSet<Peca> Montadas = new()
        {
            Peca.Fusivel, Peca.Bateria, Peca.Armario, Peca.QuadroEletrico,
            Peca.Portao, Peca.Caixote, Peca.Barril,
            Peca.CaixaFerramentas, Peca.Gaveteiro, Peca.Prateleira,
            Peca.Bancada, Peca.Pilha, Peca.Entulho, Peca.Mao
        };

        /// <summary>
        /// O que ainda falta de arte. Separa as duas coisas de proposito: peca
        /// montada em codigo ja tem forma reconhecivel, peca crua ainda e um
        /// cubo. Dizer "14 em primitiva" escondia essa diferenca.
        /// </summary>
        public static string Relatorio()
        {
            var montadas = new List<string>();
            var cruas = new List<string>();
            foreach (var par in Arquivos)
            {
                Carregar(par.Key);
                if (!SemModelo.Contains(par.Key)) continue;
                (Montadas.Contains(par.Key) ? montadas : cruas).Add(par.Value);
            }

            if (montadas.Count == 0 && cruas.Count == 0) return "todos os modelos importados";

            string texto = "";
            if (montadas.Count > 0)
                texto += $"montadas em codigo ({montadas.Count}): {string.Join(", ", montadas)}";
            if (cruas.Count > 0)
                texto += (texto.Length > 0 ? "\n" : "") +
                         $"ainda forma crua ({cruas.Count}): {string.Join(", ", cruas)}";
            return texto;
        }
    }
}

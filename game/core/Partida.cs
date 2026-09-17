using System;
using System.Collections.Generic;

namespace TurnoDaNoite.Core
{
    public enum Fase { Parada, Jogando, Morto, Escapou }

    public enum EstadoCriatura { Patrulha, Investiga, Caca, Procura }

    public enum Evento
    {
        Passo, PassoDela, Respiracao, Ofegante, Batida,
        PegouFusivel, PegouBateria, InstalouFusivel, EnergiaVoltou,
        AbriuArmario, FechouArmario, LanternaLigou, LanternaApagou,
        ElaViuVoce, ElaRugiu, ElaArranhou, VocePegou, VoceEscapou, PortaoTrancado,
        PortaoBateu, AbriuRecipiente, RecipienteVazio,
        Escada, PegouMapa
    }

    public struct Comando
    {
        public float FrenteX, FrenteZ;   // direção já resolvida no referencial do mundo
        public float Giro, Inclinacao;   // radianos, absolutos
        public bool Correr, Agachar, PrenderAr;
        public bool Interagir, AlternarLanterna;
    }

    public enum TipoItem { Fusivel, Bateria, Mapa }

    public sealed class Item
    {
        public P2 Pos;
        public int Andar;
        public bool Recolhido;
        /// <summary>Indice do recipiente que guarda este item, ou -1 se estiver solto.</summary>
        public int Dentro = -1;
        /// <summary>So da para pegar depois de abrir o recipiente.</summary>
        public bool Alcancavel(Partida p) => Dentro < 0 || p.Recipientes[Dentro].Aberto;
    }

    public sealed class Armario
    {
        public P2 Pos;
        public int Andar;
    }

    public sealed class Jogador
    {
        public P2 Pos;
        /// <summary>
        /// Em que piso você está. Posição sem andar não identifica lugar nenhum
        /// num prédio de dois: existe um ponto (12, 7) embaixo e outro em cima.
        /// </summary>
        public int Andar;
        public float Giro, Inclinacao;
        public bool Agachado, Correndo, Escondido, PrendendoAr;
        public float Folego = 1f, Bateria = 1f, Ar = 1f;
        public bool Lanterna = true;
        public int FusiveisNaMao, FusiveisInstalados;
        public float Medo;

        /// <summary>
        /// Achou a planta do prédio. Sem ela o TAB não mostra mapa nenhum —
        /// o mapa é um item que se procura, não um botão que sempre esteve lá.
        /// </summary>
        public bool TemMapa;

        public bool LuzAcesa => Lanterna && Bateria > 0f;
        public float AlturaOlho => Escondido ? 0.95f
            : (Agachado ? Regras.AlturaOlhoAgachado : Regras.AlturaOlhoEmPe);

        public P2 Frente => new P2(-MathF.Sin(Giro), -MathF.Cos(Giro));
    }

    public sealed class Criatura
    {
        public P2 Pos, Velocidade;
        public int Andar;
        public EstadoCriatura Estado = EstadoCriatura.Patrulha;
        public List<Celula> Caminho = new();
        public int PassoDoCaminho;
        public float TempoRecalculo, Paciencia, SemPista, TempoPasso, TempoGrunhido;
        public P2? UltimaPista;
        /// <summary>Em que andar a pista foi deixada. Sem isto ela procura no piso errado.</summary>
        public int AndarDaPista;
        public float Agressao;

        /// <summary>
        /// Está fechando o cerco: sabe mais ou menos onde você está e vem andando
        /// até lá. Precisa ser um estado, e não um destino solto — enquanto era só
        /// um destino, o sorteio de patrulha seguinte o descartava oito segundos
        /// depois e ela nunca terminava a caminhada. Ficar parado era seguro.
        /// </summary>
        public bool Cercando;

        public P2 Direcao
        {
            get
            {
                float m = Velocidade.Comprimento;
                return m < 0.05f ? new P2(0, 1) : new P2(Velocidade.X / m, Velocidade.Z / m);
            }
        }
    }

    /// <summary>
    /// A partida inteira, sem uma linha de motor gráfico. O Godot lê este estado
    /// e desenha; os testes rodam a mesma coisa sem janela nenhuma. O gerador
    /// aleatório recebe semente, então uma partida pode ser reproduzida.
    /// </summary>
    public sealed class Partida
    {
        public Fase Fase { get; private set; } = Fase.Parada;
        public Predio Predio { get; }
        public Jogador Jogador { get; } = new();
        public Criatura Ela { get; } = new();

        public List<Item> Fusiveis { get; } = new();
        public List<Item> Baterias { get; } = new();
        public List<Armario> Armarios { get; } = new();
        public List<Recipiente> Recipientes { get; } = new();
        /// <summary>Cenário: caixotes, tambores, bancadas, canos. Só os sólidos empurram.</summary>
        public List<Adorno> Adornos { get; private set; } = new();
        public P2 Quadro { get; private set; }
        /// <summary>O quadro fica no andar de cima: é o que obriga a escada a servir para alguma coisa.</summary>
        public int QuadroAndar { get; private set; }
        public P2 Portao { get; private set; }
        public bool PortaoAberto { get; private set; }

        /// <summary>A planta do prédio, guardada em algum recipiente do térreo.</summary>
        public Item MapaItem { get; private set; }

        /// <summary>
        /// Cômodos em que você já pôs o pé. O mapa só desenha estes: planta
        /// achada não é o mesmo que prédio conhecido, e revelar tudo de uma vez
        /// acabaria com a única coisa que o mapa tinha de bom, que é ver o
        /// desenho do lugar onde você está perdido enchendo aos poucos.
        /// </summary>
        public HashSet<string> SalasVistas { get; } = new();

        /// <summary>Células por onde você passou. São elas que desenham os corredores no mapa.</summary>
        public HashSet<int> CelulasVistas { get; } = new();

        public List<Evento> Eventos { get; } = new();
        public float Tempo { get; private set; }
        /// <summary>Posição do último som dela, para o renderizador colocar o áudio no lugar certo.</summary>
        public P2 UltimoSomDela { get; private set; }

        readonly Random _rng;
        bool _portaBateu;
        float _tempoPasso, _tempoRespiracao, _tempoBatida;
        /// <summary>Segundos antes de a escada poder ser usada de novo. Sem isto você sobe e desce no mesmo quadro.</summary>
        float _travaEscada;
        float _travaEscadaDela;

        public Partida(int semente = 0)
        {
            _rng = semente == 0 ? new Random() : new Random(semente);
            // a planta usa a MESMA semente: partida reproduzível é o que permite
            // um teste dizer "nesta semente o prédio fica assim" e continuar valendo
            Predio = new Predio(semente == 0 ? _rng.Next(1, int.MaxValue) : semente);
        }

        // ------------------------------------------------------------- começo

        /// <summary>Monta o prédio e distribui os itens. Repete até a planta ser jogável.</summary>
        public bool Comecar()
        {
            for (int tentativa = 0; tentativa < 12; tentativa++)
            {
                Predio.Construir();
                Distribuir();
                if (Validar().Count == 0)
                {
                    Fase = Fase.Jogando;
                    Tempo = 0;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Já tem alguma coisa ocupando este ponto? Vale para recipiente, armário
        /// e também para o quadro e a porta: um armário nascido em cima do quadro
        /// rouba a interação dele, e aí não há como instalar fusível nenhum — a
        /// partida fica impossível sem nada na tela explicando o porquê.
        /// </summary>
        bool TemAlgoPerto(P2 p, int andar, float limite)
        {
            foreach (var r in Recipientes)
                if (r.Andar == andar && P2.Distancia(p, r.Pos) < limite) return true;
            foreach (var a in Armarios)
                if (a.Andar == andar && P2.Distancia(p, a.Pos) < limite) return true;

            if (andar == QuadroAndar && P2.Distancia(p, Quadro) < limite) return true;
            if (andar == 0 && P2.Distancia(p, Portao) < limite) return true;

            // o poço da escada também: móvel em cima dele tranca o andar de cima
            foreach (var e in Predio.Escadas)
            {
                if (e.De != andar && e.Para != andar) continue;
                if (P2.Distancia(p, Predio.ParaMundo(new Celula(e.Cx, e.Cz, andar))) < limite + 1.4f)
                    return true;
            }
            return false;
        }

        void Distribuir()
        {
            Fusiveis.Clear(); Baterias.Clear(); Armarios.Clear(); Eventos.Clear();

            // Comeca do lado de fora. Entrar no predio pela porta da frente e a
            // primeira coisa que voce faz, e da peso ao momento em que ela bate.
            var patio = Predio.Sala(TipoSala.Patio);
            Jogador.Pos = Predio.ParaMundo(patio.Centro);
            Jogador.Andar = 0;
            Jogador.Giro = 0; Jogador.Inclinacao = 0;
            Jogador.Folego = 1; Jogador.Bateria = 1; Jogador.Ar = 1;
            Jogador.Lanterna = true; Jogador.Escondido = false;
            Jogador.FusiveisNaMao = 0; Jogador.FusiveisInstalados = 0;
            Jogador.Medo = 0; Jogador.TemMapa = false;
            SalasVistas.Clear();
            CelulasVistas.Clear();

            Portao = Predio.ParaMundo(new Celula(Predio.PortaX + 1, Predio.PortaZ, 0));
            PortaoAberto = true;    // aberta: e por ela que voce entra
            _portaBateu = false;

            var salaQuadro = Predio.Sala(TipoSala.Quadro);
            Quadro = Predio.ParaMundo(salaQuadro.Centro);
            QuadroAndar = salaQuadro.Andar;

            // Recipientes primeiro: e dentro deles que tudo vai parar. Item
            // largado no chao brilhando entrega a sala inteira de longe; item
            // guardado obriga a entrar, revistar e ficar exposto enquanto isso.
            Recipientes.Clear();
            foreach (var s in Predio.Salas)
            {
                if (s.Tipo == TipoSala.Patio) continue;
                if (_rng.NextDouble() > Regras.FracaoDeSalasComRecipiente) continue;

                for (int k = 0; k < Regras.RecipientesPorSala; k++)
                {
                    // PontoLivre sorteia uma célula qualquer da sala, e sorteio
                    // repete: dois recipientes saíram no mesmo ponto, um dentro
                    // do outro, e o de fora escondia o fusível do de dentro para
                    // sempre. Tenta de novo até achar lugar só dele.
                    P2? achado = null;
                    for (int tentativa = 0; tentativa < 24 && achado == null; tentativa++)
                    {
                        var candidato = Predio.ParaMundo(Predio.PontoLivre(s, _rng));
                        if (!TemAlgoPerto(candidato, s.Andar, Regras.EspacoEntreMoveis)) achado = candidato;
                    }
                    if (achado == null) continue;

                    Recipientes.Add(new Recipiente
                    {
                        Pos = achado.Value,
                        Andar = s.Andar,
                        Tipo = (TipoRecipiente)_rng.Next(3)
                    });
                }
            }

            // sorteia quais recipientes guardam o quê; o resto fica vazio
            var indices = new List<int>();
            for (int i = 0; i < Recipientes.Count; i++)
            {
                // nada na portaria: o primeiro fusivel nao pode estar na entrada
                var sala = Predio.SalaEm(Recipientes[i].Pos, Recipientes[i].Andar);
                if (sala?.Tipo == TipoSala.Portaria) continue;
                indices.Add(i);
            }
            Embaralhar(indices);

            int posto = 0;
            for (int i = 0; i < Regras.FusiveisNecessarios && posto < indices.Count; i++, posto++)
            {
                var r = Recipientes[indices[posto]];
                r.FusivelDentro = Fusiveis.Count;
                Fusiveis.Add(new Item { Pos = r.Pos, Andar = r.Andar, Dentro = indices[posto] });
            }
            for (int i = 0; i < Regras.BateriasNoMapa && posto < indices.Count; i++, posto++)
            {
                var r = Recipientes[indices[posto]];
                r.BateriaDentro = Baterias.Count;
                Baterias.Add(new Item { Pos = r.Pos, Andar = r.Andar, Dentro = indices[posto] });
            }

            // A planta do prédio: um item só, no térreo, e nunca na portaria.
            // Fica no térreo de propósito — encontrar o mapa tem de ser possível
            // antes de subir, senão ele só chega quando você já decorou o lugar.
            MapaItem = null;
            for (; posto < indices.Count; posto++)
            {
                var r = Recipientes[indices[posto]];
                if (r.Andar != 0) continue;
                r.MapaDentro = true;
                MapaItem = new Item { Pos = r.Pos, Andar = r.Andar, Dentro = indices[posto] };
                posto++;
                break;
            }

            foreach (var s in Predio.Salas)
            {
                if (s.Tipo == TipoSala.Portaria || s.Tipo == TipoSala.Patio) continue;
                if (_rng.NextDouble() > Regras.FracaoDeSalasComArmario) continue;

                for (int k = 0; k < Regras.ArmariosPorSala; k++)
                {
                    P2? achado = null;
                    for (int tentativa = 0; tentativa < 24 && achado == null; tentativa++)
                    {
                        var candidato = Predio.ParaMundo(Predio.PontoLivre(s, _rng, 0));
                        if (!TemAlgoPerto(candidato, s.Andar, Regras.EspacoEntreMoveis)) achado = candidato;
                    }
                    if (achado != null) Armarios.Add(new Armario { Pos = achado.Value, Andar = s.Andar });
                }
            }

            // Cenário por último: ele precisa saber o que já está no chão para
            // não nascer em cima de um gaveteiro nem entupir uma passagem.
            var tomados = new List<(P2, int)>();
            foreach (var r in Recipientes) tomados.Add((r.Pos, r.Andar));
            foreach (var a in Armarios) tomados.Add((a.Pos, a.Andar));
            tomados.Add((Quadro, QuadroAndar));
            tomados.Add((Portao, 0));
            Adornos = Cenario.Montar(Predio, _rng, tomados);

            var camara = Predio.Sala(TipoSala.Camara);
            Ela.Pos = Predio.ParaMundo(camara.Centro);
            Ela.Andar = camara.Andar;
            Ela.Estado = EstadoCriatura.Patrulha;
            Ela.Caminho.Clear();
            Ela.PassoDoCaminho = 0;
            Ela.TempoRecalculo = 0;
            Ela.UltimaPista = null;
            Ela.Agressao = 0;
            Ela.SemPista = 0;
            Ela.Cercando = false;
            Ela.Velocidade = default;
        }

        void Embaralhar<T>(List<T> lista)
        {
            for (int i = lista.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (lista[i], lista[j]) = (lista[j], lista[i]);
            }
        }

        /// <summary>Lista o que ficou inalcançável. Vazia significa planta jogável.</summary>
        public List<string> Validar()
        {
            var falhas = new List<string>();
            var inicio = Predio.ParaCelula(Jogador.Pos, Jogador.Andar);

            if (Predio.Escadas.Count == 0 && Predio.Andares > 1) falhas.Add("nenhuma escada");

            for (int i = 0; i < Fusiveis.Count; i++)
                if (!Predio.Alcancavel(inicio, Predio.ParaCelula(Fusiveis[i].Pos, Fusiveis[i].Andar)))
                    falhas.Add($"fusível {i + 1}");

            for (int i = 0; i < Recipientes.Count; i++)
                if (!Predio.Alcancavel(inicio, Predio.ParaCelula(Recipientes[i].Pos, Recipientes[i].Andar)))
                    falhas.Add($"recipiente {i + 1}");

            if (MapaItem == null) falhas.Add("planta do prédio sem lugar");

            if (!Predio.Alcancavel(inicio, Predio.ParaCelula(Quadro, QuadroAndar))) falhas.Add("quadro");
            if (!Predio.Alcancavel(inicio, Predio.ParaCelula(Ela.Pos, Ela.Andar))) falhas.Add("criatura");
            if (Ela.Andar == Jogador.Andar &&
                P2.Distancia(Ela.Pos, Jogador.Pos) < Regras.DistanciaInicialMinima)
                falhas.Add("criatura perto demais");

            return falhas;
        }

        // ------------------------------------------------------------- passo

        public void Passo(float dt, Comando cmd)
        {
            if (Fase != Fase.Jogando) return;
            if (dt > 0.05f) dt = 0.05f;   // trava: quadro lento não pode atravessar parede

            Eventos.Clear();
            Tempo += dt;
            _travaEscada = Math.Max(0, _travaEscada - dt);
            _travaEscadaDela = Math.Max(0, _travaEscadaDela - dt);

            Jogador.Giro = cmd.Giro;
            Jogador.Inclinacao = Math.Clamp(cmd.Inclinacao, -1.4f, 1.4f);

            PassoJogador(dt, cmd);
            PassoCriatura(dt);
            PassoMedo(dt);
        }

        void PassoJogador(float dt, Comando cmd)
        {
            if (cmd.AlternarLanterna) AlternarLanterna();
            if (cmd.Interagir) Interagir();

            Jogador.Agachado = cmd.Agachar;
            Jogador.PrendendoAr = cmd.PrenderAr && Jogador.Escondido;

            if (Jogador.Escondido)
            {
                if (Jogador.PrendendoAr)
                {
                    Jogador.Ar = Math.Max(0, Jogador.Ar - dt * Regras.GastoAr);
                    if (Jogador.Ar <= 0) { Jogador.PrendendoAr = false; Eventos.Add(Evento.Ofegante); }
                }
                else
                {
                    Jogador.Ar = Math.Min(1, Jogador.Ar + dt * Regras.GanhoAr);
                    _tempoRespiracao -= dt;
                    if (_tempoRespiracao <= 0) { _tempoRespiracao = 2.4f; Eventos.Add(Evento.Respiracao); }
                }
                Jogador.Folego = Math.Min(1, Jogador.Folego + dt * 0.30f);
                GastarBateria(dt);
                return;
            }

            bool querCorrer = cmd.Correr && Jogador.Folego > Regras.FolegoMinimoParaCorrer && !Jogador.Agachado;
            var mov = new P2(cmd.FrenteX, cmd.FrenteZ);
            float mag = mov.Comprimento;
            Jogador.Correndo = mag > 0 && querCorrer;

            float vel = Jogador.Agachado ? Regras.VelAgachado
                      : (Jogador.Correndo ? Regras.VelCorrendo : Regras.VelAndando);

            Jogador.Folego = Jogador.Correndo
                ? Math.Max(0, Jogador.Folego - dt * Regras.GastoFolego)
                : Math.Min(1, Jogador.Folego + dt * (Jogador.Agachado ? Regras.GanhoFolegoAgachado : Regras.GanhoFolego));

            if (mag > 0)
            {
                Jogador.Pos += mov.Normalizado * (vel * dt);
                Jogador.Pos = Predio.EmpurrarFora(Jogador.Pos, Regras.RaioJogador, Jogador.Andar);
                Jogador.Pos = Cenario.Empurrar(Jogador.Pos, Regras.RaioJogador, Adornos, Jogador.Andar);
                // a parede tem a última palavra: adorno encostado nela não pode
                // ser a coisa que te empurra para dentro do concreto
                Jogador.Pos = Predio.EmpurrarFora(Jogador.Pos, Regras.RaioJogador, Jogador.Andar);
                UsarEscadaSePisar();
                AnotarSala();

                _tempoPasso -= dt * vel;
                if (_tempoPasso <= 0)
                {
                    // Distância entre passadas, em metros. Antes era 4,2 andando:
                    // a 3,25 m/s isso dá uma passada a cada 1,3 s, que é o ritmo
                    // de quem passeia, não de quem anda. Soava errado antes mesmo
                    // de o som tocar. Agora é o comprimento de passada de gente:
                    // ~0,6 m andando, mais curta correndo, mais longa agachado.
                    _tempoPasso = Jogador.Correndo
                        ? Regras.PassadaCorrendo
                        : (Jogador.Agachado ? Regras.PassadaAgachado : Regras.PassadaAndando);
                    Eventos.Add(Evento.Passo);
                    FazerBarulho(Jogador.Correndo ? Regras.RuidoCorrendo
                               : Jogador.Agachado ? Regras.RuidoAgachado : Regras.RuidoAndando);
                }
            }

            _tempoRespiracao -= dt;
            if (_tempoRespiracao <= 0)
            {
                bool cansado = Jogador.Folego < Regras.FolegoOfegante;
                _tempoRespiracao = cansado ? 1.5f : 5.5f;
                if (cansado) { Eventos.Add(Evento.Ofegante); FazerBarulho(Regras.RuidoOfegante); }
                // respiração assustada só com ela perto: andando calmo pelo
                // prédio vazio não há por que ficar arfando no ouvido de quem joga
                else if (Jogador.Medo > Regras.MedoParaARespiracao) Eventos.Add(Evento.Respiracao);
            }

            GastarBateria(dt);
        }

        /// <summary>
        /// Pisou no poço da escada: troca de andar.
        ///
        /// Acontece ao pisar, sem apertar tecla. Escada com prompt de "E" para
        /// no meio da corrida — e escada é justamente o lugar onde você mais
        /// precisa não parar, porque é onde ela te alcança.
        ///
        /// O <see cref="_travaEscada"/> existe porque, sem ele, você chega em
        /// cima já em cima de uma escada e desce de volta no mesmo quadro,
        /// ficando preso subindo e descendo para sempre.
        /// </summary>
        void UsarEscadaSePisar()
        {
            if (_travaEscada > 0) return;

            var c = Predio.ParaCelula(Jogador.Pos, Jogador.Andar);
            var escada = Predio.EscadaEm(c);
            if (escada == null) return;

            Jogador.Andar = Predio.OutroAndar(escada, Jogador.Andar);
            _travaEscada = Regras.EsperaDaEscada;
            Eventos.Add(Evento.Escada);
            FazerBarulho(Regras.RuidoEscada);
            AnotarSala();
        }

        /// <summary>
        /// Marca onde você esteve, para o mapa ir se preenchendo.
        ///
        /// Anota o cômodo E a célula. A célula é o que faz o mapa mostrar os
        /// corredores: sem ela a planta vira uma dúzia de caixas soltas no
        /// escuro, sem nada indicando como uma leva à outra, que é exatamente
        /// a informação que se procura num mapa.
        /// </summary>
        void AnotarSala()
        {
            var sala = Predio.SalaEm(Jogador.Pos, Jogador.Andar);
            if (sala != null) SalasVistas.Add(ChaveDaSala(sala));

            // marca um quadrado de células em volta: você enxerga o corredor
            // inteiro à sua volta, não só o chão exato em que pisa
            var c = Predio.ParaCelula(Jogador.Pos, Jogador.Andar);
            for (int dz = -1; dz <= 1; dz++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = c.Cx + dx, nz = c.Cz + dz;
                    if (Predio.EhParede(nx, nz, Jogador.Andar)) continue;
                    CelulasVistas.Add(ChaveDaCelula(nx, nz, Jogador.Andar));
                }
        }

        public static int ChaveDaCelula(int cx, int cz, int andar) => (andar * 4096 + cz) * 4096 + cx;

        public bool JaViu(int cx, int cz, int andar) =>
            CelulasVistas.Contains(ChaveDaCelula(cx, cz, andar));

        public static string ChaveDaSala(Sala s) => $"{s.Andar}:{s.X},{s.Z}";

        public bool JaViu(Sala s) => SalasVistas.Contains(ChaveDaSala(s));

        /// <summary>
        /// Marca todos os cômodos como visitados. Existe só para o modo de
        /// captura de tela: sem isto a foto da planta sai em branco, porque na
        /// prática o jogador ainda não andou por lugar nenhum.
        /// </summary>
        public void RevelarTudoParaCaptura()
        {
            foreach (var s in Predio.Salas) SalasVistas.Add(ChaveDaSala(s));
            for (int a = 0; a < Predio.Andares; a++)
                for (int z = 0; z < Predio.Profundidade; z++)
                    for (int x = 0; x < Predio.Largura; x++)
                        if (!Predio.EhParede(x, z, a)) CelulasVistas.Add(ChaveDaCelula(x, z, a));
        }

        /// <summary>
        /// Distância que respeita andar. Entre pisos diferentes ela é enorme de
        /// propósito: som, medo e visão não atravessam a laje, e tratar dois
        /// pontos em andares distintos como vizinhos fazia a criatura caçar
        /// você através do chão.
        /// </summary>
        public static float DistanciaReal(P2 a, int andarA, P2 b, int andarB) =>
            andarA == andarB ? P2.Distancia(a, b) : 1e6f;

        void GastarBateria(float dt)
        {
            if (!Jogador.Lanterna || Jogador.Bateria <= 0) return;
            Jogador.Bateria = Math.Max(0, Jogador.Bateria - dt * Regras.GastoBateria);
            if (Jogador.Bateria <= 0)
            {
                Jogador.Lanterna = false;
                Eventos.Add(Evento.LanternaApagou);
            }
        }

        public void AlternarLanterna()
        {
            if (Jogador.Bateria <= 0) return;
            Jogador.Lanterna = !Jogador.Lanterna;
            Eventos.Add(Jogador.Lanterna ? Evento.LanternaLigou : Evento.LanternaApagou);
        }

        // ------------------------------------------------------------- interação

        public enum Alvo { Nenhum, Fusivel, Bateria, Mapa, Armario, Quadro, Portao, Recipiente }

        /// <summary>
        /// Desconto na distância, por tipo de alvo. Não é enfeite: com trinta e
        /// seis recipientes espalhados, um gaveteiro a 1,2 m ganhava do armário
        /// a 1,3 m e o jogo simplesmente não deixava você se esconder. Esconder
        /// é a única defesa que existe, então o armário ganha as disputas
        /// apertadas; pegar o que está na sua frente ganha de abrir mais uma
        /// gaveta; e a saída ganha de tudo.
        /// </summary>
        static float Prioridade(Alvo a) => a switch
        {
            Alvo.Portao => 1.2f,
            Alvo.Quadro => 1.0f,
            Alvo.Armario => 0.9f,
            Alvo.Fusivel or Alvo.Bateria or Alvo.Mapa => 0.7f,
            _ => 0f
        };

        public Alvo AlvoMaisPerto(out object objeto)
        {
            var melhor = Alvo.Nenhum;
            object achado = null;
            float melhorNota = float.MaxValue;
            int andar = Jogador.Andar;

            void Testar(Alvo tipo, P2 pos, int andarDoAlvo, object obj)
            {
                if (andarDoAlvo != andar) return;
                float d = P2.Distancia(pos, Jogador.Pos);
                if (d > Regras.RaioInteracao) return;

                float nota = d - Prioridade(tipo);
                if (nota >= melhorNota) return;
                melhorNota = nota; melhor = tipo; achado = obj;
            }

            // itens só entram na conta depois que o recipiente foi aberto
            foreach (var f in Fusiveis)
                if (!f.Recolhido && f.Alcancavel(this)) Testar(Alvo.Fusivel, f.Pos, f.Andar, f);
            foreach (var b in Baterias)
                if (!b.Recolhido && b.Alcancavel(this)) Testar(Alvo.Bateria, b.Pos, b.Andar, b);
            if (MapaItem != null && !MapaItem.Recolhido && MapaItem.Alcancavel(this))
                Testar(Alvo.Mapa, MapaItem.Pos, MapaItem.Andar, MapaItem);

            foreach (var r in Recipientes)
                if (!r.Aberto) Testar(Alvo.Recipiente, r.Pos, r.Andar, r);
            foreach (var a in Armarios)
                Testar(Alvo.Armario, a.Pos, a.Andar, a);

            Testar(Alvo.Quadro, Quadro, QuadroAndar, null);
            Testar(Alvo.Portao, Portao, 0, null);

            objeto = achado;
            return melhor;
        }

        public void Interagir()
        {
            if (Jogador.Escondido)
            {
                Jogador.Escondido = false;
                Eventos.Add(Evento.FechouArmario);
                FazerBarulho(Regras.RuidoArmario);
                return;
            }

            var alvo = AlvoMaisPerto(out object obj);
            switch (alvo)
            {
                case Alvo.Fusivel:
                    ((Item)obj).Recolhido = true;
                    Jogador.FusiveisNaMao++;
                    Eventos.Add(Evento.PegouFusivel);

                    // O primeiro fusível fecha a porta da frente. É o momento em
                    // que o passeio vira prisão, e o barulho denuncia onde você está.
                    if (!_portaBateu)
                    {
                        _portaBateu = true;
                        PortaoAberto = false;
                        Eventos.Add(Evento.PortaoBateu);
                        FazerBarulho(Regras.RuidoQuadro);
                    }
                    break;

                case Alvo.Bateria:
                    ((Item)obj).Recolhido = true;
                    Jogador.Bateria = Math.Min(1, Jogador.Bateria + Regras.BateriaPorPilha);
                    Eventos.Add(Evento.PegouBateria);
                    break;

                case Alvo.Recipiente:
                    var rec = (Recipiente)obj;
                    rec.Aberto = true;
                    Eventos.Add(rec.TemAlgo ? Evento.AbriuRecipiente : Evento.RecipienteVazio);
                    // revistar faz barulho: e o preco de procurar
                    FazerBarulho(Regras.RuidoRevistar);
                    break;

                case Alvo.Mapa:
                    MapaItem.Recolhido = true;
                    Jogador.TemMapa = true;
                    Eventos.Add(Evento.PegouMapa);
                    break;

                case Alvo.Armario:
                    Jogador.Escondido = true;
                    var arm = (Armario)obj;
                    Jogador.Pos = arm.Pos;
                    Jogador.Andar = arm.Andar;
                    Eventos.Add(Evento.AbriuArmario);
                    FazerBarulho(Regras.RuidoArmario);
                    break;

                case Alvo.Quadro:
                    if (Jogador.FusiveisNaMao <= 0) break;
                    Jogador.FusiveisInstalados += Jogador.FusiveisNaMao;
                    Jogador.FusiveisNaMao = 0;
                    Ela.Agressao = Jogador.FusiveisInstalados;
                    Eventos.Add(Evento.InstalouFusivel);
                    FazerBarulho(Regras.RuidoQuadro);
                    if (Jogador.FusiveisInstalados >= Regras.FusiveisNecessarios)
                    {
                        PortaoAberto = true;
                        Eventos.Add(Evento.EnergiaVoltou);
                    }
                    break;

                case Alvo.Portao:
                    // Sair exige os cinco instalados, mesmo que a porta ainda
                    // esteja aberta no comeco: senao dava para "vencer" sem entrar.
                    if (Jogador.FusiveisInstalados >= Regras.FusiveisNecessarios)
                    {
                        Fase = Fase.Escapou;
                        Eventos.Add(Evento.VoceEscapou);
                    }
                    else Eventos.Add(Evento.PortaoTrancado);
                    break;
            }
        }

        // ------------------------------------------------------------- criatura

        void FazerBarulho(float raio)
        {
            if (Ela.Estado == EstadoCriatura.Caca) return;

            // Laje abafa. Do andar de cima ela ainda ouve um barulho grande —
            // é o que impede o piso de cima de virar abrigo seguro — mas só
            // uma fração dele, e nunca o de andar agachado.
            float alcance = Ela.Andar == Jogador.Andar ? raio : raio * Regras.BarulhoAtravessaLaje;
            if (P2.Distancia(Ela.Pos, Jogador.Pos) > alcance) return;

            Ela.UltimaPista = Jogador.Pos;
            Ela.AndarDaPista = Jogador.Andar;
            if (Ela.Estado != EstadoCriatura.Investiga)
            {
                Ela.Estado = EstadoCriatura.Investiga;
                Ela.TempoRecalculo = 0;
            }
            Ela.Paciencia = Regras.PacienciaInvestiga;
        }

        public float AlcanceDeVisao()
        {
            if (Jogador.Escondido) return 0;
            if (Jogador.LuzAcesa) return Regras.VisaoComLanterna;
            float r = Jogador.Agachado ? Regras.VisaoAgachado : Regras.VisaoEmPe;
            return Jogador.Correndo ? r + Regras.BonusVisaoCorrendo : r;
        }

        public bool ElaTeVe()
        {
            float alcance = AlcanceDeVisao();
            if (alcance <= 0) return false;
            // o piso é opaco: ninguém enxerga através da laje
            if (Ela.Andar != Jogador.Andar) return false;

            var para = Jogador.Pos - Ela.Pos;
            float d = para.Comprimento;
            if (d > alcance) return false;

            // Com a lanterna acesa ela nota a luz de qualquer ângulo; no escuro,
            // só te vê se estiver olhando na sua direção.
            if (d > 2f && !Jogador.LuzAcesa)
                if (P2.Escalar(para.Normalizado, Ela.Direcao) < Regras.CossenoCampoVisao) return false;

            return Predio.Visivel(Ela.Pos, Jogador.Pos, Ela.Andar);
        }

        void PassoCriatura(float dt)
        {
            float distJ = DistanciaReal(Ela.Pos, Ela.Andar, Jogador.Pos, Jogador.Andar);
            bool enxerga = ElaTeVe();

            if (enxerga)
            {
                if (Ela.Estado != EstadoCriatura.Caca)
                {
                    Ela.Estado = EstadoCriatura.Caca;
                    Ela.TempoRecalculo = 0;
                    Eventos.Add(Evento.ElaViuVoce);
                    UltimoSomDela = Ela.Pos;
                }
                Ela.UltimaPista = Jogador.Pos;
                Ela.AndarDaPista = Jogador.Andar;
                Ela.Paciencia = Regras.PacienciaCaca;
            }
            else if (Ela.Estado == EstadoCriatura.Caca)
            {
                Ela.Paciencia -= dt;
                if (Ela.Paciencia <= 0)
                {
                    Ela.Estado = EstadoCriatura.Procura;
                    Ela.Paciencia = Regras.PacienciaProcura;
                    Ela.TempoRecalculo = 0;
                }
            }
            else if (Ela.Estado is EstadoCriatura.Investiga or EstadoCriatura.Procura)
            {
                Ela.Paciencia -= dt;
                if (Ela.Paciencia <= 0) { Ela.Estado = EstadoCriatura.Patrulha; Ela.TempoRecalculo = 0; }
            }

            // Escondido e respirando com ela ao lado: ela te acha.
            if (Jogador.Escondido && distJ < 3.2f && Ela.Estado != EstadoCriatura.Patrulha && !Jogador.PrendendoAr)
            {
                Ela.UltimaPista = Jogador.Pos;
                Ela.AndarDaPista = Jogador.Andar;
                Ela.Paciencia = Math.Max(Ela.Paciencia, 7f);
            }

            Ela.SemPista = Ela.Estado == EstadoCriatura.Patrulha ? Ela.SemPista + dt : 0f;

            // O cerco acaba de dois jeitos: uma pista de verdade, que vale mais
            // que o palpite, ou ela chegando onde achava que você estava. Se
            // chegou e você não estava lá, volta a patrulhar do zero.
            if (Ela.Cercando &&
                (Ela.Estado != EstadoCriatura.Patrulha || distJ < Regras.RaioCerco))
            {
                Ela.Cercando = false;
                Ela.SemPista = 0f;
            }

            EscolherDestino(dt);
            Mover(dt, distJ);
            Sons(dt, distJ);

            if (distJ < Regras.DistanciaParaPegar && !Jogador.Escondido) Pegar();
            if (Jogador.Escondido && distJ < Regras.DistanciaArmario
                && Ela.Estado != EstadoCriatura.Patrulha && !Jogador.PrendendoAr) Pegar();
        }

        /// <summary>
        /// Chuta um destino perto do informado, no mesmo andar. Mira imperfeita
        /// de propósito: ela pressiona, não adivinha onde você está.
        /// </summary>
        Celula Sacudir(Celula c, int raio)
        {
            var perto = new Celula(
                Math.Clamp(c.Cx + _rng.Next(-raio, raio + 1), 1, Predio.Largura - 2),
                Math.Clamp(c.Cz + _rng.Next(-raio, raio + 1), 1, Predio.Profundidade - 2),
                c.Andar);
            return Predio.EhParede(perto) ? c : perto;
        }

        void EscolherDestino(float dt)
        {
            Ela.TempoRecalculo -= dt;
            bool semCaminho = Ela.Caminho.Count == 0 || Ela.PassoDoCaminho >= Ela.Caminho.Count;

            // Caçando o alvo se move, então recalcula sempre. Patrulhando ela precisa
            // CHEGAR onde decidiu ir: recalcular a toda hora fazia ela trocar de ideia
            // antes de sair do lugar e nunca cruzar o prédio.
            bool recalcular = Ela.Estado == EstadoCriatura.Caca
                ? Ela.TempoRecalculo <= 0
                : (semCaminho || Ela.TempoRecalculo <= 0);
            if (!recalcular) return;

            Ela.TempoRecalculo = Ela.Estado == EstadoCriatura.Caca ? 0.45f : 8f;
            Celula destino;

            if (Ela.Estado == EstadoCriatura.Caca)
                destino = Predio.ParaCelula(Jogador.Pos, Jogador.Andar);
            else if (Ela.UltimaPista.HasValue &&
                     Ela.Estado is EstadoCriatura.Investiga or EstadoCriatura.Procura)
            {
                destino = Predio.ParaCelula(Ela.UltimaPista.Value, Ela.AndarDaPista);
                if (Ela.Estado == EstadoCriatura.Procura) destino = Sacudir(destino, 3);
            }
            else if (Ela.Cercando || Ela.SemPista > Regras.SegundosSemPistaAteApertar)
            {
                // Faz tempo demais sem pista: ela começa a fechar o cerco. Não é mira
                // perfeita — é pressão, para o jogo não virar passeio. E uma vez
                // começado ela vai até o fim: some o cerco antes de chegar e ficar
                // parado num canto vira a jogada mais segura do jogo.
                //
                // O cerco atravessa andar: subir a escada não pode ser um botão
                // de "fim de perseguição", senão o piso de cima vira abrigo.
                Ela.Cercando = true;
                var alvo = Predio.ParaCelula(Jogador.Pos, Jogador.Andar);
                destino = Sacudir(alvo, 4);
            }
            else
            {
                // patrulha so dentro do predio: o patio e do lado de fora
                Sala sala;
                do { sala = Predio.Salas[_rng.Next(Predio.Salas.Count)]; }
                while (sala.Tipo == TipoSala.Patio);
                destino = Predio.PontoLivre(sala, _rng);
            }

            Ela.Caminho = Predio.Caminho(Predio.ParaCelula(Ela.Pos, Ela.Andar), destino)
                          ?? new List<Celula>();
            Ela.PassoDoCaminho = 0;
        }

        void Mover(float dt, float distJ)
        {
            float vel = Ela.Estado switch
            {
                EstadoCriatura.Caca => Regras.VelCaca,
                EstadoCriatura.Investiga => Regras.VelInvestiga,
                EstadoCriatura.Procura => Regras.VelProcura,
                _ => Regras.VelPatrulha
            } + Ela.Agressao * Regras.AceleracaoPorFusivel;

            P2? alvo = null;
            if (Ela.Caminho.Count > 0)
            {
                var no = Ela.Caminho[Math.Min(Ela.PassoDoCaminho, Ela.Caminho.Count - 1)];

                // O caminho pode trocar de andar: quando o próximo nó está no
                // outro piso, é uma escada, e ela sobe. Sem isto ela ficava
                // andando em círculos no pé da escada, porque a busca em largura
                // já atravessava andares e o corpo dela não.
                if (no.Andar != Ela.Andar && _travaEscadaDela <= 0)
                {
                    Ela.Andar = no.Andar;
                    _travaEscadaDela = Regras.EsperaDaEscada;
                    Ela.Pos = Predio.ParaMundo(no);
                }

                var m = Predio.ParaMundo(no);
                alvo = m;
                if (no.Andar == Ela.Andar && P2.Distancia(m, Ela.Pos) < Predio.Celula * 0.55f)
                    Ela.PassoDoCaminho++;
                if (Ela.PassoDoCaminho >= Ela.Caminho.Count) { Ela.Caminho.Clear(); Ela.TempoRecalculo = 0; }
            }

            // Perto e com linha de visão ela larga a grade e vem reto em cima.
            if (Ela.Estado == EstadoCriatura.Caca && distJ < Regras.DistanciaInvestida
                && Ela.Andar == Jogador.Andar
                && Predio.Visivel(Ela.Pos, Jogador.Pos, Ela.Andar))
                alvo = Jogador.Pos;

            float suavizar = Math.Min(1f, dt * 5f);
            if (alvo.HasValue)
            {
                var dir = (alvo.Value - Ela.Pos).Normalizado;
                Ela.Velocidade = new P2(
                    Ela.Velocidade.X + (dir.X * vel - Ela.Velocidade.X) * suavizar,
                    Ela.Velocidade.Z + (dir.Z * vel - Ela.Velocidade.Z) * suavizar);
            }
            else
            {
                float frear = Math.Min(1f, dt * 4f);
                Ela.Velocidade = new P2(Ela.Velocidade.X * (1 - frear), Ela.Velocidade.Z * (1 - frear));
            }

            Ela.Pos += Ela.Velocidade * dt;
            Ela.Pos = Predio.EmpurrarFora(Ela.Pos, 0.5f, Ela.Andar);
        }

        void Sons(float dt, float distJ)
        {
            float andando = Ela.Velocidade.Comprimento;
            if (andando > 0.3f)
            {
                Ela.TempoPasso -= dt * andando;
                if (Ela.TempoPasso <= 0)
                {
                    Ela.TempoPasso = Ela.Estado == EstadoCriatura.Caca ? 1.5f : 2.2f;
                    Eventos.Add(Evento.PassoDela);
                    UltimoSomDela = Ela.Pos;
                }
            }

            Ela.TempoGrunhido -= dt;
            if (Ela.TempoGrunhido <= 0 && distJ < 26f)
            {
                Ela.TempoGrunhido = 6f + (float)_rng.NextDouble() * 8f;
                Eventos.Add(Ela.Estado == EstadoCriatura.Caca ? Evento.ElaRugiu : Evento.ElaArranhou);
                UltimoSomDela = Ela.Pos;
            }
        }

        void Pegar()
        {
            if (Fase != Fase.Jogando) return;
            Fase = Fase.Morto;
            Eventos.Add(Evento.VocePegou);
        }

        void PassoMedo(float dt)
        {
            // Só dá medo o que está no seu andar. Enquanto a distância ignorava
            // o piso, ela passava embaixo de você e o coração disparava sem
            // motivo nenhum visível.
            float d = DistanciaReal(Ela.Pos, Ela.Andar, Jogador.Pos, Jogador.Andar);

            // Antes a escala era 26 m: a 14 m de distância o medo já passava do
            // limiar do coração, e num prédio onde ela circula isso era quase
            // sempre. O resultado era um baque contínuo no ouvido que não
            // avisava de nada — o jogador só ouvia barulho chato.
            float alvo = Math.Clamp(1 - d / Regras.DistanciaQueDaMedo, 0, 1);
            if (Ela.Estado == EstadoCriatura.Caca && Ela.Andar == Jogador.Andar)
                alvo = Math.Max(alvo, 0.7f);
            if (d < 14f && Predio.Visivel(Ela.Pos, Jogador.Pos, Jogador.Andar))
                alvo = Math.Min(1, alvo + 0.25f);
            if (Jogador.Escondido) alvo *= 0.85f;

            Jogador.Medo += (alvo - Jogador.Medo) * Math.Min(1f, dt * 1.6f);

            _tempoBatida -= dt;
            // O coração é aviso, não trilha sonora: só bate quando ela está
            // perto de verdade ou quando você está sem fôlego nenhum.
            if (_tempoBatida <= 0 && (Jogador.Medo > Regras.MedoParaOCoracao || Jogador.Folego < 0.18f))
            {
                _tempoBatida = Math.Max(0.38f, 1.05f - Jogador.Medo * 0.55f - (1 - Jogador.Folego) * 0.2f);
                Eventos.Add(Evento.Batida);
            }
        }
    }
}

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
        ElaViuVoce, ElaRugiu, ElaArranhou, VocePegou, VoceEscapou, PortaoTrancado
    }

    public struct Comando
    {
        public float FrenteX, FrenteZ;   // direção já resolvida no referencial do mundo
        public float Giro, Inclinacao;   // radianos, absolutos
        public bool Correr, Agachar, PrenderAr;
        public bool Interagir, AlternarLanterna;
    }

    public sealed class Item
    {
        public P2 Pos;
        public bool Recolhido;
    }

    public sealed class Armario
    {
        public P2 Pos;
    }

    public sealed class Jogador
    {
        public P2 Pos;
        public float Giro, Inclinacao;
        public bool Agachado, Correndo, Escondido, PrendendoAr;
        public float Folego = 1f, Bateria = 1f, Ar = 1f;
        public bool Lanterna = true;
        public int FusiveisNaMao, FusiveisInstalados;
        public float Medo;

        public bool LuzAcesa => Lanterna && Bateria > 0f;
        public float AlturaOlho => Escondido ? 0.95f
            : (Agachado ? Regras.AlturaOlhoAgachado : Regras.AlturaOlhoEmPe);

        public P2 Frente => new P2(-MathF.Sin(Giro), -MathF.Cos(Giro));
    }

    public sealed class Criatura
    {
        public P2 Pos, Velocidade;
        public EstadoCriatura Estado = EstadoCriatura.Patrulha;
        public List<Celula> Caminho = new();
        public int PassoDoCaminho;
        public float TempoRecalculo, Paciencia, SemPista, TempoPasso, TempoGrunhido;
        public P2? UltimaPista;
        public float Agressao;

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
        public Predio Predio { get; } = new();
        public Jogador Jogador { get; } = new();
        public Criatura Ela { get; } = new();

        public List<Item> Fusiveis { get; } = new();
        public List<Item> Baterias { get; } = new();
        public List<Armario> Armarios { get; } = new();
        public P2 Quadro { get; private set; }
        public P2 Portao { get; private set; }
        public bool PortaoAberto { get; private set; }

        public List<Evento> Eventos { get; } = new();
        public float Tempo { get; private set; }
        /// <summary>Posição do último som dela, para o renderizador colocar o áudio no lugar certo.</summary>
        public P2 UltimoSomDela { get; private set; }

        readonly Random _rng;
        float _tempoPasso, _tempoRespiracao, _tempoBatida;

        public Partida(int semente = 0)
        {
            _rng = semente == 0 ? new Random() : new Random(semente);
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

        void Distribuir()
        {
            Fusiveis.Clear(); Baterias.Clear(); Armarios.Clear(); Eventos.Clear();

            var portaria = Predio.Sala(TipoSala.Portaria);
            Jogador.Pos = Predio.ParaMundo(portaria.Centro);
            Jogador.Giro = 0; Jogador.Inclinacao = 0;
            Jogador.Folego = 1; Jogador.Bateria = 1; Jogador.Ar = 1;
            Jogador.Lanterna = true; Jogador.Escondido = false;
            Jogador.FusiveisNaMao = 0; Jogador.FusiveisInstalados = 0;
            Jogador.Medo = 0;

            Portao = Predio.ParaMundo(new Celula(portaria.X + 1, portaria.Z + 2));
            PortaoAberto = false;
            Quadro = Predio.ParaMundo(Predio.Sala(TipoSala.Quadro).Centro);

            // um fusível por sala, nunca na portaria nem no quadro
            var candidatas = new List<Sala>();
            foreach (var s in Predio.Salas)
                if (s.Tipo != TipoSala.Portaria && s.Tipo != TipoSala.Quadro) candidatas.Add(s);
            Embaralhar(candidatas);

            for (int i = 0; i < Regras.FusiveisNecessarios && i < candidatas.Count; i++)
                Fusiveis.Add(new Item { Pos = Predio.ParaMundo(Predio.PontoLivre(candidatas[i], _rng)) });

            var paraBateria = new List<Sala>(Predio.Salas);
            Embaralhar(paraBateria);
            for (int i = 0; i < Regras.BateriasNoMapa && i < paraBateria.Count; i++)
                Baterias.Add(new Item { Pos = Predio.ParaMundo(Predio.PontoLivre(paraBateria[i], _rng)) });

            foreach (var s in Predio.Salas)
            {
                if (s.Tipo == TipoSala.Portaria) continue;
                for (int k = 0; k < Regras.ArmariosPorSala; k++)
                    Armarios.Add(new Armario { Pos = Predio.ParaMundo(Predio.PontoLivre(s, _rng, 0)) });
            }

            Ela.Pos = Predio.ParaMundo(Predio.Sala(TipoSala.Camara).Centro);
            Ela.Estado = EstadoCriatura.Patrulha;
            Ela.Caminho.Clear();
            Ela.PassoDoCaminho = 0;
            Ela.TempoRecalculo = 0;
            Ela.UltimaPista = null;
            Ela.Agressao = 0;
            Ela.SemPista = 0;
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
            var inicio = Predio.ParaCelula(Jogador.Pos);

            for (int i = 0; i < Fusiveis.Count; i++)
                if (!Predio.Alcancavel(inicio, Predio.ParaCelula(Fusiveis[i].Pos)))
                    falhas.Add($"fusível {i + 1}");

            if (!Predio.Alcancavel(inicio, Predio.ParaCelula(Quadro))) falhas.Add("quadro");
            if (!Predio.Alcancavel(inicio, Predio.ParaCelula(Ela.Pos))) falhas.Add("criatura");
            if (P2.Distancia(Ela.Pos, Jogador.Pos) < Regras.DistanciaInicialMinima) falhas.Add("criatura perto demais");

            return falhas;
        }

        // ------------------------------------------------------------- passo

        public void Passo(float dt, Comando cmd)
        {
            if (Fase != Fase.Jogando) return;
            if (dt > 0.05f) dt = 0.05f;   // trava: quadro lento não pode atravessar parede

            Eventos.Clear();
            Tempo += dt;

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
                Jogador.Pos = Predio.EmpurrarFora(Jogador.Pos, Regras.RaioJogador);

                _tempoPasso -= dt * vel;
                if (_tempoPasso <= 0)
                {
                    _tempoPasso = Jogador.Correndo ? 3.4f : (Jogador.Agachado ? 5.5f : 4.2f);
                    Eventos.Add(Evento.Passo);
                    FazerBarulho(Jogador.Correndo ? Regras.RuidoCorrendo
                               : Jogador.Agachado ? Regras.RuidoAgachado : Regras.RuidoAndando);
                }
            }

            _tempoRespiracao -= dt;
            if (_tempoRespiracao <= 0)
            {
                bool cansado = Jogador.Folego < Regras.FolegoOfegante;
                _tempoRespiracao = cansado ? 1.5f : 4.5f;
                if (cansado) { Eventos.Add(Evento.Ofegante); FazerBarulho(Regras.RuidoOfegante); }
                else if (Jogador.Medo > 0.4f) Eventos.Add(Evento.Respiracao);
            }

            GastarBateria(dt);
        }

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

        public enum Alvo { Nenhum, Fusivel, Bateria, Armario, Quadro, Portao }

        public Alvo AlvoMaisPerto(out object objeto)
        {
            objeto = null;
            var melhor = Alvo.Nenhum;
            float md = Regras.RaioInteracao;

            foreach (var f in Fusiveis)
                if (!f.Recolhido) { float d = P2.Distancia(f.Pos, Jogador.Pos); if (d < md) { md = d; melhor = Alvo.Fusivel; objeto = f; } }
            foreach (var b in Baterias)
                if (!b.Recolhido) { float d = P2.Distancia(b.Pos, Jogador.Pos); if (d < md) { md = d; melhor = Alvo.Bateria; objeto = b; } }
            foreach (var a in Armarios)
            { float d = P2.Distancia(a.Pos, Jogador.Pos); if (d < md) { md = d; melhor = Alvo.Armario; objeto = a; } }

            float dq = P2.Distancia(Quadro, Jogador.Pos);
            if (dq < md) { md = dq; melhor = Alvo.Quadro; objeto = null; }
            float dp = P2.Distancia(Portao, Jogador.Pos);
            if (dp < md) { melhor = Alvo.Portao; objeto = null; }

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
                    break;

                case Alvo.Bateria:
                    ((Item)obj).Recolhido = true;
                    Jogador.Bateria = Math.Min(1, Jogador.Bateria + Regras.BateriaPorPilha);
                    Eventos.Add(Evento.PegouBateria);
                    break;

                case Alvo.Armario:
                    Jogador.Escondido = true;
                    Jogador.Pos = ((Armario)obj).Pos;
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
                    if (PortaoAberto)
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
            if (P2.Distancia(Ela.Pos, Jogador.Pos) > raio) return;

            Ela.UltimaPista = Jogador.Pos;
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

            var para = Jogador.Pos - Ela.Pos;
            float d = para.Comprimento;
            if (d > alcance) return false;

            // Com a lanterna acesa ela nota a luz de qualquer ângulo; no escuro,
            // só te vê se estiver olhando na sua direção.
            if (d > 2f && !Jogador.LuzAcesa)
                if (P2.Escalar(para.Normalizado, Ela.Direcao) < Regras.CossenoCampoVisao) return false;

            return Predio.Visivel(Ela.Pos, Jogador.Pos);
        }

        void PassoCriatura(float dt)
        {
            float distJ = P2.Distancia(Ela.Pos, Jogador.Pos);
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
                Ela.Paciencia = Math.Max(Ela.Paciencia, 7f);
            }

            Ela.SemPista = Ela.Estado == EstadoCriatura.Patrulha ? Ela.SemPista + dt : 0f;

            EscolherDestino(dt);
            Mover(dt, distJ);
            Sons(dt, distJ);

            if (distJ < Regras.DistanciaParaPegar && !Jogador.Escondido) Pegar();
            if (Jogador.Escondido && distJ < Regras.DistanciaArmario
                && Ela.Estado != EstadoCriatura.Patrulha && !Jogador.PrendendoAr) Pegar();
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
                destino = Predio.ParaCelula(Jogador.Pos);
            else if (Ela.UltimaPista.HasValue &&
                     Ela.Estado is EstadoCriatura.Investiga or EstadoCriatura.Procura)
            {
                destino = Predio.ParaCelula(Ela.UltimaPista.Value);
                if (Ela.Estado == EstadoCriatura.Procura)
                {
                    var perto = new Celula(
                        Math.Clamp(destino.Cx + _rng.Next(-3, 4), 1, Predio.Largura - 2),
                        Math.Clamp(destino.Cz + _rng.Next(-3, 4), 1, Predio.Profundidade - 2));
                    if (!Predio.EhParede(perto)) destino = perto;
                }
            }
            else if (Ela.SemPista > Regras.SegundosSemPistaAteApertar)
            {
                // Faz tempo demais sem pista: ela começa a fechar o cerco. Não é mira
                // perfeita — é pressão, para o jogo não virar passeio.
                var alvo = Predio.ParaCelula(Jogador.Pos);
                var perto = new Celula(
                    Math.Clamp(alvo.Cx + _rng.Next(-4, 5), 1, Predio.Largura - 2),
                    Math.Clamp(alvo.Cz + _rng.Next(-4, 5), 1, Predio.Profundidade - 2));
                destino = Predio.EhParede(perto) ? alvo : perto;
                Ela.SemPista = 8f;
            }
            else
            {
                var sala = Predio.Salas[_rng.Next(Predio.Salas.Count)];
                destino = Predio.PontoLivre(sala, _rng);
            }

            Ela.Caminho = Predio.Caminho(Predio.ParaCelula(Ela.Pos), destino) ?? new List<Celula>();
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
                var m = Predio.ParaMundo(no);
                alvo = m;
                if (P2.Distancia(m, Ela.Pos) < Predio.Celula * 0.55f) Ela.PassoDoCaminho++;
                if (Ela.PassoDoCaminho >= Ela.Caminho.Count) { Ela.Caminho.Clear(); Ela.TempoRecalculo = 0; }
            }

            // Perto e com linha de visão ela larga a grade e vem reto em cima.
            if (Ela.Estado == EstadoCriatura.Caca && distJ < Regras.DistanciaInvestida
                && Predio.Visivel(Ela.Pos, Jogador.Pos))
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
            Ela.Pos = Predio.EmpurrarFora(Ela.Pos, 0.5f);
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
            float d = P2.Distancia(Ela.Pos, Jogador.Pos);
            float alvo = Math.Clamp(1 - d / 26f, 0, 1);
            if (Ela.Estado == EstadoCriatura.Caca) alvo = Math.Max(alvo, 0.55f);
            if (d < 22f && Predio.Visivel(Ela.Pos, Jogador.Pos)) alvo = Math.Min(1, alvo + 0.2f);
            if (Jogador.Escondido) alvo *= 0.85f;

            Jogador.Medo += (alvo - Jogador.Medo) * Math.Min(1f, dt * 1.6f);

            _tempoBatida -= dt;
            if (_tempoBatida <= 0 && (Jogador.Medo > 0.2f || Jogador.Folego < 0.4f))
            {
                _tempoBatida = Math.Max(0.32f, 0.95f - Jogador.Medo * 0.55f - (1 - Jogador.Folego) * 0.2f);
                Eventos.Add(Evento.Batida);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using NeonArena.Core;
using Xunit;

namespace NeonArena.Tests
{
    /// <summary>Atalhos para escrever partidas nos testes.</summary>
    static class SimHelp
    {
        public static InputFrame Idle => new InputFrame { SwitchWeapon = -1 };

        /// <summary>Roda n segundos de jogo em passos de 1/60 sem tocar em nada.</summary>
        public static void Run(this Sim sim, float seconds, InputFrame? input = null)
        {
            var frame = input ?? Idle;
            int steps = (int)Math.Round(seconds * 60f);
            for (int i = 0; i < steps; i++) sim.Step(1f / 60f, frame);
        }

        /// <summary>Aponta a mira exatamente para um inimigo.</summary>
        public static void AimAt(this Sim sim, EnemyState e)
        {
            Vec3 d = e.Pos - sim.Player.EyePos;
            sim.Player.Yaw = (float)Math.Atan2(-d.X, -d.Z);
            sim.Player.Pitch = (float)Math.Asin(d.Y / d.Length);
        }

        /// <summary>Põe o jogador ao lado do inimigo, com linha de visão limpa.</summary>
        public static void TeleportNextTo(this Sim sim, EnemyState e, float distance = 4f)
        {
            sim.Player.Pos = new Vec3(e.Pos.X + distance, 0f, e.Pos.Z);
            sim.AimAt(e);
        }
    }

    public class SimLifecycleTests
    {
        [Fact]
        public void ComecaParadaEVaiParaJogando()
        {
            var sim = new Sim(1);
            Assert.Equal(SimPhase.Idle, sim.Phase);
            sim.Start();
            Assert.Equal(SimPhase.Playing, sim.Phase);
        }

        [Fact]
        public void JogadorComecaComVidaCheiaECarregado()
        {
            var sim = new Sim(1);
            sim.Start();
            Assert.Equal(sim.Player.MaxHp, sim.Player.Hp);
            Assert.Equal(Rules.Rifle.MagSize, sim.Player.Ammo[0]);
            Assert.Equal(Rules.Shotgun.MagSize, sim.Player.Ammo[1]);
        }

        [Fact]
        public void PrimeiraOndaComecaSozinhaEmPoucosSegundos()
        {
            var sim = new Sim(1);
            sim.Start();
            Assert.Equal(0, sim.Wave);
            sim.Run(3f);
            Assert.Equal(1, sim.Wave);
            Assert.NotEmpty(sim.Enemies);
        }

        [Fact]
        public void PassoNaoRodaDepoisDeMorrer()
        {
            var sim = new Sim(1);
            sim.Start();
            sim.Run(3f);
            sim.HurtPlayer(999f);
            Assert.Equal(SimPhase.Dead, sim.Phase);

            float t = sim.Time;
            sim.Run(2f);
            Assert.Equal(t, sim.Time);      // o tempo parou junto
        }

        [Fact]
        public void MorteEmiteOEventoDeFimDeJogo()
        {
            var sim = new Sim(1);
            sim.Start();
            sim.Step(1f / 60f, SimHelp.Idle);
            sim.HurtPlayer(999f);
            Assert.Contains(SimEvent.GameOver, sim.Events);
        }

        [Fact]
        public void MesmaSementeDaMesmaPartida()
        {
            var a = new Sim(777); a.Start(); a.Run(8f);
            var b = new Sim(777); b.Start(); b.Run(8f);

            Assert.Equal(a.Enemies.Count, b.Enemies.Count);
            Assert.Equal(a.Wave, b.Wave);
            for (int i = 0; i < a.Enemies.Count; i++)
            {
                Assert.Equal(a.Enemies[i].Kind, b.Enemies[i].Kind);
                Assert.Equal(a.Enemies[i].Pos.X, b.Enemies[i].Pos.X, 4);
                Assert.Equal(a.Enemies[i].Pos.Z, b.Enemies[i].Pos.Z, 4);
            }
        }

        [Fact]
        public void SementesDiferentesDaoPartidasDiferentes()
        {
            var a = new Sim(1); a.Start(); a.Run(8f);
            var b = new Sim(2); b.Start(); b.Run(8f);
            bool igual = a.Enemies.Count == b.Enemies.Count;
            if (igual)
                for (int i = 0; i < a.Enemies.Count && igual; i++)
                    igual = Math.Abs(a.Enemies[i].Pos.X - b.Enemies[i].Pos.X) < 1e-4f;
            Assert.False(igual);
        }
    }

    public class SimMovementTests
    {
        [Fact]
        public void JogadorCaiParaOChaoNoInicio()
        {
            var sim = new Sim(1);
            sim.Start();
            sim.Run(1f);
            Assert.Equal(0f, sim.Player.Pos.Y, 3);
            Assert.True(sim.Player.OnGround);
        }

        [Fact]
        public void AndarParaAFrenteMove()
        {
            var sim = new Sim(1);
            sim.Start();
            sim.Run(0.5f);
            float z0 = sim.Player.Pos.Z;
            sim.Run(1f, new InputFrame { MoveZ = -1f, SwitchWeapon = -1 });
            Assert.True(sim.Player.Pos.Z < z0 - 3f, "devia ter andado pelo menos 3 unidades");
        }

        [Fact]
        public void CorrerEMaisRapidoQueAndar()
        {
            var a = new Sim(1); a.Start(); a.Run(0.5f);
            var b = new Sim(1); b.Start(); b.Run(0.5f);

            float za = a.Player.Pos.Z, zb = b.Player.Pos.Z;
            a.Run(1f, new InputFrame { MoveZ = -1f, SwitchWeapon = -1 });
            b.Run(1f, new InputFrame { MoveZ = -1f, Sprint = true, SwitchWeapon = -1 });

            Assert.True(za - a.Player.Pos.Z < zb - b.Player.Pos.Z);
        }

        [Fact]
        public void NaoAtravessaAParede()
        {
            var sim = new Sim(1);
            sim.Start();
            // empurra contra a parede do fundo por tempo de sobra
            sim.Run(6f, new InputFrame { MoveZ = 1f, Sprint = true, SwitchWeapon = -1 });
            Assert.True(sim.Player.Pos.Z <= ArenaLayout.Half - 0.5f,
                $"escapou da arena: z = {sim.Player.Pos.Z}");
        }

        [Fact]
        public void PuloSobeEVolta()
        {
            var sim = new Sim(1);
            sim.Start();
            sim.Run(1f);
            Assert.True(sim.Player.OnGround);

            sim.Step(1f / 60f, new InputFrame { Jump = true, SwitchWeapon = -1 });
            Assert.False(sim.Player.OnGround);

            float apex = 0f;
            for (int i = 0; i < 120; i++)
            {
                sim.Step(1f / 60f, SimHelp.Idle);
                apex = Math.Max(apex, sim.Player.Pos.Y);
            }
            Assert.True(apex > 1f, $"pulo baixo demais: {apex}");
            Assert.True(sim.Player.OnGround, "não voltou ao chão");
        }

        [Fact]
        public void DiagonalNaoEMaisRapidaQueReta()
        {
            var reta = new Sim(1); reta.Start(); reta.Run(0.5f);
            var diag = new Sim(1); diag.Start(); diag.Run(0.5f);

            var p0r = reta.Player.Pos; var p0d = diag.Player.Pos;
            reta.Run(1f, new InputFrame { MoveZ = -1f, SwitchWeapon = -1 });
            diag.Run(1f, new InputFrame { MoveX = 1f, MoveZ = -1f, SwitchWeapon = -1 });

            float dReta = Vec3.DistanceXZ(p0r, reta.Player.Pos);
            float dDiag = Vec3.DistanceXZ(p0d, diag.Player.Pos);
            Assert.True(dDiag <= dReta + 0.01f, $"diagonal correu mais: {dDiag} vs {dReta}");
        }
    }

    public class SimCombatTests
    {
        static Sim Armed(int seed = 5)
        {
            var sim = new Sim(seed);
            sim.Start();
            sim.Run(3f);                      // deixa a onda 1 nascer
            return sim;
        }

        [Fact]
        public void TiroComLinhaLimpaAcerta()
        {
            var sim = Armed();
            var alvo = sim.Enemies[0];
            sim.TeleportNextTo(alvo);

            float hp0 = alvo.Hp;
            var shot = sim.Fire();

            Assert.True(shot.Fired);
            Assert.True(shot.HitEnemy);
            Assert.True(alvo.Hp < hp0, "o inimigo não perdeu vida");
        }

        [Fact]
        public void TiroBloqueadoPorCoberturaNaoAcerta()
        {
            var sim = Armed();
            var alvo = sim.Enemies[0];
            // põe o inimigo do outro lado da cobertura central
            alvo.Pos = new Vec3(0, 1.7f, -14);
            sim.Player.Pos = new Vec3(0, 0, 0);
            sim.AimAt(alvo);

            float hp0 = alvo.Hp;
            var shot = sim.Fire();
            Assert.False(shot.HitEnemy);
            Assert.Equal(hp0, alvo.Hp, 3);
        }

        [Fact]
        public void AcertoNoNucleoTiraODobro()
        {
            var sim = Armed();
            var a = sim.Enemies[0];
            sim.TeleportNextTo(a);

            float hp0 = a.Hp;
            sim.Damage(a, Rules.ShotDamage(Rules.Rifle, false), new Vec3(1, 0, 0));
            float normal = hp0 - a.Hp;

            a.Hp = hp0;
            sim.Damage(a, Rules.ShotDamage(Rules.Rifle, true), new Vec3(1, 0, 0));
            float core = hp0 - a.Hp;

            Assert.Equal(normal * 2f, core, 3);
        }

        [Fact]
        public void MatarPontuaESobeOCombo()
        {
            var sim = Armed();
            var alvo = sim.Enemies[0];

            int combo0 = sim.Combo;
            bool morreu = sim.Damage(alvo, 99999f, new Vec3(1, 0, 0));

            Assert.True(morreu);
            Assert.Equal(1, sim.Kills);
            Assert.True(sim.Score > 0);
            Assert.Equal(combo0 + 1, sim.Combo);
        }

        [Fact]
        public void LevarDanoZeraOCombo()
        {
            var sim = Armed();
            sim.Damage(sim.Enemies[0], 99999f, new Vec3(1, 0, 0));
            Assert.True(sim.Combo > 1);

            sim.HurtPlayer(5f);
            Assert.Equal(1, sim.Combo);
        }

        [Fact]
        public void ComboExpiraSozinho()
        {
            var sim = Armed();
            sim.Damage(sim.Enemies[0], 99999f, new Vec3(1, 0, 0));
            Assert.True(sim.Combo > 1);

            sim.Run(4f);
            Assert.Equal(1, sim.Combo);
        }

        [Fact]
        public void MunicaoDiminuiAoAtirar()
        {
            var sim = Armed();
            int ammo0 = sim.Player.Ammo[0];
            sim.Fire();
            Assert.Equal(ammo0 - 1, sim.Player.Ammo[0]);
        }

        [Fact]
        public void CadenciaImpedeTiroImediato()
        {
            var sim = Armed();
            sim.Fire();
            int ammo = sim.Player.Ammo[0];
            var segundo = sim.Fire();
            Assert.False(segundo.Fired);
            Assert.Equal(ammo, sim.Player.Ammo[0]);
        }

        [Fact]
        public void PenteVazioDisparaRecargaAutomatica()
        {
            var sim = Armed();
            sim.Player.Ammo[0] = 0;
            sim.Step(1f / 60f, SimHelp.Idle);
            Assert.True(sim.Player.ReloadTimer > 0f);

            sim.Run(Rules.Rifle.ReloadTime + 0.2f);
            Assert.Equal(Rules.Rifle.MagSize, sim.Player.Ammo[0]);
        }

        [Fact]
        public void RecargaTiraDaReserva()
        {
            var sim = Armed();
            sim.Player.Ammo[0] = 10;
            int reserva0 = sim.Player.Reserve[0];

            sim.StartReload();
            sim.Run(Rules.Rifle.ReloadTime + 0.2f);

            Assert.Equal(Rules.Rifle.MagSize, sim.Player.Ammo[0]);
            Assert.Equal(reserva0 - (Rules.Rifle.MagSize - 10), sim.Player.Reserve[0]);
        }

        [Fact]
        public void SemReservaNaoRecarrega()
        {
            var sim = Armed();
            sim.Player.Ammo[0] = 0;
            sim.Player.Reserve[0] = 0;
            sim.StartReload();
            Assert.Equal(0f, sim.Player.ReloadTimer);
        }

        [Fact]
        public void AtirarSemMunicaoEmiteTiroSeco()
        {
            var sim = Armed();
            sim.Player.Ammo[0] = 0;
            sim.Player.Reserve[0] = 0;
            var shot = sim.Fire();
            Assert.False(shot.Fired);
            Assert.Contains(SimEvent.DryFire, sim.Events);
        }

        [Fact]
        public void PrecisaoContaTiroECerto()
        {
            var sim = Armed();
            var alvo = sim.Enemies[0];
            sim.TeleportNextTo(alvo);
            sim.Fire();
            Assert.Equal(1f, sim.Accuracy, 3);

            sim.Player.FireCooldown = 0f;
            sim.Player.Pitch = -1.4f;          // atira para o chão
            sim.Fire();
            Assert.Equal(0.5f, sim.Accuracy, 3);
        }
    }

    public class SimWaveTests
    {
        [Fact]
        public void LimparAOndaLevaAProxima()
        {
            var sim = new Sim(3);
            sim.Start();
            sim.Run(3f);
            Assert.Equal(1, sim.Wave);

            // mata tudo o que nascer, até a onda virar
            for (int i = 0; i < 60 && sim.Wave == 1; i++)
            {
                foreach (var e in sim.Enemies.ToArray()) sim.Damage(e, 99999f, new Vec3(1, 0, 0));
                sim.Player.Hp = sim.Player.MaxHp;
                sim.Run(1f);
            }
            Assert.Equal(2, sim.Wave);
        }

        [Fact]
        public void LimparAOndaDaBonusEMunicao()
        {
            var sim = new Sim(3);
            sim.Start();
            sim.Run(3f);
            sim.Player.Reserve[0] = 10;

            int score0 = sim.Score;
            for (int i = 0; i < 60 && sim.Wave == 1; i++)
            {
                foreach (var e in sim.Enemies.ToArray()) sim.Damage(e, 99999f, new Vec3(1, 0, 0));
                sim.Player.Hp = sim.Player.MaxHp;
                sim.Run(1f);
            }
            Assert.True(sim.Score > score0);
            Assert.True(sim.Player.Reserve[0] > 10, "não recebeu munição ao limpar a onda");
        }

        [Fact]
        public void OndasAvancamMuitoSemQuebrar()
        {
            var sim = new Sim(9);
            sim.Start();

            for (int i = 0; i < 400 && sim.Wave < 12; i++)
            {
                foreach (var e in sim.Enemies.ToArray()) sim.Damage(e, 99999f, new Vec3(1, 0, 0));
                sim.Player.Hp = sim.Player.MaxHp;
                sim.Run(0.5f);
            }

            Assert.True(sim.Wave >= 12, $"travou na onda {sim.Wave}");
            Assert.Equal(SimPhase.Playing, sim.Phase);
        }

        [Fact]
        public void OsTresTiposAparecemAoLongoDasOndas()
        {
            var sim = new Sim(42);
            sim.Start();
            var vistos = new HashSet<EnemyKind>();

            for (int i = 0; i < 600 && vistos.Count < 3; i++)
            {
                foreach (var e in sim.Enemies) vistos.Add(e.Kind);
                foreach (var e in sim.Enemies.ToArray()) sim.Damage(e, 99999f, new Vec3(1, 0, 0));
                sim.Player.Hp = sim.Player.MaxHp;
                sim.Run(0.5f);
            }

            Assert.Contains(EnemyKind.Drone, vistos);
            Assert.Contains(EnemyKind.Runner, vistos);
            Assert.Contains(EnemyKind.Brute, vistos);
        }

        [Fact]
        public void InimigosNascemLongeDoJogador()
        {
            var sim = new Sim(11);
            sim.Start();
            sim.Run(8f);

            foreach (var e in sim.Enemies)
            {
                // já andaram um pouco, então a folga é menor que os 14 do nascimento
                Assert.True(Vec3.DistanceXZ(e.Pos, sim.Player.Pos) > 2f,
                    "inimigo nasceu em cima do jogador");
            }
        }

        [Fact]
        public void InimigosFicamDentroDaArena()
        {
            var sim = new Sim(13);
            sim.Start();
            sim.Run(25f);

            foreach (var e in sim.Enemies)
            {
                Assert.True(Math.Abs(e.Pos.X) <= ArenaLayout.Half, $"vazou em X: {e.Pos.X}");
                Assert.True(Math.Abs(e.Pos.Z) <= ArenaLayout.Half, $"vazou em Z: {e.Pos.Z}");
            }
        }
    }

    public class SimPressureTests
    {
        [Fact]
        public void JogadorParadoLevaDanoAoLongoDoTempo()
        {
            var sim = new Sim(21);
            sim.Start();
            sim.Run(20f);
            Assert.True(sim.Player.Hp < sim.Player.MaxHp, "os inimigos não encostaram no jogador");
        }

        [Fact]
        public void ParadoSemReagirOJogadorMorre()
        {
            var sim = new Sim(21);
            sim.Start();
            for (int i = 0; i < 200 && sim.Phase == SimPhase.Playing; i++) sim.Run(1f);
            Assert.Equal(SimPhase.Dead, sim.Phase);
        }

        [Fact]
        public void NenhumValorViraNaNEmPartidaLonga()
        {
            var sim = new Sim(31);
            sim.Start();

            for (int i = 0; i < 300; i++)
            {
                sim.Player.Hp = sim.Player.MaxHp;      // imortal, só para durar
                var input = new InputFrame
                {
                    MoveX = (i % 7) - 3, MoveZ = (i % 5) - 2,
                    Yaw = i * 0.05f, Jump = i % 23 == 0, Fire = i % 3 == 0, SwitchWeapon = -1
                };
                sim.Run(0.4f, input);

                Assert.False(float.IsNaN(sim.Player.Pos.X) || float.IsNaN(sim.Player.Pos.Y) ||
                             float.IsNaN(sim.Player.Pos.Z), $"posição virou NaN no passo {i}");
                foreach (var e in sim.Enemies)
                    Assert.False(float.IsNaN(e.Pos.X) || float.IsNaN(e.Pos.Z),
                        $"inimigo virou NaN no passo {i}");
            }
        }

        [Fact]
        public void PassoGiganteNaoAtravessaParede()
        {
            var sim = new Sim(1);
            sim.Start();
            // um quadro de 2 segundos: sem trava, o jogador atravessaria a arena inteira
            for (int i = 0; i < 40; i++)
                sim.Step(2f, new InputFrame { MoveZ = 1f, Sprint = true, SwitchWeapon = -1 });

            Assert.True(sim.Player.Pos.Z <= ArenaLayout.Half, $"vazou: z = {sim.Player.Pos.Z}");
        }
    }
}

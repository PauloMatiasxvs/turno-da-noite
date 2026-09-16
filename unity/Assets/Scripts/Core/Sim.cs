using System;
using System.Collections.Generic;

namespace NeonArena.Core
{
    /// <summary>O que o jogador está pedindo neste quadro. O motor gráfico preenche isto.</summary>
    public struct InputFrame
    {
        public float MoveX, MoveZ;      // -1..1, já no referencial do jogador
        public float Yaw, Pitch;        // radianos, absolutos
        public bool Jump, Sprint, Fire, Reload;
        public int SwitchWeapon;        // -1 = nenhum, senão índice da arma
    }

    public enum SimPhase { Idle, Playing, Dead }

    public enum SimEvent
    {
        Shot, HitEnemy, CoreHit, Killed, PlayerHurt, Reloaded, DryFire,
        WeaponSwitched, EnemySpawned, WaveStarted, WaveCleared, PickupTaken, GameOver, Jumped
    }

    public class EnemyState
    {
        public EnemyKind Kind;
        public Vec3 Pos, Vel;
        public float Hp, MaxHp, Speed, Radius, Hover;
        public float FireTimer, MeleeTimer, Birth;
        public int Strafe = 1;
        public bool Dead;
    }

    public class BoltState
    {
        public Vec3 Pos, Vel;
        public float Life, Damage;
    }

    public class PickupState
    {
        public Vec3 Pos;
        public bool IsHealth;
        public float Life;
    }

    public class PlayerState
    {
        public Vec3 Pos;
        public float VelY, Yaw, Pitch;
        public float Hp = 100f, MaxHp = 100f;
        public bool OnGround = true;
        public int Weapon;
        public int[] Ammo = new int[2];
        public int[] Reserve = new int[2];
        public float FireCooldown, ReloadTimer;

        public const float Radius = 0.42f;
        public const float EyeHeight = 1.68f;
        public const float WalkSpeed = 7.1f;
        public const float SprintSpeed = 10.6f;
        public const float JumpSpeed = 8.1f;
        public const float Gravity = 23f;

        public Vec3 EyePos => new Vec3(Pos.X, Pos.Y + EyeHeight, Pos.Z);

        /// <summary>Direção para onde a mira aponta, a partir de yaw/pitch.</summary>
        public Vec3 LookDir
        {
            get
            {
                float cp = (float)Math.Cos(Pitch), sp = (float)Math.Sin(Pitch);
                return new Vec3(-(float)Math.Sin(Yaw) * cp, sp, -(float)Math.Cos(Yaw) * cp);
            }
        }
    }

    /// <summary>Resultado de um disparo, para o renderizador desenhar o rastro.</summary>
    public struct ShotResult
    {
        public bool Fired;
        public Vec3 From, To;
        public bool HitEnemy, CoreHit, KilledEnemy;
        public EnemyState Target;
    }

    /// <summary>
    /// A simulação inteira do jogo, sem uma linha de código de motor gráfico.
    /// Godot e Unity apenas leem este estado e desenham. Por ser pura e ter o
    /// gerador aleatório com semente, ela roda igualzinha nos testes.
    /// </summary>
    public class Sim
    {
        public SimPhase Phase { get; private set; } = SimPhase.Idle;
        public readonly PlayerState Player = new PlayerState();
        public readonly List<EnemyState> Enemies = new List<EnemyState>();
        public readonly List<BoltState> Bolts = new List<BoltState>();
        public readonly List<PickupState> Pickups = new List<PickupState>();
        public readonly List<SimEvent> Events = new List<SimEvent>();
        public List<Box> Boxes { get; private set; }

        public int Wave { get; private set; }
        public int Score { get; private set; }
        public int Kills { get; private set; }
        public int Combo { get; private set; } = 1;
        public int ShotsFired { get; private set; }
        public int ShotsHit { get; private set; }
        public float Time { get; private set; }

        float _comboTimer, _betweenTimer;
        readonly List<QueuedSpawn> _queue = new List<QueuedSpawn>();
        uint _rngState;

        struct QueuedSpawn { public EnemyKind Kind; public float Delay; }

        const float ComboWindow = 3.2f;
        const float GapBetweenWaves = 4.0f;

        public Sim(int seed = 12345) { _rngState = (uint)(seed == 0 ? 1 : seed); }

        // xorshift32: previsível de propósito, para o teste poder repetir a partida
        float Rand()
        {
            _rngState ^= _rngState << 13;
            _rngState ^= _rngState >> 17;
            _rngState ^= _rngState << 5;
            return (_rngState & 0xFFFFFF) / (float)0x1000000;
        }
        float Rand(float a, float b) => a + Rand() * (b - a);

        public void Start()
        {
            Boxes = ArenaLayout.BuildBoxes();
            Enemies.Clear(); Bolts.Clear(); Pickups.Clear(); Events.Clear(); _queue.Clear();

            Player.Pos = new Vec3(0, 0, 20);
            Player.VelY = 0; Player.Yaw = 0; Player.Pitch = 0;
            Player.Hp = Player.MaxHp;
            Player.Weapon = 0;
            Player.FireCooldown = 0; Player.ReloadTimer = 0;
            var weapons = Rules.Weapons;
            for (int i = 0; i < 2; i++)
            {
                Player.Ammo[i] = weapons[i].MagSize;
                Player.Reserve[i] = i == 0 ? 160 : 36;
            }

            Wave = 0; Score = 0; Kills = 0; Combo = 1;
            ShotsFired = 0; ShotsHit = 0; Time = 0;
            _comboTimer = 0; _betweenTimer = 2.4f;
            Phase = SimPhase.Playing;
        }

        public void Step(float dt, InputFrame input)
        {
            if (Phase != SimPhase.Playing) return;
            if (dt > 0.05f) dt = 0.05f;   // trava o passo: quadro lento não atravessa parede

            Events.Clear();
            Time += dt;

            Player.Yaw = input.Yaw;
            Player.Pitch = Geometry.Clamp(input.Pitch, -1.45f, 1.45f);

            StepPlayer(dt, input);
            StepEnemies(dt);
            StepBolts(dt);
            StepPickups(dt);
            StepWaves(dt);

            _comboTimer = Math.Max(0f, _comboTimer - dt);
            if (_comboTimer <= 0f) Combo = 1;
        }

        // ------------------------------------------------------------------ jogador

        void StepPlayer(float dt, InputFrame input)
        {
            if (input.SwitchWeapon >= 0 && input.SwitchWeapon < 2 &&
                input.SwitchWeapon != Player.Weapon && Player.ReloadTimer <= 0f)
            {
                Player.Weapon = input.SwitchWeapon;
                Player.FireCooldown = Math.Max(Player.FireCooldown, 0.25f);
                Events.Add(SimEvent.WeaponSwitched);
            }

            // vertical primeiro: define de que altura a colisão horizontal parte
            Player.VelY -= PlayerState.Gravity * dt;
            Player.Pos.Y += Player.VelY * dt;

            float ground = Geometry.GroundHeight(Player.Pos, PlayerState.Radius, Boxes);
            if (Player.Pos.Y <= ground)
            {
                Player.Pos.Y = ground;
                Player.VelY = 0f;
                Player.OnGround = true;
            }
            else Player.OnGround = false;

            if (input.Jump && Player.OnGround)
            {
                Player.VelY = PlayerState.JumpSpeed;
                Player.OnGround = false;
                Events.Add(SimEvent.Jumped);
            }

            // horizontal
            float speed = input.Sprint ? PlayerState.SprintSpeed : PlayerState.WalkSpeed;
            var move = new Vec3(input.MoveX, 0, input.MoveZ);
            if (move.LengthSq > 1e-6f)
            {
                move = move.Normalized * speed;
                Player.Pos.X += move.X * dt;
                Player.Pos.Z += move.Z * dt;
            }

            Player.Pos = Geometry.PushOut(Player.Pos, PlayerState.Radius, Player.Pos.Y, 1.7f, Boxes);
            float lim = ArenaLayout.Half - 0.6f;
            Player.Pos.X = Geometry.Clamp(Player.Pos.X, -lim, lim);
            Player.Pos.Z = Geometry.Clamp(Player.Pos.Z, -lim, lim);

            // armas
            Player.FireCooldown = Math.Max(0f, Player.FireCooldown - dt);
            if (Player.ReloadTimer > 0f)
            {
                Player.ReloadTimer -= dt;
                if (Player.ReloadTimer <= 0f)
                {
                    Player.ReloadTimer = 0f;
                    FinishReload();
                }
            }

            if (input.Reload) StartReload();
            if (input.Fire) Fire();

            var w = Rules.Weapons[Player.Weapon];
            if (Player.Ammo[Player.Weapon] == 0 && Player.Reserve[Player.Weapon] > 0 && Player.ReloadTimer <= 0f)
                StartReload();
        }

        public void StartReload()
        {
            var w = Rules.Weapons[Player.Weapon];
            if (Player.ReloadTimer > 0f) return;
            if (Player.Ammo[Player.Weapon] >= w.MagSize) return;
            if (Player.Reserve[Player.Weapon] <= 0) return;
            Player.ReloadTimer = w.ReloadTime;
        }

        void FinishReload()
        {
            var w = Rules.Weapons[Player.Weapon];
            int need = Math.Min(w.MagSize - Player.Ammo[Player.Weapon], Player.Reserve[Player.Weapon]);
            Player.Ammo[Player.Weapon] += need;
            Player.Reserve[Player.Weapon] -= need;
            Events.Add(SimEvent.Reloaded);
        }

        /// <summary>Dispara se puder. Devolve o traçado para o renderizador desenhar.</summary>
        public ShotResult Fire()
        {
            var result = new ShotResult();
            var w = Rules.Weapons[Player.Weapon];

            if (Player.FireCooldown > 0f || Player.ReloadTimer > 0f) return result;
            if (Player.Ammo[Player.Weapon] <= 0)
            {
                Player.FireCooldown = 0.3f;
                Events.Add(SimEvent.DryFire);
                return result;
            }

            Player.Ammo[Player.Weapon]--;
            Player.FireCooldown = w.FireRate;
            ShotsFired++;
            Events.Add(SimEvent.Shot);

            Vec3 origin = Player.EyePos, forward = Player.LookDir;
            Vec3 right = Vec3.Cross(forward, Vec3.Up).Normalized;
            Vec3 up = Vec3.Cross(right, forward);

            result.Fired = true;
            result.From = origin;
            result.To = origin + forward * w.Range;

            bool anyHit = false;
            for (int p = 0; p < w.Pellets; p++)
            {
                // dispersão triangular: concentra perto do centro, como recuo real
                float sx = (Rand() + Rand() - 1f) * w.Spread;
                float sy = (Rand() + Rand() - 1f) * w.Spread;
                Vec3 dir = (forward + right * sx + up * sy).Normalized;

                float best = w.Range;
                EnemyState target = null;

                for (int i = 0; i < Boxes.Count; i++)
                {
                    float t = Geometry.RayBox(origin, dir, Boxes[i]);
                    if (t < best) { best = t; target = null; }
                }
                for (int i = 0; i < Enemies.Count; i++)
                {
                    var e = Enemies[i];
                    if (e.Dead || e.Birth < 0.25f) continue;
                    float t = Geometry.RaySphere(origin, dir, e.Pos, e.Radius);
                    if (t < best) { best = t; target = e; }
                }

                Vec3 hitPoint = origin + dir * best;
                if (p == 0) result.To = hitPoint;

                if (target != null)
                {
                    bool core = Rules.IsCoreHit(Vec3.Distance(hitPoint, target.Pos), target.Radius);
                    bool killed = Damage(target, Rules.ShotDamage(w, core), dir);
                    anyHit = true;
                    result.HitEnemy = true;
                    result.Target = target;
                    result.CoreHit |= core;
                    result.KilledEnemy |= killed;
                    Events.Add(core ? SimEvent.CoreHit : SimEvent.HitEnemy);
                }
            }
            if (anyHit) ShotsHit++;
            return result;
        }

        /// <summary>Aplica dano. Devolve true se o inimigo morreu neste golpe.</summary>
        public bool Damage(EnemyState e, float amount, Vec3 knockDir)
        {
            if (e.Dead) return false;
            e.Hp -= amount;
            float push = 2.6f / (e.Radius * 2f);
            e.Vel.X += knockDir.X * push;
            e.Vel.Z += knockDir.Z * push;
            if (e.Hp > 0f) return false;

            e.Dead = true;
            Kills++;
            Score += Rules.KillScore(e.Kind, Combo);
            Combo = Rules.NextCombo(Combo);
            _comboTimer = ComboWindow;
            Events.Add(SimEvent.Killed);
            MaybeDropPickup(e.Pos);
            return true;
        }

        void MaybeDropPickup(Vec3 at)
        {
            float r = Rand();
            bool? health = null;
            if (Player.Hp < 55f && r < 0.42f) health = true;
            else if (r < 0.16f) health = true;
            else if (r < 0.48f) health = false;
            if (health == null) return;

            Pickups.Add(new PickupState { Pos = new Vec3(at.X, 0.8f, at.Z), IsHealth = health.Value, Life = 22f });
        }

        public void HurtPlayer(float amount)
        {
            if (Phase != SimPhase.Playing) return;
            Player.Hp -= amount;
            Combo = 1;
            _comboTimer = 0f;
            Events.Add(SimEvent.PlayerHurt);
            if (Player.Hp <= 0f)
            {
                Player.Hp = 0f;
                Phase = SimPhase.Dead;
                Events.Add(SimEvent.GameOver);
            }
        }

        // ------------------------------------------------------------------ inimigos

        void StepEnemies(float dt)
        {
            Vec3 target = new Vec3(Player.Pos.X, Player.Pos.Y + 1.0f, Player.Pos.Z);

            for (int i = Enemies.Count - 1; i >= 0; i--)
            {
                var e = Enemies[i];
                if (e.Dead) { Enemies.RemoveAt(i); continue; }

                var spec = Rules.Spec(e.Kind);
                e.Birth = Math.Min(1f, e.Birth + dt * 1.8f);
                e.MeleeTimer = Math.Max(0f, e.MeleeTimer - dt);

                float dx = target.X - e.Pos.X, dz = target.Z - e.Pos.Z;
                float dist = (float)Math.Sqrt(dx * dx + dz * dz);
                if (dist < 1e-4f) dist = 1e-4f;
                Vec3 dir = new Vec3(dx / dist, 0, dz / dist);
                Vec3 side = new Vec3(-dir.Z, 0, dir.X);

                Vec3 wish;
                if (spec.Ranged)
                {
                    const float preferred = 13f;
                    float approach = dist > preferred + 3f ? 1f : (dist < preferred - 4f ? -1f : 0f);
                    wish = dir * approach + side * (e.Strafe * 0.85f);

                    e.FireTimer -= dt;
                    if (e.Birth >= 1f && e.FireTimer <= 0f && dist < 34f &&
                        !Geometry.Blocked(e.Pos, target, Boxes))
                    {
                        e.FireTimer = spec.FireInterval * Rand(0.8f, 1.3f);
                        Vec3 bd = (target - e.Pos).Normalized;
                        Bolts.Add(new BoltState { Pos = e.Pos, Vel = bd * 17f, Life = 3.2f, Damage = spec.BoltDamage });
                    }
                }
                else
                {
                    wish = dir;
                    if (dist < e.Radius + 0.95f)
                    {
                        wish = Vec3.Zero;
                        if (e.MeleeTimer <= 0f)
                        {
                            e.MeleeTimer = spec.MeleeInterval;
                            HurtPlayer(spec.MeleeDamage);
                        }
                    }
                }

                if (Rand() < dt * 0.5f) e.Strafe = -e.Strafe;

                e.Vel.X += (wish.X * e.Speed - e.Vel.X) * Math.Min(1f, dt * 3.5f);
                e.Vel.Z += (wish.Z * e.Speed - e.Vel.Z) * Math.Min(1f, dt * 3.5f);
                e.Pos.X += e.Vel.X * dt;
                e.Pos.Z += e.Vel.Z * dt;
                e.Pos.Y = e.Hover;

                e.Pos = Geometry.PushOut(e.Pos, e.Radius, e.Pos.Y - e.Hover * 0.5f, 0.1f, Boxes);
                float lim = ArenaLayout.Half - e.Radius;
                e.Pos.X = Geometry.Clamp(e.Pos.X, -lim, lim);
                e.Pos.Z = Geometry.Clamp(e.Pos.Z, -lim, lim);
            }
        }

        void StepBolts(float dt)
        {
            Vec3 chest = new Vec3(Player.Pos.X, Player.Pos.Y + 1.0f, Player.Pos.Z);

            for (int i = Bolts.Count - 1; i >= 0; i--)
            {
                var b = Bolts[i];
                b.Life -= dt;
                Vec3 next = b.Pos + b.Vel * dt;

                bool hit = false;
                if (Vec3.Distance(next, chest) < 0.85f) { HurtPlayer(b.Damage); hit = true; }
                if (!hit)
                    for (int k = 0; k < Boxes.Count; k++)
                        if (Boxes[k].Contains(next)) { hit = true; break; }

                b.Pos = next;
                if (hit || b.Life <= 0f || Math.Abs(b.Pos.X) > ArenaLayout.Half + 2f ||
                    Math.Abs(b.Pos.Z) > ArenaLayout.Half + 2f || b.Pos.Y < 0f)
                    Bolts.RemoveAt(i);
            }
        }

        void StepPickups(float dt)
        {
            for (int i = Pickups.Count - 1; i >= 0; i--)
            {
                var p = Pickups[i];
                p.Life -= dt;
                if (p.Life <= 0f) { Pickups.RemoveAt(i); continue; }

                if (Vec3.DistanceXZ(p.Pos, Player.Pos) < 1.5f)
                {
                    if (p.IsHealth) Player.Hp = Math.Min(Player.MaxHp, Player.Hp + 32f);
                    else
                    {
                        Player.Reserve[0] = Math.Min(Rules.Rifle.ReserveMax, Player.Reserve[0] + 60);
                        Player.Reserve[1] = Math.Min(Rules.Shotgun.ReserveMax, Player.Reserve[1] + 10);
                    }
                    Events.Add(SimEvent.PickupTaken);
                    Pickups.RemoveAt(i);
                }
            }
        }

        // ------------------------------------------------------------------ ondas

        void StepWaves(float dt)
        {
            if (_queue.Count == 0 && Enemies.Count == 0)
            {
                if (_betweenTimer <= 0f) return;
                _betweenTimer -= dt;
                if (_betweenTimer > 0f) return;

                if (Wave > 0)
                {
                    Score += Rules.WaveClearBonus(Wave, Combo);
                    Player.Reserve[0] = Math.Min(Rules.Rifle.ReserveMax, Player.Reserve[0] + 50);
                    Player.Reserve[1] = Math.Min(Rules.Shotgun.ReserveMax, Player.Reserve[1] + 8);
                    Player.Hp = Math.Min(Player.MaxHp, Player.Hp + 14f);
                    Events.Add(SimEvent.WaveCleared);
                }
                BeginWave(Wave + 1);
                return;
            }

            for (int i = _queue.Count - 1; i >= 0; i--)
            {
                var q = _queue[i];
                q.Delay -= dt;
                _queue[i] = q;
                if (q.Delay > 0f) continue;

                SpawnEnemy(q.Kind);
                _queue.RemoveAt(i);
            }
            if (_queue.Count == 0) _betweenTimer = GapBetweenWaves;
        }

        void BeginWave(int wave)
        {
            Wave = wave;
            _queue.Clear();
            var kinds = Rules.BuildWave(wave, () => Rand());
            float delay = 0f;
            foreach (var kind in kinds)
            {
                _queue.Add(new QueuedSpawn { Kind = kind, Delay = delay });
                delay += Rand(0.2f, 0.75f);
            }
            Events.Add(SimEvent.WaveStarted);
        }

        public EnemyState SpawnEnemy(EnemyKind kind)
        {
            var spec = Rules.Spec(kind);
            float x = 0, z = 0;

            for (int tries = 0; tries < 40; tries++)
            {
                float a = Rand() * (float)(Math.PI * 2);
                float d = Rand(18f, ArenaLayout.Half - 4f);
                x = (float)Math.Cos(a) * d;
                z = (float)Math.Sin(a) * d;
                bool clear = ArenaLayout.IsClear(x, z, 1.4f, Boxes);
                bool farEnough = Vec3.DistanceXZ(new Vec3(x, 0, z), Player.Pos) >= 14f;
                if (clear && farEnough) break;
            }

            var e = new EnemyState
            {
                Kind = kind,
                Pos = new Vec3(x, spec.HoverHeight, z),
                Hp = Rules.ScaledHp(kind, Math.Max(1, Wave)),
                Speed = Rules.ScaledSpeed(kind, Math.Max(1, Wave)),
                Radius = spec.Radius,
                Hover = spec.HoverHeight,
                FireTimer = Rand(0.6f, 2.2f),
                Strafe = Rand() < 0.5f ? 1 : -1
            };
            e.MaxHp = e.Hp;
            Enemies.Add(e);
            Events.Add(SimEvent.EnemySpawned);
            return e;
        }

        public float Accuracy => ShotsFired == 0 ? 0f : ShotsHit / (float)ShotsFired;
    }
}

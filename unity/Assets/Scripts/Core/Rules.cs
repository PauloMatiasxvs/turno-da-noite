using System;
using System.Collections.Generic;

namespace NeonArena.Core
{
    public enum EnemyKind { Drone, Runner, Brute }

    public struct EnemySpec
    {
        public float Hp, Speed, Radius, HoverHeight;
        public int Score;
        public bool Ranged;
        public float FireInterval, BoltDamage;   // só para Ranged
        public float MeleeDamage, MeleeInterval; // só para corpo a corpo
    }

    public struct WeaponSpec
    {
        public string Name;
        public float Damage, FireRate, ReloadTime, Spread, Range, Recoil;
        public int MagSize, Pellets, ReserveMax;
        public bool FullAuto;
    }

    /// <summary>
    /// Números do jogo num lugar só: dano, vida, composição das ondas, pontuação.
    /// Separado do Unity de propósito — é o que os testes conseguem exercitar.
    /// </summary>
    public static class Rules
    {
        // ---------- armas ----------
        public static readonly WeaponSpec Rifle = new WeaponSpec
        {
            Name = "Rifle", Damage = 15f, FireRate = 0.105f, MagSize = 32, ReserveMax = 220,
            ReloadTime = 1.4f, Spread = 0.014f, Pellets = 1, FullAuto = true, Range = 120f, Recoil = 0.9f
        };

        public static readonly WeaponSpec Shotgun = new WeaponSpec
        {
            Name = "Shotgun", Damage = 11f, FireRate = 0.72f, MagSize = 6, ReserveMax = 60,
            ReloadTime = 2.1f, Spread = 0.075f, Pellets = 9, FullAuto = false, Range = 45f, Recoil = 3.4f
        };

        public static WeaponSpec[] Weapons => new[] { Rifle, Shotgun };

        // ---------- inimigos ----------
        public static EnemySpec Spec(EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.Drone:
                    return new EnemySpec
                    {
                        Hp = 38f, Speed = 4.4f, Radius = 0.72f, HoverHeight = 1.7f, Score = 100,
                        Ranged = true, FireInterval = 2.2f, BoltDamage = 8f
                    };
                case EnemyKind.Runner:
                    return new EnemySpec
                    {
                        Hp = 22f, Speed = 9.2f, Radius = 0.55f, HoverHeight = 1.15f, Score = 140,
                        MeleeDamage = 14f, MeleeInterval = 0.8f
                    };
                case EnemyKind.Brute:
                    return new EnemySpec
                    {
                        Hp = 180f, Speed = 2.9f, Radius = 1.25f, HoverHeight = 1.9f, Score = 340,
                        MeleeDamage = 26f, MeleeInterval = 1.2f
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        // ---------- escalonamento ----------
        public const int MaxWaveSize = 28;
        public const int MaxCombo = 8;

        /// <summary>Vida cresce 11% por onda. A onda 1 vale a vida base.</summary>
        public static float ScaledHp(EnemyKind kind, int wave)
        {
            if (wave < 1) throw new ArgumentOutOfRangeException(nameof(wave));
            return Spec(kind).Hp * (1f + (wave - 1) * 0.11f);
        }

        /// <summary>Velocidade cresce 2,5% por onda, com teto de +50%.</summary>
        public static float ScaledSpeed(EnemyKind kind, int wave)
        {
            if (wave < 1) throw new ArgumentOutOfRangeException(nameof(wave));
            return Spec(kind).Speed * Math.Min(1f + (wave - 1) * 0.025f, 1.5f);
        }

        public static int WaveSize(int wave) =>
            Math.Min(4 + (int)Math.Round(wave * 1.7), MaxWaveSize);

        /// <summary>
        /// Sorteia a composição da onda. Runner entra na 2, brute na 3 e fica mais
        /// comum a partir da 7. O gerador entra por parâmetro para o teste poder fixá-lo.
        /// </summary>
        public static List<EnemyKind> BuildWave(int wave, Func<double> rng)
        {
            if (wave < 1) throw new ArgumentOutOfRangeException(nameof(wave));
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            int count = WaveSize(wave);
            var list = new List<EnemyKind>(count);
            for (int i = 0; i < count; i++)
            {
                double r = rng();
                EnemyKind kind = EnemyKind.Drone;
                if (wave >= 2 && r < 0.38) kind = EnemyKind.Runner;
                if (wave >= 3 && r > 0.88) kind = EnemyKind.Brute;
                if (wave >= 7 && r > 0.78) kind = EnemyKind.Brute;
                list.Add(kind);
            }
            return list;
        }

        // ---------- dano e pontuação ----------

        /// <summary>Acerto dentro da metade do raio é no núcleo: dano dobrado.</summary>
        public static bool IsCoreHit(float distanceFromCenter, float enemyRadius)
            => distanceFromCenter < enemyRadius * 0.5f;

        public static float ShotDamage(WeaponSpec weapon, bool coreHit)
            => weapon.Damage * (coreHit ? 2f : 1f);

        public static int KillScore(EnemyKind kind, int combo)
            => Spec(kind).Score * Math.Max(1, Math.Min(combo, MaxCombo));

        public static int NextCombo(int combo) => Math.Min(combo + 1, MaxCombo);

        /// <summary>Bônus por limpar a onda, pago antes de começar a seguinte.</summary>
        public static int WaveClearBonus(int wave, int combo)
            => (250 + wave * 120) * Math.Max(1, Math.Min(combo, MaxCombo));
    }
}

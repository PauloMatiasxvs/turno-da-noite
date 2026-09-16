using System;
using System.Collections.Generic;
using NeonArena.Core;
using Xunit;

namespace NeonArena.Tests
{
    public class WaveTests
    {
        /// <summary>Gerador determinístico para o teste não depender de sorte.</summary>
        static Func<double> Fixed(params double[] values)
        {
            int i = 0;
            return () => values[i++ % values.Length];
        }

        [Fact]
        public void TamanhoCresceComAOnda()
        {
            Assert.True(Rules.WaveSize(2) > Rules.WaveSize(1));
            Assert.True(Rules.WaveSize(10) > Rules.WaveSize(5));
        }

        [Fact]
        public void TamanhoTemTeto()
        {
            Assert.Equal(Rules.MaxWaveSize, Rules.WaveSize(100));
            Assert.Equal(Rules.MaxWaveSize, Rules.WaveSize(1000));
        }

        [Fact]
        public void OndaUmSoTemDrone()
        {
            // mesmo com sorteios que pediriam runner (0.1) e brute (0.95)
            var wave = Rules.BuildWave(1, Fixed(0.1, 0.95, 0.5));
            Assert.All(wave, k => Assert.Equal(EnemyKind.Drone, k));
        }

        [Fact]
        public void RunnerApareceNaOndaDois()
        {
            var wave = Rules.BuildWave(2, Fixed(0.1));
            Assert.Contains(EnemyKind.Runner, wave);
        }

        [Fact]
        public void BruteNaoApareceAntesDaOndaTres()
        {
            var wave2 = Rules.BuildWave(2, Fixed(0.95));
            Assert.DoesNotContain(EnemyKind.Brute, wave2);

            var wave3 = Rules.BuildWave(3, Fixed(0.95));
            Assert.Contains(EnemyKind.Brute, wave3);
        }

        [Fact]
        public void OndaSeteTornaBruteMaisComum()
        {
            // 0.8 vira brute só a partir da onda 7
            Assert.DoesNotContain(EnemyKind.Brute, Rules.BuildWave(6, Fixed(0.8)));
            Assert.Contains(EnemyKind.Brute, Rules.BuildWave(7, Fixed(0.8)));
        }

        [Fact]
        public void OndaZeroOuNegativaEErro()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Rules.BuildWave(0, Fixed(0.5)));
            Assert.Throws<ArgumentOutOfRangeException>(() => Rules.WaveSize(3) is var _ ? Rules.ScaledHp(EnemyKind.Drone, 0) : 0f);
        }

        [Fact]
        public void GeradorNuloEErro()
        {
            Assert.Throws<ArgumentNullException>(() => Rules.BuildWave(1, null));
        }
    }

    public class ScalingTests
    {
        [Fact]
        public void OndaUmUsaVidaBase()
        {
            Assert.Equal(Rules.Spec(EnemyKind.Drone).Hp, Rules.ScaledHp(EnemyKind.Drone, 1), 4);
        }

        [Fact]
        public void VidaCresceOnzePorCentoPorOnda()
        {
            float basehp = Rules.Spec(EnemyKind.Drone).Hp;
            Assert.Equal(basehp * 1.11f, Rules.ScaledHp(EnemyKind.Drone, 2), 3);
            Assert.Equal(basehp * 2.10f, Rules.ScaledHp(EnemyKind.Drone, 11), 3);
        }

        [Fact]
        public void VelocidadeTemTetoDeCinquentaPorCento()
        {
            float baseSpd = Rules.Spec(EnemyKind.Runner).Speed;
            Assert.Equal(baseSpd * 1.5f, Rules.ScaledSpeed(EnemyKind.Runner, 100), 3);
        }

        [Fact]
        public void BruteEMaisDuroQueDrone()
        {
            Assert.True(Rules.ScaledHp(EnemyKind.Brute, 5) > Rules.ScaledHp(EnemyKind.Drone, 5));
            Assert.True(Rules.ScaledSpeed(EnemyKind.Brute, 5) < Rules.ScaledSpeed(EnemyKind.Runner, 5));
        }
    }

    public class DamageTests
    {
        [Fact]
        public void AcertoNoNucleoDobraODano()
        {
            float normal = Rules.ShotDamage(Rules.Rifle, false);
            float core = Rules.ShotDamage(Rules.Rifle, true);
            Assert.Equal(normal * 2f, core, 4);
        }

        [Fact]
        public void NucleoEAMetadeInternaDoRaio()
        {
            float r = Rules.Spec(EnemyKind.Drone).Radius;
            Assert.True(Rules.IsCoreHit(r * 0.4f, r));
            Assert.False(Rules.IsCoreHit(r * 0.6f, r));
            Assert.False(Rules.IsCoreHit(r, r));
        }

        [Fact]
        public void ShotgunSoltaMaisChumboComMenosDanoCada()
        {
            Assert.True(Rules.Shotgun.Pellets > Rules.Rifle.Pellets);
            Assert.True(Rules.Shotgun.Damage < Rules.Rifle.Damage);
            // mas o tiro cheio bate mais forte que uma bala de rifle
            Assert.True(Rules.Shotgun.Damage * Rules.Shotgun.Pellets > Rules.Rifle.Damage);
        }

        [Fact]
        public void RifleEAutomaticoEShotgunNao()
        {
            Assert.True(Rules.Rifle.FullAuto);
            Assert.False(Rules.Shotgun.FullAuto);
        }

        [Fact]
        public void ShotgunTemAlcanceMenor()
        {
            Assert.True(Rules.Shotgun.Range < Rules.Rifle.Range);
        }
    }

    public class ScoreTests
    {
        [Fact]
        public void PrimeiroAbateNaoGanhaMultiplicador()
        {
            Assert.Equal(Rules.Spec(EnemyKind.Drone).Score, Rules.KillScore(EnemyKind.Drone, 1));
        }

        [Fact]
        public void ComboMultiplicaAPontuacao()
        {
            Assert.Equal(Rules.Spec(EnemyKind.Drone).Score * 3, Rules.KillScore(EnemyKind.Drone, 3));
        }

        [Fact]
        public void ComboTemTeto()
        {
            Assert.Equal(Rules.MaxCombo, Rules.NextCombo(Rules.MaxCombo));
            Assert.Equal(Rules.KillScore(EnemyKind.Drone, Rules.MaxCombo),
                         Rules.KillScore(EnemyKind.Drone, 99));
        }

        [Fact]
        public void ComboZeradoNaoZeraAPontuacao()
        {
            Assert.Equal(Rules.Spec(EnemyKind.Brute).Score, Rules.KillScore(EnemyKind.Brute, 0));
        }

        [Fact]
        public void BruteValeMaisQueDrone()
        {
            Assert.True(Rules.KillScore(EnemyKind.Brute, 1) > Rules.KillScore(EnemyKind.Drone, 1));
        }

        [Fact]
        public void BonusDeOndaCresce()
        {
            Assert.True(Rules.WaveClearBonus(5, 1) > Rules.WaveClearBonus(1, 1));
        }
    }

    public class VecTests
    {
        [Fact]
        public void NormalizarVetorNuloNaoDaNaN()
        {
            var n = Vec3.Zero.Normalized;
            Assert.False(float.IsNaN(n.X) || float.IsNaN(n.Y) || float.IsNaN(n.Z));
            Assert.Equal(Vec3.Zero, n);
        }

        [Fact]
        public void NormalizadoTemComprimentoUm()
        {
            var n = new Vec3(3, 4, 12).Normalized;
            Assert.Equal(1f, n.Length, 5);
        }

        [Fact]
        public void ProdutoVetorialSegueAMaoDireita()
        {
            var c = Vec3.Cross(new Vec3(1, 0, 0), new Vec3(0, 1, 0));
            Assert.Equal(new Vec3(0, 0, 1), c);
        }

        [Fact]
        public void DistanciaXZIgnoraAltura()
        {
            var a = new Vec3(0, 100, 0);
            var b = new Vec3(3, -50, 4);
            Assert.Equal(5f, Vec3.DistanceXZ(a, b), 4);
        }
    }
}

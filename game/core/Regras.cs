namespace TurnoDaNoite.Core
{
    /// <summary>
    /// Todos os números que definem a sensação do jogo, num lugar só.
    /// Ficam separados da lógica de propósito: balancear terror é mexer nestes
    /// valores muitas vezes, e cada mexida precisa passar pelos testes.
    /// </summary>
    public static class Regras
    {
        // ---------------- jogador ----------------
        public const float RaioJogador = 0.38f;
        public const float AlturaOlhoEmPe = 1.66f;
        public const float AlturaOlhoAgachado = 1.02f;

        public const float VelAndando = 3.25f;
        public const float VelAgachado = 1.75f;
        public const float VelCorrendo = 6.0f;

        /// <summary>Fôlego: correr gasta, parar recupera. Sem fôlego você fica ofegante e se entrega.</summary>
        public const float GastoFolego = 0.27f;
        public const float GanhoFolego = 0.17f;
        public const float GanhoFolegoAgachado = 0.26f;
        public const float FolegoMinimoParaCorrer = 0.08f;
        public const float FolegoOfegante = 0.45f;

        /// <summary>Lanterna: ~95 segundos de luz contínua. Uma bateria devolve 45%.</summary>
        public const float GastoBateria = 0.0105f;
        public const float BateriaPorPilha = 0.45f;

        /// <summary>Ar preso no armário: ~3 s segurando, e recupera devagar.</summary>
        public const float GastoAr = 0.32f;
        public const float GanhoAr = 0.22f;

        // ---------------- barulho que o jogador faz ----------------
        // O raio é em metros: se a criatura estiver dentro dele, ela ouve e investiga.
        public const float RuidoCorrendo = 24f;
        public const float RuidoAndando = 8f;
        public const float RuidoAgachado = 2.5f;
        public const float RuidoOfegante = 6f;
        public const float RuidoArmario = 7f;
        public const float RuidoQuadro = 30f;   // instalar fusível faz barulho alto, de propósito

        // ---------------- o que a criatura enxerga ----------------
        // A luz acesa é o que te mata: ela te vê de quase toda a extensão do prédio.
        public const float VisaoComLanterna = 34f;
        public const float VisaoEmPe = 10f;
        public const float VisaoAgachado = 5.5f;
        public const float BonusVisaoCorrendo = 5f;
        /// <summary>Cosseno do meio-ângulo do campo de visão (~104°). Só vale no escuro: luz acesa ela nota de qualquer ângulo.</summary>
        public const float CossenoCampoVisao = 0.25f;

        // ---------------- velocidade da criatura ----------------
        public const float VelPatrulha = 2.35f;
        public const float VelInvestiga = 3.3f;
        public const float VelCaca = 5.15f;
        public const float VelProcura = 2.9f;
        /// <summary>Cada fusível instalado acelera ela. O jogo aperta quando você está quase saindo.</summary>
        public const float AceleracaoPorFusivel = 0.42f;

        public const float PacienciaCaca = 6f;
        public const float PacienciaProcura = 11f;
        public const float PacienciaInvestiga = 9f;

        /// <summary>Sem pista por tempo demais, ela começa a fechar o cerco. É o que impede o jogo de virar passeio.</summary>
        public const float SegundosSemPistaAteApertar = 25f;

        public const float DistanciaParaPegar = 1.35f;
        public const float DistanciaArmario = 1.8f;
        /// <summary>Perto e com linha de visão ela larga a grade e vem reto. Sem isto ela para no último centro de célula e nunca alcança.</summary>
        public const float DistanciaInvestida = 7f;

        // ---------------- objetivo ----------------
        public const int FusiveisNecessarios = 5;
        public const int BateriasNoMapa = 4;
        public const int ArmariosPorSala = 2;
        public const float RaioInteracao = 2.4f;
        /// <summary>Distância mínima entre onde a criatura nasce e onde você nasce.</summary>
        public const float DistanciaInicialMinima = 28f;
    }
}

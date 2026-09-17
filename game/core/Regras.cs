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

        /// <summary>
        /// Lanterna: ~5 minutos de luz contínua. Estava em 95 segundos, o que na
        /// prática parecia defeito — você acendia, andava um pouco e a luz morria
        /// antes de dar tempo de achar qualquer coisa. A escassez continua sendo
        /// mecânica, mas agora numa escala que dá para jogar.
        /// </summary>
        public const float GastoBateria = 0.0033f;
        public const float BateriaPorPilha = 0.45f;

        /// <summary>
        /// Comprimento da passada, em metros. É isto que define o ritmo do som:
        /// o contador anda junto com a velocidade, então a passada fica mais
        /// rápida quando você corre sem precisar de nenhum caso especial.
        /// Andando a 3,25 m/s dá uma passada a cada 0,57 s, que é gente andando.
        /// </summary>
        public const float PassadaAndando = 1.85f;
        public const float PassadaCorrendo = 2.10f;
        public const float PassadaAgachado = 1.25f;

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
        /// <summary>Revistar gaveta faz barulho medio: e o preco de procurar.</summary>
        public const float RuidoRevistar = 11f;
        /// <summary>Escada de metal em prédio vazio é das coisas mais altas que existem.</summary>
        public const float RuidoEscada = 16f;

        /// <summary>
        /// Quanto do barulho passa pela laje. Não é zero — o andar de cima não
        /// pode virar abrigo seguro — mas é pouco: correr lá em cima ainda a
        /// chama, andar agachado não.
        /// </summary>
        public const float BarulhoAtravessaLaje = 0.45f;

        /// <summary>Segundos de espera antes de a escada poder ser usada de novo.</summary>
        public const float EsperaDaEscada = 0.8f;

        // ---------------- medo, que é o que dispara o som ----------------
        /// <summary>
        /// A que distância ela começa a dar medo. Estava em 26 m: num prédio
        /// onde ela circula, isso deixava o medo acima do limiar do coração
        /// quase o tempo todo, e o baque contínuo no ouvido deixava de avisar
        /// qualquer coisa. Aviso que toca sempre não é aviso, é barulho.
        /// </summary>
        public const float DistanciaQueDaMedo = 17f;
        public const float MedoParaOCoracao = 0.62f;
        public const float MedoParaARespiracao = 0.55f;

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
        /// <summary>
        /// A que distância o cerco se dá por cumprido. Fica acima do alcance
        /// agachado (5,5 m) de propósito: quem se agacha e fica quieto vê ela
        /// chegar perto, olhar em volta e ir embora. Quem está de pé ou com a
        /// lanterna acesa é notado bem antes disso.
        /// </summary>
        public const float RaioCerco = 6f;

        public const float DistanciaParaPegar = 1.35f;
        public const float DistanciaArmario = 1.8f;
        /// <summary>Perto e com linha de visão ela larga a grade e vem reto. Sem isto ela para no último centro de célula e nunca alcança.</summary>
        public const float DistanciaInvestida = 7f;

        // ---------------- objetivo ----------------
        public const int FusiveisNecessarios = 5;
        public const int BateriasNoMapa = 4;
        public const int ArmariosPorSala = 1;
        /// <summary>Recipientes por sala, nas salas que ganham algum.</summary>
        public const int RecipientesPorSala = 2;

        /// <summary>
        /// Que fração dos cômodos ganha móveis para revistar.
        ///
        /// Não é 100% porque a conta importa: com nove itens escondidos, cem
        /// gavetas dão uma chance em onze por gaveta, e o jogo vira garimpo.
        /// Com cerca de cinquenta, é uma em cinco — procurar continua custando,
        /// mas cada gaveta aberta é uma aposta razoável. Sala sem recipiente
        /// também tem função: ensina que nem todo cômodo vale a visita.
        /// </summary>
        public const float FracaoDeSalasComRecipiente = 0.55f;
        public const float FracaoDeSalasComArmario = 0.5f;
        /// <summary>
        /// Distância mínima entre dois móveis. Sem ela dois recipientes nasciam
        /// no mesmo ponto, um dentro do outro, e o de fora tomava a interação —
        /// o fusível do de dentro ficava impossível de pegar.
        /// </summary>
        public const float EspacoEntreMoveis = 1.8f;
        public const float RaioInteracao = 2.4f;
        /// <summary>Distância mínima entre onde a criatura nasce e onde você nasce.</summary>
        public const float DistanciaInicialMinima = 28f;
    }
}

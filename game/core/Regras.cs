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
        /// Subiu de 1,8 para 2,4 quando os móveis ganharam corpo: dois deles a
        /// 1,8 m com meio metro de raio cada deixavam 80 cm de vão, e o jogador
        /// tem 76 cm de largura. Passava raspando, ou não passava.
        public const float EspacoEntreMoveis = 2.4f;

        // ---------------- corpo dos móveis ----------------
        // Raio de colisão, em metros. Círculo, não caixa: quem esbarra num
        // círculo desliza, quem esbarra numa quina engancha.
        public const float RaioArmario = 0.52f;
        public const float RaioQuadro = 0.50f;
        /// <summary>Raio dela contra móveis. Menor que o corpo: o caminho dela ignora mobília, então precisa caber espremendo.</summary>
        public const float RaioCriaturaEmMoveis = 0.30f;

        // ---------------- batente e escada ----------------
        /// <summary>
        /// Onde fica a ombreira da porta, medida do meio da célula. O vão livre
        /// entre as duas é o que sobra: com 0,755 e raio 0,22, passa 1,07 m —
        /// e o jogador tem 0,76 m de largura.
        /// </summary>
        public const float MeioVaoDaPorta = 0.755f;
        public const float RaioDoBatente = 0.22f;

        /// <summary>
        /// A folha da porta, escancarada contra a parede.
        ///
        /// Os números ficam aqui, e não em quem desenha, porque são lidos
        /// pelos dois lados: o Godot põe a folha onde eles mandam e o núcleo
        /// põe a colisão no mesmo lugar. Separados, eles divergem — foi
        /// exatamente assim que o barril acabou com 24 cm de desenho e 76 de
        /// corpo.
        /// </summary>
        public const float LarguraDaFolha = 1.242f;
        /// <summary>Quanto a dobradiça recua para dentro do cômodo.</summary>
        public const float RecuoDaFolha = 0.14f;
        /// <summary>Abertura em radianos. 1,42 é quase 90°: quase encostada na parede.</summary>
        public const float AberturaDaFolha = 1.42f;
        public const float RaioDaFolha = 0.22f;
        /// <summary>Círculos que dão corpo à folha. Três se encostam ao longo de 1,24 m.</summary>
        public const int PostesDaFolha = 3;

        /// <summary>Montantes do lance de escada. O meio fica livre: pisar nele é o que troca de andar.</summary>
        public const float MeioLanceDaEscada = 0.93f;
        public const float RaioDoMontante = 0.24f;
        /// <summary>
        /// Quantos círculos formam cada lateral do lance. Com um só, no meio,
        /// meio metro de viga colidia e dois e meio não — dava para atravessar
        /// a escada de lado, andando. Sete, com raio 0,24, se encostam.
        /// </summary>
        public const int PostesDoLance = 7;

        /// <summary>
        /// A folha da porta da frente, quando fechada. Larga: e uma porta de
        /// 2,4 m que tem de VEDAR o vao, e nao so atrapalhar a passagem.
        /// </summary>
        public const float RaioDoPortao = 1.45f;
        /// <summary>Altura dela, em metros. Mais alta que gente, e é para ser.</summary>
        public const float AlturaDaCriatura = 2.35f;

        /// <summary>
        /// Raio de cada móvel que se revista. Os três números são medidos
        /// contra o que `Modelos` desenha, e o auto-teste do Godot cobra isso
        /// a cada partida: móvel mais largo na tela do que na colisão é móvel
        /// que se atravessa pela quina, e mais estreito é parede invisível.
        /// </summary>
        public static float RaioDoRecipiente(TipoRecipiente t) => t switch
        {
            // caixa de ferramentas é baixa e pequena, mas você não passa por cima
            TipoRecipiente.CaixaDeFerramentas => 0.32f,
            // era 0,42 para um móvel de 66 cm de frente: nove centímetros de
            // parede invisível em volta do gaveteiro
            TipoRecipiente.Gaveteiro => 0.36f,
            // era 0,58 para uma estante de 1,30 m: sete centímetros de estante
            // ficavam do lado de fora da colisão, e a quina se atravessava
            _ => 0.66f
        };
        public const float RaioInteracao = 2.4f;
        /// <summary>Distância mínima entre onde a criatura nasce e onde você nasce.</summary>
        public const float DistanciaInicialMinima = 28f;

        // ---------------- quantas sao ----------------
        /// <summary>
        /// Quantas criaturas rondam o predio. Com uma so, decorar a rota dela
        /// resolvia o jogo e o andar em que ela nao estava virava passeio.
        /// Tres num predio de 58 comodos: voce raramente ve duas, mas nunca
        /// sabe se o corredor vazio esta vazio.
        /// </summary>
        public const int QuantidadeDeCriaturas = 3;
        /// <summary>Distancia minima entre elas ao nascer: duas no mesmo comodo valem por uma.</summary>
        public const float DistanciaEntreCriaturas = 30f;
        /// <summary>Raio do chamado: quem te ve avisa as outras, como se gritasse.</summary>
        public const float AlcanceDoChamado = 55f;
        /// <summary>So se ouve o passo da que esta perto; tres somadas viram chiado.</summary>
        public const float AlcanceDoSomDela = 30f;
    }
}

namespace TurnoDaNoite.Core
{
    public enum TipoRecipiente { CaixaDeFerramentas, Gaveteiro, Prateleira }

    /// <summary>
    /// Algo que se revista. Os fusíveis não ficam mais largados no chão:
    /// estão dentro destes, e a maior parte deles está vazia.
    ///
    /// O ganho não é visual, é de ritmo: revistar faz barulho e prende você
    /// parado num lugar por um instante, que é exatamente quando dá medo.
    /// E procurar em cinco gavetas vazias antes de achar algo vale mais que
    /// ver o item brilhando do outro lado da sala.
    /// </summary>
    public sealed class Recipiente
    {
        public P2 Pos;
        public int Andar;
        public TipoRecipiente Tipo;
        public bool Aberto;

        /// <summary>Índice do item guardado aqui, ou -1 se estiver vazio.</summary>
        public int FusivelDentro = -1;
        public int BateriaDentro = -1;
        /// <summary>A planta do prédio está aqui. Só um recipiente no mapa inteiro tem.</summary>
        public bool MapaDentro;

        public bool TemAlgo => FusivelDentro >= 0 || BateriaDentro >= 0 || MapaDentro;

        public string Nome => Tipo switch
        {
            TipoRecipiente.CaixaDeFerramentas => "caixa de ferramentas",
            TipoRecipiente.Gaveteiro => "gaveteiro",
            _ => "prateleira"
        };
    }
}

using System;

namespace TurnoDaNoite.Core
{
    /// <summary>
    /// Converte a entrada do teclado (que é relativa ao jogador) para direção
    /// no mundo (que é o que a simulação consome).
    ///
    /// Isto mora no núcleo, e não em cada motor, porque errar o sinal aqui é
    /// silencioso e cruel: com o sinal trocado o W funciona olhando para o
    /// norte e para o sul, e anda exatamente ao contrário olhando para leste e
    /// oeste. Foi assim que o bug passou despercebido até alguém jogar. Com a
    /// conta num lugar só, existe teste para ela.
    /// </summary>
    public static class Direcao
    {
        /// <summary>
        /// <paramref name="lado"/> é positivo para a direita (tecla D),
        /// <paramref name="frente"/> é negativo para a frente (tecla W), seguindo
        /// a convenção de que a câmera olha para -Z. <paramref name="giro"/> em radianos.
        /// </summary>
        public static P2 LocalParaMundo(float lado, float frente, float giro)
        {
            float sin = MathF.Sin(giro), cos = MathF.Cos(giro);
            return new P2(lado * cos + frente * sin,
                         -lado * sin + frente * cos);
        }

        /// <summary>Para onde a câmera aponta no plano, dado o giro. Andar para a frente tem de bater com isto.</summary>
        public static P2 Frente(float giro) => new P2(-MathF.Sin(giro), -MathF.Cos(giro));

        /// <summary>Para onde fica a direita do jogador.</summary>
        public static P2 Direita(float giro) => new P2(MathF.Cos(giro), -MathF.Sin(giro));
    }
}

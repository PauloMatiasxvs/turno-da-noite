# Modelos baixados que o jogo não usa

## demonio-quaternius.gltf — "Demon" (Quaternius, CC0)

Era a criatura. Saiu por dois motivos, nenhum deles técnico:

1. **É um demônio rosa de desenho, com auréola e forquilha.** Pintar de
   preto não resolve, porque o que entrega a piada é a silhueta: corpo
   redondo, asinhas, auréola. Num corredor escuro, no limite do facho da
   lanterna, aquilo vira cômico na hora — e cômico é o oposto do que este
   jogo precisa daquele bicho.

2. **O ajuste de escala ainda o esmagava.** Modelo de braços abertos é
   mais largo que alto, e o encaixe por caixa usa a dimensão mais
   apertada: a criatura de 2,35 m aparecia com setenta centímetros. Isso
   foi corrigido (`Props.CriarComAltura`), mas a essa altura a silhueta
   já tinha decidido a questão.

O lugar dela agora é `Modelos.Criatura()`, montada em código: alta demais
para ser gente, magra, braços que chegam ao chão, sem rosto, dois olhos
acesos. A caminhada é feita à mão em `Bootstrap.AnimarCriaturaNaMao`.

**Para trocar por um modelo de verdade:** ponha um arquivo chamado
`criatura.glb` (ou .gltf/.fbx) em `assets/modelos/` — uma pasta acima
desta. O registro em `Props.cs` prefere o arquivo, e a animação passa a
sair do `AnimationPlayer` dele em vez do código. Procure algo humanoide,
magro e alto; veja `docs/ASSETS.md` para fontes com licença comercial.

O arquivo fica guardado aqui, e não apagado, porque é CC0 e pode servir
de referência de escala e de rig.

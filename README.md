# Turno da Noite

Jogo de terror e sobrevivência em 3D, escrito do zero — sem engine, sem biblioteca, sem um único arquivo de imagem ou som no repositório.

Você é o vigia de uma subestação desativada. A energia caiu. Para abrir o portão você precisa achar **cinco fusíveis** e levá-los ao quadro elétrico do subsolo. Tem outra coisa lá dentro, e você não tem arma.

**Jogue agora:** abra [`horror/turno-da-noite.html`](horror/turno-da-noite.html) com duplo clique. Um arquivo, ~60 KB, nenhuma dependência. **Use fone de ouvido** — a direção do som é a única defesa que o jogo oferece.

## As três regras que fazem o jogo

1. **A lanterna é um dilema.** Acesa, você enxerga — e ela te vê de 34 metros. Apagada, ela só te percebe de perto e de frente. A bateria acaba.
2. **Ela caça por som.** Correr faz barulho num raio de 24 metros, andar 8, agachado quase nada. Ofegante, você se entrega sozinho.
3. **O armário não é imunidade.** Se ela chegar do lado e você estiver respirando, acabou. Segure o ar — mas o ar também acaba.

Cada fusível instalado deixa ela mais rápida e mais agressiva. O jogo fica pior justamente quando você está quase saindo.

| Tecla | Ação |
|---|---|
| `W A S D` | andar |
| `Shift` | correr (barulhento) |
| `Ctrl` | agachar (silencioso) |
| `F` | lanterna |
| `E` | pegar / usar / esconder |
| `Espaço` | prender a respiração |
| `Esc` / `M` | pausar / mudo |

## Como ele foi feito

Terror não precisa de gráfico bonito — precisa de escuridão, som e ritmo. Foi nisso que o motor foi gasto:

- **Lanterna no shader**: cone com borda macia, queda quadrática e um halo de vazamento em volta. É a única fonte de luz real do jogo; o ambiente é quase zero, de propósito.
- **Pós-processamento** num framebuffer separado: granulado de filme, vinheta pesada, aberração cromática e chuvisco de estática — todos crescendo com o medo, que por sua vez vem da distância e do estado dela.
- **Áudio posicional** com `PannerNode` em HRTF: os passos dela chegam do lado certo do fone. Nenhum arquivo de áudio — tudo é oscilador e ruído filtrado gerados na hora.
- **O prédio** é uma grade de células com nove cômodos e corredores escavados, com ciclos de propósito para você ter rota de fuga. Uma busca em largura valida a planta antes de começar: item inalcançável trava o jogo em silêncio, então isso é verificado, não torcido.
- **A criatura** anda pela mesma busca em largura, com quatro estados — patrulha, investiga, caça, procura. Quando fica tempo demais sem pista, ela começa a apertar o cerco na sua direção.

## Testes

Abra o jogo com `?selftest` na URL: <http://127.0.0.1:8080/horror/turno-da-noite.html?selftest>

São 12 checagens que rodam a partida sem janela — planta conectada, criatura patrulhando o prédio inteiro, perseguição alcançando o jogador, armário salvando com o ar preso e falhando sem ele. O resultado sai no console e em `window.__selftest`.

Três bugs saíram daí, e nenhum deles apareceria só olhando a tela:

- a criatura recalculava o destino a cada 1,1 s, trocava de ideia antes de sair do lugar e nunca cruzava o prédio
- ela perseguia centros de célula e parava a 1,7 m de você, sem nunca fechar o último passo
- o armário protegia mesmo sem prender a respiração, o que anulava a mecânica central

---

# Neon Arena (o protótipo anterior)

Antes deste, saiu um FPS de arena por ondas. Ele funciona e está testado, mas é um demo técnico: cubos atirando em esferas, sem peso e sem susto. Ficou no repositório porque o motor 3D dele é a base de tudo e porque os testes têm valor próprio.

Jogue em [`dist/neon-arena.html`](dist/neon-arena.html).

O que ele tem de interessante:

- **`src/`** — motor WebGL2 em 12 módulos: matrizes 4×4, projeção em perspectiva, matriz normal por inversa-transposta, shaders GLSL, raycast contra esfera e AABB
- **`unity/Assets/Scripts/Core/`** — a partida inteira em C# puro, sem uma linha de `UnityEngine`: `Sim.cs` roda física, IA, armas e ondas
- **`godot/`** e **`unity/.../Runtime/`** — dois motores lendo o mesmo núcleo, cada um só desenhando
- **`csharp/NeonArena.Tests/`** — 85 testes xUnit; mais 28 no motor web em [`tests/`](tests/)

### Rodar

```powershell
powershell -ExecutionPolicy Bypass -File tools\serve.ps1     # servidor local
powershell -ExecutionPolicy Bypass -File tools\build.ps1     # empacota src/ em dist/
cd csharp\NeonArena.Tests; dotnet test                       # 85 testes C#
godot --headless --path godot -- --selftest                  # auto-teste do Godot
```

### Instalar Godot ou Unity

**Godot 4** (gratuito de verdade, licença MIT, ~120 MB): baixe a versão **.NET/Mono** em <https://godotengine.org/download> — `winget install GodotEngine.GodotEngine.Mono`. Precisa também do .NET SDK 8+. No editor: **Import**, aponte para `godot/`, aperte F5.

**Unity 6** (gratuito no plano Personal, abaixo de um teto de faturamento; exige conta e ~12 GB): instale o Hub em <https://unity.com/download>, crie um projeto **3D**, copie `unity/Assets/Scripts` para dentro de `Assets/`, adicione o componente **Neon Arena → Game Bootstrap** a um objeto vazio e aperte Play.

## O que foi verificado, e o que não foi

| Parte | Prova |
|---|---|
| Turno da Noite | 12 checagens automáticas + jogado no navegador |
| Núcleo C# | 85 testes xUnit passando |
| Godot | compila sem avisos; auto-teste headless roda 15 s de partida |
| Neon Arena (web) | 28 testes + conferido no navegador |
| Unity | **só a sintaxe.** Compilado fora do editor: os 108 erros são todos "UnityEngine não existe". As chamadas de API nunca foram executadas — o editor não está instalado aqui |

## Licença

MIT — veja [LICENSE](LICENSE).

# Neon Arena

FPS 3D de arena por ondas, escrito do zero em três versões que compartilham a mesma lógica de jogo:

| Versão | Roda com | Estado |
|---|---|---|
| **Web** | só o navegador, sem instalar nada | motor 3D próprio em WebGL2 |
| **Godot 4** | Godot Mono (gratuito, 120 MB) | testado automaticamente |
| **Unity** | Unity 6 (gratuito no plano Personal) | sintaxe verificada, não executado |

A ideia que segura tudo: **o motor gráfico só desenha**. A partida inteira — física, colisão, IA, armas, ondas, pontuação — vive em C# puro, sem uma linha de `UnityEngine` ou `Godot`. Por isso ela cabe num projeto de testes e roda igual nos três lugares.

---

## Jogar agora, sem instalar nada

Abra **`dist/neon-arena.html`** com duplo clique. É um arquivo único de ~60 KB, sem dependências, sem servidor, sem internet.

### Controles

| Tecla | Ação |
|---|---|
| `W` `A` `S` `D` | mover |
| Mouse / clique esquerdo | mirar / atirar |
| `Shift` | correr |
| `Espaço` | pular (dá para subir nos engradados) |
| `R` | recarregar |
| `1` `2` | rifle / shotgun |
| `Esc` | pausar e liberar o mouse |
| `M` | mudo |

Acertar o **núcleo brilhante** de um inimigo causa dano dobrado. Abates rápidos acumulam combo até 8×.

---

## O que tem dentro

```
neon-arena/
├── dist/neon-arena.html        jogo web pronto, arquivo único
├── index.html                  versão de desenvolvimento (carrega os módulos)
├── src/                        motor 3D + jogo, 12 módulos ES (~1.330 linhas)
│   ├── utils.js                matemática: matrizes 4x4, vetores, normais
│   ├── gl.js                   WebGL2: contexto, shaders GLSL, malhas, desenho
│   ├── world.js                arena e colisão (raycast, empurrão, chão)
│   ├── entities.js             jogador, inimigos, partículas
│   ├── combat.js               tiro, dano, abate
│   ├── update.js               laço de simulação
│   ├── render.js               composição do quadro
│   └── ...                     áudio, entrada, HUD, estado, ponto de entrada
├── tools/
│   ├── serve.ps1               servidor de desenvolvimento
│   ├── build.ps1               empacota src/ em dist/neon-arena.html
│   └── push-github.ps1         publica no GitHub
├── unity/Assets/Scripts/
│   ├── Core/                   ⭐ a lógica compartilhada, C# puro
│   │   ├── Vec3.cs             vetor próprio (para não depender de motor)
│   │   ├── Geometry.cs         raio×caixa, raio×esfera, colisão, chão
│   │   ├── ArenaLayout.cs      a planta da arena
│   │   ├── Rules.cs            armas, inimigos, ondas, pontuação
│   │   └── Sim.cs              a partida inteira, sem motor gráfico
│   └── Runtime/                camada Unity (só desenha)
├── godot/                      projeto Godot 4 (só desenha)
└── csharp/NeonArena.Tests/     85 testes xUnit sobre o Core
```

O `Core/` mora dentro de `unity/Assets/` por uma imposição do Unity, que só compila scripts abaixo de `Assets/`. O Godot e os testes compilam **os mesmos arquivos** por caminho relativo — não existe cópia duplicada para manter em sincronia.

---

## Nada de engine na versão web

Não há Three.js, nem Babylon, nem biblioteca alguma. Está tudo escrito à mão:

- **Matrizes 4×4** em ordem de coluna, com `perspective`, `rotate`, `translate`, `scale` e a inversa-transposta que corrige as normais sob escala não uniforme
- **Shaders GLSL** (vértice e fragmento) com luz direcional, preenchimento frio, brilho de borda por fresnel, névoa exponencial e grade procedural no piso
- **Geradores de malha**: cubo, esfera UV e plano, com a ordem dos vértices certa para o descarte de faces traseiras
- **Interseções**: raio×esfera e raio×caixa pelo método das fatias
- **Áudio sintetizado** no WebAudio: nenhum arquivo de som no repositório

---

## Rodar as três versões

### Web, modo desenvolvimento

Módulos ES não carregam por `file://`, então precisa de um servidor:

```powershell
powershell -ExecutionPolicy Bypass -File tools\serve.ps1
```

Abra `http://127.0.0.1:8080/`. Para gerar o arquivo único de novo:

```powershell
powershell -ExecutionPolicy Bypass -File tools\build.ps1
```

### Godot 4

**Instalação, passo a passo:**

1. Baixe o Godot em <https://godotengine.org/download> — escolha a versão **.NET / Mono** (a comum não roda C#)
   - Pelo terminal: `winget install GodotEngine.GodotEngine.Mono`
2. Instale também o **.NET SDK 8 ou mais novo**: <https://dotnet.microsoft.com/download>
   - Pelo terminal: `winget install Microsoft.DotNet.SDK.10`
3. Abra o Godot, clique em **Import**, aponte para a pasta `godot/` deste repositório
4. Aperte **F5**

Não precisa de conta e não tem teto de faturamento: a licença é MIT.

Para conferir sem abrir janela nenhuma:

```powershell
godot --headless --path godot -- --selftest
```

Roda 15 segundos de partida e imprime se a onda começou, se os inimigos nasceram e se o dano chegou. Sai com código 0 quando está tudo certo.

### Unity

**Instalação, passo a passo:**

1. Crie uma conta gratuita em <https://unity.com> (o plano **Personal** é gratuito abaixo de um teto de faturamento — confira os termos atuais, eles mudam)
2. Baixe o **Unity Hub**: <https://unity.com/download>
3. No Hub, aba **Installs** → **Install Editor** → escolha o **Unity 6 LTS** (~12 GB com os módulos)
4. Aba **Projects** → **New project** → modelo **3D (Built-in)** ou **3D (URP)**, os dois funcionam
5. Copie a pasta `unity/Assets/Scripts` deste repositório para dentro de `Assets/` do projeto novo
6. Na cena vazia: menu **GameObject → Create Empty**, e no Inspector **Add Component → Neon Arena → Game Bootstrap**
7. Aperte **Play**

Não há prefab, material nem cena para configurar: o `GameBootstrap` monta câmera, luzes, arena e HUD por código.

---

## Testes

São 113 no total: 85 em C# e 28 no motor web.

### C# — o núcleo compartilhado

```powershell
cd csharp\NeonArena.Tests
dotnet test
```

85 testes cobrindo:

- **Geometria** — raio×caixa de frente, de lado, na diagonal, com origem dentro; raio×esfera tangente e por trás
- **Colisão** — empurrão para fora de engradado, subir em cima, não teleportar por baixo, cobertura bloqueando linha de visão
- **Arena** — as quatro paredes fecham o perímetro em 36 direções
- **Regras** — runner só a partir da onda 2, brute da 3, teto de 28 inimigos, vida escalando 11% por onda, núcleo dobrando o dano, combo com teto
- **Simulação** — pulo sobe e volta, diagonal não é mais rápida que reta, parede não é atravessada nem com quadro de 2 segundos, recarga consome reserva, combo zera ao levar dano, partida chega à onda 12, nada vira `NaN` em partida longa, e a mesma semente reproduz a mesma partida

O gerador aleatório é um xorshift com semente, então os testes repetem a partida exatamente.

### JavaScript — o motor web

Suba o servidor e abra <http://127.0.0.1:8080/tests/>. São 28 testes sobre as matrizes 4×4 (identidade, não comutatividade, quatro rotações voltando ao início, perspectiva, matriz normal sob escala não uniforme, base ortonormal), as interseções e a colisão da arena. A página mostra o resultado em verde ou vermelho e também deixa tudo em `window.__testResults`, para automação.

---

## O que foi verificado, e o que não foi

Sendo honesto sobre o nível de prova de cada parte:

| Parte | Como foi verificada |
|---|---|
| Núcleo C# | 85 testes automatizados, todos passando |
| Godot | compila sem avisos, e o auto-teste headless roda 15 s de partida |
| Web | aberto no navegador; menu, arena, raycast, dano, ondas e fim de jogo conferidos |
| Unity | **só a sintaxe.** Compilei os scripts fora do Unity: os 108 erros são todos `CS0246`/`CS0234`, isto é, "UnityEngine não existe" — nenhum erro de sintaxe. As chamadas de API do Unity **não** foram executadas, porque o editor não está instalado nesta máquina. Você será a primeira pessoa a rodar. |

---

## Licença

MIT — veja [LICENSE](LICENSE). Faça o que quiser com isso.

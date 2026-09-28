# A tabela dos itens

Tudo que existe dentro do prédio, quanto tem de cada, e de onde vem o desenho
de cada um. Os números saem de `game/core/Regras.cs`; as quantidades entre
parênteses são as de uma partida sorteada agora — mudam de partida para partida,
porque o prédio é gerado.

Para conferir os números de uma partida específica:

```bash
godot --headless --path game/godot -- --selftest
```

Para ver o acabamento dos cômodos sem jogar:

```bash
godot --path game/godot -- --screenshot --verdentro
```

---

## 1. O que se pega

| Item | Quantos | Onde nasce | O que faz | Desenho |
|---|---|---|---|---|
| **Fusível** | 5 (`FusiveisNecessarios`) | dentro de um recipiente, nunca no chão | é o objetivo: cinco no quadro e o portão abre | código (`Modelos.Fusivel`) |
| **Pilha** | 4 (`BateriasNoMapa`) | dentro de um recipiente | +45% de bateria (`BateriaPorPilha`) | código (`Modelos.Bateria`) |
| **Planta do prédio** | 1 | dentro de um recipiente, sorteado | libera o mapa no `Tab`, com névoa de guerra | código (`Modelos.Planta`) |

Item dentro de recipiente fechado **não dá para pegar**: é preciso revistar
primeiro (`Item.Alcancavel`). Foi o que levou dois móveis a nascerem no mesmo
ponto, um por cima do outro, com um fusível inalcançável dentro — hoje
`EspacoEntreMoveis` (2,4 m) impede.

Alcance para pegar: **2,4 m** (`RaioInteracao`), tecla `E`.

---

## 2. O que se revista

| Móvel | Quantos (partida de exemplo) | Regra de quantos nascem | Desenho |
|---|---|---|---|
| **Caixa de ferramentas** | — | 2 por cômodo (`RecipientesPorSala`), em 55% dos cômodos (`FracaoDeSalasComRecipiente`) | código |
| **Gaveteiro** | — | idem, sorteado entre os três tipos | código |
| **Prateleira** | — | idem | código |
| | **66 no total** | | |

Só 55% dos cômodos ganham móvel para revistar, e isso é de propósito: com nove
itens escondidos, cem gavetas dariam uma chance em onze por gaveta e o jogo
viraria garimpo. Com cerca de sessenta é uma em sete — procurar continua
custando, mas cada gaveta aberta é uma aposta razoável.

Revistar faz **barulho 11** (`RuidoRevistar`), num prédio onde andar faz 8 e
correr faz 24.

---

## 3. O que se usa, mas não se carrega

| Coisa | Quantos | Para que serve | Barulho | Desenho |
|---|---|---|---|---|
| **Armário** | 29 | esconder-se (`Ctrl`+`E`); espiar pelas venezianas | 7 ao entrar | código (`Modelos.Armario`) |
| **Quadro elétrico** | 1 | instalar os fusíveis | **30** — o mais alto do jogo, de propósito | código |
| **Portão** | 1 | a saída; fechado até os cinco fusíveis | — | código |
| **Escada** | 2 | trocar de andar (pisar no poço) | 16 | código (`Modelos.Escada`) |

Cada fusível instalado **acelera as criaturas** em `AceleracaoPorFusivel`
(0,42 m/s). O jogo aperta justamente quando você está quase saindo.

---

## 4. Cenário (não se pega, mas ocupa espaço)

Espalhado pelas bordas dos cômodos, nunca no meio: móvel no meio da sala vira
obstáculo de labirinto, encostado na parede vira cenário.

| Adorno | Raio de colisão | Desenho |
|---|---|---|
| **Caixote** | 0,45 m | `caixote.glb` (Kenney, CC0) |
| **Barril** | 0,38 m | `barril.glb` (Kenney, CC0) |
| **Bancada** | 0,44 m | `bancada.glb` (Kenney, CC0) |
| **Pilha de caixas** | 0,40 m | `pilha.glb` (Kenney, CC0) |
| **Cano de parede** | 0,12 m | código |
| **Entulho** | — atravessa | código |
| **Cano de teto** | — atravessa | código |
| **Luminária** | — no teto | `luminaria.glb` (Kenney, CC0) |

Nenhum passa de 0,45 m de raio (`Cenario.RaioMaximo`): mais largo que isso
fecha a passagem pelo meio da célula, e fechar cômodo com enfeite é a pior
forma de travar uma partida.

---

## 5. A criatura

| | |
|---|---|
| Quantas | **3** (`QuantidadeDeCriaturas`) |
| Altura | 2,35 m (`AlturaDaCriatura`) |
| Nascem a | 28 m de você, 30 m uma da outra |
| Quando uma te vê | chama as outras num raio de **55 m** (`AlcanceDoChamado`) |
| Desenho | `criatura.glb` — modelo seu, 5,2 MB |
| Animação | sem esqueleto: o corpo inteiro rasteja (sobe, desce, rola) |

---

## 6. O jogador

| | |
|---|---|
| Mãos | `mao_com_lanterna.glb` — modelo seu, com esqueleto de 37 ossos |
| Lanterna | montada em código, pendurada no osso `hand_R` |
| Bateria cheia | ~5 minutos de luz contínua (`GastoBateria` 0,0033/s) |
| Fôlego | correr gasta 0,27/s, parado recupera 0,17/s, agachado 0,26/s |
| Ar (no armário) | ~3 s segurando (`GastoAr` 0,32/s) |

Teclas: `WASD` andar · `Shift` correr · `Ctrl` agachar · `Space` prender o ar ·
`E` interagir · `F` lanterna · `Tab` mapa · `Q` largar · `Esc` pausa · `F5`
reiniciar.

---

## 7. Arquivos de modelo instalados

Em `game/godot/assets/modelos/`. O registro (`Props.cs`) procura o arquivo pelo
nome; se não achar, monta a peça em código e o jogo continua rodando.

| Arquivo | Tamanho | Origem | Usado? |
|---|---|---|---|
| `criatura.glb` | 5,2 MB | seu | ✅ a criatura |
| `mao_com_lanterna.glb` | 4,6 MB | seu | ✅ os braços em primeira pessoa |
| `caixote.glb` | 5 KB | Kenney (CC0) | ✅ cenário |
| `barril.glb` | 6 KB | Kenney (CC0) | ✅ cenário |
| `bancada.glb` | 15 KB | Kenney (CC0) | ✅ cenário |
| `pilha.glb` | 7 KB | Kenney (CC0) | ✅ cenário |
| `luminaria.glb` | 6 KB | Kenney (CC0) | ✅ teto |
| `arandela.glb` | 5 KB | Kenney (CC0) | ❌ baixado, sem uso |
| `cabide.glb` | 13 KB | Kenney (CC0) | ❌ baixado, sem uso |
| `cadeira.glb` | 39 KB | Kenney (CC0) | ❌ baixado, sem uso |
| `capacho.glb` | 14 KB | Kenney (CC0) | ❌ baixado, sem uso |
| `planta_vaso.glb` | 8 KB | Kenney (CC0) | ❌ **engano meu**: `Peca.Planta` é a PLANTA BAIXA do prédio, o mapa em papel — não um vaso |

Os quatro sem uso são móveis bons (cadeira, cabide, capacho, arandela) e caberiam
no cenário: falta só registrá-los em `Cenario.DeParede` com um raio e mapeá-los
em `Bootstrap`. São umas dez linhas cada.

### Por que os dois modelos seus encolheram

Chegaram com 23,5 MB e 21,5 MB. Os exportadores baseados em three.js gravam,
no campo `extras` do nó raiz, **uma cópia inteira da geometria em JSON** — os
mesmos vértices que já estão no bloco binário, de novo, em texto. Era 85% do
arquivo. `tools/limpar-glb.py` tira isso:

```bash
python tools/limpar-glb.py entrada.glb [saida.glb]
```

O Godot ignora `extras`, então nada se perde — mas ele lê o JSON inteiro na
importação, e o arquivo inflado ia parar no repositório e no instalador.

---

## 8. O que ainda é forma crua

Quatro peças ainda não têm modelo nem montagem em código, só uma caixa ou um
cilindro: **parede, piso, teto e cano**. As três primeiras são superfícies
texturizadas — ficam bem assim. O cano é o único que pediria um modelo.

O auto-teste imprime essa lista toda vez, no fim:

```
montadas em codigo (10): armario, quadro_eletrico, portao, fusivel, bateria,
                         caixa_ferramentas, gaveteiro, prateleira, entulho, planta_predio
ainda forma crua (4): parede, piso, teto, cano
```

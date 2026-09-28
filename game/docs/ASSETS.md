# Arte e som: o que baixar e onde colocar

Este é o documento que transforma o protótipo em jogo vendável. O código já está
pronto para receber arte: o registro em `godot/scripts/Props.cs` procura um modelo
para cada peça e, se não achar, usa uma primitiva. **Você não precisa mexer em
código nenhum** — é só pôr o arquivo na pasta com o nome certo.

## Como funciona

Coloque os arquivos em `game/godot/assets/modelos/` com estes nomes exatos
(extensão pode ser `.glb`, `.gltf`, `.obj`, `.fbx` ou `.tscn`):

| Arquivo | O que é | Tamanho que o jogo reserva |
|---|---|---|
| `parede` | segmento de parede | 3,0 × 3,4 × 3,0 m (largura varia) |
| `piso` | chão | plano grande, texturável |
| `teto` | forro | plano grande |
| `armario` | armário de vestiário, onde você se esconde | 1,0 × 2,0 × 0,62 m |
| `quadro_eletrico` | painel com os cinco encaixes | 1,5 × 1,9 × 0,42 m |
| `portao` | portão de saída | 2,4 × 3,2 × 0,3 m |
| `fusivel` | item colecionável | 0,2 × 0,34 × 0,2 m |
| `bateria` | pilha da lanterna | 0,16 × 0,26 × 0,16 m |
| `criatura` | **o inimigo** | 0,7 × 2,35 × 0,5 m |
| `luminaria` | lâmpada de teto | 1,2 × 0,12 × 0,3 m |
| `caixote` | entulho de cenário | 0,85 × 0,8 × 0,85 m |
| `barril` | entulho de cenário | 0,72 × 0,95 m |
| `cano` | tubulação de parede | 0,2 × 3,0 m |
| `caixa_ferramentas` | recipiente de revistar, no chão | 0,58 × 0,26 × 0,30 m |
| `gaveteiro` | recipiente de revistar, três gavetas | 0,66 × 1,02 × 0,52 m |
| `prateleira` | recipiente de revistar, estante de aço | 1,30 × 1,75 × 0,42 m |
| `bancada` | bancada de oficina | 0,98 × 0,88 × 0,82 m |
| `pilha` | pilha de caixas de papelão | 0,72 × 0,9 m |
| `entulho` | tábuas e cacos no chão | 0,8 × 0,1 m |
| `mao_com_lanterna` | a mão do jogador em primeira pessoa | segurando a lanterna, apontada para −Z |

### Os recipientes precisam de uma peça chamada `Tampa`

`caixa_ferramentas`, `gaveteiro` e `prateleira` são os móveis que o jogador abre.
Se o seu modelo tiver um nó filho chamado exatamente **`Tampa`**, o jogo anima esse
nó ao revistar — gira a tampa, puxa as gavetas, tomba a caixa. Sem esse nó o móvel
funciona igual, mas abre sem nenhum sinal visível, e aí dá para revistar o mesmo
gaveteiro três vezes sem perceber.

Os limites de colisão dos móveis de cenário ficam em `game/core/Cenario.cs`: nenhum
passa de 0,45 m de raio, senão ele fecha a passagem pelo meio da célula. Modelo mais
largo que isso fica bonito e intransponível ao mesmo tempo.

O jogo **redimensiona sozinho** o modelo para caber nessa caixa e apoia a base no
chão — então não se preocupe se o artista modelou em centímetros ou com o pivô no
lugar errado.

Sons gravados vão em `game/godot/assets/sons/` com o nome do evento e extensão
`.ogg` — por exemplo `PassoDela.ogg`, `VocePegou.ogg`, `ElaRugiu.ogg`. Enquanto não
existirem, o jogo sintetiza tudo na hora.

## Onde baixar, com licença para uso comercial

Todas as fontes abaixo permitem uso comercial, mas **confira a licença de cada
arquivo na hora de baixar** — elas mudam e variam por item dentro do mesmo site.

### Modelos 3D

| Fonte | Licença | Serve para |
|---|---|---|
| [Kenney](https://kenney.nl/assets) | CC0 (domínio público) | móveis, caixotes, barris, kits industriais. É o melhor ponto de partida: tudo CC0, sem atribuição obrigatória |
| [Quaternius](https://quaternius.com) | CC0 | pacotes low-poly, inclusive personagens com esqueleto |
| [Poly Haven](https://polyhaven.com/models) | CC0 | modelos realistas, poucos mas muito bons |
| [Sketchfab](https://sketchfab.com) | varia — **filtre por "Downloadable" e por licença** | maior acervo; muita coisa CC-BY (exige crédito) |

### Texturas

| Fonte | Licença | Serve para |
|---|---|---|
| [ambientCG](https://ambientcg.com) | CC0 | concreto, metal enferrujado, piso industrial — exatamente o clima deste jogo |
| [Poly Haven](https://polyhaven.com/textures) | CC0 | texturas PBR completas |

### Som

| Fonte | Licença | Serve para |
|---|---|---|
| [Freesound](https://freesound.org) | varia — **filtre por CC0** | passos, rangidos, goteira, ambiente |
| [OpenGameArt](https://opengameart.org) | varia, confira item a item | efeitos e trilha |

### A criatura (a peça mais importante)

É o único modelo que precisa de animação. Caminho mais curto:

1. Baixe um humanoide em Quaternius ou Sketchfab (procure "creature", "zombie",
   "monster" — algo magro e alto funciona melhor no escuro)
2. Suba o modelo no [Mixamo](https://www.mixamo.com) (grátis, exige conta Adobe).
   Ele coloca o esqueleto automaticamente e você escolhe as animações
3. Baixe pelo menos três: **andar devagar** (patrulha), **correr** (caça) e
   **parado olhando em volta** (procura)
4. Exporte como FBX ou glTF e salve como `criatura.glb` na pasta de modelos
5. O uso do Mixamo é liberado para projetos comerciais — confira os termos atuais

## Antes de vender

- **Licença do Godot é MIT**: você fica com 100% da receita, sem royalty, sem teto
  de faturamento. É por isso que escolhemos ele e não Unity
- **Steam** cobra US$ 100 por jogo (Steam Direct), devolvidos depois de um volume
  de vendas. **itch.io** é grátis e você escolhe a divisão
- **Guarde a licença de cada asset** que entrar no jogo, num arquivo de créditos.
  Assets CC-BY exigem crédito visível; CC0 não exige, mas é elegante creditar
- **Não use nada gerado por IA sem checar os termos** da ferramenta para uso comercial

## O que ainda falta de código para o jogo estar vendável

Ordem sugerida, do que mais pesa para o que menos pesa:

1. **Menu inicial, opções e salvamento** — hoje o jogo começa direto. Falta tela
   de título, ajuste de sensibilidade e volume na interface (o sistema de opções
   já existe em `Opcoes.cs`, falta a tela)
2. **Animação da criatura** — depende do modelo do Mixamo entrar
3. **Mais de um mapa** ou geração variada, para dar motivo de rejogar
4. **Uma segunda mecânica** além de fusível: uma que mude como você anda pelo
   prédio na segunda metade do jogo
5. **Build e assinatura** para Windows, e página na loja

## Ferramentas de diagnóstico do projeto Godot

O jogo aceita sinalizadores de linha de comando que existem para depurar o que
não dá para ver jogando:

```
godot --headless --path game/godot -- --selftest    # 10 checagens, sai com código 0 ou 1
godot --path game/godot -- --screenshot             # roda 6 s e salva captura_1.png e captura_2.png
godot --path game/godot -- --screenshot --diag      # fundo magenta e luz chapada: separa "sem geometria" de "sem luz"
godot --path game/godot -- --screenshot --semnevoa  # desliga a névoa
godot --path game/godot -- --screenshot --semnormal # desliga os mapas de normal
godot --path game/godot -- --screenshot --semambiente # roda sem WorldEnvironment
```

Foi com esse conjunto que se achou o bug que deixava a tela preta: a combinação
de tonemap ACES com `AdjustmentContrast`/`AdjustmentSaturation` ligados comia
toda a luz da lanterna — a cena ficava preta mesmo com energia 500 no holofote.

## Texturas instaladas

| Nome no jogo | De onde veio | Onde aparece |
|---|---|---|
| `parede` | **sua** (lambri de nogueira) | paredes dos cômodos |
| `piso` | **sua** (carpete) | chão |
| `criatura` | **sua** (pele) | reserva para a criatura |
| `carpete` | ambientCG (CC0) | chão, variante |
| `teto` | ambientCG — OfficeCeiling001 | forro |
| `reboco` | ambientCG — PaintedPlaster017 | lambril e frisos |
| `metal` | ambientCG (CC0) | armários, porta da frente, lanterna |
| `madeira` | ambientCG — Planks037A | caixotes, bancadas |
| `ferrugem` | ambientCG — Metal041B | tambores, quadro elétrico |
| `couro` | ambientCG — Leather037 | luva da mão montada em código |

Para trocar qualquer uma, ponha três arquivos em
`game/godot/assets/texturas/` com o mesmo nome e os sufixos `_cor`, `_normal` e
`_aspereza` (`.jpg` ou `.png`) — nenhuma linha de código muda.

Tudo que veio da ambientCG é **CC0**: uso comercial liberado, sem atribuição
obrigatória.

### Sobre usar textura de jogo comercial

Não dá, e o motivo é prático antes de ser legal: o objetivo declarado
deste projeto é **poder vender**. Todo asset aqui é CC0 ou feito em
código justamente para que ninguém precise pedir licença a ninguém.
Textura extraída de um jogo pago é do estúdio que a fez; usá-la fecha a
porta de vender, de publicar na Steam e de mostrar o repositório em
público. É o único tipo de asset que este projeto recusa.

## Braços em primeira pessoa (`mao_com_lanterna`)

**Já está instalado**: `mao_com_lanterna.glb`, modelo seu, dois braços com
esqueleto de 37 ossos, pele pintada nas cores de vértice.

O que o código faz com ele, porque o arquivo não traz pronto:

- **empurra o conjunto para a frente**, porque o modelo tem geometria de braço
  até 22 cm ATRÁS da câmera, e o que fica atrás do plano de corte aparece
  fatiado, pelo avesso;
- **fecha os dedos**, osso por osso. As quatro animações que vieram no arquivo
  (`idle`, `grab`, `punch`, `fist_guard`) mexem o pulso um grau e meio e mais
  nada — as pistas dos dedos têm todas o mesmo valor do começo ao fim;
- **liga `VertexColorUseAsAlbedo`**, senão as duas mãos saem brancas chapadas;
- **pendura a lanterna no osso `hand_R`**, calculando a posição no meio dos
  quatro dedos dobrados — assim mexer no cotovelo não deixa a lanterna para trás.

Os números de pose ficam todos juntos em `Bootstrap.cs`, logo abaixo de
`MontarBracos`. Para trocar por outro modelo, basta que ele:

- olhe para **−Z** com a origem no **olho** (não no punho);
- tenha os ossos com os nomes `hand_R`, `index1_R`…`pinky3_R`, `thumb1_R`…,
  e os mesmos com `_L`. É a nomenclatura do Mixamo sem o prefixo.

Sem arquivo nenhum, o jogo monta uma mão em código e continua rodando.

### Onde achar outros, com licença que permite vender

- **Mixamo** (Adobe, grátis, uso comercial liberado): personagens com rig.
  Baixe um, abra no Blender e exporte só os braços.
- **Sketchfab**: filtre por *Downloadable* e por **CC0** ou **CC-BY**;
  procure "fps arms". CC-BY exige crédito num arquivo de créditos.
- **Quaternius** e **Kenney**: CC0, mas não têm braços em primeira pessoa.

## GLB gordo: `tools/limpar-glb.py`

Exportador baseado em three.js grava uma cópia inteira da geometria em JSON no
campo `extras` do nó raiz — os mesmos vértices que já estão no bloco binário,
de novo, em texto. Nos dois modelos que chegaram aqui era 85% do arquivo:

    criatura.glb   23,5 MB  ->  5,2 MB
    maos_fps.glb   21,5 MB  ->  4,6 MB

```bash
python tools/limpar-glb.py entrada.glb [saida.glb]
```

O Godot ignora `extras`, então nada se perde. Rode nisso todo `.glb` que vier
de gerador online antes de pôr na pasta.

## A tabela dos itens

Quantos fusíveis, quantos armários, o que cada móvel faz e qual arquivo desenha
cada coisa: [ITENS.md](ITENS.md).

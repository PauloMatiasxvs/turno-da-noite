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
| `caixote` | entulho de cenário | 0,8 m |
| `barril` | entulho de cenário | 0,6 × 0,9 m |
| `cano` | tubulação de parede | 0,2 × 3,0 m |

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

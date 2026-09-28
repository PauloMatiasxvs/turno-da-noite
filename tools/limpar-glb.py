#!/usr/bin/env python3
"""
Tira o lixo do cabecalho de um .glb e reescreve ele.

Por que isto existe: os exportadores baseados em three.js gravam, no campo
`extras` do no raiz, uma copia INTEIRA da geometria em JSON — os mesmos
vertices que ja estao no bloco binario, de novo, em texto. Nos dois modelos
que chegaram aqui isso era 85% do arquivo:

    maos_fps.glb   21,5 MB  ->  3,2 MB
    criatura.glb   23,5 MB  ->  3,7 MB

O Godot ignora `extras`, entao nada se perde — mas ele LE o JSON inteiro na
importacao, e o arquivo inflado vai parar no repositorio e no instalador.

    python tools/limpar-glb.py entrada.glb [saida.glb]
"""
import json
import struct
import sys
from pathlib import Path

JSON_CHUNK = 0x4E4F534A
BIN_CHUNK = 0x004E4942


def ler(caminho):
    dados = Path(caminho).read_bytes()
    magica, versao, total = struct.unpack_from("<III", dados, 0)
    if magica != 0x46546C67:
        raise SystemExit(f"{caminho}: nao e um .glb")
    pedacos, pos = [], 12
    while pos < total:
        tam, tipo = struct.unpack_from("<II", dados, pos)
        pedacos.append((tipo, dados[pos + 8:pos + 8 + tam]))
        pos += 8 + tam
    return versao, pedacos


def limpar(arvore):
    """Tira `extras` de todo mundo. Devolve quantos bytes de JSON sumiram."""
    perdidos = 0
    for lista in ("nodes", "meshes", "materials", "scenes", "animations"):
        for item in arvore.get(lista, []):
            if "extras" in item:
                perdidos += len(json.dumps(item["extras"]))
                del item["extras"]
    if "extras" in arvore:
        perdidos += len(json.dumps(arvore["extras"]))
        del arvore["extras"]
    return perdidos


def alinhar(dados, enchimento):
    sobra = (-len(dados)) % 4
    return dados + enchimento * sobra


def main():
    if len(sys.argv) < 2:
        raise SystemExit(__doc__)
    entrada = Path(sys.argv[1])
    saida = Path(sys.argv[2]) if len(sys.argv) > 2 else entrada

    antes = entrada.stat().st_size
    versao, pedacos = ler(entrada)
    novos = []
    sumiu = 0
    for tipo, corpo in pedacos:
        if tipo == JSON_CHUNK:
            arvore = json.loads(corpo)
            sumiu = limpar(arvore)
            corpo = alinhar(json.dumps(arvore, separators=(",", ":")).encode("utf-8"), b" ")
        else:
            corpo = alinhar(corpo, b"\0")
        novos.append((tipo, corpo))

    total = 12 + sum(8 + len(c) for _, c in novos)
    saida.parent.mkdir(parents=True, exist_ok=True)
    with open(saida, "wb") as f:
        f.write(struct.pack("<III", 0x46546C67, versao, total))
        for tipo, corpo in novos:
            f.write(struct.pack("<II", len(corpo), tipo))
            f.write(corpo)

    print(f"{entrada.name}: {antes/1e6:.1f} MB -> {total/1e6:.1f} MB "
          f"({sumiu/1e6:.1f} MB de geometria repetida em JSON)")


if __name__ == "__main__":
    main()

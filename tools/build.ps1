<#
  Empacotador. Junta src/*.js num único <script> dentro de dist/neon-arena.html.

  Não é um bundler de verdade: como todos os módulos viram um escopo só, ele
  apenas remove as linhas de import/export e concatena os arquivos na ordem
  declarada em $order. Essa ordem é topológica — um módulo só aparece depois
  de todos cujos const/let ele usa em tempo de carga (const não sofre hoisting).

  Uso:  powershell -ExecutionPolicy Bypass -File tools\build.ps1
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$src  = Join-Path $root 'src'
$dist = Join-Path $root 'dist'

# ordem topológica — quem declara vem antes de quem usa no topo do arquivo
$order = @(
  'utils.js', 'audio.js', 'gl.js', 'world.js', 'entities.js',
  'game-state.js', 'combat.js', 'input.js', 'update.js',
  'hud.js', 'render.js', 'main.js'
)

function Strip-Modules {
  param([string[]]$lines)
  $out = New-Object System.Collections.Generic.List[string]
  $skippingExportBlock = $false
  foreach ($line in $lines) {
    if ($skippingExportBlock) {
      if ($line -match '\};\s*$') { $skippingExportBlock = $false }
      continue
    }
    # import { ... } from "./x.js";   |   import "./x.js";
    if ($line -match '^\s*import\s') { continue }
    # export { a, b, c };  (uma linha ou várias)
    if ($line -match '^\s*export\s*\{') {
      if ($line -notmatch '\};\s*$') { $skippingExportBlock = $true }
      continue
    }
    # export const/let/function  ->  const/let/function
    $out.Add(($line -replace '^\s*export\s+(const|let|var|function|class)\s', '$1 '))
  }
  return $out
}

$chunks = New-Object System.Collections.Generic.List[string]
$jsLines = 0
foreach ($name in $order) {
  $file = Join-Path $src $name
  if (-not (Test-Path $file)) { throw "módulo ausente: $name" }
  $lines = Get-Content $file
  $clean = Strip-Modules $lines
  $jsLines += $clean.Count
  $chunks.Add("/* ==== src/$name ==== */")
  $chunks.Add(($clean -join "`n"))
}

$body = $chunks -join "`n"
$bundle = "<script>`n(function(){`n`"use strict`";`n`n$body`n`n})();`n</script>"

$html = Get-Content (Join-Path $root 'index.html') -Raw
$tag = '<script type="module" src="./src/main.js"></script>'
if ($html -notlike "*$tag*") { throw "tag do módulo não encontrada em index.html" }
$html = $html.Replace($tag, $bundle)

if (-not (Test-Path $dist)) { New-Item -ItemType Directory -Path $dist | Out-Null }
$outFile = Join-Path $dist 'neon-arena.html'
[System.IO.File]::WriteAllText($outFile, $html, (New-Object System.Text.UTF8Encoding($false)))

$kb = [math]::Round((Get-Item $outFile).Length / 1KB, 1)
Write-Host "build ok  ->  dist/neon-arena.html"
Write-Host "  $($order.Count) módulos, $jsLines linhas de JS, $kb KB"
Write-Host "  arquivo único: abre com duplo clique, sem servidor."

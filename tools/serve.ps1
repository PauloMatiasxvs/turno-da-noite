<#
  Servidor estático mínimo para desenvolvimento.

  Por que existe: o jogo em src/ usa módulos ES (import/export). O navegador
  recusa módulos carregados por file:// (política de origem), então abrir o
  index.html com duplo clique não funciona. Este script serve a pasta por HTTP.

  Uso:  powershell -ExecutionPolicy Bypass -File tools\serve.ps1 [-Port 8080]
  Depois abra http://127.0.0.1:8080/

  Para jogar sem servidor nenhum, use dist\neon-arena.html (gerado pelo build).
#>
param([int]$Port = 8080)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

$mime = @{
  '.html' = 'text/html; charset=utf-8'
  '.js'   = 'text/javascript; charset=utf-8'
  '.css'  = 'text/css; charset=utf-8'
  '.json' = 'application/json; charset=utf-8'
  '.svg'  = 'image/svg+xml'
  '.png'  = 'image/png'
  '.ico'  = 'image/x-icon'
}

$listener = New-Object System.Net.Sockets.TcpListener([System.Net.IPAddress]::Loopback, $Port)
try { $listener.Start() }
catch { Write-Error "Porta $Port ocupada. Tente: -Port 8081"; exit 1 }

Write-Host "Neon Arena servindo $root"
Write-Host "  http://127.0.0.1:$Port/         (jogo)"
Write-Host "  http://127.0.0.1:$Port/tests/   (testes)"
Write-Host "Ctrl+C para parar."

while ($true) {
  $client = $listener.AcceptTcpClient()
  try {
    $stream = $client.GetStream()
    $stream.ReadTimeout = 3000
    $buf = New-Object byte[] 8192
    $read = 0
    try { $read = $stream.Read($buf, 0, $buf.Length) } catch {}
    $req = [System.Text.Encoding]::ASCII.GetString($buf, 0, [Math]::Max($read, 0))

    $path = '/'
    if ($req -match '^GET\s+(\S+)') { $path = $matches[1] }
    $path = ($path -split '\?')[0]
    if ($path.EndsWith('/')) { $path += 'index.html' }

    # resolve dentro da raiz e recusa qualquer coisa que escape dela
    $rel = $path.TrimStart('/') -replace '/', [System.IO.Path]::DirectorySeparatorChar
    $full = [System.IO.Path]::GetFullPath((Join-Path $root $rel))
    $inside = $full.StartsWith([System.IO.Path]::GetFullPath($root), [StringComparison]::OrdinalIgnoreCase)

    if ($inside -and [System.IO.File]::Exists($full)) {
      $ext = [System.IO.Path]::GetExtension($full).ToLower()
      $type = if ($mime.ContainsKey($ext)) { $mime[$ext] } else { 'application/octet-stream' }
      $bytes = [System.IO.File]::ReadAllBytes($full)
      $status = '200 OK'
    } else {
      $type = 'text/plain; charset=utf-8'
      $bytes = [System.Text.Encoding]::UTF8.GetBytes("404: $path")
      $status = '404 Not Found'
    }

    $head = "HTTP/1.1 $status`r`nContent-Type: $type`r`nContent-Length: $($bytes.Length)`r`nCache-Control: no-store`r`nConnection: close`r`n`r`n"
    $hb = [System.Text.Encoding]::ASCII.GetBytes($head)
    $stream.Write($hb, 0, $hb.Length)
    $stream.Write($bytes, 0, $bytes.Length)
    $stream.Flush()
    Write-Host "$status  $path"
  } catch {
    Write-Host "erro: $_"
  } finally {
    $client.Close()
  }
}

<#
  Sobe o repositório para o GitHub.

  Roda sozinho pela Tarefa Agendada do Windows ("Neon Arena - push GitHub"),
  mas você pode executar à mão a qualquer momento:
      powershell -ExecutionPolicy Bypass -File tools\push-github.ps1

  Pré-requisito que só você pode fazer: autenticar uma vez com
      gh auth login
  (abre o navegador; escolha GitHub.com -> HTTPS -> autenticar pelo navegador)

  O script não guarda senha nem token: quem cuida disso é o próprio gh.
#>
param(
  [string]$RepoName = 'neon-arena',
  [ValidateSet('public', 'private')][string]$Visibility = 'public'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$logFile = Join-Path $repoRoot 'push-log.txt'

function Log([string]$msg) {
  $line = "[{0}] {1}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $msg
  Write-Host $line
  Add-Content -Path $logFile -Value $line -Encoding utf8
}

# o gh pode não estar no PATH da sessão que a tarefa agendada cria
$gh = 'gh'
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
  $candidate = 'C:\Program Files\GitHub CLI\gh.exe'
  if (Test-Path $candidate) { $gh = $candidate }
  else { Log 'ERRO: gh (GitHub CLI) não encontrado. Instale com: winget install GitHub.cli'; exit 2 }
}

Set-Location $repoRoot
Log "=== push iniciado em $repoRoot ==="

# 1. autenticação — é aqui que para se você ainda não fez login
& $gh auth status 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
  Log 'PARADO: você ainda não autenticou no GitHub.'
  Log 'Rode uma vez:  gh auth login'
  Log 'Depois rode este script de novo (ou espere a próxima execução agendada).'
  exit 3
}

$user = (& $gh api user --jq .login 2>$null)
if (-not $user) { Log 'ERRO: autenticado, mas não consegui ler o usuário.'; exit 4 }
Log "autenticado como $user"

# 2. commit de qualquer coisa pendente
$dirty = git status --porcelain
if ($dirty) {
  Log 'há mudanças não commitadas; criando commit automático'
  git add -A
  git commit -q -m "Ajustes antes do push automático"
}

# 3. remote: cria o repositório se ainda não existir
$hasOrigin = (git remote) -contains 'origin'
if (-not $hasOrigin) {
  $exists = $false
  & $gh repo view "$user/$RepoName" 2>&1 | Out-Null
  if ($LASTEXITCODE -eq 0) { $exists = $true }

  if ($exists) {
    Log "repositório $user/$RepoName já existe; apenas ligando o remote"
    git remote add origin "https://github.com/$user/$RepoName.git"
  } else {
    Log "criando repositório $Visibility $user/$RepoName"
    & $gh repo create $RepoName --$Visibility --source=. --remote=origin --disable-wiki
    if ($LASTEXITCODE -ne 0) { Log 'ERRO ao criar o repositório'; exit 5 }
  }
}

# 4. push
Log 'enviando branch main'
git push -u origin main 2>&1 | ForEach-Object { Log "  git: $_" }
if ($LASTEXITCODE -ne 0) { Log 'ERRO no push'; exit 6 }

$url = "https://github.com/$user/$RepoName"
Log "PRONTO: $url"

# 5. GitHub Pages para jogar direto pelo navegador (só se for público)
if ($Visibility -eq 'public') {
  & $gh api -X POST "repos/$user/$RepoName/pages" -f "source[branch]=main" -f "source[path]=/" 2>&1 | Out-Null
  if ($LASTEXITCODE -eq 0) {
    Log "Pages ligado: https://$user.github.io/$RepoName/dist/neon-arena.html"
  } else {
    Log 'Pages não foi ligado automaticamente (dá para ligar em Settings > Pages).'
  }
}

exit 0

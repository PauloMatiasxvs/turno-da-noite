<#
  Sobe o repositorio para o GitHub.

  Uso:  powershell -ExecutionPolicy Bypass -File tools\push-github.ps1

  Pre-requisito que so voce pode fazer: autenticar uma vez com
      gh auth login
  O script nao guarda senha nem token; quem cuida disso e o proprio gh.
#>
param(
  [string]$RepoName = 'turno-da-noite',
  [ValidateSet('public', 'private')][string]$Visibility = 'public'
)

# 'Continue' de proposito. Com 'Stop', o stderr de um executavel nativo vira
# erro fatal do PowerShell -- e o gh escreve no stderr so para dizer que o
# repositorio ainda nao existe, que e justamente a resposta esperada aqui.
# O controle e feito por $LASTEXITCODE, que e como o gh de fato responde.
$ErrorActionPreference = 'Continue'

$repoRoot = Split-Path -Parent $PSScriptRoot
$logFile = Join-Path $repoRoot 'push-log.txt'

function Log([string]$msg) {
  $line = "[{0}] {1}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $msg
  Write-Host $line
  Add-Content -Path $logFile -Value $line -Encoding utf8
}

# o gh pode nao estar no PATH da sessao que a tarefa agendada cria
$gh = 'gh'
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
  $candidato = 'C:\Program Files\GitHub CLI\gh.exe'
  if (Test-Path $candidato) { $gh = $candidato }
  else { Log 'ERRO: gh (GitHub CLI) nao encontrado. Instale com: winget install GitHub.cli'; exit 2 }
}

Set-Location $repoRoot
Log "=== push iniciado em $repoRoot ==="

# 1. autenticacao
& $gh auth status 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) {
  Log 'PARADO: voce ainda nao autenticou no GitHub.'
  Log 'Rode uma vez:  gh auth login'
  exit 3
}

$user = (& $gh api user --jq .login 2>$null)
if (-not $user) { Log 'ERRO: autenticado, mas nao consegui ler o usuario.'; exit 4 }
Log "autenticado como $user"

# 2. commit do que estiver pendente
$sujo = git status --porcelain
if ($sujo) {
  Log 'ha mudancas nao commitadas; criando commit automatico'
  git add -A
  git commit -q -m "Ajustes antes do push automatico"
}

# 3. remote: cria o repositorio se ainda nao existir
$temOrigin = (git remote) -contains 'origin'
if (-not $temOrigin) {
  & $gh repo view "$user/$RepoName" 2>$null | Out-Null
  $existe = ($LASTEXITCODE -eq 0)

  if ($existe) {
    Log "repositorio $user/$RepoName ja existe; apenas ligando o remote"
    git remote add origin "https://github.com/$user/$RepoName.git"
  } else {
    Log "criando repositorio $Visibility $user/$RepoName"
    if ($Visibility -eq 'public') {
      & $gh repo create $RepoName --public --source=. --remote=origin --disable-wiki
    } else {
      & $gh repo create $RepoName --private --source=. --remote=origin --disable-wiki
    }
    if ($LASTEXITCODE -ne 0) { Log 'ERRO ao criar o repositorio'; exit 5 }
  }
}

# 4. push
Log 'enviando branch main'
git push -u origin main
if ($LASTEXITCODE -ne 0) { Log 'ERRO no push'; exit 6 }

$url = "https://github.com/$user/$RepoName"
Log "PRONTO: $url"

# 5. GitHub Pages, para jogar direto pelo navegador (so se for publico)
if ($Visibility -eq 'public') {
  & $gh api -X POST "repos/$user/$RepoName/pages" -f "source[branch]=main" -f "source[path]=/" 2>$null | Out-Null
  if ($LASTEXITCODE -eq 0) {
    Log "Pages ligado: https://$user.github.io/$RepoName/horror/turno-da-noite.html"
  } else {
    Log 'Pages nao foi ligado automaticamente (da para ligar em Settings > Pages).'
  }
}

exit 0

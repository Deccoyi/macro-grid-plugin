<#
.SYNOPSIS
  Signs the official catalog files from this machine: the plugin list and the safety (revoke) list.

.DESCRIPTION
  Writes website/public/catalog/index.signed.json (from macrogrid-index.json) and revoked.signed.json (from revoked.json), each
  with a sequence one higher than the file already there, signed with the plugin-signing key (scripts/sign-catalog.cs). The key
  never leaves the maintainer's machine. Use it to switch a plugin version off (add it to revoked.json first), to withdraw a
  version (set "withdrawn": true on it in macrogrid-index.json), or to renew the files.

  Without -Push nothing leaves this machine: the files are written and checked, and you commit them yourself. With -Push the
  script commits them to main and pushes, like release-plugin.ps1 (clean, up-to-date main required).

.EXAMPLE
  ./scripts/publish-catalog.ps1              # sign both files, check them
  ./scripts/publish-catalog.ps1 -Only revoked -Push
#>
param(
    [ValidateSet('both', 'index', 'revoked')] [string]$Only = 'both',
    [string]$KeyPath = (Join-Path $env:USERPROFILE 'signing\plugin-signing\plugin-signing-private.pem'),
    [string]$OutDir = '',
    [switch]$Push
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot
if (-not $OutDir) { $OutDir = Join-Path $repoRoot 'website\public\catalog' }
if (-not (Test-Path $KeyPath)) { throw "Signing key not found at $KeyPath" }

if ($Push) {
    git fetch origin main
    if ((git rev-parse --abbrev-ref HEAD) -ne 'main') { throw 'Check out main first.' }
    if (git status --porcelain) { throw 'The working tree is not clean.' }
    if ((git rev-parse HEAD) -ne (git rev-parse origin/main)) { throw 'main is not up to date with origin/main.' }
}

New-Item -ItemType Directory -Force $OutDir | Out-Null
$tool = Join-Path $PSScriptRoot 'sign-catalog.cs'
$files = @()
if ($Only -in 'both', 'index') { $files += @{ kind = 'index'; source = 'macrogrid-index.json'; out = 'index.signed.json' } }
if ($Only -in 'both', 'revoked') { $files += @{ kind = 'revoked'; source = 'revoked.json'; out = 'revoked.signed.json' } }

foreach ($file in $files) {
    $out = Join-Path $OutDir $file.out
    dotnet run $tool -- sign $file.kind (Join-Path $repoRoot $file.source) $out $KeyPath
    if ($LASTEXITCODE -ne 0) { throw "Signing $($file.kind) failed" }
    dotnet run $tool -- verify $out $file.kind
    if ($LASTEXITCODE -ne 0) { throw "Check of $($file.out) failed" }
}

if ($Push) {
    git add -- $OutDir
    git commit -m "chore(catalog): sign the official catalog files"
    git push origin main
    Write-Host 'Signed files pushed. The Pages site updates after its next deploy.'
} else {
    Write-Host "Signed files are in $OutDir. Commit them to main when you are ready."
}

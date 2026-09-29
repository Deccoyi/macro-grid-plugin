<#
.SYNOPSIS
  Tests scripts/sign-package-contents.cs with a throwaway key (never the real plugin key).

.DESCRIPTION
  Builds a small package folder, signs it and checks signature.json: every file listed except the two signature files,
  paths with '/', sorted, right hashes, no byte order mark, id/version/kind from plugin.json; then checks that the
  signature verifies and that a changed file or a changed signature.json is caught.
#>
$ErrorActionPreference = 'Stop'
$tool = Join-Path (Split-Path -Parent $PSScriptRoot) 'sign-package-contents.cs'
$work = Join-Path ([System.IO.Path]::GetTempPath()) ("macrogrid-signature-test-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $work | Out-Null
$failures = @()
function Assert($ok, $what) { if ($ok) { Write-Host "ok   $what" } else { Write-Host "FAIL $what"; $script:failures += $what } }
function Run { dotnet run $tool -- @args | Out-Null; return $LASTEXITCODE }

try {
    $keys = Join-Path $work 'keys'
    Assert ((Run test-keygen $keys) -eq 0) 'throwaway key pair is created'

    $pkg = Join-Path $work 'pkg'
    New-Item -ItemType Directory -Force (Join-Path $pkg 'sub') | Out-Null
    [System.IO.File]::WriteAllText((Join-Path $pkg 'plugin.json'), '{ "id": "demo", "version": "1.2.3", "kind": "csharp", "entry": "Demo.dll" }')
    [System.IO.File]::WriteAllBytes((Join-Path $pkg 'Demo.dll'), [byte[]](1..200))
    [System.IO.File]::WriteAllText((Join-Path $pkg 'sub\b.txt'), 'b')
    [System.IO.File]::WriteAllText((Join-Path $pkg 'A.txt'), 'a')
    [System.IO.File]::WriteAllText((Join-Path $pkg 'signature.json'), 'stale')   # a leftover is replaced, never listed

    Assert ((Run sign $pkg (Join-Path $keys 'test-private.pem')) -eq 0) 'sign succeeds'

    $jsonPath = Join-Path $pkg 'signature.json'
    $bytes = [System.IO.File]::ReadAllBytes($jsonPath)
    Assert (-not ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)) 'signature.json has no byte order mark'
    $sig = [System.Text.Encoding]::UTF8.GetString($bytes) | ConvertFrom-Json
    Assert ($sig.formatVersion -eq 1 -and $sig.id -eq 'demo' -and $sig.version -eq '1.2.3' -and $sig.kind -eq 'csharp') 'header fields come from plugin.json'

    $paths = @($sig.files | ForEach-Object { $_.path })
    Assert (($paths -join ',') -eq 'A.txt,Demo.dll,plugin.json,sub/b.txt') 'every file is listed, sorted, with forward slashes, without the signature files'
    $hashesOk = $true
    foreach ($f in $sig.files) {
        $actual = (Get-FileHash (Join-Path $pkg $f.path) -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actual -ne $f.sha256) { $hashesOk = $false }
    }
    Assert $hashesOk 'listed hashes match the files'

    $pub = Join-Path $keys 'test-public.pem'
    Assert ((Run verify $pkg $pub) -eq 0) 'a signed package verifies'

    [System.IO.File]::WriteAllText((Join-Path $pkg 'A.txt'), 'tampered')
    Assert ((Run verify $pkg $pub) -ne 0) 'a changed file is caught'
    [System.IO.File]::WriteAllText((Join-Path $pkg 'A.txt'), 'a')
    Assert ((Run verify $pkg $pub) -eq 0) 'the restored file verifies again'

    [System.IO.File]::WriteAllText($jsonPath, [System.IO.File]::ReadAllText($jsonPath).Replace('demo', 'evil'))
    Assert ((Run verify $pkg $pub) -ne 0) 'a changed signature.json is caught'
} finally {
    Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
}

if ($failures.Count) { throw "$($failures.Count) check(s) failed" }
Write-Host 'All checks passed.'

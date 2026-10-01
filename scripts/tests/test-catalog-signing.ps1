<#
.SYNOPSIS
  Tests scripts/sign-catalog.cs with a throwaway key (never the real plugin key).

.DESCRIPTION
  Signs a small plugin list and safety list and checks: the header fields, the rising sequence, that a signature verifies with
  the right key and fails with another key or after the payload changed, and that a file of the wrong kind is refused.
#>
$ErrorActionPreference = 'Stop'
$tool = Join-Path (Split-Path -Parent $PSScriptRoot) 'sign-catalog.cs'
$work = Join-Path ([System.IO.Path]::GetTempPath()) ("macrogrid-catalog-test-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $work | Out-Null
$failures = @()
function Assert($ok, $what) { if ($ok) { Write-Host "ok   $what" } else { Write-Host "FAIL $what"; $script:failures += $what } }
function Run { dotnet run $tool -- @args | Out-Null; return $LASTEXITCODE }

try {
    $keys = Join-Path $work 'keys'
    $others = Join-Path $work 'others'
    Assert ((Run test-keygen $keys) -eq 0) 'throwaway key pair is created'
    Assert ((Run test-keygen $others) -eq 0) 'a second key pair is created'
    $private = Join-Path $keys 'test-private.pem'
    $public = Join-Path $keys 'test-public.pem'

    $source = Join-Path $work 'revoked.json'
    [System.IO.File]::WriteAllText($source, '{ "formatVersion": 1, "plugins": [ { "id": "demo", "versions": ["1.0.0"], "reason": "Unsafe." } ] }')
    $out = Join-Path $work 'revoked.signed.json'

    Assert ((Run sign revoked $source $out $private) -eq 0) 'sign succeeds'
    $envelope = Get-Content $out -Raw | ConvertFrom-Json
    $payload = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($envelope.payload)) | ConvertFrom-Json
    Assert ($payload.kind -eq 'revoked' -and $payload.sequence -eq 1 -and $payload.issuedAt -and $payload.plugins[0].id -eq 'demo') 'the header is added and the content kept'

    Assert ((Run sign revoked $source $out $private) -eq 0) 'signing again succeeds'
    $payload = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String((Get-Content $out -Raw | ConvertFrom-Json).payload)) | ConvertFrom-Json
    Assert ($payload.sequence -eq 2) 'the sequence rises by one'

    Assert ((Run verify $out revoked) -eq 0) 'the structure check passes'
    Assert ((Run verify $out revoked $public) -eq 0) 'the signature verifies with the right key'
    Assert ((Run verify $out revoked (Join-Path $others 'test-public.pem')) -ne 0) 'another key does not verify'
    Assert ((Run verify $out index) -ne 0) 'a file of the wrong kind is refused'
    Assert ((Run sign index $source $out $private) -ne 0) 'signing over a file of another kind is refused'

    $envelope = Get-Content $out -Raw | ConvertFrom-Json
    $tampered = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($envelope.payload)).Replace('Unsafe.', 'Fine..')
    $envelope.payload = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($tampered))
    $tamperedPath = Join-Path $work 'tampered.json'
    [System.IO.File]::WriteAllText($tamperedPath, ($envelope | ConvertTo-Json -Compress))
    Assert ((Run verify $tamperedPath revoked $public) -ne 0) 'a changed payload does not verify'
}
finally {
    Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
}

if ($failures.Count) { Write-Host "$($failures.Count) check(s) failed"; exit 1 }
Write-Host 'All checks passed'

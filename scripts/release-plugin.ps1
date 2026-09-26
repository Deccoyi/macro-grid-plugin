<#
.SYNOPSIS
  Builds, signs and publishes one plugin release from this machine.

.DESCRIPTION
  Replaces the old tag-triggered workflow: the plugin-signing private key never leaves the maintainer's machine,
  so a compromised GitHub account cannot produce a signed plugin. Run it from a clean, up-to-date `main`.

  Steps: read the plugin's plugin.json, build and zip it together with the licence texts, hash and sign the zip
  (scripts/sign-package.cs) and write its software bill of materials, then, only with -Publish, create the GitHub
  release (tag plugin-<name>-v<version>) with the zip, .sha256, .sig and SBOM attached, and commit the new version into
  macrogrid-index.json on main. A C# plugin with a known vulnerable package is refused before anything is built.

  Without -Publish nothing leaves this machine: the signed zip is left in the output folder for inspection.

.EXAMPLE
  ./scripts/release-plugin.ps1 -Name hellojs            # build + sign only
  ./scripts/release-plugin.ps1 -Name hellojs -Publish   # also release and update the index
#>
param(
    [Parameter(Mandatory)] [ValidateSet('obs', 'plc-icons', 'hellojs', 'soundboard')] [string]$Name,
    [string]$KeyPath = (Join-Path $env:USERPROFILE 'signing\plugin-signing\plugin-signing-private.pem'),
    [string]$OutDir = (Join-Path ([System.IO.Path]::GetTempPath()) 'macrogrid-plugin-release'),
    [switch]$Publish
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$plugins = @{
    'obs'       = @{ dir = 'OBS';      proj = 'OBS/src/MacroGrid.Plugin.Obs.csproj' }
    'plc-icons' = @{ dir = 'PLCIcons'; proj = 'PLCIcons/src/MacroGrid.Plugin.PlcIcons.csproj' }
    'hellojs'   = @{ dir = 'HelloJs';  proj = '' }
    'soundboard' = @{ dir = 'SoundBoard'; proj = 'SoundBoard/src/MacroGrid.Plugin.SoundBoard.csproj' }
}
$entry = $plugins[$Name]
$owner = 'Deccoyi'
$repo = 'macro-grid-plugin'

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    $ghDir = 'C:\Program Files\GitHub CLI'
    if (Test-Path (Join-Path $ghDir 'gh.exe')) { $env:PATH += ";$ghDir" } else { throw 'The GitHub CLI (gh) is not installed.' }
}
if (-not (Test-Path $KeyPath)) { throw "Signing key not found at $KeyPath" }

# A release is cut from main, from exactly what is on the remote.
if ($Publish) {
    git fetch origin main --tags
    if ((git rev-parse --abbrev-ref HEAD) -ne 'main') { throw 'Check out main first.' }
    if (git status --porcelain) { throw 'The working tree is not clean.' }
    if ((git rev-parse HEAD) -ne (git rev-parse origin/main)) { throw 'main is not up to date with origin/main.' }
}

$manifest = Get-Content (Join-Path $entry.dir 'plugin.json') -Raw | ConvertFrom-Json
$version = $manifest.version
if ($manifest.macroGrid -notmatch '^\d+\.\d+\.\d+$') { throw "plugin.json needs `"macroGrid`" as MAJOR.MINOR.PATCH (the oldest Macro Grid it runs on), found '$($manifest.macroGrid)'." }
$tag = "plugin-$Name-v$version"
$zipName = "$($manifest.id)-$version.zip"
if ($Publish -and (git tag --list $tag)) { throw "Tag $tag already exists; bump version in plugin.json first." }

Write-Host "Releasing $($manifest.name) $version ($tag)"

# A release never ships a package with a known vulnerability.
if ($entry.proj) {
    dotnet restore $entry.proj --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw 'restore failed' }
    $report = dotnet list $entry.proj package --vulnerable --include-transitive 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { Write-Host $report; throw 'dotnet list package failed' }
    if ($report -match 'has the following vulnerable packages') { Write-Host $report; throw 'A package of this plugin has a known vulnerability; update it first.' }
}

# Build and package.
if (Test-Path $OutDir) { Remove-Item $OutDir -Recurse -Force }
$stage = Join-Path $OutDir 'stage'
New-Item -ItemType Directory -Force $stage | Out-Null
if ($entry.proj) {
    dotnet build $entry.proj -c Release -o $stage
    if ($LASTEXITCODE -ne 0) { throw 'build failed' }
    Get-ChildItem $stage -Filter *.pdb -Recurse | Remove-Item -Force
} else {
    # JavaScript plugin: the folder is the plugin.
    Copy-Item (Join-Path $entry.dir '*') $stage -Recurse -Force
}
# Every archive carries the licence texts.
Copy-Item 'LICENSE' (Join-Path $stage 'LICENSE') -Force
Copy-Item (Join-Path $entry.dir 'NOTICE.md') (Join-Path $stage 'NOTICE.md') -Force
Copy-Item 'THIRD_PARTY_NOTICES.md' $stage -Force

$zip = Join-Path $OutDir $zipName
# Entries use forward slashes; Windows PowerShell 5.1's CreateFromDirectory can write backslashes.
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$stageRoot = (Resolve-Path $stage).Path.TrimEnd('\') + '\'
$archive = [System.IO.Compression.ZipFile]::Open($zip, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in Get-ChildItem $stage -Recurse -File) {
        $entryName = $file.FullName.Substring($stageRoot.Length).Replace('\', '/')
        [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $entryName, [System.IO.Compression.CompressionLevel]::Optimal)
    }
} finally {
    $archive.Dispose()
}

# Hash and sign (sign-package.cs prints sha256=, size= and signature= lines).
$result = @{}
dotnet run (Join-Path $PSScriptRoot 'sign-package.cs') -- $zip $KeyPath | ForEach-Object {
    if ($_ -match '^(sha256|size|signature)=(.+)$') { $result[$Matches[1]] = $Matches[2] }
}
if (-not $result.signature) { throw 'signing failed' }
Write-Host "Signed $zipName ($($result.size) bytes, sha256 $($result.sha256))"

# Software bill of materials (CycloneDX): the packages the zip ships. The plugin SDK is left out, because a plugin is
# only built against it and the server supplies its own copy at run time. A JavaScript plugin has no packages.
$sbom = $null
if ($entry.proj) {
    $tools = Join-Path $OutDir 'tools'
    dotnet tool install CycloneDX --version 6.2.0 --tool-path $tools --add-source https://api.nuget.org/v3/index.json | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Installing the SBOM generator failed' }
    $sbomName = "$($manifest.id)-$version.cdx.json"
    & (Join-Path $tools 'dotnet-CycloneDX.exe') $entry.proj --exclude-dev --exclude-test-projects --output-format Json `
        --set-name $manifest.name --set-version $version --exclude-filter MacroGrid.Plugin.Abstractions `
        --output $OutDir --filename $sbomName
    if ($LASTEXITCODE -ne 0) { throw 'SBOM failed' }
    $sbom = Join-Path $OutDir $sbomName
    Write-Host "SBOM: $sbomName"
}

if (-not $Publish) {
    Write-Host "Dry run: nothing was published. Files are in $OutDir"
    return
}

# Publish the GitHub release; the tag is created on main's current commit.
$notes = "See $($entry.dir)/CHANGELOG.md for what changed in this version."
$assets = @($zip, "$zip.sha256", "$zip.sig") + @($sbom | Where-Object { $_ })
$ghArgs = @('release', 'create', $tag) + $assets + @('--repo', "$owner/$repo",
    '--target', 'main', '--title', "$($entry.dir) $version", '--notes', $notes)
if ($version -match '-') { $ghArgs += '--prerelease' }
& gh @ghArgs
if ($LASTEXITCODE -ne 0) { throw 'gh release create failed' }

# Record the new version in the index and push it.
& (Join-Path $PSScriptRoot 'update-plugin-index.ps1') `
    -IndexPath (Join-Path $repoRoot 'macrogrid-index.json') `
    -Id $manifest.id `
    -Name $manifest.name `
    -Description $manifest.description `
    -Author $owner `
    -Homepage "https://github.com/$owner/$repo/tree/main/$($entry.dir)" `
    -Kind $manifest.kind `
    -Version $version `
    -MacroGrid $manifest.macroGrid `
    -SdkVersion "$($manifest.sdkVersion)" `
    -MinServerVersion "$($manifest.minServerVersion)" `
    -Url "https://github.com/$owner/$repo/releases/download/$tag/$zipName" `
    -Sha256 $result.sha256 `
    -Size ([long]$result.size) `
    -Signature $result.signature `
    -Permissions @($manifest.permissions | Where-Object { $_ })

git add macrogrid-index.json
git commit -m "chore(index): add $($manifest.id) $version to the plugin index"
git push origin main
Write-Host "Released $tag and updated macrogrid-index.json"

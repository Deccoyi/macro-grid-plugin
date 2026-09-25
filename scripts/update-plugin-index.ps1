<#
.SYNOPSIS
  Adds or updates one plugin's version entry in macrogrid-index.json at the repository root.

.DESCRIPTION
  Called by the release workflow after a plugin's GitHub Release is published. Reads the existing
  index (creating it if missing), replaces the entry for -Version under plugin -Id if one already
  exists (a re-release), otherwise appends it, then writes the file back with versions sorted newest
  first. Does not commit; the caller commits and pushes the result.

  See website/reference/source-index.md for the schema this file follows.
#>
param(
    [Parameter(Mandatory)] [string]$IndexPath,
    [Parameter(Mandatory)] [string]$Id,
    [Parameter(Mandatory)] [string]$Name,
    [string]$Description = '',
    [Parameter(Mandatory)] [string]$Author,
    [Parameter(Mandatory)] [string]$Homepage,
    [Parameter(Mandatory)] [ValidateSet('csharp', 'js')] [string]$Kind,
    [Parameter(Mandatory)] [string]$Version,
    [Parameter(Mandatory)] [ValidatePattern('^\d+\.\d+\.\d+$')] [string]$MacroGrid,
    # Legacy pair, copied from plugin.json when it still has them: servers before 1.0.0 need both.
    [string]$SdkVersion = '',
    [string]$MinServerVersion = '',
    [Parameter(Mandatory)] [string]$Url,
    [Parameter(Mandatory)] [string]$Sha256,
    [Parameter(Mandatory)] [long]$Size,
    [string]$Signature = '',
    [string[]]$Permissions = @()
)

$ErrorActionPreference = 'Stop'

# Parses "major.minor.patch(-prerelease)?" into a single string that string-sorts correctly:
# a release outranks any prerelease of the same major.minor.patch, and numbers are zero-padded
# so they compare the same as numerically.
function Get-SortKey([string]$v) {
    if ($v -match '^(\d+)\.(\d+)\.(\d+)(?:-(.+))?$') {
        $pre = $Matches[4]
        # No prerelease tag sorts after any prerelease tag (1.0.0 > 1.0.0-beta).
        $preKey = if ($pre) { $pre } else { [string][char]0xFFFF }
        return '{0:D10}.{1:D10}.{2:D10}.{3}' -f [int]$Matches[1], [int]$Matches[2], [int]$Matches[3], $preKey
    }
    return "0000000000.0000000000.0000000000.$v"
}

if (Test-Path $IndexPath) {
    $index = Get-Content $IndexPath -Raw | ConvertFrom-Json
} else {
    $index = [PSCustomObject]@{
        # Stays 1 on purpose: a server before 1.0.0 refuses any other format. The 1.0.0 server reads "macroGrid" from a format 1 entry.
        formatVersion = 1
        name          = 'Macro Grid Plugins'
        author        = $Author
        plugins       = @()
    }
}
# ConvertFrom-Json gives arrays as arrays already, but a single-element JSON array can come back
# as a scalar; normalize so indexing and Where-Object always work.
$plugins = @($index.plugins)

$entry = $plugins | Where-Object { $_.id -eq $Id } | Select-Object -First 1
if (-not $entry) {
    $entry = [PSCustomObject]@{
        id          = $Id
        name        = $Name
        description = $Description
        author      = $Author
        homepage    = $Homepage
        kind        = $Kind
        versions    = @()
    }
    $plugins += $entry
} else {
    $entry.name = $Name
    $entry.description = $Description
    $entry.author = $Author
    $entry.homepage = $Homepage
    $entry.kind = $Kind
}

$versions = @($entry.versions | Where-Object { $_.version -ne $Version })
$fields = [ordered]@{ version = $Version; macroGrid = $MacroGrid }
if ($SdkVersion) { $fields.sdkVersion = $SdkVersion }
if ($MinServerVersion) { $fields.minServerVersion = $MinServerVersion }
$fields.url = $Url
$fields.sha256 = $Sha256
$fields.size = $Size
$fields.permissions = @($Permissions)
$newVersion = [PSCustomObject]$fields
if ($Signature) {
    $newVersion | Add-Member -NotePropertyName signature -NotePropertyValue $Signature
}
$versions += $newVersion
$entry.versions = @($versions | Sort-Object -Property { Get-SortKey $_.version } -Descending)

$index.plugins = @($plugins)

$json = $index | ConvertTo-Json -Depth 10
[System.IO.File]::WriteAllText($IndexPath, $json + "`n", [System.Text.UTF8Encoding]::new($false))

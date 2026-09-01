[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $ArtifactPath,

    [Parameter(Mandatory)]
    [string] $SitePath,

    [Parameter(Mandatory)]
    [string] $AppPool,

    [Parameter(Mandatory)]
    [string] $BackupRoot
)

$ErrorActionPreference = 'Stop'
Import-Module WebAdministration

$source = (Resolve-Path -LiteralPath $ArtifactPath).Path
$target = [System.IO.Path]::GetFullPath($SitePath)
$backupBase = [System.IO.Path]::GetFullPath($BackupRoot)

if ([System.IO.Path]::GetPathRoot($target) -eq $target) {
    throw "IIS SitePath ne smije biti korijen diska: $target"
}

if (-not (Test-Path -LiteralPath (Join-Path $source '_shell.html'))) {
    throw "Artifact nije validan: nedostaje _shell.html u $source"
}

$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backup = Join-Path $backupBase $timestamp
New-Item -ItemType Directory -Path $backup -Force | Out-Null
New-Item -ItemType Directory -Path $target -Force | Out-Null

try {
    if (Test-Path -LiteralPath $target) {
        & robocopy $target $backup /MIR /R:2 /W:2
        if ($LASTEXITCODE -ge 8) { throw "Backup nije uspio (robocopy exit $LASTEXITCODE)." }
    }

    Stop-WebAppPool -Name $AppPool
    & robocopy $source $target /MIR /R:3 /W:3
    if ($LASTEXITCODE -ge 8) { throw "IIS deploy nije uspio (robocopy exit $LASTEXITCODE)." }
    Start-WebAppPool -Name $AppPool
}
catch {
    if (Test-Path -LiteralPath $backup) {
        & robocopy $backup $target /MIR /R:2 /W:2 | Out-Null
    }
    Start-WebAppPool -Name $AppPool -ErrorAction SilentlyContinue
    throw
}

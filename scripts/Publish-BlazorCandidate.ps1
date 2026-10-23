[CmdletBinding()]
param(
    [ValidatePattern('^[a-zA-Z0-9][a-zA-Z0-9._-]{0,63}$')][string] $Name = 'blazor-candidate',
    [string] $NuGetConfig,
    [string] $BuildArtifactsPath = 'artifacts/blazor-deployment',
    [switch] $NoRestore
)
$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
$candidateRoot = Join-Path $repository "artifacts/blazor-candidates/$Name"
if (Test-Path -LiteralPath $candidateRoot) { throw 'Candidate already exists; choose a new Name. Existing artifacts are never overwritten.' }
$buildPath = [IO.Path]::GetFullPath((Join-Path $repository $BuildArtifactsPath))
$allowedRoot = [IO.Path]::GetFullPath((Join-Path $repository 'artifacts')) + [IO.Path]::DirectorySeparatorChar
if (!$buildPath.StartsWith($allowedRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Build output must remain under repository artifacts.' }
$project = Join-Path $repository 'src/Web.Blazor/RBBH.RelatedPartiesRegister.Web.csproj'
if (!$NoRestore) {
    if (!$NuGetConfig -or !(Test-Path -LiteralPath $NuGetConfig)) { throw 'Supply an approved NuGetConfig or use NoRestore after an isolated restore.' }
    & dotnet restore $project --artifacts-path $buildPath --configfile $NuGetConfig --packages (Join-Path $repository 'artifacts/blazor-deployment-packages')
    if ($LASTEXITCODE -ne 0) { throw 'Native restore failed.' }
}
$app = Join-Path $candidateRoot 'app'
New-Item -ItemType Directory -Path $app | Out-Null
& dotnet publish $project -c Release --no-restore --artifacts-path $buildPath --output $app -p:UseAppHost=false
if ($LASTEXITCODE -ne 0) { throw 'Native publish failed; incomplete candidate retained for diagnosis. Choose a new Name next time.' }
$files = Get-ChildItem -LiteralPath $app -File -Recurse -Force
if ($files | Where-Object { $_.Name -match '^\.env($|\.)|^secrets\.json$|\.(pfx|p12|key|user|suo)$' }) { throw 'Publish contains a prohibited local/secret file. Candidate is not releasable.' }
if (!(Test-Path -LiteralPath (Join-Path $app 'RBBH.RelatedPartiesRegister.Web.dll'))) { throw 'Native entry assembly missing.' }
if (Test-Path -LiteralPath (Join-Path $app '_shell.html')) { throw 'Legacy static/IIS artifact detected.' }
Push-Location $repository
try {
    $sourceCommit = (& git rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Cannot identify candidate source commit.' }
    $trackedDirty = @(& git status --porcelain --untracked-files=no).Count -gt 0
} finally { Pop-Location }
$inventory = @($files | Sort-Object FullName | ForEach-Object {
    [ordered]@{ path = [IO.Path]::GetRelativePath($app, $_.FullName).Replace('\','/'); bytes = $_.Length; sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
})
[ordered]@{ kind = 'NATIVE_BLAZOR_CANDIDATE_NOT_CUTOVER'; sourceCommit = $sourceCommit; trackedSourceDirty = $trackedDirty; entryAssembly = 'RBBH.RelatedPartiesRegister.Web.dll'; files = $inventory } |
    ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $candidateRoot 'manifest.json') -Encoding utf8
$archive = Join-Path $candidateRoot 'app.zip'
[IO.Compression.ZipFile]::CreateFromDirectory($app, $archive)
Write-Output "Native candidate created: $candidateRoot"
Write-Output "ZIP SHA256: $((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash)"
Write-Output 'No registry push, IIS copy, OCP change, secret injection or production cutover was performed.'

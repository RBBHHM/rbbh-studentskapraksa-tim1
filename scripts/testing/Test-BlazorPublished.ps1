[CmdletBinding()]
param([Parameter(Mandatory)][string] $PublishPath)
$ErrorActionPreference = 'Stop'
$app = (Resolve-Path -LiteralPath $PublishPath).Path
if (!(Test-Path -LiteralPath (Join-Path $app 'RBBH.RelatedPartiesRegister.Web.dll'))) { throw 'Not a published native frontend.' }
$listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
$listener.Start(); $port = $listener.LocalEndpoint.Port; $listener.Stop()
$info = [Diagnostics.ProcessStartInfo]::new((Get-Command dotnet).Source)
$info.WorkingDirectory = $app; $info.UseShellExecute = $false; $info.CreateNoWindow = $true
$info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
foreach ($argument in @('RBBH.RelatedPartiesRegister.Web.dll','--environment','Development','--urls',"http://127.0.0.1:$port",'--Authentication:Authority=','--Authentication:ClientId=','--Authentication:ClientSecret=','--Apis:BaseUrl=')) { $info.ArgumentList.Add($argument) }
$ownedProcess = [Diagnostics.Process]::Start($info)
$stdout = $ownedProcess.StandardOutput.ReadToEndAsync(); $stderr = $ownedProcess.StandardError.ReadToEndAsync()
$handler = [Net.Http.HttpClientHandler]::new(); $handler.UseProxy = $false
$client = [Net.Http.HttpClient]::new($handler); $client.BaseAddress = [Uri]::new("http://127.0.0.1:$port"); $client.Timeout = [TimeSpan]::FromSeconds(2)
try {
    $ready = $false
    for ($attempt = 0; $attempt -lt 20; $attempt++) {
        if ($ownedProcess.HasExited) { throw 'Published frontend exited before readiness.' }
        try { $response = $client.GetAsync('/app').GetAwaiter().GetResult(); $ready = $response.IsSuccessStatusCode; $response.Dispose() } catch { }
        if ($ready) { break }
        Start-Sleep -Milliseconds 200
    }
    if (!$ready) { throw 'Published frontend failed readiness.' }
    $routes = @('/','/app','/app/physical-persons','/app/legal-persons','/app/limits','/app/capital','/app/reports','/app/notifications','/app/admin/users','/app/admin/period','/app/admin/code-lists','/app/admin/audit','/app/profile','/app/guide')
    foreach ($route in $routes) {
        $response = $client.GetAsync($route).GetAwaiter().GetResult()
        try {
            $html = [Net.WebUtility]::HtmlDecode($response.Content.ReadAsStringAsync().GetAwaiter().GetResult())
            if ([int]$response.StatusCode -ne 200 -or !$html.Contains('Autentifikacija nije') -or !$html.Contains('lang="bs"')) { throw "Route is not honestly signed out/BS: $route" }
        } finally { $response.Dispose() }
    }
    $response = $client.GetAsync('/_content/RBI.DesignSystem/rbi-design-system.css').GetAwaiter().GetResult()
    try { if ([int]$response.StatusCode -ne 200 -or $response.Content.Headers.ContentType.MediaType -ne 'text/css') { throw 'Published RBI CSS failed.' } } finally { $response.Dispose() }
    $response = $client.GetAsync('/authentication/login').GetAwaiter().GetResult()
    try { if ([int]$response.StatusCode -ne 404) { throw 'Unconfigured host exposed login.' } } finally { $response.Dispose() }
    Write-Output 'PASS:14 native routes200/signed-out/BS, RBI CSS200 and unconfigured login404.'
} finally {
    $client.Dispose()
    if (!$ownedProcess.HasExited) { $ownedProcess.Kill(); if (!$ownedProcess.WaitForExit(5000)) { throw 'Owned smoke process failed to stop.' } }
    $null = $stdout.GetAwaiter().GetResult(); $null = $stderr.GetAwaiter().GetResult(); $ownedProcess.Dispose()
    Write-Output 'Only the temporary owned smoke process was stopped; existing hosts were untouched.'
}

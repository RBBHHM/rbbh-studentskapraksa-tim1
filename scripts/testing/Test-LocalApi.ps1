param([string]$BaseUrl = "http://127.0.0.1:5000")
$ErrorActionPreference = "Stop"
$checks = @(
    @{ Name = "Liveness"; Path = "/health/live"; Contains = "Healthy" },
    @{ Name = "Readiness/database"; Path = "/health/ready"; Contains = "Healthy" },
    @{ Name = "Fizička lica"; Path = "/api/related-persons"; Contains = "Amina" },
    @{ Name = "Pravna lica"; Path = "/api/legal-entities"; Contains = "RBI Poslovni partner" },
    @{ Name = "Limiti"; Path = "/api/limiti"; Contains = "tipLimita" },
    @{ Name = "Šifrarnici"; Path = "/api/code-lists"; Contains = "Status" },
    @{ Name = "Korisnici"; Path = "/api/users"; Contains = "admin1" },
    @{ Name = "Audit"; Path = "/api/audit-logs"; Contains = "logs" }
)
$failed = 0
foreach ($check in $checks) {
    try {
        $response = Invoke-WebRequest -UseBasicParsing -Uri ($BaseUrl + $check.Path) -TimeoutSec 15
        if ($response.StatusCode -ne 200 -or $response.Content -notmatch [regex]::Escape($check.Contains)) { throw "Neočekivan odgovor ili nedostaje seed podatak." }
        Write-Host "[OK] $($check.Name)" -ForegroundColor Green
    } catch { $failed++; Write-Host "[FAIL] $($check.Name): $($_.Exception.Message)" -ForegroundColor Red }
}
if ($failed -gt 0) { throw "$failed API provjera nije prošlo." }

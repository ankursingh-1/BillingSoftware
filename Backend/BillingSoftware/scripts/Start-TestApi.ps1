# Builds the API into a temp folder and runs it on a side port against a COPY of the database.
# It never touches the API you run from Visual Studio, and never the real database:
# pass the name of a database you restored from a backup.
#
#   .\Start-TestApi.ps1 -Database BillingSoftware_Verify -Port 7190
#   .\Start-TestApi.ps1 -Stop
#
# JWT key and other secrets still come from User Secrets (Development environment).

param(
    [string]$Database = 'BillingSoftware_Verify',
    [int]$Port = 7190,
    [string]$SqlServer = 'localhost',
    [switch]$Stop
)

$ErrorActionPreference = 'Stop'

$work = Join-Path $env:TEMP 'billing-verify'
$pidFile = Join-Path $env:TEMP 'billing-api.pid'
$out = Join-Path $env:TEMP 'billing-api.out.log'
$err = Join-Path $env:TEMP 'billing-api.err.log'

if (Test-Path $pidFile) {
    $old = [int](Get-Content $pidFile)
    Get-Process -Id $old -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -eq 'dotnet' } | Stop-Process -Force
    Remove-Item $pidFile -Force
    Start-Sleep -Milliseconds 500
}

if ($Stop) { Write-Host 'Test API stopped.'; return }

if ($Database -notmatch '_(Verify|Test|Copy)$') {
    throw "Refusing to run against '$Database'. Use a restored copy whose name ends in _Verify, _Test or _Copy."
}

$project = Join-Path $PSScriptRoot '..\Billing.API\Billing.API.csproj'
dotnet build $project -c Release -o $work --nologo -v q | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = "http://localhost:$Port"
$env:ConnectionStrings__DefaultConnection =
    "Server=$SqlServer;Database=$Database;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"

$process = Start-Process dotnet -ArgumentList "`"$work\Billing.API.dll`"" -WorkingDirectory $work `
    -RedirectStandardOutput $out -RedirectStandardError $err -PassThru -WindowStyle Hidden
$process.Id | Set-Content $pidFile

for ($i = 0; $i -lt 30; $i++) {
    Start-Sleep -Milliseconds 500
    try {
        $r = Invoke-WebRequest "http://localhost:$Port/api/Health" -UseBasicParsing -ErrorAction Stop
        if ($r.StatusCode -eq 200) { Write-Host "Test API running on http://localhost:$Port (database $Database)"; return }
    } catch { }
}

Get-Content $err -Tail 20
throw 'The test API did not start.'

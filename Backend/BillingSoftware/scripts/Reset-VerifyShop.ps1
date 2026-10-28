# Clears transactional data on BillingSoftware_Verify only.
# Does nothing unless you pass the exact confirmation string.
#
#   .\Reset-VerifyShop.ps1 -IConfirmReset 'RESET BillingSoftware_Verify'
#
# Never points at BillingSoftwareDb. Does not seed demo data.

param(
    [Parameter(Mandatory = $true)]
    [string]$IConfirmReset,
    [string]$SqlServer = 'localhost',
    [string]$TargetDatabase = 'BillingSoftware_Verify'
)

$ErrorActionPreference = 'Stop'

if ($TargetDatabase -ne 'BillingSoftware_Verify') {
    throw "Refusing target '$TargetDatabase'. This script only accepts BillingSoftware_Verify."
}

if ($IConfirmReset -ne 'RESET BillingSoftware_Verify') {
    throw "Refusing to run. Pass -IConfirmReset 'RESET BillingSoftware_Verify' after you have approved the plan in Documentation/DEMO-RESET-PLAN.md."
}

$sqlFile = Join-Path $PSScriptRoot 'sql\Reset-VerifyShop.sql'
if (-not (Test-Path $sqlFile)) {
    throw "Missing $sqlFile"
}

sqlcmd -S $SqlServer -E -d $TargetDatabase -b -i $sqlFile
if ($LASTEXITCODE -ne 0) {
    throw "Reset failed (sqlcmd exit $LASTEXITCODE). Transaction should have rolled back."
}

Write-Host "Reset of $TargetDatabase finished. Seed demo data from Settings after enabling Demo:Enabled on the test API."

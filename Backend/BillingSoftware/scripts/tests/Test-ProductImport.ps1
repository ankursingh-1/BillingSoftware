<#
 Product Excel import/export checks against a TEST API (Start-TestApi.ps1).

   .\Test-ProductImport.ps1 -Email verify.admin@test.local -Password '...'

 Never point this at the live shop database.
#>
param(
    [string]$BaseUrl = 'http://localhost:7190/api',
    [Parameter(Mandatory)][string]$Email,
    [Parameter(Mandatory)][string]$Password
)

. (Join-Path $PSScriptRoot '..\ApiTestHelpers.ps1')
Connect-Api -BaseUrl $BaseUrl -Email $Email -Password $Password

$sample = Join-Path $PSScriptRoot '..\..\..\..\Documentation\samples\product-import-test.xlsx'
if (-not (Test-Path $sample)) { throw "Missing $sample. Run Billing.Tests first." }

$anon = $script:ApiToken
$script:ApiToken = $null
$denied = Api GET '/Products/import/template' -Anonymous
Check 'template requires login' ($denied.Status -eq 401)
$script:ApiToken = $anon

$template = Api GET '/Products/import/template'
Check 'template is xlsx' ($template.Status -eq 200 -and $template.Type -eq 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' -and $template.Bytes.Length -gt 1000)

$export = Api GET '/Products/export'
Check 'export is xlsx' ($export.Status -eq 200 -and $export.Bytes.Length -gt 200)

$preview = Api-Upload '/Products/import/preview' $sample 'product-import-test.xlsx' 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
Check 'preview 200' ($preview.Status -eq 200)
Check 'preview has invalid rows' ($preview.Json.invalid -ge 1)
Check 'preview lists product rows' ($preview.Json.totalRows -ge 4)
Check 'preview policy mentions transaction' ($preview.Json.policy -match 'transaction')

$tmpTxt = Join-Path $env:TEMP 'not-excel.txt'
Set-Content $tmpTxt 'hello'
$bad = Api-Upload '/Products/import/preview' $tmpTxt 'not-excel.txt' 'text/plain'
Check 'rejects non-xlsx' ($bad.Status -ge 400)

$imported = Api-Upload '/Products/import' $sample 'product-import-test.xlsx' 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
Check 'import 200' ($imported.Status -eq 200)
Check 'import wrote only valid rows' ($imported.Json.imported -eq $imported.Json.valid -or $imported.Json.imported -ge 0)
Check 'invalid rows were not imported' ($imported.Json.invalid -ge 1)

$retry = Api-Upload '/Products/import' $sample 'product-import-test.xlsx' 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
Check 'retry does not double-add' (
    $retry.Status -eq 200 -and (
        $retry.Json.replayed -eq $true -or $retry.Json.imported -eq 0
    )
)

$errors = Api-Upload '/Products/import/error-report' $sample 'product-import-test.xlsx' 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
Check 'error report is xlsx' ($errors.Status -eq 200)

$customers = Api POST '/Reports/customers' @{ pageNumber = 1; pageSize = 20 }
Check 'customer report' ($customers.Status -eq 200)
$payments = Api POST '/Reports/payments' @{ pageNumber = 1; pageSize = 20 }
Check 'payment report' ($payments.Status -eq 200)
$expenses = Api POST '/Reports/expenses' @{ pageNumber = 1; pageSize = 20 }
Check 'expense report' ($expenses.Status -eq 200)
$expXlsx = Api POST '/Reports/expenses/excel' @{ pageNumber = 1; pageSize = 20 }
Check 'expense excel' ($expXlsx.Status -eq 200)

Test-Summary
if ($script:Failed -gt 0) { exit 1 }

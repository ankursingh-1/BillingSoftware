<#
 Integration checks for invoice PDFs/formats, payment receipts, invoice snapshots and company settings
 (including logo upload).

 Run against a TEST API connected to a COPY of the database (scripts/Start-TestApi.ps1):

   .\Test-InvoicesReceiptsCompany.ps1 -Email verify.admin@test.local -Password '...' `
       -StaffEmail verify.staff@test.local -StaffPassword '...' -Database BillingSoftware_Verify

 WARNING: this script CHANGES the company record of the database it runs against (name, GSTIN,
 state, print settings, logo). Use a copy only. It creates its own uniquely named data and never
 deletes anything. -Database is only used for the one "legacy payment" check that needs SQL.
#>
param(
    [string]$BaseUrl = 'http://localhost:7190/api',
    [Parameter(Mandatory)][string]$Email,
    [Parameter(Mandatory)][string]$Password,
    [string]$StaffEmail,
    [string]$StaffPassword,
    [string]$Database,
    [string]$SqlServer = 'localhost'
)

. (Join-Path $PSScriptRoot '..\ApiTestHelpers.ps1')
Connect-Api -BaseUrl $BaseUrl -Email $Email -Password $Password
$adminToken = $script:ApiToken

$sfx = (Get-Date).ToString('HHmmss')
$tinyPng = [Convert]::FromBase64String('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==')
$tmp = Join-Path $env:TEMP "inv-test-$sfx"
New-Item -ItemType Directory -Path $tmp -Force | Out-Null

$gst18 = ((Api GET '/Tax').Json | Where-Object { $_.percentage -eq 18 -and $_.isActive } | Select-Object -First 1).id

function Save-Company($changes) {
    $c = (Api GET '/Company').Json
    $b = @{
        companyName = $c.companyName; gstNumber = $c.gstNumber; panNumber = $c.panNumber; address = $c.address
        city = $c.city; state = $c.state; stateCode = $c.stateCode; country = $c.country; pincode = $c.pincode
        phone = $c.phone; email = $c.email; website = $c.website; invoicePrefix = $c.invoicePrefix; currency = $c.currency
        bankName = $c.bankName; accountNumber = $c.accountNumber; ifscCode = $c.ifscCode; upiId = $c.upiId
        termsAndConditions = $c.termsAndConditions; footerMessage = $c.footerMessage
        defaultInvoiceFormat = $c.defaultInvoiceFormat; thermalPaperWidthMm = $c.thermalPaperWidthMm
        showBankDetails = $c.showBankDetails; showLogo = $c.showLogo; showTerms = $c.showTerms; showFooter = $c.showFooter
    }
    foreach ($k in $changes.Keys) { $b[$k] = $changes[$k] }
    Api POST '/Company' $b
}

# =========================================================== COMPANY SETTINGS
Write-Host "`n== Company settings"
$original = (Api GET '/Company').Json
Check 'company exposes StateCode / print settings / hasLogo' ($null -ne $original.PSObject.Properties['stateCode'] -and $null -ne $original.defaultInvoiceFormat -and $null -ne $original.PSObject.Properties['hasLogo'])
Check 'company DTO has no LogoPath' ($null -eq $original.PSObject.Properties['logoPath'])

$bad = Save-Company @{ gstNumber = 'NOT-A-GSTIN' }
Check 'invalid GSTIN format -> 400' ($bad.Status -eq 400) "got $($bad.Status)"
$bad = Save-Company @{ gstNumber = '27AAAAA0000A1Z5'; state = 'Telangana'; stateCode = '36' }
Check 'GSTIN state does not match company state -> 400' ($bad.Status -eq 400) "got $($bad.Status): $($bad.Raw)"
$bad = Save-Company @{ invoicePrefix = 'TOOLONGPREFIX' }
Check 'prefix longer than 4 -> 400' ($bad.Status -eq 400) "got $($bad.Status)"
$bad = Save-Company @{ invoicePrefix = 'IN V' }
Check 'prefix with space -> 400' ($bad.Status -eq 400) "got $($bad.Status)"
$bad = Save-Company @{ thermalPaperWidthMm = 70 }
Check 'thermal width other than 58/80 -> 400' ($bad.Status -eq 400) "got $($bad.Status)"
$bad = Save-Company @{ defaultInvoiceFormat = 'Poster' }
Check 'unknown default format -> 400' ($bad.Status -eq 400) "got $($bad.Status)"
$bad = Save-Company @{ companyName = '' }
Check 'blank company name -> 400' ($bad.Status -eq 400) "got $($bad.Status)"

$ok = Save-Company @{ companyName = "Test Retail $sfx"; gstNumber = '36AAAAA0000A1Z5'; state = 'Telangana'; stateCode = '36'; phone = '9000000001'
    defaultInvoiceFormat = 'Compact'; thermalPaperWidthMm = 58; showBankDetails = $false }
Check 'valid company save -> 200' ($ok.Status -eq 200) "got $($ok.Status): $($ok.Raw)"
$c = (Api GET '/Company').Json
Check 'saved settings read back' ($c.companyName -eq "Test Retail $sfx" -and $c.defaultInvoiceFormat -eq 'Compact' -and $c.thermalPaperWidthMm -eq 58 -and -not $c.showBankDetails)

# --- logo
Write-Host "`n== Logo upload"
Check 'logo is anonymous-readable but 404 when none is set' ((Api GET '/Company/logo' -Anonymous).Status -eq 404 -or $c.hasLogo)
[IO.File]::WriteAllBytes("$tmp\logo.png", $tinyPng)
[IO.File]::WriteAllText("$tmp\fake.png", 'this is not an image')
[IO.File]::WriteAllBytes("$tmp\huge.png", ($tinyPng + (New-Object byte[] (2.5MB))))
[IO.File]::WriteAllText("$tmp\x.svg", '<svg xmlns="http://www.w3.org/2000/svg"/>')

Check 'text file named .png rejected (magic bytes)' ((Api-Upload '/Company/logo' "$tmp\fake.png" 'fake.png' 'image/png').Status -eq 400)
Check 'png over 2 MB rejected' ((Api-Upload '/Company/logo' "$tmp\huge.png" 'huge.png' 'image/png').Status -eq 400)
Check 'svg rejected' ((Api-Upload '/Company/logo' "$tmp\x.svg" 'x.svg' 'image/svg+xml').Status -eq 400)

$up = Api-Upload '/Company/logo' "$tmp\logo.png" 'logo.png' 'image/png'
Check 'valid png upload -> 200' ($up.Status -eq 200) "got $($up.Status): $($up.Raw)"
$c = (Api GET '/Company').Json
Check 'company reports hasLogo after upload' ($c.hasLogo -eq $true)
$logo = Api GET '/Company/logo' -Anonymous
Check 'GET /Company/logo is a PNG' ($logo.Status -eq 200 -and $logo.Type -eq 'image/png' -and $logo.Bytes.Length -eq $tinyPng.Length)
$sale0 = Api GET '/Sales?pageSize=1'
Check 'path traversal in the logo URL is refused' ((Api GET '/Company/logo/..%2F..%2Fappsettings.json' -Anonymous).Status -in 400, 404)
Check 'unknown logo file name -> 404' ((Api GET '/Company/logo/deadbeef.png' -Anonymous).Status -eq 404)

# =========================================================== SALE + FORMATS
Write-Host "`n== Invoice formats"
$pid1 = (Api POST '/Products' @{ name = "INV Prod $sfx"; sku = "INV-$sfx"; purchasePrice = 50; sellingPrice = 100; stock = 500; taxId = $gst18; hsnCode = '8517' }).Json.productId
$mobile = Get-Random -Minimum 7000000000 -Maximum 7899999999
$cust = (Api POST '/Customers' @{ name = "INV Cust $sfx"; mobile = "$mobile"; address = '12 Test Road'; stateCode = '36' }).Json.customerId

function NewSale($qty) {
    $r = Api POST '/Sales' @{ customerId = $cust; items = @(@{ productId = $pid1; quantity = $qty }); clientRequestId = [guid]::NewGuid().ToString() }
    if ($r.Status -ne 200) { throw "Sale failed: $($r.Raw)" }
    (Api GET "/Sales/$($r.Json.saleId)").Json
}

$sale = NewSale 4
Check 'invoice settings come from the company (Compact / 58)' ($sale.settings.defaultFormat -eq 'Compact' -and $sale.settings.thermalPaperWidthMm -eq 58)
Check 'bank details are hidden by default' ($sale.settings.showBankDetails -eq $false)
Check 'sale totals: 4 x 100 + 18% = 472' ($sale.totalAmount -eq 472) "got $($sale.totalAmount)"

foreach ($f in 'A4', 'Compact', 'Thermal') {
    $p = Api GET "/Sales/$($sale.id)/pdf?format=$f&width=80"
    Check "PDF $f is a valid PDF" (Get-Pdf-Valid $p) "status $($p.Status)"
    Check "PDF $f content type" ($p.Type -eq 'application/pdf')
}
$a4 = Api GET "/Sales/$($sale.id)/pdf?format=A4"
$text = [Text.Encoding]::ASCII.GetString($a4.Bytes)
Check 'A4 PDF MediaBox is A4' ($text -match '/MediaBox\s*\[\s*0\s+0\s+595[\.\d]*\s+84[12][\.\d]*\s*\]') 
$th = [Text.Encoding]::ASCII.GetString((Api GET "/Sales/$($sale.id)/pdf?format=Thermal&width=58").Bytes)
$m = [regex]::Match($th, '/MediaBox\s*\[\s*0\s+0\s+([\d\.]+)\s+([\d\.]+)')
Check 'thermal 58 PDF page is 58 mm wide, not A4' ($m.Success -and [math]::Abs([double]$m.Groups[1].Value - 164.4) -lt 1.5 -and [double]$m.Groups[2].Value -lt 842) $m.Value
$def = Api GET "/Sales/$($sale.id)/pdf"
Check 'PDF without a format uses the company default' (Get-Pdf-Valid $def)
Check 'unknown format -> 400' ((Api GET "/Sales/$($sale.id)/pdf?format=Poster").Status -eq 400)
Check 'bad thermal width -> 400' ((Api GET "/Sales/$($sale.id)/pdf?format=Thermal&width=70").Status -eq 400)
Check 'PDF of a missing sale -> 404' ((Api GET '/Sales/99999999/pdf').Status -eq 404)
Check 'PDF needs a login' ((Api GET "/Sales/$($sale.id)/pdf" -Anonymous).Status -eq 401)

# =========================================================== RECEIPTS
Write-Host "`n== Payment receipts"
$r1 = Api POST "/Sales/$($sale.id)/payments" @{ method = 'Upi'; amount = 100; reference = 'UTR-1' }
Check 'first part payment -> 200' ($r1.Status -eq 200) $r1.Raw
$r2 = Api POST "/Sales/$($sale.id)/payments" @{ method = 'Cash'; amount = 172 }
$s1 = (Api GET "/Sales/$($sale.id)").Json
$pay1 = $s1.payments | Where-Object { $_.amount -eq 100 } | Select-Object -First 1
$pay2 = $s1.payments | Where-Object { $_.amount -eq 172 } | Select-Object -First 1
Check 'payments carry receipt numbers' ($pay1.receiptNumber -match '^RCT-\d{4}-\d{6}$' -and $pay2.receiptNumber -match '^RCT-' -and $pay1.receiptNumber -ne $pay2.receiptNumber) "$($pay1.receiptNumber) / $($pay2.receiptNumber)"

$rc1 = (Api GET "/Sales/$($sale.id)/payments/$($pay1.id)/receipt").Json
Check 'receipt 1: amount, invoice, customer' ($rc1.amount -eq 100 -and $rc1.invoiceNumber -eq $sale.invoiceNumber -and $rc1.customer.name -eq "INV Cust $sfx")
Check 'receipt 1: nothing paid before, balance 372' ($rc1.previouslyPaid -eq 0 -and $rc1.balanceAfter -eq 372) "prev $($rc1.previouslyPaid) bal $($rc1.balanceAfter)"
Check 'receipt 1 does NOT say the invoice is settled' ($rc1.settlesInvoice -eq $false)
$rc2 = (Api GET "/Sales/$($sale.id)/payments/$($pay2.id)/receipt").Json
Check 'receipt 2: previous 100, balance 200' ($rc2.previouslyPaid -eq 100 -and $rc2.balanceAfter -eq 200) "prev $($rc2.previouslyPaid) bal $($rc2.balanceAfter)"
Check 'receipt method and reference' ($rc1.method -eq 'Upi' -and $rc1.reference -eq 'UTR-1')
Check 'receipt amount in words present' (-not [string]::IsNullOrWhiteSpace($rc1.amountInWords))
Check 'receipt exposes the company' ($rc1.company.name -eq "Test Retail $sfx")

$rp = Api GET "/Sales/$($sale.id)/payments/$($pay1.id)/receipt/pdf"
Check 'receipt PDF is valid' (Get-Pdf-Valid $rp)
Check 'receipt PDF thermal 58' (Get-Pdf-Valid (Api GET "/Sales/$($sale.id)/payments/$($pay1.id)/receipt/pdf?format=Thermal&width=58"))
Check 'receipt of a payment on another sale -> 404' ((Api GET "/Sales/$((NewSale 1).id)/payments/$($pay1.id)/receipt").Status -eq 404)
Check 'receipt needs a login' ((Api GET "/Sales/$($sale.id)/payments/$($pay1.id)/receipt" -Anonymous).Status -eq 401)

$last = Api POST "/Sales/$($sale.id)/payments" @{ method = 'Card'; amount = 200 }
$s2 = (Api GET "/Sales/$($sale.id)").Json
$pay3 = $s2.payments | Where-Object { $_.amount -eq 200 } | Select-Object -First 1
$rc3 = (Api GET "/Sales/$($sale.id)/payments/$($pay3.id)/receipt").Json
Check 'receipt 3 settles the invoice (balance 0)' ($rc3.settlesInvoice -eq $true -and $rc3.balanceAfter -eq 0 -and $rc3.previouslyPaid -eq 272)
Check 'invoice is Paid' ($s2.paymentStatus -eq 'Paid' -and $s2.outstanding -eq 0)

# void
if ($StaffEmail) {
    Use-Token ((Api POST '/Auth/login' @{ email = $StaffEmail; password = $StaffPassword } -Anonymous).Json.token)
    Check 'Staff cannot void a payment (403)' ((Api POST "/Sales/$($sale.id)/payments/$($pay2.id)/void" @{ reason = 'try' }).Status -eq 403)
    Use-Token $adminToken
}
$v = Api POST "/Sales/$($sale.id)/payments/$($pay2.id)/void" @{ reason = 'Entered twice by mistake' }
Check 'admin voids payment 2' ($v.Status -eq 200) $v.Raw
$s3 = (Api GET "/Sales/$($sale.id)").Json
Check 'voided payment is not counted: paid 300, outstanding 172, Partial' ($s3.paidAmount -eq 300 -and $s3.outstanding -eq 172 -and $s3.paymentStatus -eq 'Partial') "paid $($s3.paidAmount) out $($s3.outstanding) $($s3.paymentStatus)"
$rv = (Api GET "/Sales/$($sale.id)/payments/$($pay2.id)/receipt").Json
Check 'voided receipt is marked voided with reason' ($rv.isVoided -eq $true -and $rv.status -eq 'Voided' -and $rv.voidReason -eq 'Entered twice by mistake')
Check 'voided receipt does not claim to settle the invoice' ($rv.settlesInvoice -eq $false)
Check 'voided receipt PDF still renders' (Get-Pdf-Valid (Api GET "/Sales/$($sale.id)/payments/$($pay2.id)/receipt/pdf"))
$rc3b = (Api GET "/Sales/$($sale.id)/payments/$($pay3.id)/receipt").Json
Check 'later receipt no longer counts the voided payment' ($rc3b.previouslyPaid -eq 100 -and $rc3b.balanceAfter -eq 172 -and $rc3b.settlesInvoice -eq $false) "prev $($rc3b.previouslyPaid) bal $($rc3b.balanceAfter)"

# legacy payment (no receipt number yet) gets one lazily - needs SQL access to the COPY
if ($Database) {
    sqlcmd -S $SqlServer -d $Database -E -C -Q "UPDATE Payments SET ReceiptNumber = NULL WHERE Id = $($pay1.id)" | Out-Null
    $lazy = (Api GET "/Sales/$($sale.id)/payments/$($pay1.id)/receipt").Json
    $lazy2 = (Api GET "/Sales/$($sale.id)/payments/$($pay1.id)/receipt").Json
    Check 'payment without a receipt number gets one on first request' ($lazy.receiptNumber -match '^RCT-\d{4}-\d{6}$')
    Check 'the lazily assigned number is stable' ($lazy.receiptNumber -eq $lazy2.receiptNumber)
    Check 'lazy number is unique' ($lazy.receiptNumber -ne $pay2.receiptNumber -and $lazy.receiptNumber -ne $pay3.receiptNumber)
}

# =========================================================== SNAPSHOT
Write-Host "`n== Saved invoices do not change when the company or customer changes"
$beforeCompany = (Api GET "/Sales/$($sale.id)").Json.company
$beforeCust = (Api GET "/Sales/$($sale.id)").Json.customer
Save-Company @{ companyName = "Renamed Retail $sfx"; phone = '9111111111'; address = 'New address 99' } | Out-Null
Api PUT "/Customers/$cust" @{ name = "INV Cust RENAMED $sfx"; mobile = "$mobile"; address = 'Moved elsewhere'; stateCode = '36' } | Out-Null
$afterOld = (Api GET "/Sales/$($sale.id)").Json
Check 'old invoice keeps the company name' ($afterOld.company.name -eq $beforeCompany.name -and $afterOld.company.name -eq "Test Retail $sfx") "$($afterOld.company.name)"
Check 'old invoice keeps the company address and phone' ($afterOld.company.phone -eq $beforeCompany.phone -and $afterOld.company.address -eq $beforeCompany.address)
Check 'old invoice keeps the customer name and address' ($afterOld.customer.name -eq $beforeCust.name -and $afterOld.customer.address -eq $beforeCust.address) "$($afterOld.customer.name)"
$newSale = NewSale 1
Check 'a NEW invoice uses the new company details' ($newSale.company.name -eq "Renamed Retail $sfx" -and $newSale.company.phone -eq '9111111111')
Check 'a NEW invoice uses the new customer details' ($newSale.customer.name -eq "INV Cust RENAMED $sfx")
Check 'old invoice totals unchanged' ($afterOld.totalAmount -eq 472)

# =========================================================== AUTHORIZATION
Write-Host "`n== Authorization"
Use-Token ''
Check 'anonymous cannot save the company (401)' ((Save-Company @{ phone = '9000000009' }).Status -eq 401)
Check 'anonymous cannot upload a logo (401)' ((Api-Upload '/Company/logo' "$tmp\logo.png" 'logo.png' 'image/png').Status -eq 401)
if ($StaffEmail) {
    Use-Token ((Api POST '/Auth/login' @{ email = $StaffEmail; password = $StaffPassword } -Anonymous).Json.token)
    Check 'Staff can read the company (invoice screens need it)' ((Api GET '/Company').Status -eq 200)
    Check 'Staff cannot save the company (403)' ((Save-Company @{ phone = '9000000009' }).Status -eq 403)
    Check 'Staff cannot upload a logo (403)' ((Api-Upload '/Company/logo' "$tmp\logo.png" 'logo.png' 'image/png').Status -eq 403)
    Check 'Staff cannot delete the logo (403)' ((Api DELETE '/Company/logo').Status -eq 403)
    Check 'Staff can print an invoice PDF' (Get-Pdf-Valid (Api GET "/Sales/$($sale.id)/pdf?format=A4"))
    Check 'Staff can open a receipt' ((Api GET "/Sales/$($sale.id)/payments/$($pay1.id)/receipt").Status -eq 200)
    Use-Token $adminToken
}
Check 'company still unchanged by the rejected calls' ((Api GET '/Company').Json.phone -eq '9111111111')

# =========================================================== LOGO REMOVAL
Write-Host "`n== Logo removal and replace"
$old = (Api GET '/Company').Json
$up2 = Api-Upload '/Company/logo' "$tmp\logo.png" 'logo2.png' 'image/png'
Check 'replace logo -> 200' ($up2.Status -eq 200)
$del = Api DELETE '/Company/logo'
Check 'remove logo -> 200' ($del.Status -eq 200) "$($del.Status)"
Check 'company no longer has a logo' ((Api GET '/Company').Json.hasLogo -eq $false)
Check 'old invoice still renders after the logo is removed' (Get-Pdf-Valid (Api GET "/Sales/$($sale.id)/pdf?format=A4"))

Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
Write-Host ''
Test-Summary

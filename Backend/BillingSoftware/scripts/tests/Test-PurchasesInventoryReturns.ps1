<#
 Integration checks for purchases, inventory, stock adjustments and returns.

 Run against a TEST API connected to a COPY of the database (see scripts/Start-TestApi.ps1):

   .\Test-PurchasesInventoryReturns.ps1 -BaseUrl http://localhost:7190/api -Email admin@test.local -Password '...'

 It creates its own uniquely named products, suppliers and customers (prefix "IT"), so it can be
 re-run. It never deletes anything.
#>
param(
    [string]$BaseUrl = 'http://localhost:7190/api',
    [Parameter(Mandatory)][string]$Email,
    [Parameter(Mandatory)][string]$Password
)

. (Join-Path $PSScriptRoot '..\ApiTestHelpers.ps1')
Connect-Api -BaseUrl $BaseUrl -Email $Email -Password $Password

$sfx = (Get-Date).ToString('HHmmss')
$gst18 = ((Api GET '/Tax').Json | Where-Object { $_.percentage -eq 18 -and $_.isActive } | Select-Object -First 1).id
$gst5 = ((Api GET '/Tax').Json | Where-Object { $_.percentage -eq 5 -and $_.isActive } | Select-Object -First 1).id
if (-not $gst18 -or -not $gst5) { throw 'The database needs active 18% and 5% tax rates.' }

$company = (Api GET '/Company').Json
Write-Host "Company state: $($company.state) ($($company.stateCode))"

function NewProduct($name, $price, $cost, $stock, $taxId, $hsn) {
    $body = @{ name = "IT $name $sfx"; sku = "IT-$name-$sfx"; purchasePrice = $cost; sellingPrice = $price; stock = $stock }
    if ($taxId) { $body.taxId = $taxId }
    if ($hsn) { $body.hsnCode = $hsn }
    $r = Api POST '/Products' $body
    if ($r.Status -ne 200) { throw "Product create failed: $($r.Raw)" }
    $r.Json.productId
}
function Stock($id) { (Api GET "/Products/$id").Json.stock }
$mobile = Get-Random -Minimum 7000000000 -Maximum 7899999999
function NewSupplier($name, $extra) {
    $script:mobile++
    $b = @{ name = "IT $name $sfx"; companyName = "IT $name Co $sfx"; mobile = "$script:mobile" }
    foreach ($k in $extra.Keys) { $b[$k] = $extra[$k] }
    $r = Api POST '/Suppliers' $b
    if ($r.Status -ne 200) { throw "Supplier create failed: $($r.Raw)" }
    $r.Json.supplierId
}
function NewCustomer($name, $extra) {
    $script:mobile++
    $b = @{ name = "IT $name $sfx"; mobile = "$script:mobile" }
    foreach ($k in $extra.Keys) { $b[$k] = $extra[$k] }
    $r = Api POST '/Customers' $b
    if ($r.Status -ne 200) { throw "Customer create failed: $($r.Raw)" }
    $r.Json.customerId
}

$p1 = NewProduct 'P1' 100 60 0 $gst18 '8517'
$p2 = NewProduct 'P2' 40 20 0 $gst5 $null
$p3 = NewProduct 'P3' 10 0 0 $null $null
$supSame = NewSupplier 'SameState' @{ stateCode = $company.stateCode }
$supOther = NewSupplier 'OtherState' @{ stateCode = $(if ($company.stateCode -eq '27') { '29' } else { '27' }) }
$custOther = NewCustomer 'OtherState' @{ stateCode = $(if ($company.stateCode -eq '27') { '29' } else { '27' }) }
$today = (Get-Date).ToString('yyyy-MM-dd')

function Purchase($supplier, $items, $extra = @{}) {
    $b = @{ supplierId = $supplier; purchaseDate = $today; items = @($items) }
    foreach ($k in $extra.Keys) { $b[$k] = $extra[$k] }
    Api POST '/Purchases' $b
}
function PItem($product, $qty, $price) { @{ productId = $product; quantity = $qty; purchasePrice = $price } }

# =================================================================== PURCHASES
Write-Host "`n== Purchases"
$a = Purchase $supSame @((PItem $p1 10 50), (PItem $p2 5 20)) @{ supplierInvoiceNumber = "SUP-$sfx-1" }
Check 'purchase created' ($a.Status -eq 200) $a.Raw
$da = (Api GET "/Purchases/$($a.Json.purchaseId)").Json
Check 'purchase number format' ($da.purchaseNumber -match '^PUR-\d{4}-\d{6}$') $da.purchaseNumber
Check 'intra-state: subtotal 600, CGST 47.50, SGST 47.50, total 695' ($da.subtotal -eq 600 -and $da.cgstTotal -eq 47.5 -and $da.sgstTotal -eq 47.5 -and $da.igstTotal -eq 0 -and $da.totalAmount -eq 695 -and $da.isInterState -eq $false) ($da | ConvertTo-Json -Compress -Depth 3)
Check 'purchase item tax snapshot' ($da.items[0].taxRate -eq 18 -and $da.items[0].hsnCode -eq '8517' -and $da.items[0].taxAmount -eq 90 -and $da.items[0].lineTotal -eq 590)
Check 'stock increased by purchase' ((Stock $p1) -eq 10 -and (Stock $p2) -eq 5)

$b = Purchase $supOther @((PItem $p1 3 33.33))
$db = (Api GET "/Purchases/$($b.Json.purchaseId)").Json
Check 'inter-state: IGST 18.00, total 118, round-off 0.01' ($db.isInterState -eq $true -and $db.igstTotal -eq 18 -and $db.cgstTotal -eq 0 -and $db.totalAmount -eq 118 -and $db.roundOff -eq 0.01) ($db | ConvertTo-Json -Compress -Depth 3)
Check 'purchase numbers consecutive' ([int]$db.purchaseNumber.Substring(9) -eq [int]$da.purchaseNumber.Substring(9) + 1) "$($da.purchaseNumber) $($db.purchaseNumber)"
Check 'stock after second purchase' ((Stock $p1) -eq 13)

$dup = Purchase $supSame @((PItem $p1 1 50)) @{ supplierInvoiceNumber = "SUP-$sfx-1" }
Check 'same supplier invoice number twice -> 409' ($dup.Status -eq 409) $dup.Raw
$ok = Purchase $supOther @((PItem $p3 4 5)) @{ supplierInvoiceNumber = "SUP-$sfx-1" }
Check 'same number for a different supplier is allowed' ($ok.Status -eq 200) $ok.Raw

$tomorrow = (Get-Date).AddDays(2).ToString('yyyy-MM-dd')
Check 'future date -> 400' ((Api POST '/Purchases' @{ supplierId = $supSame; purchaseDate = $tomorrow; items = @((PItem $p3 1 5)) }).Status -eq 400)
Check 'quantity 0 -> 400' ((Purchase $supSame @((PItem $p3 0 5))).Status -eq 400)
Check 'price 0 -> 400' ((Purchase $supSame @((PItem $p3 1 0))).Status -eq 400)
Check 'price with 3 decimals -> 400' ((Purchase $supSame @((PItem $p3 1 5.123))).Status -eq 400)
Check 'no items -> 400' ((Api POST '/Purchases' @{ supplierId = $supSame; purchaseDate = $today; items = @() }).Status -eq 400)
Check 'unknown supplier -> 400' ((Purchase 99999999 @((PItem $p3 1 5))).Status -eq 400)
Check 'unknown product -> 400' ((Purchase $supSame @((PItem 99999999 1 5))).Status -eq 400)
Check 'rejected purchases changed no stock' ((Stock $p3) -eq 4)

$rid = [guid]::NewGuid().ToString()
$s0 = Stock $p3
$i1 = Purchase $supSame @((PItem $p3 2 5)) @{ clientRequestId = $rid }
$i2 = Purchase $supSame @((PItem $p3 2 5)) @{ clientRequestId = $rid }
Check 'idempotent purchase returns same document' ($i1.Json.purchaseId -eq $i2.Json.purchaseId -and $i2.Json.isDuplicate -eq $true) "$($i1.Raw) $($i2.Raw)"
Check 'idempotent purchase added stock once' ((Stock $p3) -eq $s0 + 2)
$rid2 = [guid]::NewGuid().ToString(); $s0 = Stock $p3
$par = Api-Parallel (1..5 | ForEach-Object { @{ Method = 'POST'; Path = '/Purchases'; Body = @{ supplierId = $supSame; purchaseDate = $today; items = @((PItem $p3 1 5)); clientRequestId = $rid2 } } })
Check 'parallel same request id -> one purchase' ((@($par | ForEach-Object { $_.Json.purchaseId } | Sort-Object -Unique)).Count -eq 1 -and @($par | Where-Object { $_.Status -ne 200 }).Count -eq 0) (($par | ForEach-Object { "$($_.Status)" }) -join ',')
Check 'parallel same request id added stock once' ((Stock $p3) -eq $s0 + 1)

$many = Api-Parallel (1..8 | ForEach-Object { @{ Method = 'POST'; Path = '/Purchases'; Body = @{ supplierId = $supSame; purchaseDate = $today; items = @((PItem $p3 1 5)) } } })
$nums = @($many | ForEach-Object { $_.Json.purchaseNumber } | Where-Object { $_ })
Check '8 parallel purchases: distinct, gapless numbers' ($nums.Count -eq 8 -and ($nums | Sort-Object -Unique).Count -eq 8 -and ([int](($nums | Sort-Object)[-1].Substring(9)) - [int](($nums | Sort-Object)[0].Substring(9)) -eq 7)) ($nums -join ',')

$cost0 = (Api GET "/Products/$p2").Json.purchasePrice
Purchase $supSame @((PItem $p2 1 25)) | Out-Null
Check 'catalogue cost unchanged by default' ((Api GET "/Products/$p2").Json.purchasePrice -eq $cost0)
Purchase $supSame @((PItem $p2 1 25)) @{ updateCatalogueCost = $true } | Out-Null
Check 'catalogue cost updated when requested' ((Api GET "/Products/$p2").Json.purchasePrice -eq 25)

$list = (Api GET "/Purchases?search=SUP-$sfx-1").Json
Check 'search by supplier invoice number' ($list.totalRecords -ge 2)
$list = (Api GET "/Purchases?supplierId=$supOther&pageSize=1").Json
Check 'supplier filter + paging' ($list.pageSize -eq 1 -and $list.data.Count -eq 1 -and @($list.data | Where-Object { $_.supplierId -ne $supOther }).Count -eq 0)
Check 'date filter includes today' ((Api GET "/Purchases?fromDate=$today&toDate=$today&supplierId=$supSame").Json.totalRecords -ge 1)
Check 'unknown purchase -> 404' ((Api GET '/Purchases/99999999').Status -eq 404)

# =================================================================== INVENTORY
Write-Host "`n== Inventory and adjustments"
$sum = (Api GET '/Inventory/summary').Json
Check 'summary adds up' ($sum.inStock + $sum.lowStock + $sum.outOfStock -eq $sum.totalProducts) ($sum | ConvertTo-Json -Compress)
$out = (Api GET '/Inventory/stock?status=out&pageSize=200').Json
Check 'out-of-stock filter only returns 0 stock' (@($out.data | Where-Object { $_.stock -gt 0 -or $_.status -ne 'Out' }).Count -eq 0 -and $out.totalRecords -eq $sum.outOfStock)
$low = (Api GET '/Inventory/stock?status=low&pageSize=200').Json
Check 'low-stock filter within threshold' (@($low.data | Where-Object { $_.stock -lt 1 -or $_.stock -gt $sum.lowStockThreshold -or $_.status -ne 'Low' }).Count -eq 0 -and $low.totalRecords -eq $sum.lowStock)
Check 'bad status filter -> 400' ((Api GET '/Inventory/stock?status=zzz').Status -eq 400)
$one = (Api GET "/Inventory/stock?search=IT-P1-$sfx").Json.data[0]
Check 'stock row has value at cost' ($one.stock -eq 13 -and $one.stockValue -eq 13 * $one.purchasePrice)

$adj = Api POST '/StockAdjustment' @{ productId = $p1; quantity = 5; increaseStock = $true; reason = 'Correction'; remarks = 'Stock count correction' }
Check 'increase adjustment' ($adj.Status -eq 200 -and $adj.Json.previousStock -eq 13 -and $adj.Json.currentStock -eq 18 -and $adj.Json.referenceNo -match '^ADJ-\d{4}-\d{6}$') $adj.Raw
$dmg = Api POST '/StockAdjustment' @{ productId = $p1; quantity = 2; increaseStock = $false; reason = 'Damaged'; remarks = 'Broken in storage' }
Check 'damaged adjustment reduces stock' ($dmg.Status -eq 200 -and $dmg.Json.currentStock -eq 16) $dmg.Raw
$corr = Api POST '/StockAdjustment' @{ productId = $p1; quantity = 1; increaseStock = $false; reason = 'Correction'; remarks = 'Recount lower' }
Check 'decrease correction' ($corr.Status -eq 200 -and (Stock $p1) -eq 15) $corr.Raw
Check 'removing more than in stock -> 409' ((Api POST '/StockAdjustment' @{ productId = $p1; quantity = 16; increaseStock = $false; reason = 'Lost'; remarks = 'x lost' }).Status -eq 409)
Check 'damaged cannot increase -> 400' ((Api POST '/StockAdjustment' @{ productId = $p1; quantity = 1; increaseStock = $true; reason = 'Damaged'; remarks = 'bad' }).Status -eq 400)
Check 'missing remarks -> 400' ((Api POST '/StockAdjustment' @{ productId = $p1; quantity = 1; increaseStock = $true; reason = 'Correction' }).Status -eq 400)
Check 'quantity 0 -> 400' ((Api POST '/StockAdjustment' @{ productId = $p1; quantity = 0; increaseStock = $true; reason = 'Correction'; remarks = 'abc' }).Status -eq 400)
Check 'unknown product -> 400' ((Api POST '/StockAdjustment' @{ productId = 99999999; quantity = 1; increaseStock = $true; reason = 'Correction'; remarks = 'abc' }).Status -eq 400)
Check 'failed adjustments changed nothing' ((Stock $p1) -eq 15)

$hist = (Api GET "/StockAdjustment?search=IT-P1-$sfx").Json
Check 'adjustment history lists the three' ($hist.totalRecords -eq 3 -and $hist.data[0].referenceNo -match '^ADJ-' -and $hist.data[0].createdBy)
$led = (Api GET "/StockLedger?productId=$p1&pageSize=50").Json
Check 'ledger rows for product' ($led.totalRecords -ge 5)
$chain = $led.data | Sort-Object id
$broken = 0; for ($i = 1; $i -lt $chain.Count; $i++) { if ($chain[$i].previousStock -ne $chain[$i - 1].currentStock) { $broken++ } }
Check 'ledger chain unbroken' ($broken -eq 0) "broken=$broken"
Check 'ledger last balance equals product stock' ($chain[-1].currentStock -eq (Stock $p1))
Check 'ledger quantityChange signed' (($led.data | Where-Object { $_.transactionType -eq 'Damaged' } | Select-Object -First 1).quantityChange -eq -2)
Check 'ledger filter by type' ((Api GET "/StockLedger?productId=$p1&transactionType=Purchase").Json.data.Count -eq 2)
Check 'ledger bad type -> 400' ((Api GET '/StockLedger?transactionType=zzz').Status -eq 400)
Check 'ledger timestamps carry a Z (UTC)' ($led.data[0].createdOn -match 'Z$') $led.data[0].createdOn

$race = Api-Parallel (1..6 | ForEach-Object { @{ Method = 'POST'; Path = '/StockAdjustment'; Body = @{ productId = $p2; quantity = 3; increaseStock = $false; reason = 'Lost'; remarks = 'race test' } } })
$okc = @($race | Where-Object { $_.Status -eq 200 }).Count; $cf = @($race | Where-Object { $_.Status -eq 409 }).Count
$p2stock = Stock $p2
Check 'parallel removals never go negative' ($p2stock -ge 0 -and $okc * 3 + $p2stock -eq 7 -and $okc + $cf -eq 6) "ok=$okc conflict=$cf stock=$p2stock"

# =================================================================== SALES RETURNS
Write-Host "`n== Sales returns"
Api POST '/StockAdjustment' @{ productId = $p2; quantity = 20; increaseStock = $true; reason = 'Correction'; remarks = 'Reset for return tests' } | Out-Null
function SaleOf($customer, $items, $pay = $null) {
    $b = @{ customerId = $customer; items = @($items); clientRequestId = [guid]::NewGuid().ToString() }
    if ($pay) { $b.payments = @($pay) }
    Api POST '/Sales' $b
}
$stockP1 = Stock $p1; $stockP2 = Stock $p2
$sale = SaleOf $custOther @(@{ productId = $p1; quantity = 3 }, @{ productId = $p2; quantity = 2 })
$sd = (Api GET "/Sales/$($sale.Json.saleId)").Json
Api POST "/Sales/$($sale.Json.saleId)/payments" @{ method = 'Cash'; amount = $sd.totalAmount } | Out-Null
$line1 = $sd.items | Where-Object { $_.productId -eq $p1 }
$line2 = $sd.items | Where-Object { $_.productId -eq $p2 }
Check 'sale for return tests is IGST' ($sd.isInterState -eq $true) ($sd | ConvertTo-Json -Compress -Depth 2)

function Ret($saleId, $items, $reason = 'Customer changed mind', $refund = 0, $method = $null) {
    $b = @{ saleId = $saleId; items = @($items); reason = $reason; refundAmount = $refund }
    if ($method) { $b.refundMethod = $method }
    Api POST '/SalesReturns' $b
}

Check 'return without a reason -> 400' ((Api POST '/SalesReturns' @{ saleId = $sd.id; items = @(@{ saleItemId = $line1.id; quantity = 1 }) }).Status -eq 400)
Check 'refund without method -> 400' ((Ret $sd.id @(@{ saleItemId = $line1.id; quantity = 1 }) 'x test' 10).Status -eq 400)
Check 'quantity 0 -> 400' ((Ret $sd.id @(@{ saleItemId = $line1.id; quantity = 0 })).Status -eq 400)
Check 'no items -> 400' ((Api POST '/SalesReturns' @{ saleId = $sd.id; items = @(); reason = 'abc' }).Status -eq 400)
Check 'unknown invoice -> 404' ((Ret 99999999 @(@{ saleItemId = 1; quantity = 1 })).Status -eq 404)
Check 'over-return -> 409' ((Ret $sd.id @(@{ saleItemId = $line1.id; quantity = 4 })).Status -eq 409)
Check 'line from another invoice -> 409' ((Ret $sd.id @(@{ saleItemId = 1; quantity = 1 })).Status -eq 409)
Check 'refund above what was paid for the goods -> 409' ((Ret $sd.id @(@{ saleItemId = $line1.id; quantity = 1 }) 'x test' 1000 'Cash').Status -eq 409)
Check 'rejected returns changed no stock' ((Stock $p1) -eq $stockP1 - 3)

$r1 = Ret $sd.id @(@{ saleItemId = $line1.id; quantity = 1 }) 'Damaged on delivery' 0
Check 'partial return created' ($r1.Status -eq 200 -and $r1.Json.returnNumber -match '^CN-\d{4}-\d{6}$' -and $r1.Json.refundDue -eq $r1.Json.totalAmount) $r1.Raw
Check 'return put stock back' ((Stock $p1) -eq $stockP1 - 2)
$d1 = (Api GET "/SalesReturns/$($r1.Json.returnId)").Json
Check 'credit note is IGST, proportional' ($d1.isInterState -eq $true -and $d1.igstTotal -gt 0 -and $d1.cgstTotal -eq 0 -and $d1.subtotal -eq $line1.unitPrice -and $d1.items[0].quantity -eq 1) ($d1 | ConvertTo-Json -Compress -Depth 3)
$sd2 = (Api GET "/Sales/$($sd.id)").Json
$l1 = $sd2.items | Where-Object { $_.productId -eq $p1 }
Check 'invoice shows returned/returnable' ($l1.returnedQuantity -eq 1 -and $l1.returnableQuantity -eq 2 -and $sd2.returnedAmount -eq $r1.Json.totalAmount -and $sd2.refundDue -eq $r1.Json.totalAmount)

$extra = Api POST "/SalesReturns/$($r1.Json.returnId)/refund" @{ amount = $r1.Json.totalAmount + 1; method = 'Cash' }
Check 'refund above the amount owed -> 409' ($extra.Status -eq 409) $extra.Raw
$half = [math]::Round($r1.Json.totalAmount / 2, 2)
$ref1 = Api POST "/SalesReturns/$($r1.Json.returnId)/refund" @{ amount = $half; method = 'Upi' }
Check 'part refund later' ($ref1.Status -eq 200 -and $ref1.Json.refundDue -eq [math]::Round($r1.Json.totalAmount - $half, 2)) $ref1.Raw
$ref2 = Api POST "/SalesReturns/$($r1.Json.returnId)/refund" @{ amount = $r1.Json.totalAmount - $half; method = 'Cash' }
Check 'remaining refund later -> nothing owed' ($ref2.Status -eq 200 -and $ref2.Json.refundDue -eq 0) $ref2.Raw
Check 'nothing left to refund -> 409' ((Api POST "/SalesReturns/$($r1.Json.returnId)/refund" @{ amount = 1; method = 'Cash' }).Status -eq 409)

# Return everything that is left, with the refund paid straight away.
$r2 = Ret $sd.id @(@{ saleItemId = $line1.id; quantity = 2 }, @{ saleItemId = $line2.id; quantity = 2 }) 'Wrong item' 0
Check 'final return created' ($r2.Status -eq 200) $r2.Raw
$sd3 = (Api GET "/Sales/$($sd.id)").Json
Check 'all returns add up to the invoice total (incl. round-off)' ($sd3.returnedAmount -eq $sd3.totalAmount) "returned=$($sd3.returnedAmount) total=$($sd3.totalAmount)"
Check 'stock fully restored' ((Stock $p1) -eq $stockP1 -and (Stock $p2) -eq $stockP2)
Check 'nothing returnable left' (@($sd3.items | Where-Object { $_.returnableQuantity -ne 0 }).Count -eq 0)
Check 'refund due equals second credit note until paid' ($sd3.refundDue -eq $r2.Json.totalAmount) "$($sd3.refundDue)"
$ref3 = Api POST "/SalesReturns/$($r2.Json.returnId)/refund" @{ amount = $r2.Json.totalAmount; method = 'BankTransfer' }
$sd4 = (Api GET "/Sales/$($sd.id)").Json
Check 'invoice settled after full return and refund' ($sd4.paymentStatus -eq 'Settled' -and $sd4.outstanding -eq 0 -and $sd4.refundDue -eq 0 -and $sd4.refundedAmount -eq $sd4.paidAmount) ($sd4 | ConvertTo-Json -Compress -Depth 2)
Check 'fully returned line cannot be returned again -> 409' ((Ret $sd.id @(@{ saleItemId = $line1.id; quantity = 1 })).Status -eq 409)
Check 'a paid payment cannot be voided once refunded' ((Api POST "/Sales/$($sd.id)/payments/$(($sd4.payments | Select-Object -First 1).id)/void" @{ reason = 'try' }).Status -eq 409)
$rlist = (Api GET "/SalesReturns?saleId=$($sd.id)").Json
Check 'returns list by invoice' ($rlist.totalRecords -eq 2)
Check 'returns search by number' ((Api GET "/SalesReturns?search=$($r1.Json.returnNumber)").Json.totalRecords -eq 1)

# Concurrency: 5 parallel requests for a line that has 3 returnable
$sale2 = SaleOf $custOther @(@{ productId = $p2; quantity = 3 }) @{ method = 'Cash'; amount = 1 }
$s2 = (Api GET "/Sales/$($sale2.Json.saleId)").Json; $s2line = $s2.items[0]
$race = Api-Parallel (1..5 | ForEach-Object { @{ Method = 'POST'; Path = '/SalesReturns'; Body = @{ saleId = $s2.id; items = @(@{ saleItemId = $s2line.id; quantity = 1 }); reason = 'race test'; refundAmount = 0 } } })
$okc = @($race | Where-Object { $_.Status -eq 200 }).Count
Check 'parallel returns: exactly 3 succeed, others 409, no 5xx' ($okc -eq 3 -and @($race | Where-Object { $_.Status -eq 409 }).Count -eq 2) (($race | ForEach-Object { $_.Status }) -join ',')
$s2b = (Api GET "/Sales/$($s2.id)").Json
Check 'parallel returns add up to the invoice' ($s2b.returnedAmount -eq $s2b.totalAmount) "$($s2b.returnedAmount) $($s2b.totalAmount)"

# Legacy invoice: no refund can be verified
$legacy = (Api GET '/Sales?paymentStatus=NotRecorded&pageSize=1').Json.data
if ($legacy.Count -gt 0) {
    $ld = (Api GET "/Sales/$($legacy[0].id)").Json
    $lit = $ld.items | Where-Object { $_.returnableQuantity -gt 0 } | Select-Object -First 1
    if ($lit) {
        Check 'legacy invoice: refund refused -> 409' ((Ret $ld.id @(@{ saleItemId = $lit.id; quantity = 1 }) 'legacy test' 1 'Cash').Status -eq 409)
    }
}

# =================================================================== PURCHASE RETURNS
Write-Host "`n== Purchase returns"
$pDet = (Api GET "/Purchases/$($a.Json.purchaseId)").Json
$pl1 = $pDet.items | Where-Object { $_.productId -eq $p1 }
$pl2 = $pDet.items | Where-Object { $_.productId -eq $p2 }
function PRet($purchaseId, $items, $reason = 'Faulty goods', $refund = 0, $method = $null, $note = $null) {
    $b = @{ purchaseId = $purchaseId; items = @($items); reason = $reason; refundReceived = $refund }
    if ($method) { $b.refundMethod = $method }
    if ($note) { $b.adjustmentNote = $note }
    Api POST '/PurchaseReturns' $b
}
Check 'purchase return needs a reason -> 400' ((Api POST '/PurchaseReturns' @{ purchaseId = $pDet.id; items = @(@{ purchaseItemId = $pl1.id; quantity = 1 }) }).Status -eq 400)
Check 'over-return -> 409' ((PRet $pDet.id @(@{ purchaseItemId = $pl1.id; quantity = 11 })).Status -eq 409)
Check 'refund without method -> 400' ((PRet $pDet.id @(@{ purchaseItemId = $pl1.id; quantity = 1 }) 'x test' 5).Status -eq 400)
Check 'refund more than the goods -> 409' ((PRet $pDet.id @(@{ purchaseItemId = $pl1.id; quantity = 1 }) 'x test' 9999 'Cash').Status -eq 409)
$before = Stock $p1
$pr1 = PRet $pDet.id @(@{ purchaseItemId = $pl1.id; quantity = 4 }) 'Faulty goods' 100 'BankTransfer' 'Rest as credit note'
Check 'purchase return created' ($pr1.Status -eq 200 -and $pr1.Json.returnNumber -match '^DN-\d{4}-\d{6}$') $pr1.Raw
Check 'stock reduced by the return' ((Stock $p1) -eq $before - 4)
$pd1 = (Api GET "/PurchaseReturns/$($pr1.Json.returnId)").Json
Check 'debit note: CGST+SGST proportional (4 x 50 = 200 @18%)' ($pd1.subtotal -eq 200 -and $pd1.cgstTotal -eq 18 -and $pd1.sgstTotal -eq 18 -and $pd1.totalAmount -eq 236 -and $pd1.refundReceived -eq 100 -and $pd1.adjustmentNote -eq 'Rest as credit note') ($pd1 | ConvertTo-Json -Compress -Depth 3)
$pDet2 = (Api GET "/Purchases/$($pDet.id)").Json
Check 'purchase shows returned quantities' (($pDet2.items | Where-Object { $_.productId -eq $p1 }).returnedQuantity -eq 4 -and ($pDet2.items | Where-Object { $_.productId -eq $p1 }).returnableQuantity -eq 6 -and $pDet2.returnedAmount -eq 236)

# The rest cannot go back if it has been sold.
$sold = SaleOf $custOther @(@{ productId = $p1; quantity = ((Stock $p1) - 2) })
Check 'stock sold to leave 2 units' ($sold.Status -eq 200 -and (Stock $p1) -eq 2) $sold.Raw
$prFail = PRet $pDet.id @(@{ purchaseItemId = $pl1.id; quantity = 6 }) 'Return the rest'
Check 'cannot return goods that are no longer in stock -> 409' ($prFail.Status -eq 409) $prFail.Raw
Check 'failed purchase return changed nothing' ((Stock $p1) -eq 2)
Api POST '/StockAdjustment' @{ productId = $p1; quantity = 10; increaseStock = $true; reason = 'Correction'; remarks = 'Restock for return test' } | Out-Null
$pr2 = PRet $pDet.id @(@{ purchaseItemId = $pl1.id; quantity = 6 }, @{ purchaseItemId = $pl2.id; quantity = 5 }) 'Return the rest'
Check 'final purchase return ok' ($pr2.Status -eq 200) $pr2.Raw
$pDet3 = (Api GET "/Purchases/$($pDet.id)").Json
Check 'debit notes add up to the purchase total (incl. round-off)' ($pDet3.returnedAmount -eq $pDet3.totalAmount) "$($pDet3.returnedAmount) $($pDet3.totalAmount)"
Check 'purchase return list' ((Api GET "/PurchaseReturns?purchaseId=$($pDet.id)").Json.totalRecords -eq 2)
Check 'unknown purchase -> 404' ((PRet 99999999 @(@{ purchaseItemId = 1; quantity = 1 })).Status -eq 404)

# =================================================================== FINAL INVARIANTS
Write-Host "`n== Invariants"
foreach ($prodId in @($p1, $p2, $p3)) {
    $all = (Api GET "/StockLedger/$prodId").Json | Sort-Object id
    $br = 0; for ($i = 1; $i -lt $all.Count; $i++) { if ($all[$i].previousStock -ne $all[$i - 1].currentStock) { $br++ } }
    Check "ledger chain unbroken for product $prodId" ($br -eq 0 -and $all[-1].currentStock -eq (Stock $prodId)) "breaks=$br last=$($all[-1].currentStock) stock=$(Stock $prodId)"
}

Test-Summary
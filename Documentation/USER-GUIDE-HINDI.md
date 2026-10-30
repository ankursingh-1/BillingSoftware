# BillingSoftware — User guide (Hindi / Hinglish)

Yeh guide shop operator ke liye hai. Har step mein menu, button, fields, aur save ke baad kya check karna hai likha hai.

Demo records ke naam `DEMO` se shuru hote hain. Asli shop data ko demo se mix mat karna.

---

## 1. Login

1. Browser mein app kholo. Verification copy: `http://localhost:5177` (API `http://localhost:7190`, database `BillingSoftware_Verify`). Visual Studio development: `http://localhost:5173` (API `https://localhost:7179`).
2. **Email** aur **Password** bharo.
3. **Login** dabao (Enter bhi chalega).
4. Dashboard khulega. Header mein aapka email aur role dikhega.

Galat password par “Invalid Email or Password” aayega. Bahut tez galat tries par server 429 de sakta hai (1 minute mein 10 login). Session khatam hone par wapas login page.

**Logout:** sidebar ke neeche **Logout**.

**Password badalna:** header → **Change password** → current + naya (kam se kam 8 letters) + confirm.

---

## 2. Company settings (sirf Admin)

1. Sidebar → **Settings**.
2. Company name, address, phone, email, GSTIN, State, invoice prefix, **Shop type** (General retail / Electronics / Grocery), print format, thermal 80/58 mm, bank details, logo.

Shop type sirf counter default badalta hai — alag app nahi:

- **Grocery:** New Sale par Walk-in auto select; warranty prompt band.
- **Electronics:** jinke product par default warranty hai, sale par register option on.
- **General retail:** dono tarike chalenge.
3. GSTIN ke pehle 2 digit state se match hone chahiye, warna save block ho jayega. App khud GSTIN nahi badlegi.
4. **Save company settings**.

Check: naya invoice header mein naya naam/logo. Purane invoices pehle wale snapshot par rehte hain.

Logo: PNG/JPEG, max 2 MB. Production mein `Storage:Root` persistent folder hona chahiye.

---

## 3. Categories, brands, units, taxes

**Categories:** sidebar → **Categories** → **Add Category** → Name → Save.

**Brands, units, tax:** sidebar → **Brands, units & tax** (Admin).

- **Brands** / **Units** / **Tax rates** tab.
- **Add** → name (tax par percentage, jaise 18) → Save.
- Inactive items purane products par rehte hain, naye form mein choose nahi hote.

---

## 4. Ek product manually

1. **Products** → **Add Product**.
2. Name, SKU (unique), Category/Brand/Unit/Tax, HSN (optional), Purchase price, Selling price.
3. Electronics ke liye: **Barcode** (optional, unique), **Default warranty** (number + Days/Months/Years), **Serial required** (tick agar har unit ka serial chahiye).
4. Default warranty sirf **nayi sale** par default hai. Baad mein product default badalne se purani invoice ki warranty dates nahi badlengi.
5. **Current stock** yahan se nahi badlega — opening stock create par, baad mein purchase / adjustment se.
6. **Save**.

Check: list mein naam/SKU. Edit par purani values load honi chahiye.

---

## 5. Excel se hundreds of products

1. **Products** → **Import Excel**.
2. **Download template**.
3. **Products** sheet bharo (Sample sheet sirf example hai — import nahi hota).
4. Columns: ProductName*, SKU*, Category, Brand, Unit, Tax, HSN, PurchasePrice*, SellingPrice*, OpeningStock / StockQuantity (set stock), AddStockQuantity (add delivery), ProductId, ReceiptId, Barcode, warranty.
5. File choose karo (.xlsx, max 5 MB / 2000 rows) → **Check file**.
6. Preview: new / update / stock change / unchanged / duplicate / conflict / error. Blank stock zero nahi hai.
7. **Confirm import**. Jo rows apply honge woh ek transaction mein save hote hain.
8. Stock ledger se jata hai. Excel stock kam nahi karta — uske liye Inventory → Adjustments.

Dobara same file: pehle wala result wapas, stock double nahi.

---

## 6. Supplier aur purchase (stock in)

1. **Suppliers** → **Add Supplier** → contact name, company, mobile, state/GSTIN.
2. **Purchases** (Admin) → **New Purchase**.
3. Supplier choose, date, line items (product, qty, purchase price).
4. Save.

Check: **Inventory** → Movements mein **Purchase**. Product stock badha.

Purchase ka bill pay karna is app mein nahi hai (payables model nahi hai).

---

## 7. Customers

**Customers** → **Add Customer** → name, mobile, address, state, GSTIN (business).

Deleted customer nayi sale mein choose nahi hota; purani invoices rehti hain.

---

## 8. Sale banana

1. **Sales** → **New Sale**.
2. Customer search **ya** **Walk-in** (counter customer, mobile `9000000000`).
3. Product name/SKU search → add. Kayi products. Qty badlo. Stock aur price dikhega.
4. Negative / extra qty save nahi hogi.
5. Optional: neeche payment (cash/UPI/card/bank/cheque) — khali chhodoge to Unpaid.
6. **Create Invoice**. Totals server calculate karta hai.

Check: invoice number, tax, grand total. Dashboard par sale dikhe.

---

## 9. Full payment

Invoice kholo → **Record payment** → amount **Full** → method → **Record payment**.

Confirmation: amount received, remaining ₹0, status **Paid**, **Print receipt**.

---

## 10. Partial payment

Amount outstanding se kam likho (jaise 500). Status **Partially Paid**, remaining balance dikhega. Invoice refresh / dubara kholo — status server se aata hai, screen par purana label nahi rehta.

---

## 11. Baaki balance collect karna

Wahi invoice → phir **Record payment** → remaining amount (ya Full) → save. Status **Paid**.

Voided payment collect total mein nahi ginati (sirf Admin void kar sakta hai).

---

## 12. Invoice print

Sale detail → format:

- A4 tax invoice
- Compact (A5)
- Thermal, phir 80 mm ya 58 mm

**Print** = browser print dialog. **Download PDF** bhi hai.

Bank details thermal/retail par tabhi jab Settings mein “Show bank details” on ho.

---

## 13. Payment receipt

Payment ke baad **Print receipt**, ya Payments table mein receipt number.

Receipt par: amount, method, invoice, date, remaining balance.

---

## 14. Return aur refund

Invoice → **Return items** → eligible qty se zyada nahi. Reason likho.

Stock wapas ledger se aata hai. Refund alag hai — paid amount se zyada refund nahi.

**Returns** menu mein credit/debit notes.

---

## 15. Stock

**Inventory**: Stock / Movements / Adjustments.

Low stock = global threshold (default 5), product-wise min field nahi.

Adjust (Admin): increase/decrease, reason, remarks. Zero stock se reduce nahi.

---

## 16. Expenses (Admin)

**Expenses** → **Add expense** → title, amount, date, category (text), notes → Save.

Yeh shop cost hai, product purchase nahi. Payment-method field nahi hai.

---

## 17. Reports (Admin)

**Reports** tabs: Sales, Purchases, Stock (All / Low / Out), Customers, Suppliers, Collections, Expenses, Gross profit.

Date filter + **Download Excel**.

Gross profit = sale-line **CostPrice**, kabhi sales minus purchases nahi. Cost missing ho to figure hide / “incomplete”.

---

## 18. Dashboard figures

Cards asli API se aati hain (aaj ki sales, purchases, customers, low/out of stock, outstanding collections). Click karke module khulta hai.

Staff ko purchase links nahi milte. Profit incomplete cost par **Unavailable**.

Agar poora dashboard “Unable to load dashboard data” dikhaye: API chal rahi hai ya nahi check karo; browser Network tab mein `/api/Dashboard/summary` 200 hona chahiye. Warranty endpoint fail ho to baaki cards phir bhi load honi chahiye, aur expiring warranty card **Unavailable** dikhega — yeh empty shop nahi hai.

`https://localhost:7179` live `BillingSoftwareDb` par warranty migration ke bina chalega to products/warranty columns missing ho sakte hain. Verification ke liye `7190` / `BillingSoftware_Verify` use karo.

---

## 19. Demo data

Sirf **copy database** (`BillingSoftware_Verify`) par, `Demo:Enabled=true`. Production default band.

Settings → **Seed demo records**. `DEMO-` SKU, walk-in customer, purchase, paid/partial/unpaid sales, return, expenses.

Dobara chalane par duplicate invoice nahi. Live `BillingSoftwareDb` par mat chalao.

Reset: `Documentation/DEMO-RESET-PLAN.md` — pehle approval.

---

## 20. Common galti

| Galti | Sahi |
|---|---|
| Product form se stock badalna | Purchase / return / adjustment |
| Excel **Sample** sheet import samajhna | Sirf **Products** sheet |
| GSTIN aur state mismatch save | Dono match karo; app tax identity nahi badlegi |
| Return ko refund maan lena | Return = stock; refund = paise |
| Gross profit = sales − purchases | Nahi |
| Demo live shop par | Copy DB + Demo:Enabled |
| Same sale do baar click | Client request id duplicate rokta hai |
| Token expire ke baad kaam | Wapas login |

Admin-only: Settings, Purchases, Expenses, Reports, Catalog writes, stock adjust, payment void, demo seed.

---

## 21. Electronics product + warranty (Hinglish)

1. **Products → Add Product.** Laptop/fridge/TV/phone ka naam, SKU, price, tax.
2. **Default warranty** bharo — example: `1` + **Years**, ya `6` + **Months**. Har product alag ho sakta hai; app mein koi hardcoded 1-year rule nahi hai.
3. Serial chahiye ho to **Serial required** tick.
4. **Sales → New sale.** Customer search (mobile). Walk-in par warranty register nahi hota — asli mobile wala customer chuno.
5. Product add. **Register warranty** tick. Duration default se aata hai; sale par override kar sakte ho (Days/Months/Years).
6. Serial box mein ek unit = ek serial. Qty 2 ho to do serial.
7. Payment + Save. Invoice par WRN number, duration, start, end print hote hain.
8. Baad mein dhoondhna: sidebar **Warranties** — warranty number, invoice, mobile, naam, product, serial.

## 22. Warranty claim aur replacement

1. **Warranties** → record kholo.
2. **Eligible by date** sirf stored start/end se aata hai. Replacement automatic promise nahi hai — inspect karo, notes likho.
3. Problem likho → **Submit claim** (Submitted).
4. Buttons se: Under inspection → Approved / Rejected. Reason required.
5. Approved ke baad **Repaired**, **Replacement issued**, ya **Closed**.
6. Replacement: replacement SKU (blank = original product) + serial. Stock **WarrantyReplacement** ledger se nikalta hai. Paid sale nahi banti. Original invoice nahi badalti.
7. Duplicate replacement same claim par block. Expired warranty searchable rehti hai, naya claim date ke bahar nahi.

## 23. Grocery / supermarket billing

1. Settings mein shop type **Grocery** (optional). New Sale par Walk-in auto.
2. Barcode box mein scan / SKU type + Enter. Same barcode dubara scan = quantity +1, stock tak.
3. Customer details grocery par zaroori nahi (Walk-in). Warranty tick hata ke rakho.
4. Quantity **poora number** hai (packet/piece). 1.5 kg decimal is schema mein nahi — 500 g pack ko 1 piece banao, ya alag packed SKU.
5. Payment Full / partial / unpaid. Receipt print pehle jaisa.

Email/address customer par optional; shop ne require nahi kiya to chhod sakte ho. Warranty ke liye mobile zaroori hai.

# BillingSoftware — User guide (Hindi / Hinglish)

Yeh guide shop operator ke liye hai. Har step mein menu, button, fields, aur save ke baad kya check karna hai likha hai.

Demo records ke naam `DEMO` se shuru hote hain. Asli shop data ko demo se mix mat karna.

---

## 1. Login

1. Browser mein app kholo (local: `http://localhost:5173`).
2. **Email** aur **Password** bharo.
3. **Login** dabao (Enter bhi chalega).
4. Dashboard khulega. Header mein aapka email aur role dikhega.

Galat password par “Invalid Email or Password” aayega. Session khatam hone par wapas login page.

**Logout:** sidebar ke neeche **Logout**.

**Password badalna:** header → **Change password** → current + naya (kam se kam 8 letters) + confirm.

---

## 2. Company settings (sirf Admin)

1. Sidebar → **Settings**.
2. Company name, address, phone, email, GSTIN, State, invoice prefix, print format, thermal 80/58 mm, bank details, logo.
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
3. **Current stock** yahan se nahi badlega — opening stock create par, baad mein purchase / adjustment se.
4. **Save**.

Check: list mein naam/SKU. Edit par purani values load honi chahiye.

---

## 5. Excel se hundreds of products

1. **Products** → **Import Excel**.
2. **Download template**.
3. **Products** sheet bharo (Sample sheet sirf example hai — import nahi hota).
4. Columns: ProductName*, SKU*, Category, Brand, Unit, Tax, HSN, PurchasePrice*, SellingPrice*, OpeningStock.
5. File choose karo (.xlsx, max 5 MB / 2000 rows) → **Check file**.
6. Preview: valid / duplicate / error rows.
7. **Confirm import**. Duplicate SKU skip; existing product overwrite nahi.
8. Opening stock stock-ledger se jata hai.

Dobara same file: pehle wale SKU skip, stock double nahi.

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

Amount outstanding se kam likho (jaise 500). Status **Partial**, remaining balance dikhega.

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

Cards asli API se aati hain (aaj ki sales, purchases, customers, low/out of stock). Click karke module khulta hai.

Staff ko purchase links nahi milte. Profit incomplete cost par **Unavailable**.

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

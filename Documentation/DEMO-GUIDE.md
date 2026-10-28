# Demo data

Demo records are labelled `DEMO` so they are obvious in lists and reports. They are **not** created automatically, and they are **disabled in Production**.

## Enable and seed

1. Use a **copy** of the database (for example `BillingSoftware_Verify`), never the live shop database.
2. In User Secrets or environment variables for that API instance:

```
Demo__Enabled=true
```

Do **not** set `Demo:AllowInProduction` except on a disposable production-like copy.

3. Start the API against that copy.
4. Sign in as an **Admin**.
5. Either click **Settings → Seed demo records**, or `POST /api/Demo/seed`.

The call is refused unless `Demo:Enabled` is true. It is safe to repeat. Existing DEMO SKUs and mobile numbers are reused; extra invoices are not created again. Do not run this against the live shop database.

## What is created

| Record | What it shows |
|---|---|
| Category `DEMO - Cables` | Category filter |
| `DEMO-USB-01` (stock) | Normal product |
| `DEMO-HDMI-01` | Second product for purchases/sales |
| `DEMO-LOW-01` | Low-stock badge |
| `DEMO-OUT-01` | Out of stock |
| Supplier `DEMO Wholesale` | Purchase supplier |
| Customers `DEMO - Anita Sharma`, `DEMO - Rahul Verma` | Intra vs inter-state (36 vs 27) |
| A DEMO purchase | Stock in via purchase |
| Paid DEMO sale (Anita) | Full payment + receipt |
| Partial DEMO sale (Rahul) | Outstanding balance |
| Expense `DEMO shop rent` | Expenses and expense report |

## Suggested walk-through

1. Products → Import Excel: download the template. Copy two Sample-style rows onto the **Products** sheet with new SKUs. Upload. Confirm. Check the list.
2. Try the same file again: SKUs are skipped, stock is not doubled.
3. Upload a row with a missing name and a negative price: preview lists Excel row + error; nothing is saved.
4. Suppliers → open DEMO Wholesale. Purchases → New purchase using DEMO products.
5. Customers → DEMO - Anita. Sales → New sale → pay in full → print A4 / Compact / Thermal → open a payment receipt.
6. Record a partial payment on Rahul’s invoice. Void (Admin) and confirm the balance returns.
7. Return one USB from Anita’s invoice. Check inventory ledger.
8. Expenses → DEMO shop rent. Reports → sales / stock / profit (profit uses sale-line cost, not sales minus purchases).
9. Settings (Admin) → logo upload, print toggles.

## Excel test file

Use **Products → Import Excel → Download template**. The Sample sheet is documentation; only the Products sheet is imported.

`Documentation/samples/product-import-test.xlsx` has mixed valid/invalid/duplicate rows for a preview check. Do not import it into the live shop database.

## Reset

There is **no** wipe of invoices. Seed does not delete anything. To start clean, restore a database backup or use a dedicated demo/copy database. Soft-deleting a DEMO product hides it from new sales; existing DEMO invoices stay for history.

## Known limits

- Demo invoices appear in reports of that database. Keep them off the live shop database.
- Returns are not auto-seeded; create one from the invoice page so quantities stay consistent.
- Company logo is not replaced by a demo file.
- `Demo:Enabled` defaults to false in `appsettings.json`.

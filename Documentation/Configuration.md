# Configuration and Secrets

The API reads configuration from (later sources override earlier ones):

1. `appsettings.json` (tracked, **contains no secrets**, only placeholders)
2. `appsettings.{Environment}.json`
3. **User Secrets** (Development environment only)
4. **Environment variables** (all environments)

The API refuses to start if a required secret is missing or too weak. Error
messages name the missing setting but never print secret values.

## Required settings

| Setting | Environment variable | Notes |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `JwtSettings:Key` | `JwtSettings__Key` | JWT signing key, **minimum 32 bytes**; use 64 random bytes (base64) |
| `JwtSettings:Issuer` / `Audience` / `ExpiryMinutes` | `JwtSettings__Issuer` ... | Non-secret, defaults in `appsettings.json` |
| `Cors:Origins` | `Cors__Origins__0`, `Cors__Origins__1`, … | Allowed frontend origins. Development defaults to `http://localhost:5173` and `http://localhost:5177`. **Production refuses to start if this list is empty.** |

## Local development (User Secrets)

Run from the repository root. Secrets are stored outside the repository in
your user profile (`%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>`).

```powershell
$proj = "Backend/BillingSoftware/Billing.API/Billing.API.csproj"

# Connection string (Windows authentication example)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER;Database=BillingSoftwareDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True" --project $proj

# Generate a random 512-bit JWT key and store it (output suppressed on purpose)
$bytes = New-Object byte[] 64
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
dotnet user-secrets set "JwtSettings:Key" ([Convert]::ToBase64String($bytes)) --project $proj *> $null

# List the NAMES of stored secrets (values hidden)
dotnet user-secrets list --project $proj | ForEach-Object { ($_ -split ' = ')[0] }
```

`dotnet user-secrets set` prints the value it stored. Suppress its output (as
above) and do not paste secrets into chat, issues, or logs.

`dotnet ef` commands use the same configuration (Development environment by
default), so migrations pick up the secret connection string automatically.

## File storage (company logo)

| Setting | Environment variable | Notes |
|---|---|---|
| `Storage:Root` | `Storage__Root` | Persistent folder for uploaded logos. Defaults to `App_Data` under the app folder, which **does not survive a deploy that replaces the app directory**. In production set this to a backed-up disk path (or a mounted volume). |

Logos are stored under `{Storage:Root}/logos` with random names. Old files are kept when you replace a logo so issued invoices still resolve their snapshot.

## Demo data

| Setting | Default | Notes |
|---|---|---|
| `Demo:Enabled` | `false` | Must be true to call `POST /api/Demo/seed` |
| `Demo:AllowInProduction` | `false` | Leave false. Seed only on a copy database |

See `Documentation/DEMO-GUIDE.md`.

## Production

Set environment variables on the host (or use your platform's secret store,
such as Azure Key Vault / AWS Secrets Manager / Docker or Kubernetes secrets,
exposed as environment variables):

```
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=<production connection string>
JwtSettings__Key=<unique random key, different from development>
Cors__Origins__0=https://your-frontend-host
Storage__Root=<persistent backed-up folder>
```

- Use a **different** JWT key in each environment.
- Prefer a SQL login with least privilege over `sa`; never commit its password.
- Changing `JwtSettings__Key` invalidates all issued tokens (everyone must log in again).
- Access tokens last `JwtSettings:ExpiryMinutes` (default 60) with a 30-second clock skew. There is **no refresh-token** flow. Do not add one without a new table and an explicit approval; login must keep returning the same JWT.
- The browser stores that JWT in `localStorage` under the key `token`. Any script that can run on the page (XSS) can read it. The API still rejects a missing, expired, or bad token. The UI only hides pages; it is not the security boundary.
- Changing the password does **not** revoke tokens that were already issued. They remain valid until `exp`. Rotating `JwtSettings:Key` does revoke them.
- `ProtectedRoute` and the login page drop a token whose `exp` is missing or in the past, then show the login form. A request that returns 401 also clears the token.
- `POST /api/Auth/login` is limited to **10 attempts per IP per minute** (HTTP 429).
- JWT metadata requires HTTPS outside Development (`RequireHttpsMetadata`).

## Rotating the JWT key

Set a new `JwtSettings:Key` and restart the API. All existing tokens become
invalid immediately. The previous development key was committed to Git history
in the initial commit and must be treated as compromised.

## Git hygiene

`.gitignore` excludes `.env*`, `secrets.json`, `appsettings.*.local.json`,
`appsettings.Production.json`, `*.pfx`, `*.pem` and `*.key`. Secrets that were
committed earlier remain in Git history until it is rewritten; rotating them is
the required mitigation.

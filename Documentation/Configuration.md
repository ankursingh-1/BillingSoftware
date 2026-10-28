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

## Production

Set environment variables on the host (or use your platform's secret store,
such as Azure Key Vault / AWS Secrets Manager / Docker or Kubernetes secrets,
exposed as environment variables):

```
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=<production connection string>
JwtSettings__Key=<unique random key, different from development>
```

- Use a **different** JWT key in each environment.
- Prefer a SQL login with least privilege over `sa`; never commit its password.
- Changing `JwtSettings__Key` invalidates all issued tokens (everyone must log in again).

## Rotating the JWT key

Set a new `JwtSettings:Key` and restart the API. All existing tokens become
invalid immediately. The previous development key was committed to Git history
in the initial commit and must be treated as compromised.

## Git hygiene

`.gitignore` excludes `.env*`, `secrets.json`, `appsettings.*.local.json`,
`appsettings.Production.json`, `*.pfx`, `*.pem` and `*.key`. Secrets that were
committed earlier remain in Git history until it is rewritten; rotating them is
the required mitigation.

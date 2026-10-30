using System.Threading.RateLimiting;
using Billing.API.Extensions;
using Billing.API.Services;
using Billing.API.Services.ProductImport;
using Billing.Application;
using Billing.Application.Interfaces;
using Billing.Infrastructure.Security;
using Billing.Persistence.Context;
using Billing.Persistence.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// -------------------- Required configuration --------------------
// Secrets are NOT stored in appsettings.json. They come from User Secrets
// (Development) or environment variables (Production), e.g.
//   JwtSettings__Key, ConnectionStrings__DefaultConnection
// Fail fast with a clear message (never printing the values themselves).

const int MinimumJwtKeyLength = 32; // HS256 needs at least 256 bits

if (string.IsNullOrWhiteSpace(
        builder.Configuration.GetConnectionString("DefaultConnection")))
{
    throw new InvalidOperationException(
        "Missing configuration 'ConnectionStrings:DefaultConnection'. " +
        "Set it with User Secrets (development) or the " +
        "'ConnectionStrings__DefaultConnection' environment variable (production). " +
        "See Documentation/Configuration.md.");
}

var jwtKey = builder.Configuration["JwtSettings:Key"];

if (string.IsNullOrWhiteSpace(jwtKey) ||
    Encoding.UTF8.GetByteCount(jwtKey) < MinimumJwtKeyLength)
{
    throw new InvalidOperationException(
        $"Missing or too short configuration 'JwtSettings:Key' " +
        $"(minimum {MinimumJwtKeyLength} bytes). Set it with User Secrets " +
        "(development) or the 'JwtSettings__Key' environment variable " +
        "(production). See Documentation/Configuration.md.");
}

foreach (var setting in new[] { "JwtSettings:Issuer", "JwtSettings:Audience" })
{
    if (string.IsNullOrWhiteSpace(builder.Configuration[setting]))
    {
        throw new InvalidOperationException(
            $"Missing configuration '{setting}'.");
    }
}

// -------------------- Services --------------------

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<ProductImportService>();
builder.Services.AddScoped<DemoDataService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<SupplierService>();
builder.Services.AddScoped<PurchaseService>();
builder.Services.AddScoped<SaleService>();
builder.Services.AddScoped<GrossProfitService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<IPdfExportService, PdfExportService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
builder.Services.AddScoped<CompanyService>();
builder.Services.AddScoped<ICompanyService>(sp => sp.GetRequiredService<CompanyService>());
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<IUnitService, UnitService>();
builder.Services.AddScoped<ITaxService, TaxService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<StockLedgerService>();
builder.Services.AddScoped<StockAdjustmentService>();
builder.Services.AddScoped<InventoryQueryService>();
builder.Services.AddSingleton(new Billing.Application.Inventory.StockAlertPolicy(
    builder.Configuration.GetValue("Inventory:LowStockThreshold",
        Billing.Application.Inventory.StockAlertPolicy.DefaultLowStockThreshold)));
builder.Services.AddScoped<PurchaseReturnService>();
builder.Services.AddScoped<SalesReturnService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IWarrantyService, WarrantyService>();
builder.Services.AddScoped<IAppSettingService, AppSettingService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IDocumentNumberService, DocumentNumberService>();
builder.Services.AddScoped<InvoicePdfService>();
builder.Services.AddScoped<ReceiptPdfService>();
builder.Services.AddScoped<CompanyLogoStore>();

var jwt = builder.Configuration.GetSection("JwtSettings");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            ClockSkew = TimeSpan.FromSeconds(30),
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? Array.Empty<string>();
corsOrigins = corsOrigins
    .Where(o => !string.IsNullOrWhiteSpace(o))
    .Select(o => o.Trim())
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

if (corsOrigins.Length == 0)
{
    if (builder.Environment.IsDevelopment())
    {
        corsOrigins = ["http://localhost:5173", "http://localhost:5177"];
    }
    else
    {
        throw new InvalidOperationException(
            "Missing configuration 'Cors:Origins'. Set at least one frontend origin " +
            "with Cors__Origins__0 (production). See Documentation/Configuration.md.");
    }
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Enums are sent as names ("Cash", "Upi") and accepted as names or numbers.
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Billing API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// -------------------- App --------------------

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseGlobalException();
app.MapControllers();
app.Run();
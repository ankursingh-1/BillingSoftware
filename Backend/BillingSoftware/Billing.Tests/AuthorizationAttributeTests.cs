using System.Reflection;
using Billing.API.Controllers;
using Billing.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Billing.Tests;

public class AuthorizationAttributeTests
{
    [Fact]
    public void Product_import_and_export_require_a_signed_in_user()
    {
        Assert.NotNull(typeof(ProductsController).GetCustomAttribute<AuthorizeAttribute>());
        Assert.Contains("import/template", Routes(typeof(ProductsController)));
        Assert.Contains("import/preview", Routes(typeof(ProductsController)));
        Assert.Contains("import", Routes(typeof(ProductsController)));
        Assert.Contains("export", Routes(typeof(ProductsController)));
    }

    [Fact]
    public void Demo_seed_is_admin_only()
    {
        var auth = typeof(DemoController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(auth);
        Assert.Equal(AppRoles.Admin, auth!.Roles);
    }

    [Fact]
    public void Expenses_and_reports_are_admin_only()
    {
        Assert.Equal(AppRoles.Admin, typeof(ExpenseController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal(AppRoles.Admin, typeof(ReportsController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
    }

    [Fact]
    public void Change_password_requires_a_signed_in_user()
    {
        var method = typeof(AuthController).GetMethod(nameof(AuthController.ChangePassword));
        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Contains("change-password", Routes(typeof(AuthController)));
    }

    [Fact]
    public void Company_edits_are_admin_only_and_the_logo_file_is_public_for_print()
    {
        var save = typeof(CompanyController).GetMethod(nameof(CompanyController.Save))!
            .GetCustomAttribute<AuthorizeAttribute>();
        Assert.Equal(AppRoles.Admin, save!.Roles);

        var logo = typeof(CompanyController).GetMethod(nameof(CompanyController.GetLogo))!
            .GetCustomAttribute<AllowAnonymousAttribute>();
        Assert.NotNull(logo);
    }

    [Fact]
    public void Import_service_writes_inside_a_transaction_and_uses_the_ledger()
    {
        var src = File.ReadAllText(Find("ProductImportService.cs"));
        Assert.Contains("BeginTransactionAsync", src);
        Assert.Contains("RollbackAsync", src);
        Assert.Contains("AdjustStockAsync", src);
        Assert.Contains("StockTransactionType.OpeningStock", src);
        Assert.Contains("Existing products are not overwritten", src);
    }

    private static string Find(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var matches = dir.GetFiles(fileName, SearchOption.AllDirectories);
            if (matches.Length > 0)
                return matches[0].FullName;
            dir = dir.Parent;
        }

        throw new FileNotFoundException(fileName);
    }

    private static List<string> Routes(Type controller)
    {
        return controller.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .SelectMany(m => m.GetCustomAttributes<HttpMethodAttribute>())
            .Select(a => a.Template ?? "")
            .ToList();
    }
}

namespace Billing.Tests;

/// <summary>
/// Locks the warranty report kinds the UI sends to the claim report,
/// so a Reports link cannot fall through to the unfiltered register.
/// </summary>
public class WarrantyReportFilterTests
{
    private static readonly string[] ClaimKinds =
    [
        "claims",
        "approved",
        "rejected",
        "replacements",
        "inspection"
    ];

    [Fact]
    public void Claim_kinds_use_the_claim_report_on_the_api_and_in_the_ui()
    {
        var api = File.ReadAllText(Find("WarrantyController.cs"));
        var ui = File.ReadAllText(Find("warrantyService.ts"));
        var page = File.ReadAllText(Find("WarrantiesPage.tsx"));

        Assert.Contains("ClaimReportAsync", api);
        Assert.Contains("loadWarrantyRows", page);

        foreach (var kind in ClaimKinds)
        {
            Assert.Contains($"\"{kind}\"", api);
            Assert.Contains($"\"{kind}\"", ui);
        }
    }

    private static string Find(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            foreach (var root in new[]
            {
                Path.Combine(dir.FullName, "Frontend", "src"),
                Path.Combine(dir.FullName, "Billing.API")
            })
            {
                if (!Directory.Exists(root))
                    continue;

                var matches = Directory.GetFiles(root, fileName, SearchOption.AllDirectories);
                if (matches.Length > 0)
                    return matches[0];
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException(fileName);
    }
}

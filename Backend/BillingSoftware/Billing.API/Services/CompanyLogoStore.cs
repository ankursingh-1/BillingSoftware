using System.Text.RegularExpressions;
using Billing.Persistence.Context;
using Billing.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

/// <summary>
/// Stores the company logo on disk, outside the web root.
///
/// Location: configuration "Storage:Root" (set it to a persistent, backed-up folder in
/// production); defaults to "App_Data" under the application folder, which is fine for
/// development but is NOT persistent on hosts that replace the folder on each deploy.
///
/// Security: only PNG and JPEG (no SVG, which can carry scripts), at most 2 MB, content
/// checked by its first bytes (not just the extension), stored under a random server-made
/// name, and read back only through a strict file-name pattern (no path traversal).
/// </summary>
public class CompanyLogoStore
{
    public const long MaxBytes = 2 * 1024 * 1024;

    private static readonly Regex StoredName =
        new(@"^logo-[0-9a-f]{32}\.(png|jpg)$", RegexOptions.Compiled);

    private readonly string _folder;
    private readonly BillingDbContext _context;

    public CompanyLogoStore(IConfiguration configuration, IHostEnvironment environment, BillingDbContext context)
    {
        var root = configuration["Storage:Root"];

        if (string.IsNullOrWhiteSpace(root))
            root = Path.Combine(environment.ContentRootPath, "App_Data");

        _folder = Path.Combine(Path.GetFullPath(root), "logos");
        _context = context;
    }

    /// <summary>Validates and stores an uploaded logo. Returns the stored file name.</summary>
    public async Task<string> SaveAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new BusinessException("Choose a logo image to upload.");

        if (file.Length > MaxBytes)
            throw new BusinessException("The logo must be 2 MB or smaller.");

        await using var input = file.OpenReadStream();
        var bytes = new byte[file.Length];
        var read = 0;
        while (read < bytes.Length)
        {
            var n = await input.ReadAsync(bytes.AsMemory(read));
            if (n == 0) break;
            read += n;
        }

        var extension = DetectExtension(bytes)
            ?? throw new BusinessException("Only PNG or JPEG images are allowed.");

        Directory.CreateDirectory(_folder);

        var name = $"logo-{Guid.NewGuid():N}.{extension}";
        await File.WriteAllBytesAsync(Path.Combine(_folder, name), bytes);

        return name;
    }

    public async Task<(byte[] Bytes, string ContentType, string FileName)?> ReadCurrentAsync()
    {
        var name = await _context.Companies
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .Select(c => c.LogoPath)
            .FirstOrDefaultAsync();

        if (name == null || !StoredName.IsMatch(name))
            return null;

        var path = Path.Combine(_folder, name);
        if (!File.Exists(path))
            return null;

        var contentType = name.EndsWith(".png", StringComparison.Ordinal) ? "image/png" : "image/jpeg";
        return (await File.ReadAllBytesAsync(path), contentType, name);
    }

    public void Delete(string? storedName)
    {
        if (storedName == null || !StoredName.IsMatch(storedName))
            return;

        var path = Path.Combine(_folder, storedName);
        if (File.Exists(path))
            File.Delete(path);
    }

    private static string? DetectExtension(byte[] b)
    {
        if (b.Length >= 8 &&
            b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47 &&
            b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A)
            return "png";

        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
            return "jpg";

        return null;
    }
}

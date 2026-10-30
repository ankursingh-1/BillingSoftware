using Billing.API.Models;
using Billing.API.Services;
using Billing.API.Services.ProductImport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly ProductService _service;
    private readonly ProductImportService _import;

    public ProductsController(ProductService service, ProductImportService import)
    {
        _service = service;
        _import = import;
    }

    // POST: api/products
    [HttpPost]
    public async Task<IActionResult> Create(ProductDto dto)
    {
        var id = await _service.CreateAsync(dto);

        return Ok(new
        {
            Success = true,
            Message = "Product created successfully.",
            ProductId = id
        });
    }

    // GET: api/products
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var data = await _service.GetAllAsync();

        return Ok(data);
    }

    // GET: api/products/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _service.GetByIdAsync(id);

        if (product == null)
            return NotFound();

        return Ok(product);
    }

    // PUT: api/products/1
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ProductDto dto)
    {
        await _service.UpdateAsync(id, dto);

        return Ok(new
        {
            Success = true,
            Message = "Product updated successfully."
        });
    }

    // DELETE: api/products/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);

        return Ok(new
        {
            Success = true,
            Message = "Product deleted successfully."
        });
    }
    [HttpPost("search")]
    public async Task<IActionResult> Search(ProductSearchRequest request)
    {
        var result = await _service.SearchAsync(request);

        return Ok(result);
    }

    /// <summary>Exact SKU or barcode match for POS scan.</summary>
    [HttpGet("scan")]
    public async Task<IActionResult> Scan([FromQuery] string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest("Scan a barcode or SKU.");

        var product = await _service.GetByCodeAsync(code);
        if (product == null)
            return NotFound(new { message = $"No product matches '{code.Trim()}'." });

        return Ok(product);
    }

    [HttpGet("import/template")]
    public async Task<IActionResult> ImportTemplate()
    {
        var bytes = await _import.TemplateAsync();
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "product-import-template.xlsx");
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export()
    {
        var bytes = await _import.ExportAsync();
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "products.xlsx");
    }

    [HttpPost("import/preview")]
    [RequestSizeLimit(ProductImportWorkbook.MaxFileBytes + 64 * 1024)]
    public async Task<IActionResult> ImportPreview(IFormFile file)
    {
        return Ok(await _import.PreviewAsync(file));
    }

    [HttpPost("import")]
    [RequestSizeLimit(ProductImportWorkbook.MaxFileBytes + 64 * 1024)]
    public async Task<IActionResult> Import(IFormFile file)
    {
        var result = await _import.ImportAsync(file);
        return Ok(result);
    }

    [HttpPost("import/error-report")]
    [RequestSizeLimit(ProductImportWorkbook.MaxFileBytes + 64 * 1024)]
    public async Task<IActionResult> ImportErrorReport(IFormFile file)
    {
        var bytes = await _import.ErrorReportAsync(file);
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "product-import-errors.xlsx");
    }
}
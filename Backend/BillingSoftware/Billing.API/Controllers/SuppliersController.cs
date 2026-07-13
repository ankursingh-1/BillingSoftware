using Billing.API.Models;
using Billing.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SuppliersController : ControllerBase
{
    private readonly SupplierService _service;

    public SuppliersController(SupplierService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(SupplierDto dto)
    {
        var id = await _service.CreateAsync(dto);

        return Ok(new
        {
            Success = true,
            Message = "Supplier created successfully.",
            SupplierId = id
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var supplier = await _service.GetByIdAsync(id);

        if (supplier == null)
            return NotFound();

        return Ok(supplier);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, SupplierDto dto)
    {
        await _service.UpdateAsync(id, dto);

        return Ok(new
        {
            Success = true,
            Message = "Supplier updated successfully."
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);

        return Ok(new
        {
            Success = true,
            Message = "Supplier deleted successfully."
        });
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search(SupplierSearchRequest request)
    {
        return Ok(await _service.SearchAsync(request));
    }
}
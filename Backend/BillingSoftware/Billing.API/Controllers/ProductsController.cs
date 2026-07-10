using Billing.API.Models;
using Billing.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly ProductService _service;

    public ProductsController(ProductService service)
    {
        _service = service;
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
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _service.GetByIdAsync(id);

        if (product == null)
            return NotFound();

        return Ok(product);
    }

    // PUT: api/products/1
    [HttpPut("{id}")]
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
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);

        return Ok(new
        {
            Success = true,
            Message = "Product deleted successfully."
        });
    }
}
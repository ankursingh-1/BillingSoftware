using Billing.API.Models;
using Billing.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly CustomerService _service;

    public CustomersController(CustomerService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CustomerDto dto)
    {
        var id = await _service.CreateAsync(dto);

        return Ok(new
        {
            Success = true,
            Message = "Customer created successfully.",
            CustomerId = id
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
        var customer = await _service.GetByIdAsync(id);

        if (customer == null)
            return NotFound();

        return Ok(customer);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, CustomerDto dto)
    {
        await _service.UpdateAsync(id, dto);

        return Ok(new
        {
            Success = true,
            Message = "Customer updated successfully."
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);

        return Ok(new
        {
            Success = true,
            Message = "Customer deleted successfully."
        });
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search(CustomerSearchRequest request)
    {
        return Ok(await _service.SearchAsync(request));
    }
}
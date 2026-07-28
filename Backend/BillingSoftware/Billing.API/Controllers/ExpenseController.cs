using Billing.Application.DTOs.Expense;
using Billing.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Billing.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExpenseController : ControllerBase
    {
        private readonly IExpenseService _expenseService;

        public ExpenseController(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateExpenseDto dto)
        {
            await _expenseService.CreateAsync(dto);

            return Ok(new
            {
                Success = true,
                Message = "Expense created successfully."
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _expenseService.GetAllAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var expense = await _expenseService.GetByIdAsync(id);

            if (expense == null)
                return NotFound();

            return Ok(expense);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, CreateExpenseDto dto)
        {
            await _expenseService.UpdateAsync(id, dto);

            return Ok(new
            {
                Success = true,
                Message = "Expense updated successfully."
            });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _expenseService.DeleteAsync(id);

            return Ok(new
            {
                Success = true,
                Message = "Expense deleted successfully."
            });
        }
    }
}
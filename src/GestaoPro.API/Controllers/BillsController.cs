using GestaoPro.API.Models;
using GestaoPro.API.DTOs;
using GestaoPro.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestaoPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BillsController : ControllerBase
{
    private readonly IBillService _billService;

    public BillsController(IBillService billService)
    {
        _billService = billService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBillDto dto)
    {
        var result = await _billService.CreateBillAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var bill = await _billService.GetBillByIdAsync(id);
        return bill == null ? NotFound() : Ok(bill);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUserId(string userId)
    {
        var bills = await _billService.GetUserBillsAsync(userId);
        return Ok(bills);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateBillDto dto)
    {
        var bill = await _billService.UpdateBillAsync(id, dto);
        return bill is null ? NotFound() : Ok(bill);
    }
}


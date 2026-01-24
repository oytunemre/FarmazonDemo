using FarmazonDemo.Models.Dto.ListingDto;
using FarmazonDemo.Services.Listings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmazonDemo.Controllers;

[Route("api/listings")]
[ApiController]
public class ListingController : ControllerBase
{
    private readonly IListingService _service;

    public ListingController(IListingService service)
    {
        _service = service;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
        => Ok(await _service.GetByIdAsync(id));

    [HttpPost]
    [Authorize(Roles = "Admin,Seller")]
    public async Task<IActionResult> Create([FromBody] AddListingDto dto) // <-- AddListingDto
        => Ok(await _service.CreateAsync(dto));

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Seller")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateListingDto dto)
        => Ok(await _service.UpdateAsync(id, dto));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Seller")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.SoftDeleteAsync(id);
        return NoContent();
    }
}

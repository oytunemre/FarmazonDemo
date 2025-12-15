using FarmazonDemo.Data;
using FarmazonDemo.Models.Dto.ListingDto;
using FarmazonDemo.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmazonDemo.Controllers
{
    [Route("api/listings")]
    [ApiController]
    public class ListingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public ListingController(ApplicationDbContext context) => _context = context;

        // GET api/listings
        [HttpGet]
        public IActionResult GetAll()
        {
            var listings = _context.Listings
                .Include(l => l.Product)
                .Include(l => l.Seller)
                .ToList();

            return Ok(listings);
        }

        // GET api/listings/5
        [HttpGet("{id:int}")]
        public IActionResult GetById(int id)
        {
            var listing = _context.Listings
                .Include(l => l.Product)
                .Include(l => l.Seller)
                .FirstOrDefault(l => l.ListingId == id);

            return listing is null ? NotFound() : Ok(listing);
        }

        // POST api/listings
        [HttpPost]
        public IActionResult Create([FromBody] CreateListingDto dto)
        {
            if (!_context.Products.Any(p => p.ProductId == dto.ProductId))
                return BadRequest("Product not found.");

            if (!_context.Users.Any(u => u.UserId == dto.SellerId))
                return BadRequest("Seller (User) not found.");

            if (dto.Price <= 0) return BadRequest("Price must be > 0.");
            if (dto.Stock < 0) return BadRequest("Stock must be >= 0.");

            var listing = new Listing
            {
                ProductId = dto.ProductId,
                SellerId = dto.SellerId,
                Price = dto.Price,
                Stock = dto.Stock,
                Condition = dto.Condition ?? "New",
                IsActive = true,
              
            };

            _context.Listings.Add(listing);
            _context.SaveChanges();

            return Ok(listing);
        }

        // PUT api/listings/5
        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] UpdateListingDto dto)
        {
            var listing = _context.Listings.Find(id);
            if (listing is null) return NotFound();

            if (dto.Price <= 0) return BadRequest("Price must be > 0.");
            if (dto.Stock < 0) return BadRequest("Stock must be >= 0.");

            listing.Price = dto.Price;
            listing.Stock = dto.Stock;
            listing.Condition = dto.Condition ?? listing.Condition;
            listing.IsActive = dto.IsActive;
      

            _context.SaveChanges();
            return Ok(listing);
        }

        // DELETE api/listings/5
        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            var listing = _context.Listings.Find(id);
            if (listing is null) return NotFound();

            _context.Listings.Remove(listing);
            _context.SaveChanges();

            return Ok();
        }
    }
}

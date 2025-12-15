using FarmazonDemo.Data;
using FarmazonDemo.Models.Dto.CartDto;
using FarmazonDemo.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmazonDemo.Services.Carts;

public class CartService : ICartService
{
    private readonly ApplicationDbContext _db;

    public CartService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<CartResponseDto> GetCartAsync(int userId)
    {
        var cart = await GetOrCreateCartEntityAsync(userId);
        return MapCart(cart);
    }

    public async Task<CartResponseDto> AddToCartAsync(AddToCartDto dto)
    {
        if (dto.Quantity <= 0)
            throw new InvalidOperationException("Quantity 0 veya negatif olamaz.");

        var listing = await _db.Listings
            .Include(l => l.Product)
            .FirstOrDefaultAsync(l => l.ListingId == dto.ListingId);

        if (listing is null)
            throw new KeyNotFoundException("Listing bulunamadı.");

        if (!listing.IsActive)
            throw new InvalidOperationException("Listing aktif değil.");

        if (listing.Stock < dto.Quantity)
            throw new InvalidOperationException("Yetersiz stok.");

        var cart = await GetOrCreateCartEntityAsync(dto.UserId);

        // Aynı listing sepette var mı?
        var existingItem = cart.Items.FirstOrDefault(i => i.ListingId == dto.ListingId);

        if (existingItem is not null)
        {
            var newQty = existingItem.Quantity + dto.Quantity;

            if (listing.Stock < newQty)
                throw new InvalidOperationException("Sepetteki toplam adet stoğu aşıyor.");

            existingItem.Quantity = newQty;
            // fiyatı değiştirmiyoruz -> UnitPrice snapshot
            existingItem.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                ListingId = listing.ListingId,
                Quantity = dto.Quantity,
                UnitPrice = listing.Price, // snapshot
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // tekrar include’lu çekip döndürelim
        cart = await LoadCartAsync(cart.CartId);
        return MapCart(cart!);
    }

    public async Task<CartResponseDto> UpdateCartItemQuantityAsync(int cartItemId, int quantity)
    {
        var item = await _db.CartItems
            .Include(i => i.Cart)
                .ThenInclude(c => c.Items)
            .FirstOrDefaultAsync(i => i.CartItemId == cartItemId);

        if (item is null)
            throw new KeyNotFoundException("CartItem bulunamadı.");

        if (quantity <= 0)
        {
            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();

            var cartAfterRemove = await LoadCartAsync(item.CartId);
            return MapCart(cartAfterRemove!);
        }

        var listing = await _db.Listings.FirstOrDefaultAsync(l => l.ListingId == item.ListingId);
        if (listing is null)
            throw new KeyNotFoundException("Listing bulunamadı.");

        if (!listing.IsActive)
            throw new InvalidOperationException("Listing aktif değil.");

        if (listing.Stock < quantity)
            throw new InvalidOperationException("Yetersiz stok.");

        item.Quantity = quantity;
        item.UpdatedAt = DateTime.UtcNow;

        item.Cart.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var cart = await LoadCartAsync(item.CartId);
        return MapCart(cart!);
    }

    public async Task<CartResponseDto> RemoveItemAsync(int cartItemId)
    {
        var item = await _db.CartItems.FirstOrDefaultAsync(i => i.CartItemId == cartItemId);
        if (item is null)
            throw new KeyNotFoundException("CartItem bulunamadı.");

        var cartId = item.CartId;

        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync();

        var cart = await LoadCartAsync(cartId);
        return MapCart(cart!);
    }

    // ---------- helpers ----------

    private async Task<Cart> GetOrCreateCartEntityAsync(int userId)
    {
        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is not null) return cart;

        // user var mı kontrol et (istersen kaldırırsın)
        var userExists = await _db.Users.AnyAsync(u => u.UserId == userId);
        if (!userExists)
            throw new KeyNotFoundException("User bulunamadı.");

        cart = new Cart
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Carts.Add(cart);
        await _db.SaveChangesAsync();

        // items include ile dön
        return await _db.Carts.Include(c => c.Items).FirstAsync(c => c.CartId == cart.CartId);
    }

    private Task<Cart?> LoadCartAsync(int cartId)
    {
        return _db.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Listing)
                    .ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(c => c.CartId == cartId);
    }

    private CartResponseDto MapCart(Cart cart)
    {
        // Listing/Product include değilse ürün adını boş dönebilir; LoadCart ile çağırınca dolu olur.
        var items = cart.Items.Select(i => new CartItemResponseDto
        {
            CartItemId = i.CartItemId,
            ListingId = i.ListingId,
            ProductId = i.Listing?.ProductId ?? 0,
            ProductName = i.Listing?.Product?.ProductName ?? "",
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity,
            LineTotal = i.UnitPrice * i.Quantity
        }).ToList();

        return new CartResponseDto
        {
            CartId = cart.CartId,
            UserId = cart.UserId,
            Items = items,
            CartTotal = items.Sum(x => x.LineTotal)
        };
    }
}

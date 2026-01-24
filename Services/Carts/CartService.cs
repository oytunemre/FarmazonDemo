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
        var cart = await _db.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Listing)
                    .ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null)
            return new CartResponseDto
            {
                UserId = userId,
                CartId = 0,
                Items = new(),
                CartTotal = 0
            };

        return MapCart(cart);
    }

    public async Task<CartResponseDto> AddToCartAsync(AddToCartDto dto)
    {
        if (dto.Quantity <= 0)
            throw new InvalidOperationException("Quantity 0 veya negatif olamaz.");

        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var listing = await _db.Listings
                    .Include(l => l.Product)
                    .FirstOrDefaultAsync(l => l.Id == dto.ListingId);

                if (listing is null) throw new KeyNotFoundException("Listing bulunamadı.");
                if (!listing.IsActive) throw new InvalidOperationException("Listing aktif değil.");
                if (listing.Stock < dto.Quantity) throw new InvalidOperationException("Yetersiz stok.");

                var cart = await GetOrCreateCartEntityAsync(dto.UserId);

                var existingItem = await _db.CartItems
                    .FirstOrDefaultAsync(i => i.CartId == cart.Id && i.ListingId == dto.ListingId);

                if (existingItem is not null)
                {
                    var newQty = existingItem.Quantity + dto.Quantity;
                    if (listing.Stock < newQty)
                        throw new InvalidOperationException("Sepetteki toplam adet stoğu aşıyor.");

                    existingItem.Quantity = newQty;
                }
                else
                {
                    _db.CartItems.Add(new CartItem
                    {
                        CartId = cart.Id,
                        ListingId = listing.Id,
                        Quantity = dto.Quantity,
                        UnitPrice = listing.Price
                    });
                }

                try
                {
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateException ex) when (IsUniqueCartItemViolation(ex))
                {
                    var item = await _db.CartItems
                        .FirstAsync(i => i.CartId == cart.Id && i.ListingId == dto.ListingId);

                    var newQty = item.Quantity + dto.Quantity;
                    if (listing.Stock < newQty)
                        throw new InvalidOperationException("Sepetteki toplam adet stoğu aşıyor.");

                    item.Quantity = newQty;
                    await _db.SaveChangesAsync();
                }

                await tx.CommitAsync();

                var cartLoaded = await LoadCartAsync(cart.Id);
                return MapCart(cartLoaded!);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<CartResponseDto> UpdateCartItemQuantityAsync(int userId, int cartItemId, int quantity)
    {
        var item = await _db.CartItems
            .Include(i => i.Cart)
            .FirstOrDefaultAsync(i => i.Id == cartItemId && i.Cart.UserId == userId);

        if (item is null)
            throw new KeyNotFoundException("CartItem bulunamadı.");

        if (quantity <= 0)
        {
            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();

            var cartAfterRemove = await LoadCartAsync(item.CartId);
            return MapCart(cartAfterRemove!);
        }

        var listing = await _db.Listings.FirstOrDefaultAsync(l => l.Id == item.ListingId);
        if (listing is null) throw new KeyNotFoundException("Listing bulunamadı.");
        if (!listing.IsActive) throw new InvalidOperationException("Listing aktif değil.");
        if (listing.Stock < quantity) throw new InvalidOperationException("Yetersiz stok.");

        item.Quantity = quantity;

        await _db.SaveChangesAsync();

        var cart = await LoadCartAsync(item.CartId);
        return MapCart(cart!);
    }

    public async Task<CartResponseDto> RemoveItemAsync(int userId, int cartItemId)
    {
        var item = await _db.CartItems
            .Include(i => i.Cart)
            .FirstOrDefaultAsync(i => i.Id == cartItemId && i.Cart.UserId == userId);

        if (item is null)
            throw new KeyNotFoundException("CartItem bulunamadı.");

        var cartId = item.CartId;

        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync();

        var cart = await LoadCartAsync(cartId);
        return MapCart(cart!);
    }

    private async Task<Cart> GetOrCreateCartEntityAsync(int userId)
    {
        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is not null) return cart;

        var userExists = await _db.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
            throw new KeyNotFoundException("User bulunamadı.");

        cart = new Cart { UserId = userId };

        _db.Carts.Add(cart);
        await _db.SaveChangesAsync();

        return await _db.Carts.Include(c => c.Items).FirstAsync(c => c.Id == cart.Id);
    }

    private Task<Cart?> LoadCartAsync(int cartId)
    {
        return _db.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Listing)
                    .ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(c => c.Id == cartId);
    }

    private CartResponseDto MapCart(Cart cart)
    {
        var items = cart.Items.Select(i => new CartItemResponseDto
        {
            CartItemId = i.Id,
            ListingId = i.ListingId,
            ProductId = i.Listing?.ProductId ?? 0,
            ProductName = i.Listing?.Product?.ProductName ?? "",
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity,
            LineTotal = i.UnitPrice * i.Quantity
        }).ToList();

        return new CartResponseDto
        {
            CartId = cart.Id,
            UserId = cart.UserId,
            Items = items,
            CartTotal = items.Sum(x => x.LineTotal)
        };
    }

    private static bool IsUniqueCartItemViolation(DbUpdateException ex)
    {
        if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx)
            return sqlEx.Number is 2601 or 2627;

        return false;
    }
}

using FarmazonDemo.Data;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Services.Carts;
using FarmazonDemo.Services.Carts;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddScoped<ICartService, CartService>();


static async Task SeedAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // DB migrate (istersen kapatabilirsin)
    await db.Database.MigrateAsync();

    // Zaten veri varsa seed basma
    if (await db.Users.AnyAsync()) return;

    var now = DateTime.UtcNow;

    var user = new Users
    {
        Name = "Ahmet Yýlmaz",
        Email = "ahmet.yilmaz@test.com",
        Password = "123456",
        Username = "ahmety",
        CreatedAt = now,
        UpdatedAt = now
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    var product = new Product
    {
        ProductName = "Logitech MX Master 3S",
        ProductDescription = "Kablosuz ergonomik mouse.",
        ProductBarcode = "LOGI-MX3S",
        CreatedAt = now,
        UpdatedAt = now
    };

    db.Products.Add(product);
    await db.SaveChangesAsync();

    var listing = new Listing
    {
        ProductId = product.ProductId,
        SellerId = user.UserId,
        Price = 3999.90m,
        Stock = 10,
        Condition = "New",
        IsActive = true,
        CreatedAt = now,
        UpdatedAt = now
    };

    db.Listings.Add(listing);
    await db.SaveChangesAsync();
}


var app = builder.Build();


await SeedAsync(app);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();



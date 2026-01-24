using FarmazonDemo.Data;
using FarmazonDemo.Services.Carts;
using FarmazonDemo.Services.Common;
using FarmazonDemo.Services.Listings;
using FarmazonDemo.Services.Products;
using FarmazonDemo.Services.Users;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using FarmazonDemo.Services.Orders;
using FarmazonDemo.Services.Payments;
using FarmazonDemo.Services.Shipments;



var builder = WebApplication.CreateBuilder(args);

// --------------------
// SERVICES (Build'den �NCE)
// --------------------

// Controllers + Enum String Converter
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// OpenAPI/Swagger (senin AddOpenApi kullan�m�)
builder.Services.AddOpenApi();

// DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// DI - Services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IListingService, ListingService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IShipmentService, ShipmentService>();



// --------------------
// BUILD
// --------------------
var app = builder.Build();

// --------------------
// PIPELINE
// --------------------
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// --------------------
// SEED (Build'den sonra, Run'dan �nce)
// --------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    // await DbSeeder.SeedAsync(db);
}

app.Run();

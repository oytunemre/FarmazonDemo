# FarmazonDemo

Marketplace e-commerce platform built with ASP.NET Core 10, Entity Framework Core, and SQL Server.

## Features

- **Authentication & Authorization**: JWT-based authentication with role-based access control (Admin, Seller, Customer)
- **User Management**: User registration, login, and profile management
- **Product Catalog**: Product and barcode management with soft delete support
- **Marketplace Listings**: Sellers can create listings with pricing and stock management
- **Shopping Cart**: User-specific shopping cart with real-time stock validation
- **Order Management**: Multi-seller order processing with automatic order splitting
- **Payment Processing**: Flexible payment methods (Cash on Delivery, Bank Transfer, Manual Card)
- **Shipment Tracking**: Carrier integration with status tracking and timeline
- **Health Checks**: Built-in health monitoring endpoints

## Tech Stack

- **Framework**: ASP.NET Core 10.0
- **Database**: SQL Server with Entity Framework Core 10.0
- **Authentication**: JWT Bearer Token
- **Validation**: FluentValidation 11.3.1
- **API Documentation**: OpenAPI/Swagger
- **Password Hashing**: BCrypt.Net

## Architecture

- Clean/Layered Architecture
- Repository Pattern via Services
- Soft Delete Pattern
- Transaction Support with Retry Strategies
- Global Exception Handling Middleware

## Getting Started

### Prerequisites

- .NET 10.0 SDK
- SQL Server (LocalDB, Express, or Full)
- Visual Studio 2022 / VS Code / Rider

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd FarmazonDemo
   ```

2. **Configure Database Connection**

   Update the connection string in `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_SERVER;Database=FarmazonDemodb;..."
   }
   ```

3. **Configure JWT Settings**

   Update JWT settings in `appsettings.json`:
   ```json
   "JwtSettings": {
     "SecretKey": "YourSuperSecretKey_MinimumLength32Characters!",
     "Issuer": "FarmazonDemo",
     "Audience": "FarmazonDemoUsers",
     "ExpiryMinutes": 1440
   }
   ```

4. **Apply Database Migrations**
   ```bash
   dotnet ef database update
   ```

5. **Run the Application**
   ```bash
   dotnet run
   ```

6. **Access Swagger UI**

   Navigate to: `https://localhost:7193/openapi/v1.json`

### Environment Variables (Production)

For production deployment, use environment variables instead of hardcoded values:

```bash
# Copy the example file
cp .env.example .env

# Edit .env with your production values
```

Required environment variables:
- `ConnectionStrings__DefaultConnection`: Database connection string
- `JwtSettings__SecretKey`: JWT secret key (min 32 characters)
- `Cors__AllowedOrigins__0`: Allowed frontend origins

## API Endpoints

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login with credentials
- `GET /api/auth/me` - Get current user info (authenticated)

### Users (Admin only)
- `GET /api/user` - List all users
- `GET /api/user/{id}` - Get user by ID
- `POST /api/user` - Create user
- `PUT /api/user/{id}` - Update user
- `DELETE /api/user/{id}` - Soft delete user

### Products (Public read, Admin/Seller write)
- `GET /api/product` - List all products
- `GET /api/product/{id}` - Get product by ID
- `POST /api/product` - Create product (Admin/Seller)
- `PUT /api/product/{id}` - Update product (Admin/Seller)
- `DELETE /api/product/{id}` - Delete product (Admin/Seller)

### Listings (Public read, Admin/Seller write)
- `GET /api/listings` - List all listings
- `GET /api/listings/{id}` - Get listing by ID
- `POST /api/listings` - Create listing (Admin/Seller)
- `PUT /api/listings/{id}` - Update listing (Admin/Seller)
- `DELETE /api/listings/{id}` - Delete listing (Admin/Seller)

### Cart (Authenticated)
- `GET /api/cart/{userId}` - Get user's cart
- `POST /api/cart/add` - Add item to cart
- `PUT /api/cart/{userId}/item/{cartItemId}` - Update item quantity
- `DELETE /api/cart/{userId}/item/{cartItemId}` - Remove item

### Orders (Authenticated)
- `POST /api/orders/checkout` - Checkout cart
- `GET /api/orders/{orderId}` - Get order details
- `GET /api/orders/user/{userId}` - Get user's orders

### Payments (Authenticated)
- `POST /api/payments/intents` - Create payment intent
- `GET /api/payments/order/{orderId}` - Get payment by order
- `POST /api/payments/{id}/mark-received` - Mark payment received
- `POST /api/payments/{id}/simulate-success` - Simulate success (testing)
- `POST /api/payments/{id}/simulate-fail` - Simulate failure (testing)

### Shipments (Admin/Seller)
- `GET /api/shipments/seller-orders/{id}` - Get shipment details
- `PATCH /api/shipments/seller-orders/{id}/ship` - Mark as shipped
- `PATCH /api/shipments/seller-orders/{id}/deliver` - Mark as delivered
- `GET /api/shipments/seller-orders/{id}/timeline` - Get timeline

### Seller (Admin/Seller)
- `GET /api/seller/{sellerId}/orders` - Get seller's orders
- `PATCH /api/seller/orders/{id}/ship` - Ship order
- `PATCH /api/seller/orders/{id}/deliver` - Mark delivered

### Health Checks
- `GET /health` - Application health status
- `GET /health/ready` - Readiness probe

## User Roles

- **Customer** (0): Default role for registered users
- **Seller** (1): Can manage products, listings, and orders
- **Admin** (2): Full access to all endpoints

## Security Features

- JWT Bearer Token Authentication
- Password Hashing with BCrypt
- Role-Based Authorization
- CORS Policy Configuration
- HTTPS Enforcement
- Soft Delete Pattern (data never truly deleted)
- Global Exception Handling

## Database Schema

Key entities:
- **Users**: User accounts with roles
- **Product**: Product catalog with barcodes
- **Listing**: Seller's marketplace listings
- **Cart**: Shopping cart with items
- **Order**: Customer orders with seller order splitting
- **SellerOrder**: Individual seller's portion of an order
- **PaymentIntent**: Payment processing records
- **Shipment**: Shipment tracking information

## Development

### Database Seeding

Uncomment the seeder in `Program.cs` to populate test data:
```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DbSeeder.SeedAsync(db);  // Uncomment this line
}
```

### Creating Migrations

```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

## License

MIT License

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

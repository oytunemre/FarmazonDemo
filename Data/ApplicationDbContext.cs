using FarmazonDemo.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FarmazonDemo.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Users> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductBarcode> ProductBarcodes { get; set; }
        public DbSet<Listing> Listings { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<SellerOrder> SellerOrders { get; set; }
        public DbSet<SellerOrderItem> SellerOrderItems { get; set; }
        public DbSet<PaymentIntent> PaymentIntents => Set<PaymentIntent>();
        public DbSet<PaymentEvent> PaymentEvents => Set<PaymentEvent>();
        public DbSet<Shipment> Shipments => Set<Shipment>();
        public DbSet<ShipmentEvent> ShipmentEvents => Set<ShipmentEvent>();
        public DbSet<RefreshToken> RefreshTokens { get; set; }





        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Soft delete global query filter (BaseEntity olan her şeye) ---
            ApplySoftDeleteQueryFilters(modelBuilder);

            // Cart -> User
            modelBuilder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // CartItem -> Cart
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            // CartItem -> Listing
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Listing)
                .WithMany()
                .HasForeignKey(ci => ci.ListingId)
                .OnDelete(DeleteBehavior.Restrict);

            // CartItem unique (soft delete ile çakışmaması için FILTER)
            modelBuilder.Entity<CartItem>()
                .HasIndex(ci => new { ci.CartId, ci.ListingId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            // ProductBarcode -> Product
            modelBuilder.Entity<ProductBarcode>()
                .HasOne(pb => pb.Product)
                .WithMany(p => p.Barcodes)
                .HasForeignKey(pb => pb.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Barcode unique (soft delete ile çakışmaması için FILTER)
            modelBuilder.Entity<ProductBarcode>()
                .HasIndex(pb => pb.Barcode)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
            // Listing Condition enum to string
            modelBuilder.Entity<Listing>()
               .Property(l => l.Condition)
               .HasConversion<string>()
                  .HasMaxLength(30);

            // Order -> Buyer
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Buyer)
                .WithMany()
                .HasForeignKey(o => o.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .Property(o => o.Status)
                .HasConversion<string>()
                .HasMaxLength(30);

            // Order -> SellerOrders
            modelBuilder.Entity<SellerOrder>()
                .HasOne(so => so.Order)
                .WithMany(o => o.SellerOrders)
                .HasForeignKey(so => so.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SellerOrder>()
                .HasOne(so => so.Seller)
                .WithMany()
                .HasForeignKey(so => so.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SellerOrder>()
                .Property(so => so.Status)
                .HasConversion<string>()
                .HasMaxLength(30);

            // SellerOrderItem -> SellerOrder
            modelBuilder.Entity<SellerOrderItem>()
                .HasOne(i => i.SellerOrder)
                .WithMany(so => so.Items)
                .HasForeignKey(i => i.SellerOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // SellerOrderItem -> Listing
            modelBuilder.Entity<SellerOrderItem>()
                .HasOne(i => i.Listing)
                .WithMany()
                .HasForeignKey(i => i.ListingId)
                .OnDelete(DeleteBehavior.Restrict);

            // PAYMENT
            modelBuilder.Entity<PaymentIntent>(b =>
            {
                b.HasKey(x => x.Id);

                b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
                b.Property(x => x.Currency).HasMaxLength(3);

                b.Property(x => x.Provider).HasMaxLength(50);
                b.Property(x => x.ExternalReference).HasMaxLength(100);
                b.Property(x => x.FailureReason).HasMaxLength(500);

                b.HasOne(x => x.Order)
                    .WithMany()
                    .HasForeignKey(x => x.OrderId)
                    .OnDelete(DeleteBehavior.Restrict);

                // 1 order = 1 intent (MVP)
                b.HasIndex(x => x.OrderId).IsUnique();
            });

            modelBuilder.Entity<PaymentEvent>(b =>
            {
                b.HasKey(x => x.Id);

                b.HasOne(x => x.PaymentIntent)
                    .WithMany()
                    .HasForeignKey(x => x.PaymentIntentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });


            // SHIPMENT
            modelBuilder.Entity<Shipment>(b =>
            {
                b.HasKey(x => x.Id);

                b.Property(x => x.Carrier).HasMaxLength(50);
                b.Property(x => x.TrackingNumber).HasMaxLength(80);

                b.HasOne(x => x.SellerOrder)
                    .WithOne(x => x.Shipment)
                    .HasForeignKey<Shipment>(x => x.SellerOrderId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => x.SellerOrderId).IsUnique();
                b.HasIndex(x => x.TrackingNumber);

                b.HasQueryFilter(x => !x.IsDeleted);
            });

            modelBuilder.Entity<ShipmentEvent>(b =>
            {
                b.HasKey(x => x.ShipmentEventId);

                b.HasOne(x => x.Shipment)
                    .WithMany()
                    .HasForeignKey(x => x.ShipmentId)
                    .OnDelete(DeleteBehavior.Cascade);

                // ✅ Matching filter: Shipment gizlenirse event de gizlensin
                b.HasQueryFilter(e => !e.Shipment.IsDeleted);
            });

            // RefreshToken -> User
            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

        }

        public override int SaveChanges()
        {
            var now = DateTime.UtcNow;
            TouchCartsIfCartItemsChanged(now);
            ApplyAuditAndSoftDelete(now);
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            TouchCartsIfCartItemsChanged(now);
            ApplyAuditAndSoftDelete(now);
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyAuditAndSoftDelete(DateTime now)
        {
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Property(x => x.CreatedAt).IsModified = false;
                    entry.Entity.UpdatedAt = now;
                }
                else if (entry.State == EntityState.Deleted)
                {
                    // Hard delete'i soft delete'e çevir
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedDate ??= now;
                    entry.Entity.UpdatedAt = now;

                    entry.Property(x => x.CreatedAt).IsModified = false;
                }
            }
        }

        private void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                    continue;

                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var isDeletedProp = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                var body = Expression.Equal(isDeletedProp, Expression.Constant(false));
                var lambda = Expression.Lambda(body, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }

        private void TouchCartsIfCartItemsChanged(DateTime now)
        {
            var cartIds = ChangeTracker.Entries<CartItem>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
                .Select(e => e.Entity.CartId)
                .Distinct()
                .ToList();

            foreach (var cartId in cartIds)
            {
                // Eğer aynı Cart zaten track ediliyorsa, tekrar Attach etme!
                var trackedCartEntry = ChangeTracker.Entries<Cart>()
                    .FirstOrDefault(e => e.Entity.Id == cartId);

                if (trackedCartEntry != null)
                {
                    trackedCartEntry.Entity.UpdatedAt = now;
                    trackedCartEntry.Property(x => x.UpdatedAt).IsModified = true;
                    trackedCartEntry.Property(x => x.CreatedAt).IsModified = false;
                    continue;
                }

                // Track edilmiyorsa stub attach et
                var cart = new Cart { Id = cartId };
                Carts.Attach(cart);

                Entry(cart).Property(x => x.UpdatedAt).CurrentValue = now;
                Entry(cart).Property(x => x.UpdatedAt).IsModified = true;
                Entry(cart).Property(x => x.CreatedAt).IsModified = false;
            }
        }

    } 
}

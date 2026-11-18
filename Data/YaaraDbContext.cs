using Microsoft.EntityFrameworkCore;
using YaaraFramesGifts.Models;

namespace YaaraFramesGifts.Data;

public sealed class YaaraDbContext(DbContextOptions<YaaraDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(category => category.Slug);

            entity.Property(category => category.Slug)
                .HasMaxLength(64)
                .IsRequired();
            entity.Property(category => category.Name)
                .HasMaxLength(120)
                .IsRequired();
            entity.Property(category => category.Description)
                .HasMaxLength(300);
            entity.Property(category => category.Icon)
                .HasMaxLength(32);
            entity.Property(category => category.AccentColor)
                .HasMaxLength(16);

            entity.HasMany(category => category.Products)
                .WithOne(product => product.Category)
                .HasForeignKey(product => product.CategorySlug)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(product => product.Id);

            entity.Property(product => product.Id)
                .UseIdentityColumn();
            entity.Property(product => product.Name)
                .HasMaxLength(160)
                .IsRequired();
            entity.Property(product => product.Description)
                .HasMaxLength(1000)
                .IsRequired();
            entity.Property(product => product.Price)
                .HasPrecision(18, 2)
                .IsRequired();
            entity.Property(product => product.ImageUrl)
                .HasMaxLength(500)
                .IsRequired();
            entity.Property(product => product.CategorySlug)
                .HasMaxLength(64)
                .IsRequired();
            entity.Property(product => product.Badge)
                .HasMaxLength(32);
            entity.Property(product => product.IsPersonalized)
                .IsRequired();

            entity.HasIndex(product => product.CategorySlug);
            entity.HasIndex(product => product.Badge);

            entity.HasMany(product => product.CartItems)
                .WithOne(cartItem => cartItem.Product)
                .HasForeignKey(cartItem => cartItem.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.ToTable("Carts");
            entity.HasKey(cart => cart.Id);

            entity.Property(cart => cart.Id)
                .ValueGeneratedNever();
            entity.Property(cart => cart.CreatedAtUtc)
                .HasColumnType("datetime2")
                .IsRequired();
            entity.Property(cart => cart.UpdatedAtUtc)
                .HasColumnType("datetime2")
                .IsRequired();

            entity.HasMany(cart => cart.Items)
                .WithOne(cartItem => cartItem.Cart)
                .HasForeignKey(cartItem => cartItem.CartId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("CartItems", table =>
                table.HasCheckConstraint(
                    "CK_CartItems_Quantity_Range",
                    "[Quantity] >= 1 AND [Quantity] <= 99"));
            entity.HasKey(cartItem => cartItem.Id);

            entity.Property(cartItem => cartItem.Id)
                .UseIdentityColumn();
            entity.Property(cartItem => cartItem.CartId)
                .IsRequired();
            entity.Property(cartItem => cartItem.ProductId)
                .IsRequired();
            entity.Property(cartItem => cartItem.Quantity)
                .IsRequired();

            entity.HasIndex(cartItem => new { cartItem.CartId, cartItem.ProductId })
                .IsUnique();
        });
    }
}

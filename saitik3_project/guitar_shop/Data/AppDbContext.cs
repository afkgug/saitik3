using Microsoft.EntityFrameworkCore;
using guitar_shop.Models;

namespace guitar_shop.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Таблицы
    public DbSet<Guitar> Guitars { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<User> Users { get; set; }
    
    // ДОБАВЛЕНО: Таблица для корзины авторизованных пользователей
    public DbSet<CartItem> CartItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Настройка связи User -> CartItems (Один ко многим)
        modelBuilder.Entity<CartItem>()
            .HasOne(c => c.User)
            .WithMany() // Если у User нет свойства List<CartItem>, оставляем пустым или добавляем в модель User
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade); // При удалении пользователя удаляем его корзину

        // Настройка связи Order -> OrderItems
        modelBuilder.Entity<OrderItem>()
            .HasOne<Order>()
            .WithMany(o => o.Items)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Настройка связи User -> Orders
        modelBuilder.Entity<Order>()
            .HasOne<User>()
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
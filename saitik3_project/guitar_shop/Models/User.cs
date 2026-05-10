using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace guitar_shop.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Username { get; set; }

    [Required]
    public string Email { get; set; }

    [Required]
    public string PasswordHash { get; set; }

    public string? FullName { get; set; }
    public string? DeliveryAddress { get; set; }
    
    // Удалены: IsConfirmed, ConfirmationToken
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Роль пользователя (Customer или Admin)
    public string Role { get; set; } = "Customer";

    // Навигационное свойство для заказов
    public virtual ICollection<Order>? Orders { get; set; }
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using guitar_shop.Data;
using guitar_shop.Models;

namespace guitar_shop.Controllers;

public class OrderController : Controller
{
    private readonly AppDbContext _db;

    public OrderController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        ViewData["Title"] = "Оформление заказа";

        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            // Сохраняем текущий URL, чтобы вернуться сюда после входа
            HttpContext.Session.SetString("ReturnUrlAfterAuth", "/Order/Checkout");
            return View("RequireAuth");
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
        if (user == null)
        {
            HttpContext.Session.Clear();
            return View("RequireAuth");
        }

        var cartItems = await _db.CartItems
            .Where(c => c.UserId == userId.Value)
            .ToListAsync();

        if (!cartItems.Any())
        {
            return RedirectToAction("Index", "Cart");
        }

        
        var model = new OrderViewModel
        {
            FullName = user.FullName ?? user.Username,
            Email = user.Email,
            Address = user.DeliveryAddress ?? string.Empty,
            TotalPrice = cartItems.Sum(i => i.Price * i.Quantity),
            Items = cartItems.Select(i => new OrderItem 
            { 
                ProductName = i.Name, 
                Price = i.Price, 
                Quantity = i.Quantity 
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Checkout(OrderViewModel model)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth", new { returnUrl = "/Order/Checkout" });
        }

        if (!ModelState.IsValid)
        {
            var cartItems = await _db.CartItems.Where(c => c.UserId == userId.Value).ToListAsync();
            model.TotalPrice = cartItems.Sum(i => i.Price * i.Quantity);
            model.Items = cartItems.Select(i => new OrderItem 
            { 
                ProductName = i.Name, 
                Price = i.Price, 
                Quantity = i.Quantity 
            }).ToList();
            return View(model);
        }

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var cartItems = await _db.CartItems.Where(c => c.UserId == userId.Value).ToListAsync();
            if (!cartItems.Any()) return RedirectToAction("Index", "Cart");

            // Проверяем наличие товаров на складе
            foreach (var item in cartItems)
            {
                var guitar = await _db.Guitars.FirstOrDefaultAsync(g => g.Name == item.Name);
                if (guitar == null || guitar.StockQuantity < item.Quantity)
                {
                    TempData["Error"] = $"Товар \"{item.Name}\" отсутствует на складе или его количество недостаточно.";
                    return RedirectToAction("Index", "Cart");
                }
            }

            // Вычисляем номер заказа для этого пользователя
            var userOrderCount = await _db.Orders.Where(o => o.UserId == userId.Value).CountAsync();
            var newUserOrderNumber = userOrderCount + 1;

            var order = new Order
            {
                UserId = userId.Value,
                UserOrderNumber = newUserOrderNumber,
                FullName = model.FullName,
                Email = model.Email,
                Address = model.Address,
                TotalPrice = model.TotalPrice,
                CreatedAt = DateTime.UtcNow,
                Status = "New"
            };

            foreach (var item in cartItems)
            {
                order.Items.Add(new OrderItem
                {
                    ProductName = item.Name,
                    Price = item.Price,
                    Quantity = item.Quantity
                });

                // Уменьшаем количество на складе
                var guitar = await _db.Guitars.FirstOrDefaultAsync(g => g.Name == item.Name);
                if (guitar != null)
                {
                    guitar.StockQuantity -= item.Quantity;
                }
            }

            _db.Orders.Add(order);
            _db.CartItems.RemoveRange(cartItems);
            
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = "Заказ успешно оформлен!";
            return RedirectToAction("Index", "Profile");
        }
        catch
        {
            await transaction.RollbackAsync();
            ViewBag.Error = "Ошибка при оформлении заказа. Попробуйте позже.";
            return View(model);
        }
    }
}
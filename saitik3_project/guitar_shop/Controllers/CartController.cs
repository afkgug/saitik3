using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using guitar_shop.Data;
using guitar_shop.Models;

namespace guitar_shop.Controllers;

public class CartController : Controller
{
    private readonly AppDbContext _db;

    public CartController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Корзина";
        
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            // Для гостей показываем корзину из сессии (если есть)
            var guestCart = HttpContext.Session.GetString("GuestCart");
            if (string.IsNullOrEmpty(guestCart))
            {
                return View(new List<CartItem>());
            }
            
            var guestItems = System.Text.Json.JsonSerializer.Deserialize<List<CartItem>>(guestCart);
            return View(guestItems ?? new List<CartItem>());
        }

        var cartItems = await _db.CartItems
            .Where(c => c.UserId == userId.Value)
            .ToListAsync();

        return View(cartItems);
    }

    [HttpPost]
    public async Task<IActionResult> Add(int id)
    {
        var guitar = await _db.Guitars.FindAsync(id);
        if (guitar == null) return NotFound();

        var userId = HttpContext.Session.GetInt32("UserId");
        
        if (!userId.HasValue)
        {
            // Гость - сохраняем в сессию и показываем предупреждение
            var guestCart = HttpContext.Session.GetString("GuestCart");
            var guestItems = string.IsNullOrEmpty(guestCart) 
                ? new List<CartItem>() 
                : System.Text.Json.JsonSerializer.Deserialize<List<CartItem>>(guestCart);
            
            var existingItem = guestItems?.FirstOrDefault(i => i.Name == guitar.Name);
            if (existingItem != null)
            {
                existingItem.Quantity++;
            }
            else
            {
                guestItems?.Add(new CartItem
                {
                    Name = guitar.Name,
                    Price = guitar.Price,
                    Quantity = 1,
                    Image = guitar.Image
                });
            }
            
            HttpContext.Session.SetString("GuestCart", System.Text.Json.JsonSerializer.Serialize(guestItems));
            
            // Показываем модальное окно с предложением авторизоваться
            TempData["ShowAuthModal"] = "true";
            TempData["ReturnUrl"] = "/Shop";
            
            return RedirectToAction("Index", "Shop");
        }

        // Авторизованный пользователь
        var cartItem = await _db.CartItems.FirstOrDefaultAsync(c => c.UserId == userId && c.Name == guitar.Name);
        if (cartItem != null)
        {
            cartItem.Quantity++;
        }
        else
        {
            _db.CartItems.Add(new CartItem
            {
                UserId = userId.Value,
                Name = guitar.Name,
                Price = guitar.Price,
                Quantity = 1,
                Image = guitar.Image
            });
        }

        await _db.SaveChangesAsync();
        return RedirectToAction("Index", "Shop");
    }

    [HttpPost]
    public async Task<IActionResult> Remove(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            // Для гостей удаляем из сессии
            var guestCart = HttpContext.Session.GetString("GuestCart");
            if (!string.IsNullOrEmpty(guestCart))
            {
                var guestItems = System.Text.Json.JsonSerializer.Deserialize<List<CartItem>>(guestCart);
                var itemToRemove = guestItems?.FirstOrDefault(i => i.Id == id);
                if (itemToRemove != null)
                {
                    guestItems?.Remove(itemToRemove);
                    HttpContext.Session.SetString("GuestCart", System.Text.Json.JsonSerializer.Serialize(guestItems));
                }
            }
            return RedirectToAction("Index");
        }

        var cartItem = await _db.CartItems.FindAsync(id);
        if (cartItem != null && cartItem.UserId == userId)
        {
            _db.CartItems.Remove(cartItem);
            await _db.SaveChangesAsync();
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> UpdateQuantity(int id, int quantity)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return Unauthorized();

        var cartItem = await _db.CartItems.FindAsync(id);
        if (cartItem != null && cartItem.UserId == userId)
        {
            if (quantity <= 0)
            {
                _db.CartItems.Remove(cartItem);
            }
            else
            {
                cartItem.Quantity = quantity;
            }
            await _db.SaveChangesAsync();
        }

        return RedirectToAction("Index");
    }

    // Метод для переноса корзины гостя после авторизации
    public async Task<IActionResult> MergeGuestCart()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return Json(new { success = false });

        var guestCart = HttpContext.Session.GetString("GuestCart");
        if (string.IsNullOrEmpty(guestCart))
        {
            return Json(new { success = true, message = "Корзина гостя пуста" });
        }

        var guestItems = System.Text.Json.JsonSerializer.Deserialize<List<CartItem>>(guestCart);
        if (guestItems == null || !guestItems.Any())
        {
            return Json(new { success = true, message = "Корзина гостя пуста" });
        }

        foreach (var item in guestItems)
        {
            var existingItem = await _db.CartItems.FirstOrDefaultAsync(c => c.UserId == userId && c.Name == item.Name);
            if (existingItem != null)
            {
                existingItem.Quantity += item.Quantity;
            }
            else
            {
                _db.CartItems.Add(new CartItem
                {
                    UserId = userId.Value,
                    Name = item.Name,
                    Price = item.Price,
                    Quantity = item.Quantity,
                    Image = item.Image
                });
            }
        }

        await _db.SaveChangesAsync();
        HttpContext.Session.Remove("GuestCart");

        return Json(new { success = true, message = "Товары добавлены в корзину" });
    }
}

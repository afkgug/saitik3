using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using guitar_shop.Models;
using guitar_shop.Services;
using guitar_shop.Data;

namespace guitar_shop.Controllers;

public class CartController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private const string CartSessionKey = "CartItems";

    public CartController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<IActionResult> Index()
    {
        var cart = await GetCartItemsAsync();
        ViewData["Title"] = "Корзина";
        return View(cart);
    }

    [HttpPost]
    public async Task<IActionResult> Add(int id)
    {
        var guitar = GuitarService.GetById(id, _env);
        if (guitar == null) return RedirectToAction(nameof(ShopController.Index), "Shop");

        var userId = HttpContext.Session.GetInt32("UserId");

        if (userId.HasValue)
        {
            // Авторизованный пользователь: сохраняем в БД
            var existingItem = await _db.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId.Value && c.Name == guitar.Name);

            if (existingItem != null)
            {
                existingItem.Quantity++;
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
        }
        else
        {
            // Гость: сохраняем в сессию
            var cart = GetSessionCart();
            var existing = cart.FirstOrDefault(i => i.Id == id);
            if (existing != null)
            {
                existing.Quantity++;
            }
            else
            {
                cart.Add(new CartItem 
                { 
                    Id = id, 
                    Name = guitar.Name, 
                    Price = guitar.Price, 
                    Quantity = 1,
                    Image = guitar.Image // Добавляем изображение
                });
            }
            SaveSessionCart(cart);
        }

        return RedirectToAction(nameof(ShopController.Index), "Shop");
    }

    [HttpPost]
    public async Task<IActionResult> Remove(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");

        if (userId.HasValue)
        {
            var item = await _db.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId.Value && c.Id == id);
            
            if (item != null)
            {
                _db.CartItems.Remove(item);
                await _db.SaveChangesAsync();
            }
        }
        else
        {
            var cart = GetSessionCart();
            var item = cart.FirstOrDefault(x => x.Id == id);
            if (item != null)
            {
                cart.Remove(item);
                SaveSessionCart(cart);
            }
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Clear()
    {
        var userId = HttpContext.Session.GetInt32("UserId");

        if (userId.HasValue)
        {
            var items = await _db.CartItems.Where(c => c.UserId == userId.Value).ToListAsync();
            _db.CartItems.RemoveRange(items);
            await _db.SaveChangesAsync();
        }
        else
        {
            HttpContext.Session.Remove(CartSessionKey);
        }

        return RedirectToAction(nameof(Index));
    }

    // Вспомогательный метод для получения корзины (БД или Сессия)
    private async Task<List<CartItem>> GetCartItemsAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId.HasValue)
        {
            return await _db.CartItems.Where(c => c.UserId == userId.Value).ToListAsync();
        }
        return GetSessionCart();
    }

    private List<CartItem> GetSessionCart()
    {
        var json = HttpContext.Session.GetString(CartSessionKey);
        return json != null ? JsonSerializer.Deserialize<List<CartItem>>(json) : new List<CartItem>();
    }

    private void SaveSessionCart(List<CartItem> items)
    {
        HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(items));
    }
}
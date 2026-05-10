using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using guitar_shop.Data;
using guitar_shop.Models;

namespace guitar_shop.Controllers;

public class AdminController : Controller
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db)
    {
        _db = db;
    }

    // Проверка, является ли пользователь администратором
    private bool IsAdmin()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return false;
        
        var user = _db.Users.Find(userId.Value);
        return user != null && user.Role == "Admin";
    }

    public async Task<IActionResult> Index()
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Auth");
        }

        ViewData["Title"] = "Панель администратора";
        
        var users = await _db.Users
            .Include(u => u.Orders)
                .ThenInclude(o => o.Items)
            .Include(u => u.CartItems)
            .ToListAsync();
        
        var guitars = await _db.Guitars.ToListAsync();
        
        var model = new AdminViewModel
        {
            Users = users,
            Guitars = guitars
        };

        return View(model);
    }

    // Управление гитарами - добавление
    [HttpGet]
    public IActionResult AddGuitar()
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Auth");
        ViewData["Title"] = "Добавить гитару";
        return View(new Guitar());
    }

    [HttpPost]
    public async Task<IActionResult> AddGuitar(Guitar model)
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Auth");
        
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        _db.Guitars.Add(model);
        await _db.SaveChangesAsync();
        
        TempData["Success"] = "Гитара успешно добавлена";
        return RedirectToAction("Index");
    }

    // Управление гитарами - удаление
    [HttpPost]
    public async Task<IActionResult> DeleteGuitar(int id)
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Auth");
        
        var guitar = await _db.Guitars.FindAsync(id);
        if (guitar != null)
        {
            _db.Guitars.Remove(guitar);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Гитара успешно удалена";
        }
        
        return RedirectToAction("Index");
    }

    // Управление гитарами - редактирование количества на складе
    [HttpPost]
    public async Task<IActionResult> UpdateStock(int id, int stockQuantity)
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Auth");
        
        var guitar = await _db.Guitars.FindAsync(id);
        if (guitar != null)
        {
            guitar.StockQuantity = stockQuantity;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Количество на складе обновлено";
        }
        
        return RedirectToAction("Index");
    }

    // Просмотр заказов всех пользователей
    public async Task<IActionResult> Orders()
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Auth");
        
        var orders = await _db.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
        
        return View(orders);
    }

    // Просмотр корзин всех пользователей
    public async Task<IActionResult> Carts()
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Auth");
        
        var cartItems = await _db.CartItems
            .Include(c => c.User)
            .ToListAsync();
        
        return View(cartItems);
    }
}

// Модель для представления админ-панели
public class AdminViewModel
{
    public List<User> Users { get; set; } = new();
    public List<Guitar> Guitars { get; set; } = new();
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using guitar_shop.Data;
using guitar_shop.Models;

namespace guitar_shop.Controllers;

public class ProfileController : Controller
{
    private readonly AppDbContext _db;

    public ProfileController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Профиль";

        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth", new { returnUrl = "/Profile" });
        }

        var user = await _db.Users
            .Include(u => u.Orders)
                .ThenInclude(o => o.Items)
            .FirstOrDefaultAsync(u => u.Id == userId.Value);

        if (user == null)
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }

        var cartItems = await _db.CartItems
            .Where(c => c.UserId == userId.Value)
            .ToListAsync();

        var model = new ProfileViewModel
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName ?? user.Username,
            DeliveryAddress = user.DeliveryAddress,
            CartItems = cartItems,
            Orders = user.Orders.OrderByDescending(o => o.CreatedAt).ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        ViewData["Title"] = "Редактировать профиль";

        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth", new { returnUrl = "/Profile/Edit" });
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
        if (user == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        var model = new ProfileViewModel
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName ?? user.Username,
            DeliveryAddress = user.DeliveryAddress
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return RedirectToAction("Login", "Auth");

        var user = await _db.Users.FindAsync(userId.Value);
        if (user == null) return NotFound();

        user.FullName = model.FullName;
        user.DeliveryAddress = model.DeliveryAddress;
        
        await _db.SaveChangesAsync();
        
        HttpContext.Session.SetString("UserName", user.FullName ?? user.Username);

        TempData["Success"] = "Профиль успешно обновлен";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    public async Task<IActionResult> GetCartPartial()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return PartialView("_CartPartial", new List<CartItem>());

        var cartItems = await _db.CartItems
            .Where(c => c.UserId == userId.Value)
            .ToListAsync();
        
        return PartialView("_CartPartial", cartItems);
    }
}
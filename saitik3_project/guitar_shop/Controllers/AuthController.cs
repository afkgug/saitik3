using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using guitar_shop.Data;
using guitar_shop.Models;

namespace guitar_shop.Controllers;

public class AuthController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AppDbContext db, ILogger<AuthController> logger)
    {
        _db = db;
        _logger = logger;
    }
    
    public IActionResult Register(string? returnUrl)
    {
        ViewData["Title"] = "Регистрация";
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl)
    {
        if (!ModelState.IsValid)
        {
            TempData["Email"] = model.Email;
            TempData["FullName"] = model.FullName;
            TempData["DeliveryAddress"] = model.DeliveryAddress;
            
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                ViewBag.Error = error.ErrorMessage;
                break;
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
        if (existingUser != null)
        {
            TempData["Email"] = model.Email;
            TempData["FullName"] = model.FullName;
            TempData["DeliveryAddress"] = model.DeliveryAddress;
            ViewBag.Error = "Пользователь с таким email уже существует";
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var user = new User
        {
            Email = model.Email,
            Username = model.FullName,
            FullName = model.FullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            DeliveryAddress = model.DeliveryAddress,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Автоматический вход после регистрации
        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("UserEmail", user.Email);
        HttpContext.Session.SetString("UserName", user.FullName);

        _logger.LogInformation($"Пользователь {user.Email} зарегистрировался и вошел в систему");

        // Логика перенаправления
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Profile");
    }

    public IActionResult Login(string? returnUrl)
    {
        ViewData["Title"] = "Вход";
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl)
    {
        if (!ModelState.IsValid)
        {
            TempData["Email"] = model.Email;
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

        // Проверка на администратора по специальным учетным данным (login: admin, password: admin123)
        if (model.Email == "admin" && model.Password == "admin123")
        {
            // Проверяем, существует ли админ в базе, если нет - создаем
            var adminUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == "admin@admin.com");
            if (adminUser == null)
            {
                adminUser = new User
                {
                    Email = "admin@admin.com",
                    Username = "Admin",
                    FullName = "Администратор",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    Role = "Admin",
                    CreatedAt = DateTime.UtcNow
                };
                _db.Users.Add(adminUser);
                await _db.SaveChangesAsync();
            }
            else if (adminUser.Role != "Admin")
            {
                adminUser.Role = "Admin";
                await _db.SaveChangesAsync();
            }

            HttpContext.Session.SetInt32("UserId", adminUser.Id);
            HttpContext.Session.SetString("UserEmail", adminUser.Email);
            HttpContext.Session.SetString("UserName", adminUser.FullName ?? adminUser.Username);
            HttpContext.Session.SetString("UserRole", adminUser.Role);

            _logger.LogInformation($"Администратор {adminUser.Email} вошел в систему");

            return RedirectToAction("Index", "Admin");
        }

        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        {
            TempData["Email"] = model.Email;
            ViewBag.Error = "Неверный email или пароль";
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("UserEmail", user.Email);
        HttpContext.Session.SetString("UserName", user.FullName ?? user.Username);
        HttpContext.Session.SetString("UserRole", user.Role ?? "Customer");

        _logger.LogInformation($"Пользователь {user.Email} вошел в систему");

        // Логика перенаправления
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        // Если администратор входит через обычную форму входа
        if (user.Role == "Admin")
        {
            return RedirectToAction("Index", "Admin");
        }

        return RedirectToAction("Index", "Profile");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    public async Task<IActionResult> CheckEmail(string email)
    {
        var exists = await _db.Users.AnyAsync(u => u.Email == email);
        return Json(new { isUnique = !exists });
    }
}
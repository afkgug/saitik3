using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using guitar_shop.Data;
using guitar_shop.Models;
using guitar_shop.Services;

namespace guitar_shop.Controllers;

public class LoginController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILogger<LoginController> _logger;

    public LoginController(AppDbContext db, ILogger<LoginController> logger)
    {
        _db = db;
        _logger = logger;
    }

    public IActionResult Index()
    {
        ViewData["Title"] = "Вход";
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Index(string email, string password)
    {
        // Сохраняем email для восстановления формы при ошибке
        TempData["Email"] = email;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Введите email и пароль";
            return View();
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            ViewBag.Error = "Пользователь не найден";
            return View();
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            ViewBag.Error = "Неверный пароль";
            return View();
        }


        // Устанавливаем сессию
        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("UserEmail", user.Email);
        HttpContext.Session.SetString("UserName", user.FullName ?? user.Username);

        _logger.LogInformation($"Пользователь {user.Email} вошел в систему");

        return RedirectToAction("Index", "Home");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}
using System.ComponentModel.DataAnnotations;

namespace guitar_shop.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Логин или email обязателен")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

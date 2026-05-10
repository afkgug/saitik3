using System.ComponentModel.DataAnnotations;

namespace guitar_shop.Models
{
    public class OrderViewModel
    {
        [Required(ErrorMessage = "Введите ФИО")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите Email")]
        [EmailAddress(ErrorMessage = "Некорректный Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите адрес доставки")]
        public string Address { get; set; } = string.Empty;

        public decimal TotalPrice { get; set; }
        
        public List<OrderItem> Items { get; set; } = new();
    }
}
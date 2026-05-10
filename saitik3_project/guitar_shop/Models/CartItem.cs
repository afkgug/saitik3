using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace guitar_shop.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public decimal Price { get; set; }
        public int Quantity { get; set; }

        public int? UserId { get; set; } 
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
        public string? Image { get; set; } 
    }
}
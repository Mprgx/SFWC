using System.ComponentModel.DataAnnotations;

namespace MobyPark.Models
{
    public class PaymentsDto
    {
        [Required]
        public string Transaction { get; set; } = string.Empty;

        [Required]
        public decimal Amount { get; set; } = 0m;
    }
}
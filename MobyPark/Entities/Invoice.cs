using System.ComponentModel.DataAnnotations;

namespace MobyPark.Entities
{
    public class Invoice
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid? CompanyId { get; set; }
        public Company Company { get; set; }

        [Required]
        public DateTimeOffset DateRequested { get; set; }

        [Required]
        public Guid UserId { get; set; }
        public User User { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public string PaymentStatus { get; set; }

        [Required]
        public int Month { get; set; }

        [Required]
        public int Year { get; set; }

    }

}

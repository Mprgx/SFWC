using System.ComponentModel.DataAnnotations;

using MobyPark.Entities;

namespace MobyPark.Models
{
    public class InvoiceReadDto
    {

        [Required]
        public Guid InvoiceId { get; set; }

        [Required]
        public string Location { get; set; }

        [Required]
        public string Username { get; set; }

        public string? CompanyName { get; set; } = null;

        [Required]
        public string LicensePlate { get; set; }

        [Required]
        public DateTimeOffset Date { get; set; }

        [Required]
        public DateTimeOffset Started { get; set; }

        [Required]
        public DateTimeOffset Stopped { get; set; }

        [Required]
        public string Duration { get; set; }

        [Required]
        public decimal Rate { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public string DiscountCode { get; set; }

        [Required]
        public decimal PriceAfterDiscount { get; set; }

        [Required]
        public string PaymentStatus { get; set; }

        [Required]
        public string PaymentMethod { get; set; }

    }

    public class BillingInvoiceRowDto
    {
        public string Location { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string LicensePlate { get; set; } = default!;
        public DateTimeOffset Started { get; set; }
        public DateTimeOffset Stopped { get; set; }
        public decimal Cost { get; set; }
        public string? DiscountCode { get; set; }
        public decimal DiscountedCost { get; set; }
    }


}

using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace MobyPark.Models
{
    public class PaymentInitiationDto
    {
        public string Transaction { get; init; } = "";
        public decimal Amount { get; init; }
        public string Validation { get; init; } = "";
    }

    public class PaymentReadDto
    {
        public string Transaction { get; init; } = "";
        public decimal Amount { get; init; }

        public string DiscountCode { get; set; } = "";
        public decimal AmountWithDiscount { get; set; }

        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? Completed { get; init; }

        public string Status { get; init; } = "";

        public Guid SessionId { get; init; }
        public int ParkingLotId { get; init; }
        public string ParkingLotName { get; init; } = "";

        public PaymentSessionSummaryDto? Session { get; init; }
    }

    public class PaymentSessionSummaryDto
    {
        public Guid Id { get; init; }
        public string LicensePlate { get; init; } = "";
        public DateTimeOffset Started { get; init; }
        public DateTimeOffset? Stopped { get; init; }
        public int DurationMinutes { get; init; }
        public decimal Cost { get; init; }
        public decimal DiscountedCost { get; set; }
        public string PaymentStatus { get; init; } = "";
    }

    public class PaymentsDto
    {
        [Required]
        public string Transaction { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; } = 0m;

        public string DiscountCode { get; set; } = "";
    }

    public class PaymentValidationDto
    {
        [Required]
        public string Validation { get; set; } = "";
        [Required]
        public JsonElement T_Data { get; set; }
    }
}

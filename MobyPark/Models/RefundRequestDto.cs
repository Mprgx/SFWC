using System.ComponentModel.DataAnnotations;

namespace MobyPark.Models
{
    public class RefundRequestDto
    {
        [Required(ErrorMessage = "IBAN is required for refund processing.")]
        [RegularExpression(@"^[A-Z]{2}[0-9]{2}[A-Z0-9]{1,30}$",
            ErrorMessage = "Invalid IBAN format. Example: NL91ABNA0417164300")]
        public string IBAN { get; set; } = "NL91ABNA0417164300";

        [MaxLength(250)]
        public string? Reason { get; set; }
    }
}

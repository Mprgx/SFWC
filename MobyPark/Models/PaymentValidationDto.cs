using System.Text.Json;

namespace MobyPark.Models
{
    public class PaymentValidationDto
    {
        public string Validation { get; set; }

        // Exact hetzelfde object als in jouw JSON
        public JsonElement T_Data { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
namespace MobyPark.Models
{
    public class SessionStopDto
    {
        [Required]
        public string LicensePlate { get; set; } = string.Empty;
    }
}
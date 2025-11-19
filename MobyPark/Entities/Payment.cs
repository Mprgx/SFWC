using Microsoft.EntityFrameworkCore;

namespace MobyPark.Entities
{
    public class Payment
    {
        public Guid Id { get; set; }
        public User User { get; set; } = null!;
        public Guid Initiator { get; set; }
        public string Transaction { get; set; } = string.Empty;

        [Precision(18, 2)]
        public decimal Amount { get; set; }
        public bool Completed { get; set; }
        public string Hash { get; set; } = string.Empty;
    }
}

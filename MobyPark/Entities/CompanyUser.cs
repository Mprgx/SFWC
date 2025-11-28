using System.ComponentModel.DataAnnotations;

namespace MobyPark.Entities
{
    public class CompanyUser
    {
        public Guid UserId { get; set; }
        public User? User { get; set; }

        public Guid CompanyId { get; set; }
        public Company? Company { get; set; }

    }
}
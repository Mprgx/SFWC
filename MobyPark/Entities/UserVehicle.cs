using System.ComponentModel.DataAnnotations;

namespace MobyPark.Entities
{
    public class UserVehicle
    {
        public Guid UserId { get; set; }
        public User? User { get; set; }

        public int VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }

    }
}
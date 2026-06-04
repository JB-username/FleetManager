using Microsoft.AspNetCore.Identity;

namespace FleetManager.Models
{
    public class ApplicationUser: IdentityUser
    {
        public string FullName { get; set; } = "";

        public int? VehicleId { get; set; }

        public Vehicle? Vehicle { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

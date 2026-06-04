namespace FleetManager.Models
{
    public class Vehicle
    {
        public int Id { get; set; }

        public string RegistrationNumber { get; set; } = "";

        public bool IsActive { get; set; } = true;
    }
}

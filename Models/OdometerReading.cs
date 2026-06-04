namespace FleetManager.Models
{
    public class OdometerReading
    {

        public int Id { get; set; }

        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; } = null!;

        public string UserId { get; set; } = "";
        public ApplicationUser User { get; set; } = null!;

        public int KilometerReading { get; set; }

        public string ImagePath { get; set; } = "";

        public DateTime CapturedAt { get; set; }

    }
}

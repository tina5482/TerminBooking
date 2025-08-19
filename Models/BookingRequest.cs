namespace TerminBooking.Models
{
    public class BookingRequest
    {
        public List<int> AppointmentIds { get; set; } = new();
        public int ServiceId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }
}

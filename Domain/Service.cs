namespace TerminBooking.Domain;
public class Service
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public int DurationMin { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;

    public int? StaffId { get; set; }
    public Staff? Staff { get; set; }

}

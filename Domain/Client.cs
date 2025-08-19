namespace TerminBooking.Domain;
public class Client
{
    public int Id { get; set; }
    public string FullName { get; set; } = default!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public string? Tags { get; set; }
}


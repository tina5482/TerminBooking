namespace TerminBooking.Domain;

public class Staff
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;     // npr. Bella, Petra
    public string? Skills { get; set; }              // opis: šminka/obrve/...
    public bool IsActive { get; set; } = true;
    public string? ColorHex { get; set; }            // za kalendar (opcionalno)
}

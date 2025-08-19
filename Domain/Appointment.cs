namespace TerminBooking.Domain;
public class Appointment
{
    public int Id { get; set; }

    public int? ClientId { get; set; }
    public Client? Client { get; set; }

    public int? ServiceId { get; set; }   // može biti null kad je Free
    public Service? Service { get; set; }

    public int? StaffId { get; set; }
    public Staff? Staff { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Free;
    public string? Notes { get; set; }

    public string? CreatedByUserId { get; set; } // Identity user id
}


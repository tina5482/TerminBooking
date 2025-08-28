using System.ComponentModel.DataAnnotations;

namespace TerminBooking.Domain;

public enum ContactStatus
{
    New = 0,
    Replied = 1,
    Closed = 2
}

public class ContactMessage
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string FullName { get; set; } = default!;

    [MaxLength(256), EmailAddress]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(200)]
    public string? Subject { get; set; }

    [Required, MaxLength(4000)]
    public string Body { get; set; } = default!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ContactStatus Status { get; set; } = ContactStatus.New;

    [MaxLength(2000)]
    public string? InternalNotes { get; set; }
}

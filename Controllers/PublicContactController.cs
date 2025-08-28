using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using TerminBooking.Data;
using TerminBooking.Domain;
using System.ComponentModel.DataAnnotations;

namespace TerminBooking.Controllers;

[ApiController]
[Route("api/contact")]
[EnableCors("PublicSite")] // koristi postojeću CORS politiku
public class PublicContactController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public PublicContactController(ApplicationDbContext db) => _db = db;

    public class ContactDto
    {
        [Required, MaxLength(150)]
        public string FullName { get; set; } = default!;
        [EmailAddress, MaxLength(256)]
        public string? Email { get; set; }
        [MaxLength(50)]
        public string? Phone { get; set; }
        [MaxLength(200)]
        public string? Subject { get; set; }
        [Required, MaxLength(4000)]
        public string Body { get; set; } = default!;
    }

    [HttpPost]
    [Produces("application/json")]
    public async Task<IActionResult> Post([FromBody] ContactDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var msg = new ContactMessage
        {
            FullName = dto.FullName.Trim(),
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email!.Trim(),
            Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone!.Trim(),
            Subject = string.IsNullOrWhiteSpace(dto.Subject) ? null : dto.Subject!.Trim(),
            Body = dto.Body.Trim(),
            Status = ContactStatus.New
        };

        _db.ContactMessages.Add(msg);
        await _db.SaveChangesAsync();

        // (opcionalno) ovdje poslati mail notifikaciju salonu

        return Ok(new { ok = true, id = msg.Id });
    }
}

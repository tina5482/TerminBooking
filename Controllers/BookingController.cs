using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TerminBooking.Data;
using TerminBooking.Domain;

namespace TerminBooking.Controllers;

[ApiController]
[Route("api")]
public class BookingController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public BookingController(ApplicationDbContext db) => _db = db;

    // ===== Konstante / postavke =====
    private const int LeadTimeMinutes = 30;      // minimalno koliko unaprijed se smije rezervirati
    private static readonly TimeOnly WorkStart = new(9, 0);
    private static readonly TimeOnly WorkEnd = new(17, 0);
    private const int SlotMinutes = 30;

    // ------------ DTOs ------------
    public class BookingRequest
    {
        [Required] public List<int> AppointmentIds { get; set; } = new();
        [Required] public int ServiceId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Notes { get; set; }
    }

    public class BookByStartRequest
    {
        public int StaffId { get; set; }
        public string Date { get; set; } = default!;      // "yyyy-MM-dd"
        public string StartTime { get; set; } = default!; // "HH:mm"
        public int ServiceId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }

    // ------------ Endpoints ------------

    // GET /api/staff
    [HttpGet("staff")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStaff()
        => Ok(await _db.Staff
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new { id = s.Id, name = s.Name })
            .ToListAsync());

    // GET /api/services?staffId=1
    [HttpGet("services")]
    [AllowAnonymous]
    public async Task<IActionResult> GetServices([FromQuery] int staffId)
        => Ok(await _db.Services
            .Where(x => x.StaffId == staffId && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new { id = x.Id, name = x.Name, durationMin = x.DurationMin })
            .ToListAsync());

    // GET /api/slots?staffId=1&date=yyyy-MM-dd
    [HttpGet("slots")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSlots([FromQuery] int staffId, [FromQuery] string date)
    {
        if (staffId <= 0) return BadRequest("Invalid staffId.");
        if (string.IsNullOrWhiteSpace(date)) return BadRequest("Missing date (yyyy-MM-dd).");
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out var d))
            return BadRequest("Invalid date format. Use yyyy-MM-dd.");

        // Vikend -> nema termina
        var dayOfWeek = d.ToDateTime(TimeOnly.MinValue).DayOfWeek;
        if (dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return Ok(Array.Empty<object>());

        var now = DateTime.Now;

        // Prošli dan -> nema slotova
        if (d.ToDateTime(TimeOnly.MinValue).Date < now.Date)
            return Ok(Array.Empty<object>());

        // Radno vrijeme 09–17, 30-min korak
        var workStart = d.ToDateTime(WorkStart);
        var workEnd = d.ToDateTime(WorkEnd);

        // Učitavanje bookiranih u okviru dana (radi kolizija)
        var dayStart = d.ToDateTime(TimeOnly.MinValue);
        var dayEnd = d.ToDateTime(new TimeOnly(23, 59, 59));

        var booked = await _db.Appointments
            .Where(a => a.StaffId == staffId
                     && a.Status == AppointmentStatus.Booked
                     && a.Start < dayEnd && a.End > dayStart)
            .Select(a => new { a.Start, a.End })
            .ToListAsync();

        static bool Overlaps(DateTime s1, DateTime e1, DateTime s2, DateTime e2)
            => s1 < e2 && s2 < e1;

        // Minimalni dopušteni start: danas je now + lead, ostalim danima nema reza
        DateTime minAllowedStart =
            d.ToDateTime(TimeOnly.MinValue).Date == now.Date
            ? now.AddMinutes(LeadTimeMinutes)
            : DateTime.MinValue;

        var freeBlocks = new List<object>();
        for (var cur = workStart; cur < workEnd; cur = cur.AddMinutes(SlotMinutes))
        {
            var nxt = cur.AddMinutes(SlotMinutes);

            // odbaci prošlost / lead-time za današnji datum
            if (cur < minAllowedStart) continue;

            // odbaci ako se preklapa s bookiranim
            var overlaps = booked.Any(b => Overlaps(cur, nxt, b.Start, b.End));
            if (!overlaps)
            {
                freeBlocks.Add(new { start = cur.ToString("yyyy-MM-ddTHH:mm:ss") });
            }
        }

        return Ok(freeBlocks);
    }

    // POST /api/book-by-start
    [HttpPost("book-by-start")]
    [AllowAnonymous]
    public async Task<IActionResult> BookByStart([FromBody] BookByStartRequest req)
    {
        if (req.StaffId <= 0) return BadRequest("Invalid staff.");
        if (!DateOnly.TryParseExact(req.Date, "yyyy-MM-dd", out var d))
            return BadRequest("Invalid date.");
        if (!TimeOnly.TryParse(req.StartTime, out var t))
            return BadRequest("Invalid time.");

        var service = await _db.Services
            .FirstOrDefaultAsync(s => s.Id == req.ServiceId && s.IsActive);
        if (service == null) return BadRequest("Invalid service.");
        if (service.StaffId != req.StaffId)
            return BadRequest("Service does not belong to selected staff.");

        var startDt = d.ToDateTime(t);
        var endDt = startDt.AddMinutes(service.DurationMin);

        // Lead-time zaštita: zabrani prošlost i < 30 min unaprijed
        var now = DateTime.Now;
        if (startDt < now.AddMinutes(LeadTimeMinutes))
            return BadRequest("Preblizu je početku ili u prošlosti.");

        // Provjera preklapanja
        var overlaps = await _db.Appointments.AnyAsync(a =>
            a.StaffId == req.StaffId &&
            a.Status == AppointmentStatus.Booked &&
            a.Start < endDt && startDt < a.End);

        if (overlaps) return Conflict("Slot je upravo zauzet.");

        // Upsert klijenta
        Client? client = null;
        var email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim();
        var phone = string.IsNullOrWhiteSpace(req.Phone) ? null : req.Phone.Trim();
        var name = string.IsNullOrWhiteSpace(req.FullName) ? null : req.FullName.Trim();

        if (!string.IsNullOrWhiteSpace(email))
            client = await _db.Clients.FirstOrDefaultAsync(c => c.Email == email);
        if (client is null && !string.IsNullOrWhiteSpace(phone))
            client = await _db.Clients.FirstOrDefaultAsync(c => c.Phone == phone);
        if (client is null && name is not null)
            client = await _db.Clients.FirstOrDefaultAsync(c => c.FullName == name && c.Email == email && c.Phone == phone);

        if (client is null)
        {
            client = new Client { FullName = name ?? "", Email = email, Phone = phone };
            _db.Clients.Add(client);
            await _db.SaveChangesAsync();
        }
        else
        {
            // dopuni prazna polja, ako treba
            if (string.IsNullOrWhiteSpace(client.FullName) && !string.IsNullOrWhiteSpace(name)) client.FullName = name!;
            if (string.IsNullOrWhiteSpace(client.Email) && !string.IsNullOrWhiteSpace(email)) client.Email = email!;
            if (string.IsNullOrWhiteSpace(client.Phone) && !string.IsNullOrWhiteSpace(phone)) client.Phone = phone!;
            await _db.SaveChangesAsync();
        }

        var appt = new Appointment
        {
            StaffId = req.StaffId,
            ServiceId = req.ServiceId,
            Start = startDt,
            End = endDt,
            Status = AppointmentStatus.Booked,
            Notes = null,
            ClientId = client.Id
        };

        _db.Appointments.Add(appt);
        await _db.SaveChangesAsync();

        return Ok(new { id = appt.Id, start = appt.Start, end = appt.End });
    }

    // dijagnostika
    [HttpGet("staff/diag")]
    [AllowAnonymous]
    public IActionResult StaffDiag()
    {
        var cn = _db.Database.GetDbConnection();
        return Ok(new
        {
            server = cn.DataSource,
            database = cn.Database,
            staffCount = _db.Staff.Count(),
            servicesCount = _db.Services.Count(),
            appointmentsCount = _db.Appointments.Count()
        });
    }
}

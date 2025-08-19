using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TerminBooking.Data;
using TerminBooking.Domain;
using TerminBooking.Services;
using System.ComponentModel.DataAnnotations;

namespace TerminBooking.Controllers;

[ApiController]
[Route("api")]
public class BookingController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public BookingController(ApplicationDbContext db) => _db = db;

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

    // ------------ Endpoints ------------

    // GET /api/staff
    [HttpGet("staff")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStaff()
        => Ok(await _db.Staff
            .Where(s => s.IsActive)
            .Select(s => new { id = s.Id, name = s.Name, colorHex = s.ColorHex })
            .OrderBy(s => s.name)
            .ToListAsync());

    // GET /api/services?staffId=1
    [HttpGet("services")]
    [AllowAnonymous]
    public async Task<IActionResult> GetServices([FromQuery] int staffId)
        => Ok(await _db.Services
            .Where(x => x.StaffId == staffId && x.IsActive)
            .Select(x => new { id = x.Id, name = x.Name, durationMin = x.DurationMin, price = x.Price })
            .OrderBy(x => x.name)
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

        var dayStart = d.ToDateTime(TimeOnly.MinValue);
        var dayEnd = d.ToDateTime(new TimeOnly(23, 59, 59));

        // Učitaj SVE termine za dan/djelatnika
        var all = await _db.Appointments
            .Where(a => a.StaffId == staffId && a.Start >= dayStart && a.End <= dayEnd)
            .OrderBy(a => a.Start)
            .AsNoTracking()
            .ToListAsync();

        // Booked intervali
        var booked = all.Where(a => a.Status == TerminBooking.Domain.AppointmentStatus.Booked).ToList();

        // Free slotovi koji se NE preklapaju ni s jednim booked intervalom
        bool Overlaps(Appointment free, Appointment b)
            => free.Start < b.End && b.Start < free.End;

        var freeNonOverlapping = all
            .Where(a => a.Status == TerminBooking.Domain.AppointmentStatus.Free)
            .Where(free => !booked.Any(b => Overlaps(free, b)))
            .Select(a => new
            {
                appointmentId = a.Id,
                start = a.Start,  // frontend formatira
                end = a.End
            })
            .ToList();

        return Ok(freeNonOverlapping);
    }


    public class BookByStartRequest
    {
        public int StaffId { get; set; }
        public string Date { get; set; } = default!;  // "yyyy-MM-dd"
        public string StartTime { get; set; } = default!; // "HH:mm"
        public int ServiceId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }

    [HttpPost("book-by-start")]
    [AllowAnonymous]
    public async Task<IActionResult> BookByStart([FromBody] BookByStartRequest req)
    {
        if (req.StaffId <= 0) return BadRequest("Invalid staff.");
        if (!DateOnly.TryParseExact(req.Date, "yyyy-MM-dd", out var d))
            return BadRequest("Invalid date.");
        if (!TimeOnly.TryParse(req.StartTime, out var t))
            return BadRequest("Invalid time.");

        var service = await _db.Services.FirstOrDefaultAsync(s => s.Id == req.ServiceId && s.IsActive);
        if (service == null) return BadRequest("Invalid service.");

        if (service.StaffId != req.StaffId)
            return BadRequest("Service does not belong to selected staff.");

        var need = (int)Math.Ceiling(service.DurationMin / 30.0); // koliko 30-min slotova treba

        var startDt = d.ToDateTime(t);
        var dayEnd = d.ToDateTime(new TimeOnly(23, 59, 59));
        var lastEnd = startDt.AddMinutes(need * 30);
        if (lastEnd > dayEnd) return Conflict("Out of working hours.");

        // Učitaj kandidatske slotove iz baze (samo 30-min)
        var candidates = await _db.Appointments
            .Where(a => a.StaffId == req.StaffId
                     && a.Status == AppointmentStatus.Free
                     && a.Start >= startDt
                     && a.End <= lastEnd
                     && EF.Functions.DateDiffMinute(a.Start, a.End) == 30)
            .OrderBy(a => a.Start)
            .ToListAsync();

        // Formiraj lanac uzastopnih 30-min od točno startDt duljine "need"
        var chain = new List<Appointment>();
        var cur = startDt;
        for (int i = 0; i < need; i++)
        {
            var slot = candidates.FirstOrDefault(x => x.Start == cur);
            if (slot == null) return Conflict("Selected start time is no longer available.");
            chain.Add(slot);
            cur = cur.AddMinutes(30);
        }

        using var tx = await _db.Database.BeginTransactionAsync();

        // Re-check fresh
        var ids = chain.Select(c => c.Id).ToArray();
        var fresh = await _db.Appointments
            .Where(a => ids.Contains(a.Id))
            .OrderBy(a => a.Start)
            .ToListAsync();

        if (fresh.Any(a => a.Status != AppointmentStatus.Free))
            return Conflict("Slot became unavailable.");

        foreach (var a in fresh)
        {
            a.Status = AppointmentStatus.Booked;
            a.ServiceId = req.ServiceId;
            a.Notes = BuildNotes(new BookingRequest
            {
                FullName = req.FullName,
                Email = req.Email,
                Phone = req.Phone
            });
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new { ok = true });
    }


    // ------------ Helpers ------------
    private static string? BuildNotes(BookingRequest r)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(r.FullName)) parts.Add(r.FullName!.Trim());
        if (!string.IsNullOrWhiteSpace(r.Email)) parts.Add(r.Email!.Trim());
        if (!string.IsNullOrWhiteSpace(r.Phone)) parts.Add(r.Phone!.Trim());
        if (!string.IsNullOrWhiteSpace(r.Notes)) parts.Add(r.Notes!.Trim());
        return parts.Count > 0 ? $"Online: {string.Join(" / ", parts)}" : "Online rezervacija";
    }
}

using Microsoft.EntityFrameworkCore;
using TerminBooking.Data;
using TerminBooking.Domain;

namespace TerminBooking.Services
{
    public static class SlotGenerator
    {
        /// Ensures 30-min grid between 09:00–17:00 for given staff and date.
        public static async Task<int> EnsureDailyGridAsync(
            ApplicationDbContext db,
            int staffId,
            DateOnly date,
            TimeOnly? dayStartOpt = null,
            TimeOnly? dayEndOpt = null)
        {
            var dayStart = date.ToDateTime(dayStartOpt ?? new TimeOnly(9, 0));
            var dayEnd = date.ToDateTime(dayEndOpt ?? new TimeOnly(17, 0));

            // Učitaj sve termine za taj dan i tog djelatnika
            var sameDay = await db.Appointments
                .Where(a => a.StaffId == staffId
                         && a.Start < dayEnd && a.End > dayStart)
                .ToListAsync();

            // Označi sve zauzete 30-min "kockice" iz SVIH termina (bilo kojeg trajanja)
            var occupiedTicks = new HashSet<long>();
            foreach (var a in sameDay)
            {
                for (var t = a.Start; t < a.End; t = t.AddMinutes(30))
                    occupiedTicks.Add(t.Ticks);
            }

            var toAdd = new List<Appointment>();

            var cur = dayStart;
            while (cur < dayEnd)
            {
                var next = cur.AddMinutes(30);
                if (!occupiedTicks.Contains(cur.Ticks))
                {
                    toAdd.Add(new Appointment
                    {
                        StaffId = staffId,
                        Start = cur,
                        End = next,
                        Status = AppointmentStatus.Free
                    });
                }
                cur = next;

            }

            if (toAdd.Count > 0)
            {
                await db.Appointments.AddRangeAsync(toAdd);
                await db.SaveChangesAsync();
            }

            return toAdd.Count;
        }
    }
}

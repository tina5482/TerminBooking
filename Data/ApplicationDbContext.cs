using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TerminBooking.Domain;

namespace TerminBooking.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSet-ovi (tablice)
        public DbSet<Service> Services => Set<Service>();
        public DbSet<Client> Clients => Set<Client>();
        public DbSet<Appointment> Appointments => Set<Appointment>();
        public DbSet<Staff> Staff => Set<Staff>();
        public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();


        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // -------- Service --------
            b.Entity<Service>(e =>
            {
                e.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(150);

                e.Property(x => x.Price)
                    .HasColumnType("decimal(10,2)");

                e.Property(x => x.IsActive)
                    .HasDefaultValue(true);

                // obvezna veza na Staff
                e.HasOne(x => x.Staff)
                 .WithMany()
                 .HasForeignKey(x => x.StaffId)
                 .OnDelete(DeleteBehavior.Restrict) // ne briši usluge/termine kad se briše staff
                 .IsRequired();
            });

            // -------- Client --------
            b.Entity<Client>(e =>
            {
                e.Property(x => x.FullName)
                    .IsRequired()
                    .HasMaxLength(150);

                e.Property(x => x.Email)
                    .HasMaxLength(256);

                e.Property(x => x.Phone)
                    .HasMaxLength(50);

                e.HasIndex(x => x.Email);
                e.HasIndex(x => x.Phone);
            });

            // -------- Staff --------
            b.Entity<Staff>(e =>
            {
                e.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                e.Property(x => x.IsActive)
                    .HasDefaultValue(true);
            });

            // -------- Appointment --------
            b.Entity<Appointment>(e =>
            {
                // opcionalna veza na Service (za Free termin može biti NULL)
                e.HasOne(x => x.Service)
                 .WithMany()
                 .HasForeignKey(x => x.ServiceId)
                 .OnDelete(DeleteBehavior.SetNull); // ako se usluga obriše, stavi NULL

                // opcionalna veza na Client
                e.HasOne(x => x.Client)
                 .WithMany()
                 .HasForeignKey(x => x.ClientId)
                 .OnDelete(DeleteBehavior.SetNull);

                // obvezna veza na Staff
                e.HasOne(x => x.Staff)
                 .WithMany()
                 .HasForeignKey(x => x.StaffId)
                 .OnDelete(DeleteBehavior.Restrict)
                 .IsRequired();

                // indeksi i ograničenja
                e.HasIndex(x => new { x.StaffId, x.Start, x.End });
                e.HasIndex(x => new { x.Start, x.End });

                e.ToTable(tb =>
                {
                    tb.HasCheckConstraint("CK_Appointment_Time", "[Start] < [End]");
                });

                e.Property(x => x.Status)
                    .HasDefaultValue(AppointmentStatus.Free);

                e.Property(x => x.Notes)
                    .HasMaxLength(1000);
            });

            b.Entity<ContactMessage>(e =>
            {
                e.Property(x => x.FullName).IsRequired().HasMaxLength(150);
                e.Property(x => x.Email).HasMaxLength(256);
                e.Property(x => x.Phone).HasMaxLength(50);
                e.Property(x => x.Subject).HasMaxLength(200);
                e.Property(x => x.Body).IsRequired().HasMaxLength(4000);
                e.Property(x => x.InternalNotes).HasMaxLength(2000);
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                e.Property(x => x.Status).HasDefaultValue(ContactStatus.New);
                e.HasIndex(x => x.Status);
                e.HasIndex(x => x.CreatedAt);
            });

        }
    }
}

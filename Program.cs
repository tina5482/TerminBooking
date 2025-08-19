using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TerminBooking.Areas.Identity;
using TerminBooking.Data;
using TerminBooking.Domain; // Staff/Service/Appointment modeli

namespace TerminBooking
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ===== DB =====
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            // ===== Identity =====
            builder.Services.AddDefaultIdentity<IdentityUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false; // lakši login u razvoju
            })
            .AddEntityFrameworkStores<ApplicationDbContext>();

            // ===== MVC/Blazor =====
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddControllers();
            builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();

            // ===== CORS (frontend na drugom originu) =====
            const string CorsPolicy = "PublicSite";
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicy, policy =>
                {
                    policy
                        .WithOrigins(
                            "http://127.0.0.1:5500",
                            "http://localhost:5500"
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            var app = builder.Build();

            // ===== SEED: Staff + Services (samo ako nema) =====
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.Migrate(); // primijeni migracije

                if (!db.Staff.Any())
                {
                    var bella = new Staff { Name = "Bella", Skills = "Šminkanje, Obrve, Trepavice", IsActive = true, ColorHex = "#e91e63" };
                    var petra = new Staff { Name = "Petra", Skills = "Nokti", IsActive = true, ColorHex = "#3f51b5" };

                    db.Staff.AddRange(bella, petra);
                    db.SaveChanges(); // da dobiju Id

                    db.Services.AddRange(
                        new Service { Name = "Šminkanje", DurationMin = 60, Price = 40, StaffId = bella.Id, IsActive = true },
                        new Service { Name = "Obrve", DurationMin = 30, Price = 20, StaffId = bella.Id, IsActive = true },
                        new Service { Name = "Trepavice", DurationMin = 45, Price = 30, StaffId = bella.Id, IsActive = true },

                        new Service { Name = "Manikura", DurationMin = 60, Price = 35, StaffId = petra.Id, IsActive = true },
                        new Service { Name = "Gel nokti", DurationMin = 90, Price = 50, StaffId = petra.Id, IsActive = true }
                    );
                    db.SaveChanges();
                }
            }

            // ===== Pipeline =====
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseCors(CorsPolicy);

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorPages();   // Identity UI
            app.MapControllers();  // API (BookingController)
            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            app.Run();
        }
    }
}

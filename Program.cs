using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TerminBooking.Areas.Identity;
using TerminBooking.Data;

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

            // 1) Standardni DbContext za Identity i klasièan DI,
            //    ali s optionsLifetime = Singleton (bitno zbog DbContextFactory)
            builder.Services.AddDbContext<ApplicationDbContext>(
                options => options.UseSqlServer(connectionString),
                contextLifetime: ServiceLifetime.Scoped,
                optionsLifetime: ServiceLifetime.Singleton
            );

            // 2) Tvornica konteksta za Blazor komponente (sprjeèava concurrency probleme)
            builder.Services.AddDbContextFactory<ApplicationDbContext>(
                options => options.UseSqlServer(connectionString)
            );

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            // ===== Identity =====
            builder.Services.AddDefaultIdentity<IdentityUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>();

            // ===== MVC/Blazor =====
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddControllers();
            builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();

            // ===== CORS =====
            const string CorsPolicy = "PublicSite";
            const string DevOpenCors = "DevOpen";

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicy, p =>
                    p.WithOrigins(
                         "http://127.0.0.1:5500",
                         "http://localhost:5500",
                         "https://127.0.0.1:5500",
                         "https://localhost:5500"
                     )
                     .AllowAnyHeader()
                     .AllowAnyMethod()
                );

                options.AddPolicy(DevOpenCors, p =>
                    p.AllowAnyOrigin()
                     .AllowAnyHeader()
                     .AllowAnyMethod());
            });

            var app = builder.Build();

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

            if (app.Environment.IsDevelopment())
                app.UseCors(DevOpenCors);
            else
                app.UseCors(CorsPolicy);

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorPages();
            app.MapControllers();   // /api/*
            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            app.Run();
        }
    }
}

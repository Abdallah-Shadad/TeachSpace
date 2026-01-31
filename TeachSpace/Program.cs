using TeachSpace.Models;
using Microsoft.EntityFrameworkCore;
using StackExchange.Profiling;

namespace TeachSpace
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // 2. Session Setup (Essential: Cache + Session itself)
            builder.Services.AddDistributedMemoryCache(); // The Storage (RAM)
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // Session lifespan
                options.Cookie.HttpOnly = true; // Security: Prevents JavaScript from accessing cookies
                options.Cookie.IsEssential = true; // Essential for privacy laws (GDPR)
            });


            // Add DbContext for DI
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            var connectionString = ("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=AssignmentDB1;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=true;Application Intent=ReadWrite;Multi Subnet Failover=False");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(connectionString);

                options.LogTo(Console.WriteLine, LogLevel.Information);

                options.EnableSensitiveDataLogging();
            });



            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseRouting();
            app.UseAuthorization();

            app.UseSession();

            // Use MiniProfiler middleware
            app.UseMiniProfiler();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();



            app.Run();
        }
    }
}

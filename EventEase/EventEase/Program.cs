using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using EventEase.Data;
using EventEase.Models; // ? Make sure this is added for EventType

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// SEED EVENT TYPES HERE
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (!context.EventTypes.Any())
    {
        context.EventTypes.AddRange(
            new EventType { Name = "Conference" },
            new EventType { Name = "Wedding" },
            new EventType { Name = "Birthday" },
            new EventType { Name = "Seminar" }
        );
        context.SaveChanges();
    }
}

app.Run();

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rise.Persistence;

public class CustomWebApplicationFactory<Program> : WebApplicationFactory<Program>
    where Program : class
{
    protected override IHost CreateHost(IHostBuilder builder)
    {   
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            // Verwijder de bestaande ApplicationDbContext registratie
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
            );
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Voeg een in-memory database toe voor testen
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

            // Zorg ervoor dat de database is geïnitialiseerd met de testdata
            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                var seeder = new Seeder(db);
                seeder.Seed();
            }
        });

        return base.CreateHost(builder);
    }
}

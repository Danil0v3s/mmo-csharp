// This is an EXAMPLE of how to use DatabaseSeeder in your server's Program.cs
// DO NOT compile this file - it's for reference only

using Core.Database;
using Core.Database.Context;
using Core.Database.Seeds;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Register database services
builder.Services.AddGameDatabase(
    builder.Configuration.GetConnectionString("GameDatabase") 
    ?? throw new InvalidOperationException("Database connection string not found"));

// 2. Build the application
var app = builder.Build();

// 3. Apply migrations and seed database on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    
    try
    {
        // Get required services
        var context = services.GetRequiredService<GameDbContext>();
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<Program>();
        
        logger.LogInformation("Applying database migrations...");
        
        // Apply any pending migrations
        await context.Database.MigrateAsync();
        
        logger.LogInformation("Database migrations applied successfully");
        logger.LogInformation("Seeding database...");
        
        // Seed database from SQL scripts
        var seeder = new DatabaseSeeder(context, loggerFactory.CreateLogger<DatabaseSeeder>());
        await seeder.SeedAsync();
        
        logger.LogInformation("Database setup completed successfully");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while setting up the database");
        throw;
    }
}

// 4. Configure middleware and run
app.MapGet("/", () => "Server is running!");

await app.RunAsync();


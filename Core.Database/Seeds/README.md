# Database Seeding

## Overview

This project uses **SQL scripts** for database seeding instead of C# seed classes. This approach is more maintainable for large datasets and allows you to use your existing SQL files directly.

## How It Works

1. **SQL Scripts**: Located in `/Scripts/` directory
2. **DatabaseSeeder**: Service that executes SQL scripts on startup
3. **Automatic**: Runs once when database is empty (idempotent)

## File Structure

```
Core.Database/
├── Scripts/
│   └── seed_initial_data.sql    # Initial reference data (clans, default accounts)
└── Seeds/
    ├── DatabaseSeeder.cs         # Service that executes SQL scripts
    └── README.md                 # This file
```

## Usage

### In Your Server's Program.cs

```csharp
using Core.Database;
using Core.Database.Seeds;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Register database
builder.Services.AddGameDatabase(
    builder.Configuration.GetConnectionString("GameDatabase"));

var app = builder.Build();

// Seed database on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Apply migrations
        await context.Database.MigrateAsync();
        
        // Seed data from SQL scripts
        var seeder = new DatabaseSeeder(context, logger);
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating or seeding the database");
        throw;
    }
}

await app.RunAsync();
```

## Adding New Seed Data

### Option 1: Add to Existing Script

Edit `/Scripts/seed_initial_data.sql` and add your INSERT statements:

```sql
-- Your new data
INSERT INTO `your_table` VALUES (1, 'value1', 'value2');
```

### Option 2: Create New Script

1. Create a new `.sql` file in `/Scripts/`
2. Add your INSERT statements
3. Update `DatabaseSeeder.cs` to execute it:

```csharp
public async Task SeedAsync(CancellationToken ct = default)
{
    if (await IsDatabaseSeededAsync(ct))
        return;

    await ExecuteSqlScriptAsync("Scripts/seed_initial_data.sql", ct);
    await ExecuteSqlScriptAsync("Scripts/seed_your_new_data.sql", ct); // Add this
}
```

## Extracting Data from Existing SQL Files

If you have large SQL files (like item databases), extract just the INSERT statements:

```bash
# Extract INSERT statements from a SQL file
grep "^INSERT INTO" your_file.sql > seed_extracted.sql
```

Or use the provided helper:

```bash
# This will extract INSERTs from rathena SQL files
cd /path/to/rathena/sql-files
grep "^INSERT INTO" main.sql > seed_main_data.sql
```

## Benefits

✅ **No Code Generation**: No need for huge C# seed classes  
✅ **Use Existing SQL**: Directly use SQL files from rathena or other sources  
✅ **Easy Updates**: Just edit SQL files, no code changes needed  
✅ **Fast**: SQL execution is faster than EF Core entity creation  
✅ **Maintainable**: SQL is more readable for data than C# arrays  
✅ **Version Control**: SQL files are easier to diff and merge  

## Notes

- The seeder checks if data exists before running (idempotent)
- Scripts are executed in transaction context
- Errors are logged with details
- Scripts are copied to output directory during build
- Database must be created and migrated before seeding


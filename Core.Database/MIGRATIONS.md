# Running EF Core Migrations

## Setup

The `Core.Database` project contains its own `appsettings.json` for migrations.

### Configure Connection String

Edit `/Core.Database/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "GameDatabase": "Server=localhost;Port=3306;Database=ragnarok;Uid=ragnarok;Pwd=ragnarok;CharSet=utf8mb4;"
  }
}
```

**Note:** Update the connection string to match your MySQL server configuration.

## Running Migrations

### From Core.Database Directory (Recommended)

```bash
cd Core.Database

# Create initial migration
dotnet ef migrations add InitialCreate

# Apply migrations to database
dotnet ef database update

# List migrations
dotnet ef migrations list
```

### From Solution Root

```bash
# From /mmo-csharp directory
cd /path/to/mmo-csharp

# Create migration
dotnet ef migrations add InitialCreate --project Core.Database

# Apply migrations
dotnet ef database update --project Core.Database

# List migrations
dotnet ef migrations list --project Core.Database
```

## How It Works

The `GameDbContextFactory` class provides design-time support for EF Core:

1. **Design-Time Factory:** `GameDbContextFactory.cs` implements `IDesignTimeDbContextFactory<GameDbContext>`
2. **Connection String:** Reads from `Core.Database/appsettings.json`
3. **No Startup Project Needed:** Can run migrations directly from Core.Database

## Common Commands

```bash
# Create a migration
dotnet ef migrations add MigrationName

# Apply all migrations
dotnet ef database update

# Rollback to specific migration
dotnet ef database update MigrationName

# Rollback all migrations
dotnet ef database update 0

# Remove last migration (if not applied)
dotnet ef migrations remove

# Generate SQL script
dotnet ef migrations script -o migration.sql

# Generate idempotent SQL script (safe to run multiple times)
dotnet ef migrations script --idempotent -o migration.sql
```

## Troubleshooting

### Error: "Connection string 'GameDatabase' not found"

**Solution:** Ensure `Core.Database/appsettings.json` exists with proper connection string.

### Error: "Unable to connect to MySQL"

**Solutions:**
1. Start MySQL server: `brew services start mysql` or `mysql.server start`
2. Create database: `CREATE DATABASE ragnarok CHARACTER SET utf8mb4;`
3. Create user and grant permissions:
   ```sql
   CREATE USER 'ragnarok'@'localhost' IDENTIFIED BY 'ragnarok';
   GRANT ALL PRIVILEGES ON ragnarok.* TO 'ragnarok'@'localhost';
   FLUSH PRIVILEGES;
   ```

### Error: "Build failed"

**Solution:** Build the project first:
```bash
dotnet build Core.Database
```

## Production Deployment

For production, generate SQL scripts instead of running migrations directly:

```bash
# Generate SQL for all migrations
dotnet ef migrations script --idempotent -o deploy.sql

# Review deploy.sql, then apply manually:
mysql -u ragnarok -p ragnarok < deploy.sql
```

## Security Note

⚠️ **Do not commit `appsettings.json` with real credentials to Git!**

The included `appsettings.json` is a template with default credentials for local development only.

For production:
- Use environment variables
- Use a secrets manager (Azure Key Vault, AWS Secrets Manager, etc.)
- Update `.gitignore` to exclude environment-specific config files


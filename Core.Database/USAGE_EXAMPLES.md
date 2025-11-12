# Core.Database Usage Examples

## ✅ Recommended Pattern: Direct Repository Injection

Inject only the repositories you need. This follows the Dependency Inversion and Interface Segregation principles.

### Example 1: Simple Service with One Repository

```csharp
public class CharacterService
{
    private readonly ICharacterRepository _characters;
    private readonly GameDbContext _dbContext;

    public CharacterService(
        ICharacterRepository characters,
        GameDbContext dbContext)
    {
        _characters = characters;
        _dbContext = dbContext;
    }

    public async Task<CharEntity?> GetCharacterAsync(int charId, CancellationToken ct = default)
    {
        return await _characters.GetByIdAsync(charId, ct);
    }

    public async Task<CharEntity> CreateCharacterAsync(CharEntity character, CancellationToken ct = default)
    {
        await _characters.AddAsync(character, ct);
        await _dbContext.SaveChangesAsync(ct);
        return character;
    }
}
```

### Example 2: Service with Multiple Repositories

```csharp
public class CharacterCreationService
{
    private readonly ICharacterRepository _characters;
    private readonly IInventoryRepository _inventories;
    private readonly ISkillRepository _skills;
    private readonly GameDbContext _dbContext;

    // Only inject what you actually need!
    public CharacterCreationService(
        ICharacterRepository characters,
        IInventoryRepository inventories,
        ISkillRepository skills,
        GameDbContext dbContext)
    {
        _characters = characters;
        _inventories = inventories;
        _skills = skills;
        _dbContext = dbContext;
    }

    public async Task<CharEntity> CreateNewCharacterAsync(
        string name,
        int accountId,
        CancellationToken ct = default)
    {
        var character = new CharEntity
        {
            Name = name,
            AccountId = accountId,
            BaseLevel = 1,
            JobLevel = 1
        };

        await _characters.AddAsync(character, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Add starter inventory
        var starterItem = new InventoryEntity
        {
            CharId = character.CharId,
            NameId = 501, // Red Potion
            Amount = 10
        };
        await _inventories.AddAsync(starterItem, ct);
        await _dbContext.SaveChangesAsync(ct);

        return character;
    }
}
```

### Example 3: Using Transactions

```csharp
public class GuildService
{
    private readonly IGuildRepository _guilds;
    private readonly IGuildMemberRepository _guildMembers;
    private readonly ICharacterRepository _characters;
    private readonly GameDbContext _dbContext;

    public GuildService(
        IGuildRepository guilds,
        IGuildMemberRepository guildMembers,
        ICharacterRepository characters,
        GameDbContext dbContext)
    {
        _guilds = guilds;
        _guildMembers = guildMembers;
        _characters = characters;
        _dbContext = dbContext;
    }

    public async Task<GuildEntity> CreateGuildAsync(
        string guildName,
        int masterCharId,
        CancellationToken ct = default)
    {
        // Use transaction for multi-step operations
        using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
        try
        {
            // Create guild
            var guild = new GuildEntity
            {
                Name = guildName,
                CharId = masterCharId,
                Master = (await _characters.GetByIdAsync(masterCharId, ct))?.Name ?? "",
                GuildLv = 1,
                MaxMember = 16
            };
            await _guilds.AddAsync(guild, ct);
            await _dbContext.SaveChangesAsync(ct);

            // Add creator as guild master
            var member = new GuildMemberEntity
            {
                GuildId = guild.GuildId,
                CharId = masterCharId,
                Position = 0 // Guild Master
            };
            await _guildMembers.AddAsync(member, ct);
            await _dbContext.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
            return guild;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
```

### Example 4: Using Optional IUnitOfWork (Alternative)

If you prefer a dedicated transaction wrapper, you can use `IUnitOfWork`:

```csharp
public class GuildService
{
    private readonly IGuildRepository _guilds;
    private readonly IGuildMemberRepository _guildMembers;
    private readonly ICharacterRepository _characters;
    private readonly IUnitOfWork _unitOfWork;

    public GuildService(
        IGuildRepository guilds,
        IGuildMemberRepository guildMembers,
        ICharacterRepository characters,
        IUnitOfWork unitOfWork)
    {
        _guilds = guilds;
        _guildMembers = guildMembers;
        _characters = characters;
        _unitOfWork = unitOfWork;
    }

    public async Task<GuildEntity> CreateGuildAsync(
        string guildName,
        int masterCharId,
        CancellationToken ct = default)
    {
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var guild = new GuildEntity
            {
                Name = guildName,
                CharId = masterCharId,
                Master = (await _characters.GetByIdAsync(masterCharId, ct))?.Name ?? "",
                GuildLv = 1,
                MaxMember = 16
            };
            await _guilds.AddAsync(guild, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            var member = new GuildMemberEntity
            {
                GuildId = guild.GuildId,
                CharId = masterCharId,
                Position = 0
            };
            await _guildMembers.AddAsync(member, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            await _unitOfWork.CommitTransactionAsync(ct);
            return guild;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }
}
```

## 📋 Testing Examples

### Example: Unit Test with Mocked Repository

```csharp
public class CharacterServiceTests
{
    [Fact]
    public async Task GetCharacterAsync_WithValidId_ReturnsCharacter()
    {
        // Arrange
        var mockCharRepo = new Mock<ICharacterRepository>();
        var mockDbContext = new Mock<GameDbContext>();
        
        var expectedChar = new CharEntity { CharId = 1, Name = "TestChar" };
        mockCharRepo
            .Setup(r => r.GetByIdAsync(1, default))
            .ReturnsAsync(expectedChar);

        var service = new CharacterService(
            mockCharRepo.Object,
            mockDbContext.Object);

        // Act
        var result = await service.GetCharacterAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestChar", result.Name);
        mockCharRepo.Verify(r => r.GetByIdAsync(1, default), Times.Once);
    }
}
```

## 🎯 Benefits of This Approach

1. **Explicit Dependencies**: Constructor shows exactly what repositories are needed
2. **Interface Segregation**: Services only depend on interfaces they use
3. **Easy Testing**: Mock only what's needed, not a giant UnitOfWork
4. **No Service Locator**: No hidden dependencies
5. **Better Performance**: No lazy initialization overhead
6. **Clear Intent**: Code clearly shows its data access needs

## 🚫 Anti-Pattern to Avoid

**DON'T do this:**
```csharp
// ❌ BAD: Service has access to all 25+ repositories
public class CharacterService
{
    private readonly IUnitOfWork _unitOfWork;
    
    public CharacterService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task GetCharacterAsync(int id)
    {
        // Uses only 1 repository but has access to 25+
        return await _unitOfWork.Characters.GetByIdAsync(id);
    }
}
```

**DO this instead:**
```csharp
// ✅ GOOD: Service has access only to what it needs
public class CharacterService
{
    private readonly ICharacterRepository _characters;
    private readonly GameDbContext _dbContext;
    
    public CharacterService(
        ICharacterRepository characters,
        GameDbContext dbContext)
    {
        _characters = characters;
        _dbContext = dbContext;
    }
    
    public async Task GetCharacterAsync(int id)
    {
        return await _characters.GetByIdAsync(id);
    }
}
```

## 🌱 Database Seeding

The project uses SQL scripts for seeding instead of C# seed classes:

```csharp
// In your server's Program.cs
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    // Apply migrations
    await context.Database.MigrateAsync();
    
    // Seed from SQL scripts
    var seeder = new DatabaseSeeder(context, logger);
    await seeder.SeedAsync();
}

await app.RunAsync();
```

See `/Seeds/README.md` for more details on database seeding.

## 📖 Additional Resources

- [EF Core DbContext as Unit of Work](https://docs.microsoft.com/en-us/ef/core/saving/transactions)
- [Dependency Injection Best Practices](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines)
- [Repository Pattern with EF Core](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)


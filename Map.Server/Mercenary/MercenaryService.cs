using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Map.Server.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Mercenary;

/// <summary>
/// Default <see cref="IMercenaryService"/>. Catalog loaded from
/// <c>mercenary_db</c> SQL (~21 classes seeded from rAthena YAML).
/// Per-character merc state persists via IPC.
/// </summary>
public sealed class MercenaryService : IMercenaryService
{
    private readonly Dictionary<uint, MercenaryDbEntity> _catalog = new();
    private readonly IServiceScopeFactory? _scopes;
    private readonly ILogger<MercenaryService> _logger;

    public MercenaryService(IServiceScopeFactory scopes, ILogger<MercenaryService> logger)
    {
        _scopes = scopes;
        _logger = logger;
        Reload();
    }

    public MercenaryService(ILogger<MercenaryService> logger) { _logger = logger; }

    /// <summary>Catalog lookup by merc id.</summary>
    public MercenaryDbEntity? GetCatalogEntry(uint mercId)
        => _catalog.TryGetValue(mercId, out var v) ? v : null;

    /// <summary>Reload catalog from SQL.</summary>
    public void Reload()
    {
        _catalog.Clear();
        if (_scopes == null) return;
        try
        {
            using var scope = _scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IMercenaryDbRepository>();
            foreach (var m in repo.GetAllAsync().GetAwaiter().GetResult())
                _catalog[m.MercId] = m;
            _logger.LogInformation("mercenary_db loaded: {N} classes", _catalog.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "mercenary_db load failed");
        }
    }

    public bool Create(PlayerEntity master, int classId, int lifetimeMs) => false;
    public bool Dead(PlayerEntity master) => false;
    public int Delete(PlayerEntity master, byte reason) => 0;
    public bool RecvData(PlayerEntity master) => false;
    public void Save(PlayerEntity master) { }
    public int GetCalls(int classId) => 0;
    public void SetCalls(PlayerEntity master, int delta) { }
    public int GetFaith(PlayerEntity master) => 0;
    public void SetFaith(PlayerEntity master, int delta) { }
    public long GetLifetimeMs(PlayerEntity master) => 0;
    public void Heal(PlayerEntity master, int hp, int sp) { }
    public void KillBonus(PlayerEntity master) { }
    public void Kills(PlayerEntity master) { }
    public ushort CheckSkill(PlayerEntity master, ushort skillId) => 0;
    public void ContractInit(PlayerEntity master) { }
    public void ContractStop(PlayerEntity master) { }

    /// <inheritdoc />
    public Core.Server.IPC.MercenaryData? SerializeSnapshot(int mercId)
    {
        // T7.3 — canonical entry point. Same shape as HomunculusService:
        // when the per-master Create / RecvData paths land their
        // _aliveByMercId map, this lookup projects the merc onto
        // MercenaryData. Returning null today means "no live entity,
        // skip dispatch."
        return null;
    }
}

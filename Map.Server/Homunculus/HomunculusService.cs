using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Map.Server.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Homunculus;

/// <summary>
/// Default <see cref="IHomunculusService"/>. Catalog loaded from
/// <c>homunculus_db</c> SQL (seeded from
/// <c>db/re/homunculus_db.yml</c>, ~14 classes).
/// Per-character homunculus state persists via IPC.
/// </summary>
public sealed class HomunculusService : IHomunculusService
{
    private readonly Dictionary<string, HomunculusDbEntity> _catalog = new(StringComparer.OrdinalIgnoreCase);
    private readonly IServiceScopeFactory? _scopes;
    private readonly ILogger<HomunculusService> _logger;

    public HomunculusService(IServiceScopeFactory scopes, ILogger<HomunculusService> logger)
    {
        _scopes = scopes;
        _logger = logger;
        Reload();
    }

    public HomunculusService(ILogger<HomunculusService> logger) { _logger = logger; }

    /// <summary>Catalog lookup by class Aegis name (e.g. "HOMUNCULUS_LIF").</summary>
    public HomunculusDbEntity? GetCatalogEntry(string classAegis)
        => _catalog.TryGetValue(classAegis, out var v) ? v : null;

    public bool Call(PlayerEntity master) => false;
    public bool CreateRequest(PlayerEntity master, int classId) => false;
    public int RecvData(PlayerEntity master) => 0;
    public void Save(PlayerEntity master) { }
    public void Alloc(PlayerEntity master) { }
    public int Dead(PlayerEntity master) => 0;
    public int Delete(PlayerEntity master) => 0;
    public int Resurrect(PlayerEntity master, byte percent, short x, short y) => 0;
    public void Revive(PlayerEntity master) { }
    public int Vaporize(PlayerEntity master, byte flag) => 0;
    public int Evolution(PlayerEntity master) => 0;
    public int Mutate(PlayerEntity master, int newClass) => 0;
    public int Shuffle(PlayerEntity master) => 0;
    public int LevelUp(PlayerEntity master) => 0;
    public void GainExp(PlayerEntity master, long amount) { }
    public void Heal(PlayerEntity master, int hp, int sp) { }
    public int Food(PlayerEntity master) => 0;
    public int ChangeName(PlayerEntity master, string newName) => 0;
    public void ChangeNameAck(PlayerEntity master, byte ok) { }
    public int Class2MapId(int classId) => classId;
    public int DecreaseIntimacy(PlayerEntity master, int delta) => 0;
    public int IncreaseIntimacy(PlayerEntity master, int delta) => 0;

    public byte GetIntimacyGrade(int intimacy)
    {
        // rAthena tiers: 0=hate, 1=awkward, 2=neutral, 3=cordial, 4=loyal.
        if (intimacy <= 100) return 0;
        if (intimacy <= 250) return 1;
        if (intimacy <= 750) return 2;
        if (intimacy <= 910) return 3;
        return 4;
    }

    public uint IntimacyGrade2Intimacy(byte grade) => grade switch
    {
        0 => 1u, 1 => 100u, 2 => 250u, 3 => 750u, 4 => 910u, _ => 0u,
    };

    public int SkillTreeGetMax(int classId, ushort skillId) => 0;
    public ushort SkillGetMinLevel(ushort skillId) => 0;
    public void SkillUp(PlayerEntity master, ushort skillId) { }
    public void CalcSkillTree(PlayerEntity master) { }
    public void CalcSkillTreeSub(PlayerEntity master) { }
    public void AddSpiritBall(PlayerEntity master, int max) { }
    public void DelSpiritBall(PlayerEntity master, int count, bool one) { }
    public void ResetStats(PlayerEntity master) { }
    public void Menu(PlayerEntity master, int choice) { }
    public void Reload()
    {
        _catalog.Clear();
        if (_scopes == null) return;
        try
        {
            using var scope = _scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IHomunculusDbRepository>();
            foreach (var h in repo.GetAllAsync().GetAwaiter().GetResult())
                _catalog[h.ClassAegis] = h;
            _logger.LogInformation("homunculus_db loaded: {N} classes", _catalog.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "homunculus_db load failed");
        }
    }
    public void InitTimers(PlayerEntity master) { }
    public int HungryTimerDelete(PlayerEntity master) => 0;

    /// <inheritdoc />
    public Core.Server.IPC.HomunculusData? SerializeSnapshot(int homunId)
    {
        // T7.3 — canonical entry point. The HomunculusEntity per-master
        // table + Alloc / RecvData paths are stubs today; once they
        // populate an `_aliveByHomunId` map, this lookup flips to the
        // PetService.SerializeSnapshot shape: walk the map, find the
        // homun with matching id, project its fields onto HomunculusData.
        // Returning null here means SavePet-style "no live entity, skip
        // dispatch" — char-side row stays unchanged.
        return null;
    }
}

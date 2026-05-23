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
///
/// AT-D2 wave: per-master live-merc table + faith/calls accumulators +
/// kills/killbonus counters + ContractInit/Stop with deathtime calc.
/// Skill tree backfill (CheckSkill) reads from the catalog row when
/// the per-class skill set is available; until then returns 0.
/// </summary>
public sealed class MercenaryService : IMercenaryService
{
    private readonly Dictionary<uint, MercenaryDbEntity> _catalog = new();
    private readonly Dictionary<(uint cls, ushort skill), ushort> _skillsFromDb = new();
    private readonly Dictionary<EntityId, LiveMerc> _alive = new();
    private readonly Dictionary<(int accountId, int classId), int> _calls = new();
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
        _skillsFromDb.Clear();
        if (_scopes == null) return;
        try
        {
            using var scope = _scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IMercenaryDbRepository>();
            foreach (var m in repo.GetAllAsync().GetAwaiter().GetResult())
                _catalog[m.MercId] = m;
            _logger.LogInformation("mercenary_db loaded: {N} classes", _catalog.Count);

            // AT-F: load Skills[] from the child table (mercenary_skill_db).
            // If empty (fresh DB without seed), the baked MercSkillTable
            // fallback inside CheckSkill keeps a viable default.
            var sk = scope.ServiceProvider.GetRequiredService<IMercenarySkillDbRepository>();
            foreach (var row in sk.GetAllAsync().GetAwaiter().GetResult())
                _skillsFromDb[(row.MercId, row.SkillId)] = row.MaxLevel;
            if (_skillsFromDb.Count > 0)
                _logger.LogInformation("mercenary_skill_db loaded: {N} rows (DB-sourced)", _skillsFromDb.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "mercenary_db load failed");
        }
    }

    public bool Create(PlayerEntity master, int classId, int lifetimeMs)
    {
        if (_alive.ContainsKey(master.Id)) return false; // one merc per master
        if (!_catalog.ContainsKey((uint)classId))
        {
            _logger.LogWarning("mercenary_create: unknown class {Class}", classId);
            return false;
        }
        var live = new LiveMerc
        {
            ClassId = classId,
            Hp = 1,
            Sp = 0,
            ContractEnd = DateTime.UtcNow.AddMilliseconds(lifetimeMs),
            Faith = 0,
            KillCount = 0,
        };
        _alive[master.Id] = live;
        ContractInit(master);
        _logger.LogInformation("mercenary_create: master={Master} class={Class} lifetime={Ms}ms",
            master.Name, classId, lifetimeMs);
        return true;
    }

    public bool Dead(PlayerEntity master)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return false;
        live.Hp = 0;
        Delete(master, reason: 0);
        return true;
    }

    public int Delete(PlayerEntity master, byte reason)
    {
        if (!_alive.Remove(master.Id, out var live)) return 0;
        _logger.LogInformation("mercenary_delete: master={Master} reason={Reason}", master.Name, reason);
        return live.ClassId;
    }

    public bool RecvData(PlayerEntity master)
    {
        // Char-server unmarshalling lives in MercenaryRecvDataHandler.
        // Once it lands, this method is called with the hydrated
        // record. For now just acknowledge the canonical entry.
        return _alive.ContainsKey(master.Id);
    }

    public void Save(PlayerEntity master)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return;
        _logger.LogDebug("mercenary_save: master={Master} class={Class} hp={Hp}/{MaxHp}",
            master.Name, live.ClassId, live.Hp, live.Hp);
    }

    public int GetCalls(int classId)
        => _calls.Where(kv => kv.Key.classId == classId).Sum(kv => kv.Value);

    public void SetCalls(PlayerEntity master, int delta)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return;
        var key = (master.AccountId, live.ClassId);
        _calls[key] = _calls.GetValueOrDefault(key) + delta;
    }

    public int GetFaith(PlayerEntity master)
        => _alive.TryGetValue(master.Id, out var live) ? live.Faith : 0;

    public void SetFaith(PlayerEntity master, int delta)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return;
        live.Faith = Math.Max(0, live.Faith + delta);
    }

    public long GetLifetimeMs(PlayerEntity master)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return 0;
        var remaining = (long)(live.ContractEnd - DateTime.UtcNow).TotalMilliseconds;
        return Math.Max(0, remaining);
    }

    public void Heal(PlayerEntity master, int hp, int sp)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return;
        live.Hp = Math.Max(0, live.Hp + hp);
        live.Sp = Math.Max(0, live.Sp + sp);
    }

    public void KillBonus(PlayerEntity master)
    {
        // rAthena: on master kill, faith += battle_config.mercenary_kill_faith.
        SetFaith(master, +1);
    }

    public void Kills(PlayerEntity master)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return;
        live.KillCount++;
        if (live.KillCount % 100 == 0) KillBonus(master);
    }

    public ushort CheckSkill(PlayerEntity master, ushort skillId)
    {
        // rAthena mercenary_checkskill — returns the max level the
        // master's merc class is granted for <skillId>, or 0 when the
        // class doesn't grant the skill. DBR-0: now sourced solely from
        // mercenary_skill_db (44 rows seeded from db/re/mercenary_db.yml
        // Skills:[] arrays); the prior baked MercSkillTable was a temp
        // safety net until SeedGen landed. DatabaseSeeder + import
        // pipeline now both populate this table.
        if (!_alive.TryGetValue(master.Id, out var live)) return 0;
        return _skillsFromDb.TryGetValue(((uint)live.ClassId, skillId), out var lvl) ? lvl : (ushort)0;
    }

    public void ContractInit(PlayerEntity master)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return;
        _logger.LogInformation("merc_contract_init: master={Master} class={Class} ends={End:O}",
            master.Name, live.ClassId, live.ContractEnd);
    }

    public void ContractStop(PlayerEntity master)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return;
        live.ContractEnd = DateTime.UtcNow;
        Delete(master, reason: 1);
    }

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

    private sealed class LiveMerc
    {
        public int ClassId;
        public int Hp;
        public int Sp;
        public DateTime ContractEnd;
        public int Faith;
        public int KillCount;
    }
}

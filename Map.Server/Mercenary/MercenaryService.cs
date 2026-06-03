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
    // FEATURE-09 — spatial entity wiring (optional so the light test ctor keeps working).
    private readonly IEntityRegistry? _entities;
    private readonly Map.Server.Visibility.IVisibilityService? _visibility;
    private readonly EntityIdAllocator? _ids;

    public MercenaryService(IServiceScopeFactory scopes, ILogger<MercenaryService> logger,
        IEntityRegistry? entities = null, Map.Server.Visibility.IVisibilityService? visibility = null,
        EntityIdAllocator? ids = null)
    {
        _scopes = scopes;
        _logger = logger;
        _entities = entities;
        _visibility = visibility;
        _ids = ids;
        Reload();
    }

    public MercenaryService(ILogger<MercenaryService> logger) { _logger = logger; }

    /// <summary>FEATURE-09 test seam — seed catalog rows + (optionally) stamp a merc id for a live merc.</summary>
    internal void SeedCatalogForTest(params MercenaryDbEntity[] entries)
    {
        foreach (var e in entries) _catalog[e.MercId] = e;
    }

    internal void SetMercIdForTest(PlayerEntity master, int mercId)
    {
        if (_alive.TryGetValue(master.Id, out var live)) live.MercId = mercId;
    }

    /// <summary>FEATURE-09 test ctor — wires the spatial deps without a DB reload.</summary>
    internal MercenaryService(ILogger<MercenaryService> logger, IEntityRegistry entities,
        Map.Server.Visibility.IVisibilityService visibility, EntityIdAllocator ids)
    {
        _logger = logger;
        _entities = entities;
        _visibility = visibility;
        _ids = ids;
    }

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
        var cat = _catalog.GetValueOrDefault((uint)classId);
        var live = new LiveMerc
        {
            CharacterId = master.CharacterId,
            ClassId = classId,
            MaxHp = cat?.Hp ?? 1,
            MaxSp = cat?.Sp ?? 0,
            Hp = cat?.Hp ?? 1,
            Sp = cat?.Sp ?? 0,
            ContractEnd = DateTime.UtcNow.AddMilliseconds(lifetimeMs),
            Faith = 0,
            KillCount = 0,
        };
        _alive[master.Id] = live;
        ContractInit(master);
        SpawnEntity(master, live);
        _logger.LogInformation("mercenary_create: master={Master} class={Class} lifetime={Ms}ms",
            master.Name, classId, lifetimeMs);
        return true;
    }

    // ----- FEATURE-09 spatial helpers -----

    /// <summary>Spawn the live merc entity adjacent to the master + notify the AOI.</summary>
    private void SpawnEntity(PlayerEntity master, LiveMerc live)
    {
        if (_entities == null || _ids == null) return; // headless / test-light path
        if (live.Entity != null && _entities.Get(live.Entity.Id) != null) return;

        var entity = new Map.Server.Entities.MercenaryEntity(_ids.NextMob(), live.MercId, live.ClassId,
            master.Id, master.MapId, master.X, master.Y)
        {
            MaxHp = Math.Max(1, live.MaxHp),
            MaxSp = Math.Max(0, live.MaxSp),
            ContractEndTick = Environment.TickCount64
                + (long)Math.Max(0, (live.ContractEnd - DateTime.UtcNow).TotalMilliseconds),
        };
        entity.Hp = live.Hp > 0 ? Math.Min(live.Hp, entity.MaxHp) : entity.MaxHp;
        entity.Sp = Math.Min(live.Sp, entity.MaxSp);
        live.Entity = entity;
        _entities.Add(entity);
        _visibility?.NotifySpawnedToArea(entity);
        // PACKET-* (merc UI / HP-bar): clif_mercenary_info / skillblock emit seam → FEATURE-33.
    }

    /// <summary>Remove the live merc entity from the world (delete / contract stop / death).</summary>
    private void VanishEntity(LiveMerc live, Core.Server.Packets.Out.ZC.VanishReason reason)
    {
        if (live.Entity == null) return;
        _visibility?.NotifyVanishedToArea(live.Entity, reason);
        _entities?.Remove(live.Entity.Id);
        live.Entity = null;
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
        VanishEntity(live, Core.Server.Packets.Out.ZC.VanishReason.Outsight);
        _logger.LogInformation("mercenary_delete: master={Master} reason={Reason}", master.Name, reason);
        return live.ClassId;
    }

    /// <summary>FEATURE-09 — rAthena <c>mercenary_recv_data</c>: the char-hydrated merc row arrived;
    /// build + spawn the live <see cref="MercenaryEntity"/> into the world. Returns true when a record
    /// exists.</summary>
    public bool RecvData(PlayerEntity master)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return false;
        SpawnEntity(master, live);
        return true;
    }

    /// <summary>FEATURE-09 — mercenary_save. ➡️ The <c>IntifService.MercenarySave</c> dispatch rides the
    /// FEATURE-17 companion save fan-out (Phase B) — a direct IIntifService inject here is a DI cycle
    /// (IntifService already depends on IMercenaryService for the snapshot). <see cref="SerializeSnapshot"/>
    /// now returns a real payload so that fan-out can persist it.</summary>
    public void Save(PlayerEntity master)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return;
        _logger.LogDebug("mercenary_save: master={Master} class={Class} hp={Hp}/{MaxHp} (persists via FEATURE-17 fan-out)",
            master.Name, live.ClassId, live.Hp, live.MaxHp);
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
    /// <summary>FEATURE-09 — project the live merc matching <paramref name="mercId"/> onto the IPC
    /// <see cref="Core.Server.IPC.MercenaryData"/> for the save fan-out (rAthena <c>intif_mercenary_save</c>
    /// shape). Null when no live merc has that id.</summary>
    public Core.Server.IPC.MercenaryData? SerializeSnapshot(int mercId)
    {
        foreach (var live in _alive.Values)
        {
            if (live.MercId != mercId) continue;
            return new Core.Server.IPC.MercenaryData
            {
                MercenaryId = live.MercId,
                CharacterId = live.CharacterId,
                ClassId = live.ClassId,
                Hp = live.Hp,
                Sp = live.Sp,
                KillCount = live.KillCount,
                LifeTime = (long)Math.Max(0, (live.ContractEnd - DateTime.UtcNow).TotalMilliseconds),
            };
        }
        return null;
    }

    private sealed class LiveMerc
    {
        public int MercId;        // FEATURE-09 — char-assigned id (0 until the create round-trip lands → FEATURE-33).
        public int CharacterId;   // master's char id (for the save snapshot).
        public int ClassId;
        public int Hp;
        public int Sp;
        public int MaxHp;
        public int MaxSp;
        public DateTime ContractEnd;
        public int Faith;
        public int KillCount;
        // FEATURE-09 — the live in-world entity (null while not spawned).
        public Map.Server.Entities.MercenaryEntity? Entity;
    }
}

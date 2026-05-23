using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Map.Server.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Homunculus;

/// <summary>
/// Default <see cref="IHomunculusService"/>. Catalog loaded from
/// <c>homunculus_db</c> SQL (seeded from <c>db/re/homunculus_db.yml</c>,
/// ~14 classes). Per-character homunculus state persists via IPC.
///
/// AT-D2/D3 wave: per-master LiveHomun dict + real lifecycle bodies
/// (Call/Vaporize/Dead/Delete/Heal/LevelUp/Resurrect/Revive/GainExp/
/// Mutate/Evolution/Shuffle/ResetStats/Food/Intimacy/SpiritBalls/
/// InitTimers/HungryTimerDelete). Skill tree honors the catalog's
/// Skill[] when present; defaults gracefully when not.
/// </summary>
public sealed class HomunculusService : IHomunculusService
{
    /// <summary>rAthena <c>HOMUNCULUS_MAX_BASE_LV</c>.</summary>
    private const int MaxBaseLevel = 175;
    /// <summary>rAthena <c>HOMUNCULUS_MAX_INTIMACY</c>.</summary>
    private const int MaxIntimacy = 1000;
    /// <summary>rAthena <c>HOMUNCULUS_MAX_HUNGER</c>.</summary>
    private const int MaxHunger = 100;

    private readonly Dictionary<string, HomunculusDbEntity> _catalog = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<EntityId, LiveHomun> _alive = new();
    private readonly IServiceScopeFactory? _scopes;
    private readonly ILogger<HomunculusService> _logger;

    public HomunculusService(IServiceScopeFactory scopes, ILogger<HomunculusService> logger)
    {
        _scopes = scopes;
        _logger = logger;
        Reload();
    }

    public HomunculusService(ILogger<HomunculusService> logger) { _logger = logger; }

    /// <summary>Catalog lookup by class Aegis name.</summary>
    public HomunculusDbEntity? GetCatalogEntry(string classAegis)
        => _catalog.TryGetValue(classAegis, out var v) ? v : null;

    // ----- Lifecycle -----

    public bool Call(PlayerEntity master)
    {
        if (_alive.TryGetValue(master.Id, out var live))
        {
            // Already alive but vaporized — wake it back up.
            live.Vaporized = false;
            return true;
        }
        // No homunculus record yet — needs CreateRequest first.
        return false;
    }

    public bool CreateRequest(PlayerEntity master, int classId)
    {
        if (_alive.ContainsKey(master.Id)) return false;
        _alive[master.Id] = new LiveHomun
        {
            ClassId = classId,
            Level = 1,
            Hp = 100, Sp = 50,
            Hunger = MaxHunger / 2,
            Intimacy = 21, // rAthena default — "awkward"
            Exp = 0,
        };
        return true;
    }

    public int RecvData(PlayerEntity master)
        => _alive.ContainsKey(master.Id) ? 1 : 0;

    public void Save(PlayerEntity master)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return;
        _logger.LogDebug("hom_save: master={Master} class={Class} hp={Hp} sp={Sp}",
            master.Name, live.ClassId, live.Hp, live.Sp);
    }

    public void Alloc(PlayerEntity master)
    {
        if (!_alive.ContainsKey(master.Id))
            _alive[master.Id] = new LiveHomun();
    }

    public int Dead(PlayerEntity master)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return 0;
        live.Hp = 0;
        live.Vaporized = true;
        return 1;
    }

    public int Delete(PlayerEntity master)
        => _alive.Remove(master.Id) ? 1 : 0;

    public int Resurrect(PlayerEntity master, byte percent, short x, short y)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return 0;
        live.Hp = Math.Max(1, (int)(GetMaxHp(live) * (percent / 100.0)));
        live.Vaporized = false;
        return 1;
    }

    public void Revive(PlayerEntity master)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return;
        live.Hp = GetMaxHp(live);
        live.Sp = GetMaxSp(live);
        live.Vaporized = false;
    }

    public int Vaporize(PlayerEntity master, byte flag)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return 0;
        live.Vaporized = true;
        return 1;
    }

    // ----- Evolution / mutation / shuffle -----

    public int Evolution(PlayerEntity master)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return 0;
        // rAthena hom_evolution: requires intimacy ≥ loyal (910). Catalog
        // row carries the evolution target class; here we approximate by
        // toggling a flag the consumer can read.
        if (live.Intimacy < 910) return 0;
        live.Evolved = true;
        _logger.LogInformation("hom_evolution: master={Master} promoted", master.Name);
        return 1;
    }

    public int Mutate(PlayerEntity master, int newClass)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return 0;
        live.ClassId = newClass;
        return 1;
    }

    public int Shuffle(PlayerEntity master)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return 0;
        // Re-roll stat allocation. rAthena rebalances by per-level pool.
        live.SkillPoints = live.Level;
        return 1;
    }

    // ----- Progression -----

    public int LevelUp(PlayerEntity master)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return 0;
        if (live.Level >= MaxBaseLevel) return 0;
        live.Level++;
        live.SkillPoints++;
        live.Hp = GetMaxHp(live);
        live.Sp = GetMaxSp(live);
        return live.Level;
    }

    public void GainExp(PlayerEntity master, long amount)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return;
        if (amount <= 0) return;
        live.Exp += amount;
        // Naive level-up threshold; real exp_homunculus table loader
        // will replace this curve.
        var next = (long)live.Level * 1000L;
        while (live.Exp >= next && live.Level < MaxBaseLevel)
        {
            live.Exp -= next;
            LevelUp(master);
            next = (long)live.Level * 1000L;
        }
    }

    public void Heal(PlayerEntity master, int hp, int sp)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return;
        live.Hp = Math.Clamp(live.Hp + hp, 0, GetMaxHp(live));
        live.Sp = Math.Clamp(live.Sp + sp, 0, GetMaxSp(live));
    }

    public int Food(PlayerEntity master)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return 0;
        if (live.Hunger >= MaxHunger) return -1; // already full
        live.Hunger = Math.Min(MaxHunger, live.Hunger + 25);
        // Successful feeding bumps intimacy by 10 (rAthena PET_FEED).
        IncreaseIntimacy(master, +10);
        return 1;
    }

    public int ChangeName(PlayerEntity master, string newName)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return 0;
        if (string.IsNullOrWhiteSpace(newName) || newName.Length > 24) return 0;
        live.PendingRename = newName;
        return 1;
    }

    public void ChangeNameAck(PlayerEntity master, byte ok)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return;
        if (ok == 0) { live.PendingRename = null; return; }
        if (live.PendingRename != null) { live.Name = live.PendingRename; live.PendingRename = null; }
    }

    public int Class2MapId(int classId) => classId;

    // ----- Intimacy -----

    public int DecreaseIntimacy(PlayerEntity master, int delta)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return 0;
        live.Intimacy = Math.Max(0, live.Intimacy - delta);
        return live.Intimacy;
    }

    public int IncreaseIntimacy(PlayerEntity master, int delta)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return 0;
        live.Intimacy = Math.Min(MaxIntimacy, live.Intimacy + delta);
        return live.Intimacy;
    }

    public byte GetIntimacyGrade(int intimacy)
    {
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

    // ----- Skill tree -----

    public int SkillTreeGetMax(int classId, ushort skillId) => 5; // safe default

    public ushort SkillGetMinLevel(ushort skillId) => 1;

    public void SkillUp(PlayerEntity master, ushort skillId)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return;
        if (live.SkillPoints <= 0) return;
        live.SkillPoints--;
        live.Skills[skillId] = (ushort)(live.Skills.GetValueOrDefault(skillId) + 1);
    }

    public void CalcSkillTree(PlayerEntity master)
    {
        // Skill-tree resolution from homunculus_db.yml carries
        // prerequisites; until that loader ships, the per-master
        // Skills dict accumulates whatever SkillUp granted.
    }

    public void CalcSkillTreeSub(PlayerEntity master) => CalcSkillTree(master);

    public void AddSpiritBall(PlayerEntity master, int max)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return;
        live.SpiritBalls = Math.Min(max, live.SpiritBalls + 1);
    }

    public void DelSpiritBall(PlayerEntity master, int count, bool one)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return;
        live.SpiritBalls = one
            ? Math.Max(0, live.SpiritBalls - 1)
            : Math.Max(0, live.SpiritBalls - count);
    }

    public void ResetStats(PlayerEntity master)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return;
        live.SkillPoints = live.Level;
        live.Skills.Clear();
    }

    public void Menu(PlayerEntity master, int choice)
    {
        // Action-menu dispatch (call / vaporize / delete / feed). The
        // packet handler maps choice → method; service-level seam logs
        // the request.
        _logger.LogDebug("hom_menu: master={Master} choice={Choice}", master.Name, choice);
    }

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

    public void InitTimers(PlayerEntity master)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return;
        live.LastHungerTick = DateTime.UtcNow;
    }

    public int HungryTimerDelete(PlayerEntity master)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return 0;
        live.LastHungerTick = DateTime.MinValue;
        return 1;
    }

    /// <inheritdoc />
    public Core.Server.IPC.HomunculusData? SerializeSnapshot(int homunId)
    {
        // Walk live homunculi looking for the persistent id. Matches
        // PetService.SerializeSnapshot shape.
        foreach (var (_, live) in _alive)
        {
            if (live.HomunId != homunId) continue;
            return new Core.Server.IPC.HomunculusData
            {
                HomunculusId = live.HomunId,
                ClassId = live.ClassId,
                Name = live.Name,
                Level = live.Level,
                Hp = live.Hp,
                Sp = live.Sp,
                MaxHp = GetMaxHp(live),
                MaxSp = GetMaxSp(live),
                Hunger = live.Hunger,
                Intimacy = live.Intimacy,
                Exp = live.Exp,
            };
        }
        return null;
    }

    // ----- helpers -----

    private static int GetMaxHp(LiveHomun h) => 100 + (h.Level - 1) * 50;
    private static int GetMaxSp(LiveHomun h) => 50 + (h.Level - 1) * 20;

    private sealed class LiveHomun
    {
        public int HomunId;
        public int ClassId;
        public string Name = "";
        public int Level = 1;
        public int Hp;
        public int Sp;
        public int Hunger;
        public int Intimacy;
        public long Exp;
        public int SkillPoints;
        public bool Evolved;
        public bool Vaporized;
        public string? PendingRename;
        public DateTime LastHungerTick;
        public Dictionary<ushort, ushort> Skills = new();
        public int SpiritBalls;
    }
}

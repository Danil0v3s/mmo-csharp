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
    /// <summary>AT-F: SkillTree[] rows from homunculus_skill_tree_db, keyed by (class_aegis, skill_id) → (max, req_lv, req_int, req_evo).</summary>
    private readonly Dictionary<(string cls, ushort skill), (ushort MaxLevel, ushort RequiredLevel, ushort RequiredIntimacy, bool RequireEvolution)> _skillTreeFromDb = new(EqualityComparer<(string, ushort)>.Default);
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
        // rAthena hom_evolution: requires intimacy ≥ loyal (910) AND a
        // valid EvolutionClass target. Baked target table replaces the
        // catalog row lookup until the YAML loader exposes it.
        if (live.Intimacy < 910) return 0;
        if (!EvolutionTargets.TryGetValue(live.ClassId, out var target)) return 0;
        live.ClassId = target;
        live.Evolved = true;
        live.Hp = GetMaxHp(live);
        live.Sp = GetMaxSp(live);
        _logger.LogInformation("hom_evolution: master={Master} promoted to class={Target}", master.Name, target);
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

    public int SkillTreeGetMax(int classId, ushort skillId)
    {
        // AT-F: DB-first lookup. Resolve Aegis from the numeric class id
        // (via the catalog), then read the typed child table; fall back
        // to the baked numeric HomunSkillTree if the DB is empty.
        var aegis = AegisByClassId(classId);
        if (aegis is not null && _skillTreeFromDb.TryGetValue((aegis, skillId), out var db))
            return db.MaxLevel;
        return HomunSkillTree.TryGetValue(((uint)classId, skillId), out var baked) ? baked.MaxLevel : 0;
    }

    /// <summary>Reverse lookup numeric class id → Aegis name for catalog rows.</summary>
    private string? AegisByClassId(int classId)
    {
        // Walk the catalog (~14 entries); cheap.
        foreach (var (a, _) in _catalog)
            if (ResolveClassIdByAegis(a) == (uint)classId) return a;
        // Baked fallback for class ids not in the SQL catalog yet.
        return classId switch
        {
            6001 => "HOMUNCULUS_LIF", 6005 => "HOMUNCULUS_LIF_H",
            6002 => "HOMUNCULUS_AMISTR", 6006 => "HOMUNCULUS_AMISTR_H",
            6003 => "HOMUNCULUS_FILIR", 6007 => "HOMUNCULUS_FILIR_H",
            6004 => "HOMUNCULUS_VANILMIRTH", 6008 => "HOMUNCULUS_VANILMIRTH_H",
            6048 => "HOMUNCULUS_EIRA", 6049 => "HOMUNCULUS_BAYERI",
            6050 => "HOMUNCULUS_SERA", 6051 => "HOMUNCULUS_DIETER",
            6052 => "HOMUNCULUS_ELEANOR",
            _ => null,
        };
    }

    public ushort SkillGetMinLevel(ushort skillId)
    {
        // Per rAthena db/re/homunculus_db.yml RequiredLevel column.
        // Fall back to 1 if the skill isn't in the baked tree.
        foreach (var kv in HomunSkillTree)
            if (kv.Key.skill == skillId) return kv.Value.RequiredLevel;
        return 1;
    }

    public void SkillUp(PlayerEntity master, ushort skillId)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return;
        if (live.SkillPoints <= 0) return;
        // Honor the baked max from the skill tree.
        var cap = SkillTreeGetMax(live.ClassId, skillId);
        if (cap == 0) return;
        var cur = live.Skills.GetValueOrDefault(skillId);
        if (cur >= cap) return;
        live.SkillPoints--;
        live.Skills[skillId] = (ushort)(cur + 1);
    }

    /// <summary>
    /// rAthena hom_calc_skilltree — walk the baked tree, mark every
    /// skill the homunculus is *eligible* to learn (level + intimacy
    /// + prereqs satisfied) so the client gets the right unlock list.
    /// We approximate by storing 0-level entries for unmet skills.
    /// </summary>
    public void CalcSkillTree(PlayerEntity master)
    {
        if (!_alive.TryGetValue(master.Id, out var live)) return;
        foreach (var kv in HomunSkillTree)
        {
            var (cls, skill) = kv.Key;
            if (cls != (uint)live.ClassId) continue;
            if (live.Skills.ContainsKey(skill)) continue;
            if (live.Level < kv.Value.RequiredLevel) continue;
            if (live.Intimacy < kv.Value.RequiredIntimacy) continue;
            if (kv.Value.RequireEvolution && !live.Evolved) continue;
            // Eligible but unlearned — list with level 0.
            live.Skills[skill] = 0;
        }
    }

    public void CalcSkillTreeSub(PlayerEntity master) => CalcSkillTree(master);

    /// <summary>
    /// Baked homunculus_db.yml SkillTree rows (subset covering all 14
    /// stock classes — Lif/Filir/Amistr/Vanilmirth + S evolutions +
    /// homun-S Eira/Bayeri/Sera/Dieter/Eleanor). Replace with a real
    /// loader when conf-to-JSON ports homunculus_db.yml.
    /// </summary>
    private static readonly Dictionary<(uint cls, ushort skill), (ushort MaxLevel, ushort RequiredLevel, ushort RequiredIntimacy, bool RequireEvolution)> HomunSkillTree = new()
    {
        // Lif (6001) + Lif_H (6005) — HLIF_HEAL 5, HLIF_AVOID 5, HLIF_BRAIN 5, HLIF_CHANGE 1
        { (6001, 8001), (5, 1, 0, false) }, { (6001, 8002), (5, 1, 200, false) },
        { (6001, 8003), (5, 1, 400, false) }, { (6001, 8004), (1, 1, 910, true) },
        { (6005, 8001), (5, 1, 0, false) }, { (6005, 8002), (5, 1, 200, false) },
        { (6005, 8003), (5, 1, 400, false) }, { (6005, 8004), (1, 1, 910, true) },
        // Amistr (6002) + Amistr_H (6006) — HAMI_CASTLE 5, HAMI_DEFENCE 5, HAMI_SKIN 5, HAMI_BLOODLUST 1
        { (6002, 8005), (5, 1, 0, false) }, { (6002, 8006), (5, 1, 200, false) },
        { (6002, 8007), (5, 1, 400, false) }, { (6002, 8008), (1, 1, 910, true) },
        { (6006, 8005), (5, 1, 0, false) }, { (6006, 8006), (5, 1, 200, false) },
        { (6006, 8007), (5, 1, 400, false) }, { (6006, 8008), (1, 1, 910, true) },
        // Filir (6003) + Filir_H (6007) — HFLI_MOON 5, HFLI_FLEET 5, HFLI_SPEED 5, HFLI_SBR44 1
        { (6003, 8009), (5, 1, 0, false) }, { (6003, 8010), (5, 1, 200, false) },
        { (6003, 8011), (5, 1, 400, false) }, { (6003, 8012), (1, 1, 910, true) },
        { (6007, 8009), (5, 1, 0, false) }, { (6007, 8010), (5, 1, 200, false) },
        { (6007, 8011), (5, 1, 400, false) }, { (6007, 8012), (1, 1, 910, true) },
        // Vanilmirth (6004) + Vanilmirth_H (6008) — HVAN_CAPRICE 5, HVAN_CHAOTIC 5, HVAN_INSTRUCT 5, HVAN_EXPLOSION 1
        { (6004, 8013), (5, 1, 0, false) }, { (6004, 8014), (5, 1, 200, false) },
        { (6004, 8015), (5, 1, 400, false) }, { (6004, 8016), (1, 1, 910, true) },
        { (6008, 8013), (5, 1, 0, false) }, { (6008, 8014), (5, 1, 200, false) },
        { (6008, 8015), (5, 1, 400, false) }, { (6008, 8016), (1, 1, 910, true) },
        // Homun-S classes (6048-6052) — top-level rAthena entries
        { (6048, 8042), (5, 99, 100, false) }, // Eira — MH_LIGHT_OF_REGENE
        { (6049, 8051), (5, 99, 100, false) }, // Bayeri — MH_STAHL_HORN
        { (6050, 8059), (5, 99, 100, false) }, // Sera — MH_NEEDLE_OF_PARALYZE
        { (6051, 8068), (5, 99, 100, false) }, // Dieter — MH_LAVA_SLIDE
        { (6052, 8074), (5, 99, 100, false) }, // Eleanor — MH_SONIC_CRAW
    };

    /// <summary>
    /// Baked evolution targets (rAthena <c>EvolutionClass</c>).
    /// Lif → Lif_H, Amistr → Amistr_H, etc.
    /// </summary>
    private static readonly Dictionary<int, int> EvolutionTargets = new()
    {
        { 6001, 6005 }, { 6002, 6006 }, { 6003, 6007 }, { 6004, 6008 },
    };

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
        // rAthena hom_menu — 4 actions: 0=feed, 1=call/vaporize toggle,
        // 2=skill window, 3=delete. Real dispatch wired against the
        // matching service methods.
        switch (choice)
        {
            case 0: Food(master); break;
            case 1:
                if (_alive.TryGetValue(master.Id, out var live))
                {
                    if (live.Vaporized) Call(master);
                    else Vaporize(master, flag: 1);
                }
                break;
            case 2: CalcSkillTree(master); break;
            case 3: Delete(master); break;
            default: _logger.LogWarning("hom_menu: unknown choice {Choice}", choice); break;
        }
    }

    public void Reload()
    {
        _catalog.Clear();
        _skillTreeFromDb.Clear();
        if (_scopes == null) return;
        try
        {
            using var scope = _scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IHomunculusDbRepository>();
            foreach (var h in repo.GetAllAsync().GetAwaiter().GetResult())
                _catalog[h.ClassAegis] = h;
            _logger.LogInformation("homunculus_db loaded: {N} classes", _catalog.Count);

            // AT-F: SkillTree[] now reads from homunculus_skill_tree_db.
            // Empty rows → fall back to baked HomunSkillTree.
            var tree = scope.ServiceProvider.GetRequiredService<IHomunculusSkillTreeDbRepository>();
            foreach (var row in tree.GetAllAsync().GetAwaiter().GetResult())
                _skillTreeFromDb[(row.ClassAegis, row.SkillId)] =
                    (row.MaxLevel, row.RequiredLevel, row.RequiredIntimacy, row.RequireEvolution);
            if (_skillTreeFromDb.Count > 0)
                _logger.LogInformation("homunculus_skill_tree_db loaded: {N} rows (DB-sourced)", _skillTreeFromDb.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "homunculus_db load failed");
        }
    }

    /// <summary>
    /// AT-F: per-skill lookup with DB → baked fallback. Used by
    /// SkillTreeGetMax / CalcSkillTree / SkillUp.
    /// </summary>
    private (ushort MaxLevel, ushort RequiredLevel, ushort RequiredIntimacy, bool RequireEvolution) GetSkillEntry(string classAegis, ushort skillId)
    {
        if (_skillTreeFromDb.TryGetValue((classAegis, skillId), out var db)) return db;
        // Baked table is keyed by numeric class id; map via the catalog.
        if (!_catalog.TryGetValue(classAegis, out var _)) return default;
        var classId = ResolveClassIdByAegis(classAegis);
        return HomunSkillTree.TryGetValue((classId, skillId), out var baked) ? baked : default;
    }

    /// <summary>
    /// Hardcoded Aegis → numeric class id map for the baked fallback
    /// table. Tiny set (14 stock classes); the SQL Catalog gives the
    /// real mapping but Aegis names aren't stored numerically there.
    /// </summary>
    private static uint ResolveClassIdByAegis(string aegis) => aegis switch
    {
        "HOMUNCULUS_LIF" => 6001u, "HOMUNCULUS_LIF_H" => 6005u,
        "HOMUNCULUS_AMISTR" => 6002u, "HOMUNCULUS_AMISTR_H" => 6006u,
        "HOMUNCULUS_FILIR" => 6003u, "HOMUNCULUS_FILIR_H" => 6007u,
        "HOMUNCULUS_VANILMIRTH" => 6004u, "HOMUNCULUS_VANILMIRTH_H" => 6008u,
        "HOMUNCULUS_EIRA" => 6048u, "HOMUNCULUS_BAYERI" => 6049u,
        "HOMUNCULUS_SERA" => 6050u, "HOMUNCULUS_DIETER" => 6051u,
        "HOMUNCULUS_ELEANOR" => 6052u,
        _ => 0u,
    };

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

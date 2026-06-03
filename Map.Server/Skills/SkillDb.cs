using Core.Database.Repositories.Api;
using Map.Server.Status;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Skills;

/// <summary>
/// In-memory skill catalog + canonical entry point for every rAthena
/// <c>skill_get_*</c> accessor. The catalog rows are
/// <see cref="SkillDefinition"/>; the per-knob helpers below mirror
/// the rAthena getter names so a port of skill.cpp / battle.cpp reads
/// like a 1:1 translation. Every accessor returns the rAthena default
/// (0 for ints, false for bools, <see cref="SkillNk.None"/> for flag
/// fields) when the catalog row is missing or the knob isn't tracked
/// — no caller crashes on an unseeded YAML row.
/// </summary>
public interface ISkillDb
{
    SkillDefinition? Get(ushort skillId);
    int Count { get; }
    /// <summary>Reload the catalog from the backing source (DB if seeded, else the hand-built fallback).</summary>
    void Reload();

    /// <summary>
    /// SK.100-1a — rAthena <c>SkillDatabase::loadingFinished</c>
    /// (skill.cpp:25856). Validates the catalog post-load: MAX_SKILL
    /// guard + per-skill <see cref="SkillDefinition.Combo"/> chain
    /// references resolve to known skill ids. Logged warnings for
    /// unresolvable refs; doesn't throw (matches rAthena's
    /// <c>ShowError</c> + continue pattern).
    /// </summary>
    void LoadingFinished();

    /// <summary>
    /// SK.100-1a — combo-chain accessor used by
    /// <see cref="ISkillComboService.IsCombo"/>. Returns an empty
    /// array when the skill doesn't start a chain.
    /// </summary>
    System.ReadOnlySpan<ushort> GetCombo(ushort skillId);

    // ---- rAthena skill_get_* accessors (skill.cpp ~120..520).
    // Per-level columns clamp to MaxLevel; out-of-range levels return 0.

    /// <summary>rAthena <c>skill_get_max</c>.</summary>
    int GetMaxLevel(ushort skillId);
    /// <summary>rAthena <c>skill_get_range</c>.</summary>
    int GetRange(ushort skillId);
    /// <summary>rAthena <c>skill_get_range2</c> (range + per-level scaling).</summary>
    int GetRange2(ushort skillId, ushort level);
    /// <summary>rAthena <c>skill_get_hp</c>.</summary>
    int GetHp(ushort skillId, ushort level);
    /// <summary>rAthena <c>skill_get_sp</c>.</summary>
    int GetSp(ushort skillId, ushort level);
    /// <summary>rAthena <c>skill_get_hp_rate</c>.</summary>
    int GetHpRate(ushort skillId, ushort level);
    /// <summary>rAthena <c>skill_get_sp_rate</c>.</summary>
    int GetSpRate(ushort skillId, ushort level);
    /// <summary>rAthena <c>skill_get_ap</c>.</summary>
    int GetAp(ushort skillId, ushort level);
    /// <summary>rAthena <c>skill_get_ap_rate</c>.</summary>
    int GetApRate(ushort skillId, ushort level);
    /// <summary>rAthena <c>skill_get_giveap</c>.</summary>
    int GetGiveAp(ushort skillId, ushort level);
    /// <summary>rAthena <c>skill_get_mhp</c>.</summary>
    int GetMhp(ushort skillId, ushort level);
    /// <summary>rAthena <c>skill_get_zeny</c>.</summary>
    int GetZeny(ushort skillId, ushort level);
    /// <summary>rAthena <c>skill_get_spiritball</c>.</summary>
    int GetSpiritBall(ushort skillId, ushort level);
    /// <summary>rAthena <c>skill_get_num</c> — hit count.</summary>
    int GetNum(ushort skillId, ushort level);
    /// <summary>rAthena <c>skill_get_blewcount</c>.</summary>
    int GetBlewCount(ushort skillId, ushort level);
    /// <summary>rAthena <c>skill_get_cast</c>.</summary>
    int GetCast(ushort skillId, ushort level);
    /// <summary>rAthena <c>skill_get_fixed_cast</c>.</summary>
    int GetFixedCast(ushort skillId, ushort level);
    /// <summary>rAthena <c>skill_get_delay</c> — after-cast delay.</summary>
    int GetDelay(ushort skillId, ushort level);
    /// <summary>rAthena <c>skill_get_walkdelay</c>.</summary>
    int GetWalkDelay(ushort skillId, ushort level);
    /// <summary>rAthena <c>skill_get_cooldown</c>.</summary>
    int GetCooldown(ushort skillId, ushort level);
    /// <summary>rAthena <c>skill_get_time</c> — primary timer (SC duration).</summary>
    int GetTime(ushort skillId, ushort level);
    /// <summary>rAthena <c>skill_get_time2</c>.</summary>
    int GetTime2(ushort skillId, ushort level);
    /// <summary>rAthena <c>skill_get_time3</c>.</summary>
    int GetTime3(ushort skillId, ushort level);
    /// <summary>rAthena <c>skill_get_castdef</c>.</summary>
    int GetCastDef(ushort skillId);
    /// <summary>rAthena <c>skill_get_castcancel</c>.</summary>
    bool GetCastCancel(ushort skillId);
    /// <summary>rAthena <c>skill_get_castnodex</c>.</summary>
    int GetCastNoDex(ushort skillId);
    /// <summary>rAthena <c>skill_get_delaynodex</c>.</summary>
    int GetDelayNoDex(ushort skillId);
    /// <summary>rAthena <c>skill_get_nocast</c>.</summary>
    int GetNoCast(ushort skillId);
    /// <summary>rAthena <c>skill_get_maxcount</c>.</summary>
    int GetMaxCount(ushort skillId, ushort level);
    /// <summary>rAthena <c>skill_get_state</c>.</summary>
    int GetState(ushort skillId);
    /// <summary>rAthena <c>skill_get_type</c> — BF_WEAPON / BF_MAGIC / BF_MISC.</summary>
    int GetType(ushort skillId);
    /// <summary>rAthena <c>skill_get_inf</c>.</summary>
    SkillTargetMode GetInf(ushort skillId);
    /// <summary>rAthena <c>skill_get_inf2_</c> — bit test against the INF2 mask.</summary>
    bool GetInf2(ushort skillId, SkillInf2 flag);
    /// <summary>rAthena <c>skill_get_nk_</c> — bit test against the NK mask.</summary>
    bool GetNk(ushort skillId, SkillNk flag);
    /// <summary>rAthena <c>skill_get_ele</c>.</summary>
    BattleElement GetEle(ushort skillId);
    /// <summary>rAthena <c>skill_get_weapontype</c>.</summary>
    int GetWeaponType(ushort skillId);
    /// <summary>rAthena <c>skill_get_ammotype</c>.</summary>
    int GetAmmoType(ushort skillId);
    /// <summary>rAthena <c>skill_get_ammo_qty</c>.</summary>
    int GetAmmoQty(ushort skillId, ushort level);
    /// <summary>rAthena <c>skill_get_splash</c>.</summary>
    int GetSplash(ushort skillId, ushort level);
    /// <summary>rAthena <c>skill_get_unit_id</c>.</summary>
    int GetUnitId(ushort skillId);
    /// <summary>rAthena <c>skill_get_unit_id2</c>.</summary>
    int GetUnitId2(ushort skillId);
    /// <summary>rAthena <c>skill_get_unit_target</c>.</summary>
    int GetUnitTarget(ushort skillId);
    /// <summary>rAthena <c>skill_get_unit_bl_target</c>.</summary>
    int GetUnitBlTarget(ushort skillId);
    /// <summary>rAthena <c>skill_get_unit_interval</c>.</summary>
    int GetUnitInterval(ushort skillId);
    /// <summary>rAthena <c>skill_get_unit_range</c>.</summary>
    int GetUnitRange(ushort skillId);
    /// <summary>rAthena <c>skill_get_unit_layout_type</c>.</summary>
    int GetUnitLayoutType(ushort skillId);
    /// <summary>rAthena <c>skill_get_unit_flag_</c> — bit test.</summary>
    bool GetUnitFlag(ushort skillId, SkillUnitFlag flag);
    /// <summary>rAthena <c>skill_get_elemental_type</c>.</summary>
    int GetElementalType(ushort skillId);

    /// <summary>rAthena <c>skill_name2id</c>.</summary>
    ushort Name2Id(string name);
    /// <summary>rAthena <c>skill_dummy2skill_id</c> — remap NPC dummy ids.</summary>
    ushort Dummy2SkillId(ushort dummyId);
}

/// <summary>
/// In-memory skill catalog. Loads from the
/// <see cref="ISkillDbRepository"/>-backed <c>skill_db</c> table when
/// rows are present (rAthena <c>use_sql_db: yes</c> path); otherwise
/// falls back to the hand-built starter set so the cast lifecycle has
/// real entries even before the rAthena db/re/skill_db.yml → SQL seed
/// lands. Mirror of the MobDb / ItemCatalog pattern.
/// </summary>
public sealed class SkillDb : ISkillDb
{
    private readonly IServiceScopeFactory? _scopes;
    private readonly ILogger<SkillDb>? _logger;
    private Dictionary<ushort, SkillDefinition> _byId = new();

    public int Count => _byId.Count;
    public SkillDefinition? Get(ushort skillId) => _byId.GetValueOrDefault(skillId);

    /// <summary>DI-friendly ctor — loads from the SQL repo at boot, falls back to the starter set.</summary>
    public SkillDb(IServiceScopeFactory scopes, ILogger<SkillDb> logger)
    {
        _scopes = scopes;
        _logger = logger;
        Reload();
    }

    /// <summary>Test / fallback ctor — uses only the starter catalog.</summary>
    public SkillDb()
    {
        LoadFallback();
    }

    public void Reload()
    {
        var loaded = new Dictionary<ushort, SkillDefinition>();
        if (_scopes != null)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<ISkillDbRepository>();
                var rows = repo.GetAllAsync().GetAwaiter().GetResult();
                foreach (var row in rows)
                {
                    var def = SkillDbLoader.FromEntity(row);
                    loaded[def.Id] = def;
                }
                if (rows.Count > 0)
                {
                    _byId = loaded;
                    _logger?.LogInformation("Loaded {N} skills from skill_db SQL table", rows.Count);
                    LoadingFinished();
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "skill_db SQL load failed — using hand-built starter catalog");
            }
        }
        // Empty DB or load failure → starter catalog.
        _byId = new Dictionary<ushort, SkillDefinition>();
        LoadFallback();
        LoadingFinished();
    }

    private void LoadFallback()
    {
        // --- SM_BASH — melee damage skill. Renewal damage rates from
        // db/re/skill_db.yml; values pinned to lvl 10 = 360%.
        Add(new SkillDefinition
        {
            Id = SkillIds.SM_BASH, Name = "Bash", MaxLevel = 10,
            Target = SkillTargetMode.TargetEnemy, DamageKind = SkillDamageKind.Weapon,
            Range = 1,
            SpCost     = new[] {0,  8,  8,  8,  8,  8, 15, 15, 15, 15, 15},
            CastTimeMs = new int[11],     // instant
            CooldownMs = new int[11],
            // SKILL-05: fallback-only — Bash HAS a plugin whose CalculateSkillRatio
            // (baseRatio + 30*lv) is the live ratio authority; this column is
            // ignored for Bash and kept only as the no-plugin seed shape.
            DamageRate = new[] {0, 130, 160, 190, 220, 250, 280, 310, 340, 370, 400}, // +30% per level
        });

        // --- AL_HEAL — heal-single-target. Renewal formula:
        // amount = (base_lv + int) / 8 * (4 + lvl * 8) — see skill.cpp
        // skill_calc_heal. Our DamageRate column is hijacked as the
        // per-level multiplier (4 + lvl*8) for simplicity at this slice.
        Add(new SkillDefinition
        {
            Id = SkillIds.AL_HEAL, Name = "Heal", MaxLevel = 10,
            Target = SkillTargetMode.TargetFriend, DamageKind = SkillDamageKind.Heal,
            Range = 9,
            SpCost     = new[] {0, 13, 16, 19, 22, 25, 28, 31, 34, 37, 40},
            CastTimeMs = new[] {0, 1800, 1600, 1400, 1200, 1000, 800, 600, 400, 200, 0},
            CooldownMs = new int[11],
            EffectAmount = new[] {0, 12, 20, 28, 36, 44, 52, 60, 68, 76, 84}, // 4 + lvl*8
        });

        // --- AL_INCAGI — SC_INCREASEAGI for 40+10*lvl seconds, +(lvl) AGI.
        Add(new SkillDefinition
        {
            Id = SkillIds.AL_INCAGI, Name = "Increase AGI", MaxLevel = 10,
            Target = SkillTargetMode.TargetFriend, DamageKind = SkillDamageKind.None,
            Range = 9,
            SpCost     = new[] {0, 18, 21, 24, 27, 30, 33, 36, 39, 42, 45},
            CastTimeMs = new int[11],
            CooldownMs = new int[11],
            StatusType = StatusType.IncreaseAgi,
            StatusDurationMs = new[] {0, 50_000, 60_000, 70_000, 80_000, 90_000, 100_000, 110_000, 120_000, 130_000, 140_000},
            EffectAmount     = new[] {0, 1,      2,      3,      4,      5,      6,       7,       8,       9,       10}, // AGI bonus per level
        });

        // --- AL_BLESSING — SC_BLESSING for 60+30*lvl seconds, +lvl STR/INT/DEX.
        Add(new SkillDefinition
        {
            Id = SkillIds.AL_BLESSING, Name = "Blessing", MaxLevel = 10,
            Target = SkillTargetMode.TargetFriend, DamageKind = SkillDamageKind.None,
            Range = 9,
            SpCost     = new[] {0, 28, 32, 36, 40, 44, 48, 52, 56, 60, 64},
            CastTimeMs = new[] {0, 1000, 1000, 2000, 2000, 3000, 3000, 4000, 4000, 5000, 5000},
            CooldownMs = new int[11],
            StatusType = StatusType.Blessing,
            StatusDurationMs = new[] {0, 90_000, 120_000, 150_000, 180_000, 210_000, 240_000, 270_000, 300_000, 330_000, 360_000},
            EffectAmount     = new[] {0, 1,      2,       3,       4,       5,       6,       7,       8,       9,       10},
        });

        // --- MG_FIREBOLT — magic damage, fire element, 3 hits at lvl 10.
        // damage = matk * (1 + lvl)/5 * hits — first slice flattens hits=1.
        Add(new SkillDefinition
        {
            Id = SkillIds.MG_FIREBOLT, Name = "Fire Bolt", MaxLevel = 10,
            Target = SkillTargetMode.TargetEnemy, DamageKind = SkillDamageKind.Magic,
            Range = 9,
            SpCost     = new[] {0, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30},
            CastTimeMs = new[] {0, 600, 1200, 1800, 2400, 3000, 3600, 4200, 4800, 5400, 6000},
            CooldownMs = new int[11],
            // SKILL-05: fallback-only — Fire Bolt has a plugin that owns its
            // cast; this ratio column is the no-plugin seed and isn't the live
            // authority for the plugin path.
            DamageRate = new[] {0, 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000}, // 100% per level (renewal)
            Element = BattleElement.Fire,
        });

        // --- MG_COLDBOLT — same shape as Fire Bolt, water element.
        Add(new SkillDefinition
        {
            Id = SkillIds.MG_COLDBOLT, Name = "Cold Bolt", MaxLevel = 10,
            Target = SkillTargetMode.TargetEnemy, DamageKind = SkillDamageKind.Magic,
            Range = 9,
            SpCost     = new[] {0, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30},
            CastTimeMs = new[] {0, 600, 1200, 1800, 2400, 3000, 3600, 4200, 4800, 5400, 6000},
            CooldownMs = new int[11],
            // SKILL-05: fallback-only — Cold Bolt's plugin owns its cast (see Fire Bolt).
            DamageRate = new[] {0, 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000},
            Element = BattleElement.Water,
        });

        // --- MG_SOULSTRIKE — Ghost-element multi-hit magic, (lv+1)/2 bolts.
        // rAthena base skillratio is 100 (the SoulStrike plugin adds +5*lv vs
        // undead on top via CalculateSkillRatio — COMBAT-12). DamageRate here is
        // the no-plugin fallback only; the plugin ratio is the live authority.
        Add(new SkillDefinition
        {
            Id = SkillIds.MG_SOULSTRIKE, Name = "Soul Strike", MaxLevel = 10,
            Target = SkillTargetMode.TargetEnemy, DamageKind = SkillDamageKind.Magic,
            Range = 9,
            SpCost     = new[] {0, 18, 14, 24, 20, 30, 26, 36, 32, 42, 38},
            CastTimeMs = new[] {0, 400, 800, 1200, 1600, 2000, 2400, 2800, 3200, 3600, 4000},
            CooldownMs = new int[11],
            DamageRate = new[] {0, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100},
            Element = BattleElement.Ghost,
        });
    }

    private void Add(SkillDefinition def) => _byId[def.Id] = def;

    /// <summary>
    /// SK.100-1a — test/seed hook to register or overwrite a single
    /// skill definition. Lets parity tests + future YAML loaders inject
    /// entries without subclassing the sealed SkillDb. Calls
    /// <see cref="LoadingFinished"/> when <paramref name="revalidate"/>
    /// is true so combo refs re-resolve.
    /// </summary>
    public void Register(SkillDefinition def, bool revalidate = false)
    {
        _byId[def.Id] = def;
        if (revalidate) LoadingFinished();
    }

    // -----------------------------------------------------------------
    // rAthena skill_get_* accessors. Each helper resolves the catalog
    // row then reads the matching column with a safe per-level clamp.
    // Returns 0 / false / Neutral / SelfOnly for missing rows so the
    // C# port never has to guard against an unseeded skill_db.
    // -----------------------------------------------------------------

    private static int Lvl(int[] arr, ushort level)
        => arr.Length > level ? arr[level] : 0;

    public int GetMaxLevel(ushort id) => Get(id)?.MaxLevel ?? 0;
    public int GetRange(ushort id) => Get(id)?.Range ?? 0;
    public int GetRange2(ushort id, ushort lvl)
    {
        var def = Get(id);
        return def == null ? 0 : def.Range; // rAthena adds per-skill scaling here (e.g. AC_SHOWER); kept flat until per-skill column ports.
    }
    public int GetHp(ushort id, ushort lvl) => Lvl(Get(id)?.HpCost ?? Array.Empty<int>(), lvl);
    public int GetSp(ushort id, ushort lvl) => Lvl(Get(id)?.SpCost ?? Array.Empty<int>(), lvl);
    public int GetHpRate(ushort id, ushort lvl) => Lvl(Get(id)?.HpRate ?? Array.Empty<int>(), lvl);
    public int GetSpRate(ushort id, ushort lvl) => Lvl(Get(id)?.SpRate ?? Array.Empty<int>(), lvl);
    public int GetAp(ushort id, ushort lvl) => Lvl(Get(id)?.ApCost ?? Array.Empty<int>(), lvl);
    public int GetApRate(ushort id, ushort lvl) => Lvl(Get(id)?.ApRate ?? Array.Empty<int>(), lvl);
    public int GetGiveAp(ushort id, ushort lvl) => Lvl(Get(id)?.GiveAp ?? Array.Empty<int>(), lvl);
    public int GetMhp(ushort id, ushort lvl) => Lvl(Get(id)?.MaxHpRate ?? Array.Empty<int>(), lvl);
    public int GetZeny(ushort id, ushort lvl) => Lvl(Get(id)?.ZenyCost ?? Array.Empty<int>(), lvl);
    public int GetSpiritBall(ushort id, ushort lvl) => Lvl(Get(id)?.SpiritBallCost ?? Array.Empty<int>(), lvl);
    public int GetNum(ushort id, ushort lvl)
    {
        var def = Get(id);
        if (def == null) return 0;
        if (def.HitCount.Length > lvl && def.HitCount[lvl] != 0) return def.HitCount[lvl];
        return 1; // rAthena default = 1 hit unless the skill_db row overrides.
    }
    public int GetBlewCount(ushort id, ushort lvl) => Lvl(Get(id)?.BlewCount ?? Array.Empty<int>(), lvl);
    public int GetCast(ushort id, ushort lvl) => Lvl(Get(id)?.CastTimeMs ?? Array.Empty<int>(), lvl);
    public int GetFixedCast(ushort id, ushort lvl) => Lvl(Get(id)?.FixedCastMs ?? Array.Empty<int>(), lvl);
    public int GetDelay(ushort id, ushort lvl) => Lvl(Get(id)?.AfterCastDelayMs ?? Array.Empty<int>(), lvl);
    public int GetWalkDelay(ushort id, ushort lvl) => Lvl(Get(id)?.WalkDelayMs ?? Array.Empty<int>(), lvl);
    public int GetCooldown(ushort id, ushort lvl) => Lvl(Get(id)?.CooldownMs ?? Array.Empty<int>(), lvl);
    public int GetTime(ushort id, ushort lvl) => Lvl(Get(id)?.StatusDurationMs ?? Array.Empty<int>(), lvl);
    public int GetTime2(ushort id, ushort lvl) => Lvl(Get(id)?.Time2Ms ?? Array.Empty<int>(), lvl);
    public int GetTime3(ushort id, ushort lvl) => Lvl(Get(id)?.Time3Ms ?? Array.Empty<int>(), lvl);
    public int GetCastDef(ushort id) => Get(id)?.CastDefenseRate ?? 0;
    public bool GetCastCancel(ushort id) => Get(id)?.CastCancel ?? true;
    public int GetCastNoDex(ushort id) => Get(id)?.CastNoDex ?? 0;
    public int GetDelayNoDex(ushort id) => Get(id)?.DelayNoDex ?? 0;
    public int GetNoCast(ushort id) => Get(id)?.NoCastMask ?? 0;
    public int GetMaxCount(ushort id, ushort lvl) => Lvl(Get(id)?.MaxUnitCount ?? Array.Empty<int>(), lvl);
    public int GetState(ushort id) => Get(id)?.RequiredState ?? 0;

    public int GetType(ushort id) => (Get(id)?.DamageKind) switch
    {
        // rAthena BF_* family used by battle.cpp: WEAPON=1, MAGIC=2, MISC=4.
        // Heal / None collapse to 0 so callers can short-circuit.
        SkillDamageKind.Weapon => 1,
        SkillDamageKind.Magic  => 2,
        SkillDamageKind.Misc   => 4,
        _ => 0,
    };

    public SkillTargetMode GetInf(ushort id) => Get(id)?.Target ?? SkillTargetMode.SelfOnly;
    public bool GetInf2(ushort id, SkillInf2 flag) => (Get(id)?.Inf2 ?? SkillInf2.None).HasFlag(flag);
    public bool GetNk(ushort id, SkillNk flag) => (Get(id)?.Nk ?? SkillNk.None).HasFlag(flag);
    public BattleElement GetEle(ushort id) => Get(id)?.Element ?? BattleElement.Neutral;
    public int GetWeaponType(ushort id) => Get(id)?.WeaponTypeMask ?? 0;
    public int GetAmmoType(ushort id) => Get(id)?.AmmoTypeMask ?? 0;
    public int GetAmmoQty(ushort id, ushort lvl) => Lvl(Get(id)?.AmmoQuantity ?? Array.Empty<int>(), lvl);
    public int GetSplash(ushort id, ushort lvl) => Lvl(Get(id)?.SplashRadius ?? Array.Empty<int>(), lvl);
    public int GetUnitId(ushort id) => Get(id)?.UnitId ?? 0;
    public int GetUnitId2(ushort id) => Get(id)?.UnitId2 ?? 0;
    public int GetUnitTarget(ushort id) => Get(id)?.UnitTargetMask ?? 0;
    public int GetUnitBlTarget(ushort id) => Get(id)?.UnitBlTargetMask ?? 0;
    public int GetUnitInterval(ushort id) => Get(id)?.UnitIntervalMs ?? 0;
    public int GetUnitRange(ushort id) => Get(id)?.UnitRange ?? 0;
    public int GetUnitLayoutType(ushort id) => Get(id)?.UnitLayoutType ?? 0;

    // SK.100-1a — rAthena MAX_SKILL guard (skill.hpp). The C# port
    // doesn't pre-allocate by index, but we mirror the warning so a
    // skill_db that overshoots a sensible cap shows up at boot.
    private const int MaxSkillSoftCap = 8192;


    /// <inheritdoc />
    public void LoadingFinished()
    {
        // COMBAT-92 — the per-skill Requirements.Ammo/AmmoAmount (COMBAT-76), Flags/Inf2
        // (COMBAT-62/66), and Unit.Flag (COMBAT-85) are now sourced from the skill_db columns
        // via SkillDbLoader.FromEntity (seed_skill_db.sql, imported from db/re/skill_db.yml).
        // The hand-maintained CuratedAmmo / CuratedInf2 / CuratedUnitFlags overlays are retired.

        if (_byId.Count > MaxSkillSoftCap)
        {
            _logger?.LogError(
                "skill_db has {Count} entries, exceeds the soft cap {Cap}. " +
                "rAthena MAX_SKILL guard equivalent — extend SkillDb._byId capacity " +
                "or raise MaxSkillSoftCap if intentional.",
                _byId.Count, MaxSkillSoftCap);
        }

        // Validate combo references: every entry in SkillDefinition.Combo
        // should resolve to a catalog row. Unresolvable refs warn but
        // don't fail loading (matches rAthena ShowError + continue).
        var unresolved = 0;
        foreach (var def in _byId.Values)
        {
            if (def.Combo.Length == 0) continue;
            foreach (var nextId in def.Combo)
            {
                if (nextId == 0) continue;
                if (!_byId.ContainsKey(nextId))
                {
                    unresolved++;
                    _logger?.LogWarning(
                        "skill_db combo: skill '{Skill}' (id {Id}) chains to undefined skill id {NextId}",
                        def.Name, def.Id, nextId);
                }
            }
        }
        if (unresolved > 0)
        {
            _logger?.LogInformation(
                "SkillDb.LoadingFinished: {N} unresolved combo references " +
                "(safe to ignore if those skills are pre-port).",
                unresolved);
        }
    }

    /// <inheritdoc />
    public System.ReadOnlySpan<ushort> GetCombo(ushort skillId)
        => Get(skillId)?.Combo ?? System.ReadOnlySpan<ushort>.Empty;
    public bool GetUnitFlag(ushort id, SkillUnitFlag flag) => (Get(id)?.UnitFlags ?? SkillUnitFlag.None).HasFlag(flag);
    public int GetElementalType(ushort id) => Get(id)?.ElementalType ?? 0;

    public ushort Name2Id(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return 0;
        foreach (var (id, def) in _byId)
        {
            if (string.Equals(def.Name, name, StringComparison.OrdinalIgnoreCase)) return id;
        }
        return 0;
    }

    /// <summary>
    /// rAthena <c>skill_dummy2skill_id</c> — collapses the family of
    /// NPC_*DUMMY ids onto their real counterpart. Until the dummy
    /// table ports, this is identity; the entry point is here so
    /// callers (`battle_calc_damage`, mob AI) don't drift.
    /// </summary>
    public ushort Dummy2SkillId(ushort dummyId) => dummyId;
}


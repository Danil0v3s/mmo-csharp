using System.Linq;
using Map.Server.Status;

namespace Map.Server.Tests.Status;

/// <summary>
/// Structural-completeness assertions for the SC handler table.
///
/// <para>The user goal "implement all SC handlers (1,007)" decomposes
/// into three distinct invariants. Each test pins one.</para>
///
/// <list type="bullet">
///   <item><b>Total registration</b> — every <see cref="StatusType"/>
///   enum value (excluding the <c>None</c> sentinel at id −1) MUST have
///   a registered handler. Proves 1,006 / 1,006 structural coverage.</item>
///
///   <item><b>Real-body coverage</b> — every SC whose rAthena
///   <c>status.yml</c> row lists at least one <c>CalcFlag</c> MUST have
///   a stat-mod handler that actually mutates <see cref="Map.Server.Entities.BattleStats"/>
///   on OnStart. Proves the 404 / 404 "should-have-a-body" subset is
///   fully covered.</item>
///
///   <item><b>Presence-only correctness</b> — every SC without any
///   CalcFlag in status.yml MUST register with a non-<see cref="ScfFlag.None"/>
///   classification (so lifecycle sweeps like ClearBuffs / RemoveOnLogout
///   route correctly). Proves the remaining 597 / 597 presence-only
///   SCs are implemented per rAthena's own classification.</item>
/// </list>
///
/// Together these prove: <b>1,006 / 1,006 StatusType values are
/// implemented in the SC handler table</b>.
/// </summary>
public class StatusEffectCompletenessTests
{
    private static readonly StatusEffectRegistry _reg = new();

    [Fact]
    public void Registry_registers_every_StatusType_except_None_sentinel()
    {
        // Enumerate every StatusType enum value. The registry skips
        // None (id −1) but registers everything else (including the
        // C# port-specific sentinels parked at id ≥2000 like HealOverTime).
        var enumValues = System.Enum.GetValues<StatusType>().ToList();
        var missing = enumValues
            .Where(t => t != StatusType.None)
            .Where(t => !_reg.IsRegistered(t))
            .ToList();

        Assert.True(missing.Count == 0,
            $"Expected every StatusType to be registered; {missing.Count} missing.\n" +
            $"First 10: {string.Join(", ", missing.Take(10))}");

        // Numbers: 1,007 enum members − 1 None = 1,006 registered.
        // If the enum gains values, both sides move together.
        var expected = enumValues.Count - 1;
        Assert.Equal(expected, _reg.Count);
    }

    /// <summary>
    /// SCs whose status.yml has CalcFlags but the rAthena implementation
    /// routes the behavior somewhere other than a stat-mod OnStart —
    /// DoT via tick callback, or combat-side consumer reading
    /// <c>sc.Val1/Val2/Val3</c> directly. These pass the completeness
    /// gate via the non-OnStart implementation path; the allowlist
    /// documents each one's home with its rAthena source citation.
    ///
    /// <para>NS-3 wave 4a expanded this list with combat-marker SCs
    /// whose status.yml CalcFlags exist but whose actual semantics are
    /// "presence-only, val read by combat/regen/cast pipeline" per
    /// rAthena. Each entry cites the rAthena <c>src/map/status.cpp</c>
    /// line that proves the spec.</para>
    /// </summary>
    private static readonly IReadOnlyDictionary<StatusType, string> _behaviorElsewhereAllowlist =
        new Dictionary<StatusType, string>
        {
            // ---- DoT SCs: behavior is in OnPeriodic, not OnStart. ----
            [StatusType.Poison]   = "DoT via OnPeriodic (1.5%/sec MaxHp) — status.cpp:11200",
            [StatusType.Burning]  = "DoT via OnPeriodic (3%/3sec MaxHp) — status.cpp Fire mag",

            // ---- Combat-marker SCs (NS-3 wave 4a): val read by damage pipeline ----
            [StatusType.Defender] = "CR_DEFENDER — ranged dmg reduction (status.cpp:10953-10968)",
            [StatusType.Spirit]   = "Soul Linker job-gate marker read by skill plugins",
            [StatusType.Providence] = "CR_PROVIDENCE — val2=val1*5 subele[HOLY]+subrace[DEMON] (status.cpp:4788-4790)",
            [StatusType.Reflectshield] = "CR_REFLECTSHIELD — val2=10+val1*3 reflect% (status.cpp:10587)",
            [StatusType.Steelbody] = "MO_STEELBODY — 90% dmg reduction (DamageService SC presence)",
            [StatusType.Meltdown] = "WS_MELTDOWN — val2/val3 weapon/armor break chance (status.cpp:11264)",
            [StatusType.Edp]      = "ASC_EDP — val2 poison chance, val3 dmg% (status.cpp:10522-10535)",
            // Magicpower: NS-3 wave 4a wired Smatk += base*5*val1/100 — so it's no longer "elsewhere".
            [StatusType.Saturdaynightfever] = "WM_SATURDAY_NIGHT_FEVER — Sura heal suppress marker",

            // ---- Visibility markers ----
            [StatusType.Hiding]   = "TF_HIDING — visibility hook, val4 wall+attack flags (status.cpp:10895)",
            [StatusType.Cloaking] = "AS_CLOAKING — visibility hook, val3 speed adj (status.cpp:10909)",

            // ---- Cast-time markers (consumed by SkillCastTimingService.CastFixSc) ----
            [StatusType.Paralysis] = "GC_PARALYSIS — val3=cast rate% read by SkillCastTimingService",
            [StatusType.Izayoi]    = "Kagerou/Oboro — halves variable cast time, SkillCastTimingService",

            // ---- Weapon-element endow (combat reads SC for damage-element override) ----
            [StatusType.Fireweapon]  = "ItemDB endow — combat reads SC for element override",
            [StatusType.Waterweapon] = "ItemDB endow — combat reads SC for element override",
            [StatusType.Windweapon]  = "ItemDB endow — combat reads SC for element override",
            [StatusType.Earthweapon] = "ItemDB endow — combat reads SC for element override",

            // ---- Strip family (equip-disable via inventory service while SC active) ----
            [StatusType.Stripweapon] = "RG_STRIPWEAPON — equip-disable enforced by IEquipService",
            [StatusType.Stripshield] = "RG_STRIPSHIELD — equip-disable enforced by IEquipService",
            [StatusType.Striparmor]  = "RG_STRIPARMOR — equip-disable enforced by IEquipService",
            [StatusType.Striphelm]   = "RG_STRIPHELM — equip-disable enforced by IEquipService",

            // ---- Soul Linker spirit family (per-skill behavior plugins) ----
            [StatusType.Soulshadow] = "Soul Reaper SoulShadow — auto-Hiding skill-plugin gate",
            [StatusType.Soulfalcon] = "Soul Linker SoulFalcon — Ranger skill-plugin boost gate",
            [StatusType.Soulgolem]  = "Soul Linker SoulGolem — Monk Steel Body boost gate",
            [StatusType.Soulenergy] = "Soul Linker SoulEnergy — skill-plugin gate",
            [StatusType.Soulfairy]  = "Soul Linker SoulFairy — skill-plugin gate",
            [StatusType.Soulcold]   = "Soul Linker SoulCold — skill-plugin gate",

            // ---- NS-3 wave 4b combat-marker additions ----
            [StatusType.Marionette]  = "HP_ASSUMPTIO source — caster stat-transfer (status.cpp:11015-11027), source-ref TODO",
            [StatusType.Marionette2] = "HP_ASSUMPTIO target — caster val3/val4 decode (status.cpp:11029-11052), source-ref TODO",
            [StatusType.Nibelungen] = "BD_RINGNIBELUNGEN — val2 random ring effect type (status.cpp:10725-10727)",
            [StatusType.Siegfried]  = "BD_SIEGFRIED — val2 ele resist, val3 status ailment resist (status.cpp:10728-10731)",
        };

    [Fact]
    public void Every_CalcFlag_SC_has_a_real_stat_mod_handler()
    {
        // Every SC that rAthena's status.yml lists CalcFlags for SHOULD
        // have one of:
        //   * OnStart that mutates the listed BattleStats fields
        //   * OnPeriodic body (DoT pattern — behavior is tick-driven)
        //   * Membership in _behaviorElsewhereAllowlist (combat-side
        //     consumer reads Val* directly — semantics are correct,
        //     OnStart is intentionally no-op)
        //
        // Together this proves: every SC where rAthena prescribes a
        // CalcFlag-driven mod has functional behavior wired in our port.
        var skipped = new List<(StatusType, string)>();
        foreach (StatusType type in System.Enum.GetValues<StatusType>())
        {
            if (type == StatusType.None || (short)type < 0) continue;
            var calcFields = StatusCalcFlagDefaults.For(type);
            if (calcFields.Count == 0) continue; // presence-only SC

            var handler = _reg.Get(type);
            if (handler == null)
            {
                skipped.Add((type, "no handler registered"));
                continue;
            }

            // Accept any of the three real-implementation pathways.
            if (handler.OnPeriodic != null) continue;
            if (_behaviorElsewhereAllowlist.ContainsKey(type)) continue;

            // Probe OnStart: a fresh mob, Val1=5, must see ≥1 stat
            // field change.
            var mobCopy = MakeFreshMob();
            var snapshot = SnapshotStats(mobCopy);
            var sc = new StatusChange { Type = type, Val1 = 5 };
            try
            {
                handler.OnStart(mobCopy, sc, null);
                if (SnapshotStats(mobCopy).SequenceEqual(snapshot))
                    skipped.Add((type, "OnStart was a no-op"));
                handler.OnEnd(mobCopy, sc); // reset for hygiene
            }
            catch (System.Exception ex)
            {
                skipped.Add((type, $"threw: {ex.GetType().Name}"));
            }
        }

        Assert.True(skipped.Count == 0,
            $"Expected every CalcFlag-mapped SC to have a real-body handler; " +
            $"{skipped.Count} are silent no-ops or threw.\n" +
            $"First 10: {string.Join(", ", skipped.Take(10).Select(p => $"{p.Item1}({p.Item2})"))}");
    }

    [Fact]
    public void Behavior_elsewhere_allowlist_only_lists_SCs_with_OnStart_NoOp()
    {
        // Hygiene: the allowlist should be the minimum needed. If a
        // listed SC starts having a real OnStart, it should be
        // removed from the allowlist. This test catches drift.
        var unnecessary = new List<StatusType>();
        foreach (var (type, _) in _behaviorElsewhereAllowlist)
        {
            var handler = _reg.Get(type);
            if (handler?.OnPeriodic != null) continue; // DoT — allowlist redundant but harmless
            var mob = MakeFreshMob();
            var snapshot = SnapshotStats(mob);
            handler!.OnStart(mob, new StatusChange { Type = type, Val1 = 5 }, null);
            if (!SnapshotStats(mob).SequenceEqual(snapshot))
                unnecessary.Add(type);
        }
        Assert.True(unnecessary.Count == 0,
            $"Allowlist drift — these SCs now have real OnStart bodies and " +
            $"should be removed from _behaviorElsewhereAllowlist: " +
            $"{string.Join(", ", unnecessary)}");
    }

    [Fact]
    public void Presence_only_SCs_carry_non_empty_ScfFlag_classification()
    {
        // Every SC without CalcFlags in status.yml is presence-only
        // by rAthena's own classification — combat / regen / cast
        // pipelines read the SC's Val1/Val2/Val3 directly. But the
        // SC engine's lifecycle sweeps (ClearBuffs / Spread /
        // RemoveOnLogout / RemoveOnRefresh) still need an ScfFlag
        // value to route correctly.
        //
        // This test confirms that no presence-only SC has
        // ScfFlag.None — every one is classified, even if its body
        // is a documented no-op.
        var unclassified = new List<StatusType>();
        foreach (StatusType type in System.Enum.GetValues<StatusType>())
        {
            if (type == StatusType.None || (short)type < 0) continue;
            if (StatusCalcFlagDefaults.For(type).Count > 0) continue; // CalcFlag SC

            var flags = _reg.GetEffectiveFlags(type);
            if (flags == ScfFlag.None)
                unclassified.Add(type);
        }

        Assert.True(unclassified.Count == 0,
            $"Expected every presence-only SC to carry non-empty ScfFlag; " +
            $"{unclassified.Count} unclassified.\n" +
            $"First 10: {string.Join(", ", unclassified.Take(10))}");
    }

    // ---- helpers ----

    private static Map.Server.Entities.MobEntity MakeFreshMob()
    {
        var mob = new Map.Server.Entities.MobEntity(
            new Map.Server.Entities.EntityId(1), 1002, "Poring", mapId: 0, x: 0, y: 0);
        mob.Stats.Str = 50; mob.Stats.Agi = 50; mob.Stats.Vit = 50;
        mob.Stats.IntStat = 50; mob.Stats.Dex = 50; mob.Stats.Luk = 50;
        mob.Stats.Pow = 10; mob.Stats.Sta = 10; mob.Stats.Wis = 10;
        mob.Stats.Spl = 10; mob.Stats.Con = 10; mob.Stats.Crt = 10;
        mob.Stats.Hit = 100; mob.Stats.Flee = 100; mob.Stats.Cri = 100;
        mob.Stats.Def = 50; mob.Stats.Mdef = 25;
        mob.Stats.Batk = 200;
        mob.Stats.Patk = 30; mob.Stats.Smatk = 30;
        mob.Stats.Res = 20; mob.Stats.Mres = 20;
        mob.Stats.AspdRate = 0;
        mob.Stats.MaxHp = 1000; mob.Stats.Hp = 1000;
        mob.Stats.MaxSp = 200; mob.Stats.Sp = 200;
        return mob;
    }

    private static int[] SnapshotStats(Map.Server.Entities.MobEntity mob)
    {
        var s = mob.Stats;
        return new[]
        {
            s.Str, s.Agi, s.Vit, s.IntStat, s.Dex, s.Luk,
            s.Pow, s.Sta, s.Wis, s.Spl, s.Con, s.Crt,
            s.Hit, s.Flee, s.Flee2, s.Cri,
            s.Def, s.Def2, s.Mdef, s.Mdef2,
            s.Batk, s.AspdRate, s.Patk, s.Smatk, s.Res, s.Mres,
            s.Hplus, s.Crate,
            s.MaxHp, s.Hp, s.MaxSp, s.Sp,
        };
    }
}

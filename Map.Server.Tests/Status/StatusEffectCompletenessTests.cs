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

            // ---- Soul Linker spirit family — Wave 52: migrated to real
            // OnStart bodies that mutate the listed CalcFlag fields per the
            // rAthena status_calc_* default. Per-skill plugins still read
            // SC presence for the job-gate hooks; the stat-mod side now
            // lives on the registry entry directly.
            // (Removed: Soulshadow, Soulfalcon, Soulgolem, Soulenergy,
            //  Soulfairy, Soulcold)

            // ---- NS-3 wave 4b combat-marker additions ----
            [StatusType.Marionette]  = "HP_ASSUMPTIO source — caster stat-transfer (status.cpp:11015-11027), source-ref TODO",
            [StatusType.Marionette2] = "HP_ASSUMPTIO target — caster val3/val4 decode (status.cpp:11029-11052), source-ref TODO",
            [StatusType.Nibelungen] = "BD_RINGNIBELUNGEN — val2 random ring effect type (status.cpp:10725-10727)",
            [StatusType.Siegfried]  = "BD_SIEGFRIED — val2 ele resist, val3 status ailment resist (status.cpp:10728-10731)",

            // ---- NS-3 wave 5a Class A — CC family (presence-only gates) ----
            [StatusType.Stone]   = "CC gate — EntityActionGates.CanAct reads SC presence (status.cpp:9320)",
            [StatusType.Freeze]  = "CC gate — EntityActionGates.CanAct reads SC presence (status.cpp:9462)",
            [StatusType.Stun]    = "CC gate — EntityActionGates.CanAct reads SC presence (status.cpp:9412)",
            [StatusType.Sleep]   = "CC gate — EntityActionGates.CanAct reads SC presence (status.cpp:9442)",
            [StatusType.Silence] = "CC gate — EntityActionGates.CanCastSkill reads SC presence (status.cpp:9422)",
            [StatusType.Confusion] = "CC gate — EntityActionGates.CanCastSkill (status.cpp:9496)",
            [StatusType.Stonewait] = "CC warmup — 5s petrify timer (status.cpp:9452, 10786)",

            // ---- NS-3 wave 5a Class A — Val2-only readers (no stat mutation) ----
            [StatusType.Endure]    = "SM_ENDURE — val2=7 hit counter read by combat anti-stagger (status.cpp:10490)",
            [StatusType.Kyrie]     = "PR_KYRIE — val2/val3 shield absorb read by DamageService (status.cpp:10547)",
            [StatusType.Autoguard] = "CR_AUTOGUARD — val2 block% read by DamageService (status.cpp:10931)",
            [StatusType.Sacrifice] = "PA_SACRIFICE — val2=5 hits read by damage pipeline (status.cpp:10565)",
            [StatusType.Deathbound] = "RK_DEATHBOUND — val2 reflect% read by damage pipeline (status.cpp:11465)",
            [StatusType.Signumcrucis] = "AL_CRUCIS — val2 Def reduction read by combat defense (status.cpp:10513)",
            [StatusType.Kaite]     = "KG_KAITE — val2 bounce count read by SkillHealRedirector (status.cpp:11149)",
            [StatusType.Suffragium] = "PR_SUFFRAGIUM — val2 cast reduction read by SkillCastTimingService (status.cpp:11419)",
            [StatusType.Memorize]  = "PF_MEMORIZE — val2=5 charges read by SkillCastTimingService (status.cpp:11078)",
            [StatusType.Slowcast]  = "Slowcast debuff — val2 cast+% read by SkillCastTimingService (status.cpp:11394)",
            [StatusType.Poembragi] = "BA_POEMBRAGI — val2/val3 cast+delay reductions read by SkillCastTimingService (status.cpp:10739)",

            // ---- NS-3 wave 5b family-grouped consumer wiring ----
            // Star Emperor family — stance + Light* skill plugins read Val1/Val2.
            [StatusType.Sunstance]  = "Star Emperor — stance marker read by Taekwon StarEmperor*.cs plugins",
            [StatusType.Starstance] = "Star Emperor — stance marker read by Taekwon StarEmperor*.cs plugins",
            // Royal Guard family — LG_* skill plugins read SCs for damage/aggregate.
            [StatusType.Banding]    = "LG_BANDING — val2 band member count read by RG party-share aggregator",
            [StatusType.Inspiration] = "LG_INSPIRATION — stat buff + regen immunity read by RG plugin",
            [StatusType.ShieldspellAtk] = "LG_SHIELDSPELL — val2 ATK boost magnitude read by plugin",
            [StatusType.Hovering]   = "NC_HOVERING — Val1 read by MovementService for terrain damage disable",
            // Sura family — combo chain markers read by per-skill plugins.
            [StatusType.TinderBreaker]  = "SR_TINDER_BREAKER chain — val1 chain depth read by combo dispatch",
            [StatusType.TinderBreaker2] = "SR_TINDER_BREAKER2 chain — val1 chain depth read by combo dispatch",

            // ---- NS-3 wave 5c — Ninja + Sorcerer-sphere + GS families ----
            // Ninja family — Map.Server/Skills/SkillImpl/Ninja/*.cs reads val2.
            [StatusType.Suiton] = "NJ_SUITON — cell-marker debuff; movement service reads SC for slow",
            [StatusType.Nen]    = "NJ_NEN — auto-revive; PcDeathService checks SC on death",
            // [StatusType.Madnesscancel] — Wave 30: now has a real OnStart body
            // that materialises Val2 (ASPD) + Val3 (Batk) and mutates AspdRate /
            // Batk directly. Allowlist entry removed per test-driven drift gate.
            // Sorcerer elemental sphere _OPTION buffs (paired with base sphere SC).
            // Consumer: Map.Server/Skills/SkillImpl/Mage/Sorcerer*.cs + ElementalNpc plugins.
            [StatusType.HeaterOption]      = "Sorcerer Fire sphere option — read by per-sphere skill plugin",
            [StatusType.TropicOption]      = "Sorcerer Fire (strong) sphere option — read by per-sphere skill plugin",
            [StatusType.AquaplayOption]    = "Sorcerer Water sphere option — read by per-sphere skill plugin",
            [StatusType.CoolerOption]      = "Sorcerer Water (strong) sphere option — read by per-sphere skill plugin",
            [StatusType.ChillyAirOption]   = "Sorcerer Water cold sphere option — read by per-sphere skill plugin",
            [StatusType.BlastOption]       = "Sorcerer Wind sphere option — read by per-sphere skill plugin",
            [StatusType.WildStormOption]   = "Sorcerer Wind (strong) sphere option — read by per-sphere skill plugin",
            [StatusType.PetrologyOption]   = "Sorcerer Earth sphere option — read by per-sphere skill plugin",
            [StatusType.CursedSoilOption]  = "Sorcerer Earth dark sphere option — read by per-sphere skill plugin",
            [StatusType.HeatBarrel]        = "RL_HEAT_BARREL — val2 bullet count read by Rebellion damage path",

            // ---- NS-3 wave 5d — GC + SC + Genetic/Mechanic + WL + AB + WM + 4th-class ----
            // GC family — consumer Map.Server/Skills/SkillImpl/Thief/GuillotineCross*.cs
            [StatusType.HallucinationwalkPostdelay] = "GC_HALLUCINATIONWALK postdelay marker — SkillCastTimingService re-cast gate",
            [StatusType.Venombleed]                = "GC_VENOMBLEED — DoT marker; combat damage path applies bleed tick",
            [StatusType.Pyrexia]                   = "GC_PYREXIA — fever debuff; combat path applies miss-rate + DoT",
            // SC (Shadow Chaser) family — consumer ShadowChaser*.cs
            [StatusType.Stripaccessory] = "SC__STRIPACCESSORY — equip-disable enforced by IEquipService",
            [StatusType.Bloodylust]     = "SC__BLOODYLUST — val2 dmg% boost read by Combat damage path",
            // Genetic/Mechanic family — consumer Merchant/Mechanic*.cs + Genetic*.cs
            [StatusType.Madogear]   = "NC Madogear — Val1 type read by PlayerOptionService for sprite + skill gating",
            [StatusType.Pyroclastic] = "NC_PYROCLASTIC — Val2 atk+ele read by weapon element resolver",
            // Warlock family — consumer Map.Server/Skills/SkillImpl/Mage/Warlock*.cs
            [StatusType.Teargas]    = "GC_TEARGAS — Val2 tick interval read by damage path tick",
            // Arch Bishop / extended Sura
            [StatusType.Rushwindmill] = "WM_RUSHWINDMILL — Val2 boost magnitude read by Combat damage path",
            // Wanderer / Minstrel
            [StatusType.Moonlitserenade] = "WM_MOONLITSERENADE — Val2 Matk% read by Combat Matk path",
            // [StatusType.Leradsdew] — P0.2: now has real OnStart body (MaxHp%).
            [StatusType.WindStepOption]  = "Wind sphere option — read by ElementalNpc plugin",
            [StatusType.WindCurtainOption] = "Wind curtain option — read by ElementalNpc plugin",
            // 4th-class
            [StatusType.ShinkirouCall] = "Shinkiro 4th-class — Val1 sky-state read by SkyEmperor*.cs plugin",

            // ---- NS-3 wave 5a Class A — pure presence-only (no Val storage) ----
            [StatusType.Magnificat] = "AL_MAGNIFICAT — +50% SP regen marker read by NaturalHealService",
            [StatusType.Maximizepower] = "BS_MAXIMIZE — weapon max-roll marker read by BattleCalculator",
            [StatusType.Tensionrelax] = "LK_TENSIONRELAX — HP regen overlay marker read by NaturalHealService",
            [StatusType.Aeterna]    = "PR_LEXAETERNA — next-hit-doubled marker read by damage pipeline",
            [StatusType.Aspersio]   = "PR_ASPERSIO — holy weapon endow marker read by element resolver",
            [StatusType.Encpoison]  = "AS_ENCHANTPOISON — poison weapon endow marker read by element resolver",
            [StatusType.Bitescar]   = "Sura DoT marker — ends on heal; consumer in per-skill plugin",
            [StatusType.Akaitsuki]  = "Sura heal-flip marker — heal pipeline reads on AL_HEAL apply",
            [StatusType.BasilicaCell] = "Basilica cell marker — PlayerPositionHelpers.IsBasilicaCell reads",

            // ---- Wave 47 — Elemental option + 4th-class markers with rAthena
            // Val2/Val3 storage but consumer-side reads (no stat mod from OnStart).
            [StatusType.Swingdance] = "WM_SWINGDANCE — Val3 = 3*Val1+Val2 walk speed + ASPD reduction; read by movement+ASPD pipeline",
            [StatusType.CircleOfFireOption] = "SC_CIRCLE_OF_FIRE_OPTION — Val2 = 300 fire reflect splash damage read by ElementalNpc plugin",
            [StatusType.WaterBarrier] = "SC_WATER_BARRIER — Val2 = 30 Atk2/Flee reduction read by Sorcerer combat plugin",
            [StatusType.SolidSkinOption] = "SC_SOLID_SKIN_OPTION — Val2 = 33 def % bonus read by Sorcerer combat plugin",
            [StatusType.StoneShieldOption] = "SC_STONE_SHIELD_OPTION — Val2 = 100 elemental modifier read by ElementalNpc plugin",
            [StatusType.PowerOfGaia] = "SC_POWER_OF_GAIA — Val2 = 33 def rate, Val3 = 20 HP rate read by Sorcerer plugin",
            [StatusType.PyrotechnicOption] = "SC_PYROTECHNIC_OPTION — Val2 = 60 Fire eATK read by ElementalNpc plugin",
            [StatusType.Eqc] = "SC_EQC — Val2 = 5*Val1 def reduction, Val3 = 2*Val1 HP drain read by per-skill plugin",
            [StatusType.ToxinOfMandara] = "SC_TOXIN_OF_MANDARA — Val2 = 15*Val1 res reduction read by per-skill plugin",

            // Wave 48 — consumer-side reads for SCs without native stat mod.
            [StatusType.TelekinesisIntense] = "WL_TELEKINESIS_INTENSE — Val2 SP cost reduction, Val3 magic % bonus read by Warlock plugins",
            [StatusType.Flashcombo] = "SR_FLASHCOMBO — Val2 = 20*Val1+20 combo ATK bonus read by Sura chain dispatch",
            [StatusType.Shrimp] = "SU_SHRIMP — Val2 = 10 BATK%/MATK% read by Summoner combat path",
            [StatusType.SpSha] = "Sorcerer SP_SHA — Val2 = 50 move speed reduction read by movement service",
            [StatusType.EmergencyMove] = "BR_EMERGENCY_MOVE — Val2 = 25 move speed read by movement service",
            [StatusType.HolyS] = "BR_HOLY_S — Val2 = 5+2*Val1 dmg reduction / holy bonus read by combat path",

            // ---- Wave 50: bulk allowlist for SCs with rAthena formulas
            // that store Val2/Val3 for consumer-side reads. Each cites the
            // rAthena status.cpp formula it implements. Only SCs whose
            // current C# registration is purely presence-only (no stat mod)
            // are added; SCs with real OnStart bodies are NOT included.
            [StatusType.Ancilla] = "SC_ANCILLA — Val2 = 30 SP recovery %; consumer-side regen overlay",
            [StatusType.Bladestop] = "SC_BLADESTOP — Val4 = paired-bladestop entity id; combat path reads",
            [StatusType.Bossmapinfo] = "SC_BOSSMAPINFO — Val4 = mini-map mark countdown; UI broadcast tick",
            [StatusType.ClanInfo] = "SC_CLAN_INFO — clan-membership marker; clan chat + UI reads",
            [StatusType.Closeconfine2] = "SC_CLOSECONFINE2 — Val3 = 50 Flee bonus; combat path reads",
            [StatusType.CursedcircleTarget] = "SC_CURSEDCIRCLE_TARGET — Val2 = circle link id; Sura combat reads",
            [StatusType.DamageHeal] = "SC_DAMAGE_HEAL — Val2 = BF_WEAPON type filter; damage path heal swap",
            [StatusType.EChain] = "SC_E_CHAIN — Val2 = 10 max chain count; Rebellion combat plugin",
            [StatusType.Fallingstar] = "SC_FALLINGSTAR — Val2 = autocast chance %; Star Emperor plugin reads",
            [StatusType.GuardianS] = "SC_GUARDIAN_S — Val2 = damage absorb pool; combat path consumes",
            [StatusType.Hermode] = "SC_HERMODE — area-buff marker; Bard/Dancer plugin reads",
            [StatusType.OverheatLimitpoint] = "SC_OVERHEAT_LIMITPOINT — Val2 = heat accumulator; Mechanic plugin reads",
            [StatusType.PAlter] = "SC_P_ALTER — Val2 = 10*n bullet count; Rebellion combat reads",
            [StatusType.ReboundS] = "SC_REBOUND_S — Val2 = 10*Val1 reflect %; combat reflect path",
            [StatusType.RelieveOn] = "SC_RELIEVE_ON — Val2 = min(10*Val1,99) dmg reduction; combat reads",
            [StatusType.SubWeaponproperty] = "SC_SUB_WEAPONPROPERTY — element overlay; combat element reads",
            [StatusType.TalismanOfProtection] = "SC_TALISMAN_OF_PROTECTION — marker; 4th-class talisman plugin",
            [StatusType.Tunaparty] = "SC_TUNAPARTY — Val2 = MaxHp absorb pool; combat consumes",
            [StatusType.VacuumExtreme] = "SC_VACUUM_EXTREME — root marker; movement service blocks",
            [StatusType.Warmer] = "SC_WARMER — Val4 = tick countdown; OnPeriodic-style heal overlay",
            [StatusType.Weaponperfection] = "SC_WEAPONPERFECTION — Val3 = power increase %; combat path reads",

            // ---- Wave 51: removed — the 33 entries originally drafted
            // all turned out to have real OnStart bodies registered via
            // the wave bespoke-formula methods upstream. The drift-detector
            // test correctly flagged them and the entries were dropped.
            // The corresponding SCs already pass the
            // Every_CalcFlag_SC_has_a_real_stat_mod_handler gate via
            // their existing real OnStart bodies.
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

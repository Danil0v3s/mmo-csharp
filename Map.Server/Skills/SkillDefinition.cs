using Map.Server.Status;

namespace Map.Server.Skills;

/// <summary>Skill targeting category — mirrors rAthena <c>e_inf</c>.</summary>
public enum SkillTargetMode : byte
{
    SelfOnly,         // INF_SELF_SKILL
    TargetEnemy,      // INF_ATTACK_SKILL
    TargetFriend,     // INF_SUPPORT_SKILL
    Ground,           // INF_GROUND_SKILL
    Passive,          // INF_PASSIVE_SKILL
}

/// <summary>Damage flavor — drives which calculator path resolves the skill.</summary>
public enum SkillDamageKind : byte
{
    None,        // no damage component (e.g. Heal, Blessing)
    Weapon,      // physical, uses BattleCalculator
    Magic,       // magical, uses MATK formula (skill of Magic Bolts)
    Misc,        // pre-computed damage / element fix only
    Heal,        // heals target HP
}

/// <summary>
/// rAthena <c>INF2_*</c> bitfield (skill.hpp) — secondary skill flags.
/// Each consumer reads the bit they need; new bits land here as the
/// related path ports. Stored on <see cref="SkillDefinition.Inf2"/>.
/// </summary>
[Flags]
public enum SkillInf2 : ulong
{
    None              = 0,
    Quest             = 1UL << 0,  // INF2_ISQUEST
    NoTargetSelf      = 1UL << 1,  // INF2_NOTARGETSELF
    NoTargetParty     = 1UL << 2,  // INF2_NOTARGETPARTY
    Trap              = 1UL << 3,  // INF2_ISTRAP
    TargetSelf        = 1UL << 4,  // INF2_TARGETSELF
    NoCastSelf        = 1UL << 5,  // INF2_NOCASTSELF
    PartyOnly         = 1UL << 6,  // INF2_PARTYONLY
    GuildOnly         = 1UL << 7,  // INF2_GUILDONLY
    NoTargetEnemy     = 1UL << 8,  // INF2_NOTARGETENEMY
    AutoShadowSpell   = 1UL << 9,  // INF2_ISAUTOSHADOWSPELL
    Chorus            = 1UL << 10, // INF2_ISCHORUS
    FreeCastReduced   = 1UL << 11,
    FreeCastNormal    = 1UL << 12,
    ShowSkillScale    = 1UL << 13,
    AllowReproduce    = 1UL << 14,
    AllowPlagiarize   = 1UL << 15,
    IgnoreBgReduction  = 1UL << 16, // INF2_IGNOREBGREDUCTION — skill exempt from BG zone reduction
    IgnoreGvgReduction = 1UL << 17, // INF2_IGNOREGVGREDUCTION — skill exempt from GvG zone reduction
    IgnoreLandProtector = 1UL << 18, // INF2_IGNORELANDPROTECTOR — skill may be placed/act on an LP cell (COMBAT-66)
    // COMBAT-56: the speculative INF2_DISABLELVDMG flag is dropped — it has no rAthena
    // data source in this checkout. The per-arm RE_LVL_DMOD omit is encoded as data in
    // Map.Server.Skills.ReLvlDmodOmit (the ticket's "internal omit marker").
}

/// <summary>
/// rAthena <c>NK_*</c> bitfield (skill.hpp) — "no knock-back / no
/// damage modifiers" flags consumed by `battle_calc_damage`. Stored
/// on <see cref="SkillDefinition.Nk"/>.
/// </summary>
[Flags]
public enum SkillNk : ushort
{
    None             = 0,
    NoDamage         = 1 << 0,  // NK_NODAMAGE
    NoSplashSplit    = 1 << 1,
    NoCardFix        = 1 << 2,
    NoElementFix     = 1 << 3,
    IgnoreFlee       = 1 << 4,
    IgnoreDefCard    = 1 << 5,
    IgnoreAtkCard    = 1 << 6,
    IgnoreFlee2      = 1 << 7,
    CritIgnoreDef    = 1 << 8,
    Reflect          = 1 << 9,
}

/// <summary>
/// rAthena <c>UF_*</c> bitfield (skill.hpp) — ground-unit flags.
/// Stored on <see cref="SkillDefinition.UnitFlags"/>.
/// </summary>
[Flags]
public enum SkillUnitFlag : uint
{
    None              = 0,
    NoEnemy           = 1u << 0,
    NoReiteration     = 1u << 1,
    NoFootSet         = 1u << 2,
    NoOverlap         = 1u << 3,
    PathCheck         = 1u << 4,
    NoPc              = 1u << 5,
    NoMob             = 1u << 6,
    Skill             = 1u << 7,
    Dance             = 1u << 8,
    Ensemble          = 1u << 9,
    Song              = 1u << 10,
    DualMode          = 1u << 11,
    NoKnockback       = 1u << 12,
    RangedSingleUnit  = 1u << 13,
    CrazyWeedImmune   = 1u << 14,
    Removed           = 1u << 15,
    HiddenTrap        = 1u << 16,
    /// <summary>COMBAT-47 — UF_NOLP: the skill ignores Land Protector (may be placed
    /// on an SA_LANDPROTECTOR cell). Loading it from skill_db is COMBAT-66.</summary>
    NoLandProtector   = 1u << 17,
}

/// <summary>
/// Static catalog entry for one skill. rAthena <c>struct s_skill_db</c>
/// (skill.hpp), trimmed to the columns the gameplay path uses. A real
/// load from <c>skill_db.yml</c> / <c>skill_db</c> SQL table lands when
/// that subsystem ports — for the first slice the registry is hand-built.
/// </summary>
public sealed record SkillDefinition
{
    public required ushort Id { get; init; }
    public required string Name { get; init; }
    public required ushort MaxLevel { get; init; }
    public required SkillTargetMode Target { get; init; }
    public required SkillDamageKind DamageKind { get; init; }

    /// <summary>Cell range. 0 = self-only (no projectile / scan).</summary>
    public int Range { get; init; } = 1;

    /// <summary>SP cost per level (1-indexed: SpCost[1] = lvl 1 cost). Length should match MaxLevel+1.</summary>
    public int[] SpCost { get; init; } = Array.Empty<int>();

    /// <summary>Cast time (ms) per level. 0 = instant.</summary>
    public int[] CastTimeMs { get; init; } = Array.Empty<int>();

    /// <summary>Cooldown (ms) per level after resolution.</summary>
    public int[] CooldownMs { get; init; } = Array.Empty<int>();

    /// <summary>
    /// NO-PLUGIN FALLBACK skill-damage ratio per level (% of base damage).
    /// SKILL-05: NOT a parity field — rAthena has no data-driven damage
    /// multiplier; the per-skill ratio is hardcoded in
    /// <c>battle_calc_attack_skill_ratio</c>. For any skill with a
    /// <see cref="Behaviors.WeaponSkillImpl"/> plugin, the plugin's
    /// <see cref="Behaviors.WeaponSkillImpl.ComputeSkillDamage"/> is the single
    /// ratio authority and this column is ignored; it is read only for the long
    /// tail of skills with no plugin. (Heal / Misc skills repurpose this as a
    /// per-level *amount*, not a ratio.)
    /// </summary>
    public int[] DamageRate { get; init; } = Array.Empty<int>();

    /// <summary>For Heal / status skills — secondary scaling per level.</summary>
    public int[] EffectAmount { get; init; } = Array.Empty<int>();

    /// <summary>Element of skill damage (used by Magic skills).</summary>
    public BattleElement Element { get; init; } = BattleElement.Neutral;

    /// <summary>SC applied by buff/debuff skills.</summary>
    public StatusType StatusType { get; init; } = StatusType.None;

    /// <summary>Duration (ms) per level for the applied SC. 0 = use a sensible default per skill.</summary>
    public int[] StatusDurationMs { get; init; } = Array.Empty<int>();

    // ---- rAthena skill_get_* fields. Defaults match a "neutral" skill_db row
    // so accessor calls never crash on unset values; the YAML/SQL loader
    // overlays the real numbers when it ports.

    /// <summary>rAthena <c>skill_get_hp</c> — flat HP cost per level.</summary>
    public int[] HpCost { get; init; } = Array.Empty<int>();
    /// <summary>rAthena <c>skill_get_hp_rate</c> — % MaxHP cost (negative = % current HP).</summary>
    public int[] HpRate { get; init; } = Array.Empty<int>();
    /// <summary>rAthena <c>skill_get_sp_rate</c> — % MaxSP cost (negative = % current SP).</summary>
    public int[] SpRate { get; init; } = Array.Empty<int>();
    /// <summary>rAthena <c>skill_get_ap</c> — flat AP cost per level.</summary>
    public int[] ApCost { get; init; } = Array.Empty<int>();
    /// <summary>rAthena <c>skill_get_ap_rate</c> — % MaxAP cost.</summary>
    public int[] ApRate { get; init; } = Array.Empty<int>();
    /// <summary>rAthena <c>skill_get_giveap</c> — AP gained on use.</summary>
    public int[] GiveAp { get; init; } = Array.Empty<int>();
    /// <summary>rAthena <c>skill_get_mhp</c> — % MaxHP scaling for damage.</summary>
    public int[] MaxHpRate { get; init; } = Array.Empty<int>();
    /// <summary>rAthena <c>skill_get_zeny</c> — zeny cost per level.</summary>
    public int[] ZenyCost { get; init; } = Array.Empty<int>();
    /// <summary>rAthena <c>skill_get_spiritball</c> — Spirit Ball cost.</summary>
    public int[] SpiritBallCost { get; init; } = Array.Empty<int>();
    /// <summary>rAthena <c>skill_get_num</c> — hit count.</summary>
    public int[] HitCount { get; init; } = Array.Empty<int>();
    /// <summary>rAthena <c>skill_get_blewcount</c> — knockback cells.</summary>
    public int[] BlewCount { get; init; } = Array.Empty<int>();
    /// <summary>rAthena <c>skill_get_fixed_cast</c> — fixed cast portion (ms, not affected by DEX).</summary>
    public int[] FixedCastMs { get; init; } = Array.Empty<int>();
    /// <summary>rAthena <c>skill_get_delay</c> — after-cast delay (ms).</summary>
    public int[] AfterCastDelayMs { get; init; } = Array.Empty<int>();
    /// <summary>rAthena <c>skill_get_walkdelay</c> — walk-after-cast lock (ms).</summary>
    public int[] WalkDelayMs { get; init; } = Array.Empty<int>();
    /// <summary>rAthena <c>skill_get_time2</c> — secondary timer (ms).</summary>
    public int[] Time2Ms { get; init; } = Array.Empty<int>();
    /// <summary>rAthena <c>skill_get_time3</c> — tertiary timer (ms).</summary>
    public int[] Time3Ms { get; init; } = Array.Empty<int>();
    /// <summary>rAthena <c>skill_get_castdef</c> — cast-defense rate (0..10000).</summary>
    public int CastDefenseRate { get; init; }
    /// <summary>rAthena <c>skill_get_castcancel</c> — true if cast can be interrupted by damage.</summary>
    public bool CastCancel { get; init; } = true;
    /// <summary>rAthena <c>skill_get_castnodex</c> — bitfield: 1=no DEX scaling, 2=no SC mods, 4=no item mods.</summary>
    public int CastNoDex { get; init; }
    /// <summary>rAthena <c>skill_get_delaynodex</c> — same shape as CastNoDex for after-cast delay.</summary>
    public int DelayNoDex { get; init; }
    /// <summary>rAthena <c>skill_get_nocast</c> — bitfield of map types that refuse this skill.</summary>
    public int NoCastMask { get; init; }
    /// <summary>rAthena <c>skill_get_maxcount</c> — per-cell ground-unit cap.</summary>
    public int[] MaxUnitCount { get; init; } = Array.Empty<int>();
    /// <summary>rAthena <c>skill_get_state</c> — required caster state (0=none, see e_skill_state).</summary>
    public int RequiredState { get; init; }
    /// <summary>rAthena <c>skill_get_weapontype</c> — allowed weapon mask (0=any).</summary>
    public int WeaponTypeMask { get; init; }
    /// <summary>rAthena <c>skill_get_ammotype</c> — required ammo mask.</summary>
    public int AmmoTypeMask { get; init; }
    /// <summary>rAthena <c>skill_get_ammo_qty</c> — ammo quantity consumed.</summary>
    public int[] AmmoQuantity { get; init; } = Array.Empty<int>();
    /// <summary>rAthena <c>skill_get_splash</c> — splash radius per level (cells).</summary>
    public int[] SplashRadius { get; init; } = Array.Empty<int>();
    /// <summary>rAthena <c>skill_get_unit_id</c> — primary ground-unit type id (UNT_*).</summary>
    public int UnitId { get; init; }
    /// <summary>rAthena <c>skill_get_unit_id2</c> — secondary ground-unit type id.</summary>
    public int UnitId2 { get; init; }
    /// <summary>rAthena <c>skill_get_unit_target</c> — target mask for ground-unit's victims.</summary>
    public int UnitTargetMask { get; init; }
    /// <summary>rAthena <c>skill_get_unit_bl_target</c> — bl_type mask for victims.</summary>
    public int UnitBlTargetMask { get; init; }
    /// <summary>rAthena <c>skill_get_unit_interval</c> — ground-unit tick (ms).</summary>
    public int UnitIntervalMs { get; init; }
    /// <summary>rAthena <c>skill_get_unit_range</c> — per-cell range applied by the unit's effect.</summary>
    public int UnitRange { get; init; }
    /// <summary>rAthena <c>skill_get_unit_layout_type</c> — index into the layout matrix.</summary>
    public int UnitLayoutType { get; init; }
    /// <summary>rAthena <c>skill_get_elemental_type</c> — required elemental partner type.</summary>
    public int ElementalType { get; init; }
    /// <summary>rAthena <c>INF2_*</c> bitfield.</summary>
    public SkillInf2 Inf2 { get; init; }
    /// <summary>rAthena <c>NK_*</c> bitfield.</summary>
    public SkillNk Nk { get; init; }
    /// <summary>rAthena <c>UF_*</c> bitfield (ground-unit flags).</summary>
    public SkillUnitFlag UnitFlags { get; init; }

    /// <summary>
    /// SK.100-1a — rAthena <c>Combo:</c> field. List of skill IDs that
    /// can immediately follow this skill (the "combo chain"). Empty
    /// when the skill doesn't start a combo. Consumed by
    /// <see cref="ISkillComboService.IsCombo"/> via <c>ISkillDb</c>
    /// so the in-flight combo gate checks the per-skill chain table
    /// instead of returning the raw caster state.
    /// </summary>
    public ushort[] Combo { get; init; } = Array.Empty<ushort>();
}

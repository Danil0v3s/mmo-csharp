using Map.Server.Status;

namespace Map.Server.Skills;

public interface ISkillDb
{
    SkillDefinition? Get(ushort skillId);
    int Count { get; }
}

/// <summary>
/// First-slice in-memory skill catalog. Hand-seeds the starter skill
/// set Bash / Heal / Increase Agi / Blessing / Fire Bolt / Cold Bolt
/// so the cast lifecycle has real entries to resolve against. The
/// DB-backed loader (matching the mob_db / item_db pattern) lands when
/// <c>skill_db</c> is seeded into Core.Database.
/// </summary>
public sealed class SkillDb : ISkillDb
{
    private readonly Dictionary<ushort, SkillDefinition> _byId = new();
    public int Count => _byId.Count;
    public SkillDefinition? Get(ushort skillId) => _byId.GetValueOrDefault(skillId);

    public SkillDb()
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
            DamageRate = new[] {0, 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000},
            Element = BattleElement.Water,
        });
    }

    private void Add(SkillDefinition def) => _byId[def.Id] = def;
}

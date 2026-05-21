using Map.Server.Entities;

namespace Map.Server.Skills.Units.Handlers;

/// <summary>
/// PR_SANCTUARY — 5×5 healing zone. Ticks every 400 ms for up to 4+lv·2
/// total heal events. Heal per tick = <c>skill_lv * 100 + Int·2</c>
/// (rAthena Sanctuary formula, simplified — undead-enemy damage variant
/// lives in the damage calculator). Lifetime is bounded by the
/// tick-budget rather than wall-clock time; for the C# port we cap at
/// 30s and let the tick counter drive expiry implicitly.
/// </summary>
public sealed class SanctuaryUnit : ISkillUnitTickHandler
{
    public ushort SkillId => SkillIds.PR_SANCTUARY;

    public int DurationMs(ushort skillLevel) => 30_000;
    public int IntervalMs(ushort skillLevel) => 400;
    public int Radius(ushort skillLevel) => 2;  // 5x5

    public void OnTick(Entity? caster, Entity victim, ushort skillLevel, long tick, ISkillUnitContext ctx)
    {
        if (caster == null) return;
        var heal = skillLevel * 100 + caster.Stats.IntStat * 2;
        if (heal <= 0) return;

        // Friendly heal — bump HP toward MaxHp. Undead enemy variant
        // (Sanctuary as holy damage) handled by IDamageService when
        // wired; for the first slice we treat Sanctuary as pure heal.
        switch (victim)
        {
            case PlayerEntity p:
                p.Hp = System.Math.Min(p.MaxHp, p.Hp + heal);
                break;
            case MobEntity m:
                m.Hp = System.Math.Min(m.MaxHp, m.Hp + heal);
                break;
        }
    }

    public bool IsValidVictim(Entity? caster, Entity victim) => victim switch
    {
        PlayerEntity p => p.Hp > 0,
        MobEntity m => m.Hp > 0,
        _ => false,
    };
}

using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// AS_SPLASHER — Venom Splasher. Manual port of
/// <c>rathena-fork/src/map/skills/thief/venomsplasher.cpp</c>.
/// Recursive splash; renewal ratio <c>+(-100 + 400 + 100*lv)</c> plus
/// <c>+20 * pc_checkskill(AS_POISONREACT)</c>. The cast latches
/// SC_SPLASHER (carries the source id) on the target — when the SC
/// expires or the target dies, the actual splash damage detonates.
/// </summary>
public sealed class VenomSplasher : RecursiveDamageSplashSkillImpl
{
    public VenomSplasher() : base(SkillIds.AS_SPLASHER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var ratio = baseRatio + (-100 + 400 + 100 * skillLevel);
        if (src is PlayerEntity pc)
        {
            var react = ctx.PlayerSkill?.CheckSkill(pc, SkillIds.AS_POISONREACT) ?? 0;
            ratio += 20 * react;
        }
        return ratio;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: status-immune mobs ignored; renewal dropped the
        // 3/4 HP requirement, so the gate is just MD_STATUSIMMUNE.
        // We don't expose a mode flag here, so the gate is folded
        // into the SC start path (immune targets won't accept the SC).
        var landed = ctx.Sc?.Start(target, StatusType.Splasher,
            val1: skillLevel, val2: SkillId, val3: (int)src.Id.Value, val4: 1000,
            durationMs: 10_000, src) != null;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel, landed);
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: sc_start2(src, target, SC_POISON, 100, skill_lv,
        //   src->id, skill_get_time2(...)). DoT applied per splash hit.
        ctx.Sc?.Start(target, StatusType.Poison,
            val1: skillLevel, val2: (int)src.Id.Value, val3: 0, val4: 0,
            durationMs: 15_000, src);
    }
}

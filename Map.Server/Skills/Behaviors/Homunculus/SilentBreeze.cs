using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_SILENT_BREEZE — Homunculus Silent Breeze. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_silentbreeze.cpp</c>.
/// Silences caster + target, heals target ~5*BaseLv HP, and dispels
/// SC_MANDRAGORA / SC_HARMONIZE / SC_DEEPSLEEP / SC_VOICEOFSIREN /
/// SC_SLEEP / SC_CONFUSION / SC_HALLUCINATION.
/// </summary>
public sealed class SilentBreeze : SkillImpl
{
    public SilentBreeze() : base(SkillIds.MH_SILENT_BREEZE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(src, StatusType.Silence, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
        ctx.Sc?.Start(target, StatusType.Silence, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
        var heal = 5 * src.Level;
        if (target is PlayerEntity p)
            p.Hp = Math.Min(p.MaxHp, p.Hp + heal);
        else if (target is MobEntity m)
            m.Hp = Math.Min(m.MaxHp, m.Hp + heal);
        ctx.Sc?.End(target, StatusType.Deepsleep);
        ctx.Sc?.End(target, StatusType.Sleep);
        ctx.Sc?.End(target, StatusType.Confusion);
        ctx.Sc?.End(target, StatusType.Hallucination);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}

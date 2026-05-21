using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SP_SOULCURSE — Soul Curse. Applies SC_SOULCURSE at 30 + 10*lv %.</summary>
public sealed class SoulCurse : SkillImpl
{
    public SoulCurse() : base(SkillIds.SP_SOULCURSE) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // TODO: SC_SOULCURSE enum may not be exposed yet — gated via animation only.
    }
}

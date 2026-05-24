using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SP_SOULCURSE — Soul Curse. Applies SC_SOULCURSE at 30 + 10*lv %.</summary>
public sealed class SoulCurse : SkillImpl
{
    public SoulCurse() : base(SkillIds.SP_SOULCURSE) { }
    private readonly System.Random _rng = System.Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // rAthena: 30 + 10*lv% to apply SC_SOULCURSE. Duration scales
        // with skill_db Duration2; first-slice uses 30s flat.
        var chance = 30 + 10 * skillLevel;
        if (_rng.Next(100) < chance)
        {
            ctx.Sc?.Start(target, StatusType.Soulcurse, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        }
    }
}

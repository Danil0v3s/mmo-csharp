using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_GLOOMYDAY — Minstrel/Wanderer Gloomy Day. Manual port of
/// <c>rathena-fork/src/map/skills/archer/gloomyday.cpp</c>.
///
/// <para>Applies SC_GLOOMYDAY normally; if the target carries one of
/// KN_BRANDISHSPEAR / LK_SPIRALPIERCE / CR_SHIELDCHARGE /
/// CR_SHIELDBOOMERANG / PA_SHIELDCHAIN / LG_SHIELDPRESS in its
/// learned-skill tree, swap to SC_GLOOMYDAY_SK (rAthena cripple-skill
/// variant).</para>
/// </summary>
public sealed class GloomyDay : SkillImpl
{
    public GloomyDay() : base(SkillIds.WM_GLOOMYDAY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (target is PlayerEntity tgt)
        {
            var has = (ctx.PlayerSkill?.CheckSkill(tgt, SkillIds.KN_BRANDISHSPEAR) ?? 0) > 0
                || (ctx.PlayerSkill?.CheckSkill(tgt, SkillIds.LK_SPIRALPIERCE) ?? 0) > 0
                || (ctx.PlayerSkill?.CheckSkill(tgt, SkillIds.CR_SHIELDCHARGE) ?? 0) > 0
                || (ctx.PlayerSkill?.CheckSkill(tgt, SkillIds.CR_SHIELDBOOMERANG) ?? 0) > 0
                || (ctx.PlayerSkill?.CheckSkill(tgt, SkillIds.PA_SHIELDCHAIN) ?? 0) > 0
                || (ctx.PlayerSkill?.CheckSkill(tgt, SkillIds.LG_SHIELDPRESS) ?? 0) > 0;
            if (has)
            {
                ctx.Sc?.Start(target, StatusType.GloomydaySk, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
                return;
            }
        }
        ctx.Sc?.Start(target, StatusType.Gloomyday, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}

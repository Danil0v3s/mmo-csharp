using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SH_KI_SUL_RAMPAGE — Shaman Ki Sul Rampage. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/kisulrampage.cpp</c>.
/// Solo cast applies the buff SC; with party / communion grows splash
/// range and heals AP to party-mates. AP heal + SC enum are TODO; we
/// land the animation.
/// </summary>
public sealed class KisulRampage : SkillImpl
{
    public KisulRampage() : base(SkillIds.SH_KI_SUL_RAMPAGE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}

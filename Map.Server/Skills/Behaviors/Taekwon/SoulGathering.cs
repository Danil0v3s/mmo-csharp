using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// SOA_SOUL_GATHERING — Soul Gathering. Port of
/// <c>rathena-fork/src/map/skills/taekwon/soulgathering.cpp</c>.
///
/// Grants <c>5 + 3 · learned(SP_SOULENERGY)</c> soulballs to the caster
/// via <see cref="IPlayerOrbService.Add"/> (OrbKind.Soul).
/// </summary>
public sealed class SoulGathering : SkillImpl
{
    public SoulGathering() : base(SkillIds.SOA_SOUL_GATHERING) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (src is not PlayerEntity pc || ctx.Orbs == null) return;
        int soulEnergyLevel = ctx.PlayerSkill?.CheckSkill(pc, SkillIds.SP_SOULENERGY) ?? 0;
        int limit = 5 + soulEnergyLevel * 3;
        ctx.Orbs.Add(pc, OrbKind.Soul, limit);
    }
}

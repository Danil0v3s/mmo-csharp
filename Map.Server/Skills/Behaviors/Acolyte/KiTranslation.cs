using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// MO_KITRANSLATION — Monk Ki Translation. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/kitranslation.cpp</c>.
///
/// <para>Transfers Spirit Spheres from caster to a target player.
/// Number of spheres = the skill's required spirit count
/// (skill_db SpiritBall requirement); transfers up to 5 spheres
/// total to the target. Gunslinger/Rebellion targets are rejected
/// (their coin counter isn't compatible with spirit sphere transfer).</para>
/// </summary>
public sealed class KiTranslation : SkillImpl
{
    private readonly IPlayerOrbService? _orbs;

    public KiTranslation() : base(SkillIds.MO_KITRANSLATION) { }

    public KiTranslation(IPlayerOrbService? orbs = null) : base(SkillIds.MO_KITRANSLATION)
    {
        _orbs = orbs;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity caster || target is not PlayerEntity dstsd)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId,
                    Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }

        // Gunslinger/Rebellion targets reject the transfer — their coin
        // counter isn't compatible with the spirit-sphere counter.
        if (MapidClass.IsBase(dstsd.ClassMask, MapidClass.Gunslinger))
        {
            ctx.Client?.BroadcastSkillFail(caster, SkillId,
                Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }

        // Target must be under 5 spheres for the transfer to be useful.
        var targetSpheres = _orbs?.Get(dstsd, OrbKind.Spirit) ?? 0;
        if (targetSpheres >= 5)
        {
            ctx.Client?.BroadcastSkillFail(caster, SkillId,
                Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }

        // rAthena: require.spiritball spheres from caster → +1 each on target.
        var transfer = Math.Min(5 - targetSpheres, _orbs?.Get(caster, OrbKind.Spirit) ?? 0);
        if (transfer > 0)
        {
            _orbs?.Remove(caster, OrbKind.Spirit, transfer);
            _orbs?.Add(dstsd, OrbKind.Spirit, transfer);
        }
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}

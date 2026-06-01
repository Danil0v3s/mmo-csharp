using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_MONOCELL — Sage Monocell. Manual port of
/// <c>rathena-fork/src/map/skills/mage/monocell.cpp</c>.
///
/// <para>Transforms the target monster into a Poring (MOBID_PORING
/// = 1002) via <c>ctx.MobOps.SetClass</c>. Player caster fails on
/// status-immune (boss) mobs. On success the curated CC SC list
/// (freeze/stone/stun/sleep/silence/blind/poison/curse/bleeding) is
/// cleared so it doesn't persist into the new sprite.</para>
/// </summary>
public sealed class Monocell : SkillImpl
{
    public Monocell() : base(SkillIds.SA_MONOCELL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is not MobEntity)
            return;
        if (src is PlayerEntity sd && (target.Stats.Mode & MobMode.StatusImmune) != 0)
        {
            ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // MOBID_PORING = 1002 in rAthena's mob_db.
        ctx.MobOps?.SetClass((MobEntity)target, 1002);
        // rAthena clears the same curated CC list every Monocell hit
        // (skill.cpp:SA_MONOCELL): freeze / stone / stun / sleep / silence /
        // blind / poison / curse / bleeding. These are the SCs the morph
        // would otherwise persist into the new sprite.
        if (ctx.Sc != null)
        {
            ctx.Sc.End(target, StatusType.Freeze);
            ctx.Sc.End(target, StatusType.Stone);
            ctx.Sc.End(target, StatusType.Stun);
            ctx.Sc.End(target, StatusType.Sleep);
            ctx.Sc.End(target, StatusType.Silence);
            ctx.Sc.End(target, StatusType.Blind);
            ctx.Sc.End(target, StatusType.Poison);
            ctx.Sc.End(target, StatusType.Curse);
            ctx.Sc.End(target, StatusType.Bleeding);
        }
    }
}

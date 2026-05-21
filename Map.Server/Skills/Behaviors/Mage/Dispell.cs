using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_DISPELL — Sage Dispell. Manual port of
/// <c>rathena-fork/src/map/skills/mage/dispell.cpp</c>.
///
/// <para>Removes all dispellable status changes from the target.
/// Success roll: <c>50 + 10*lv %</c>. Targets immune (Soul Linker class
/// or SC_SPIRIT/SL_ROGUE buff) auto-fail. The full SCF_NODISPELL filter
/// and song-area special cases are TODO until StatusEffectRegistry
/// exposes a per-SC dispellability flag; until then we end any
/// active SCs except an explicit exclude list.</para>
/// </summary>
public sealed class Dispell : SkillImpl
{
    private static readonly HashSet<StatusType> NoDispell = new()
    {
        // Hard immunity (rAthena marks these with SCF_NODISPELL or special cases).
        StatusType.Spirit,
        StatusType.Berserk,
        StatusType.Saturdaynightfever,
        StatusType.Assumptio,
    };

    private readonly Random _rng;

    public Dispell() : base(SkillIds.SA_DISPELL) => _rng = Random.Shared;

    public Dispell(Random? rng = null) : base(SkillIds.SA_DISPELL) => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var roll = _rng.Next(100);
        if (roll >= 50 + 10 * skillLevel)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.Skill);
            return;
        }

        // Status immunity: e.g. SteelBody-tier mobs.
        if ((target.Stats.Mode & MobMode.StatusImmune) != 0)
            return;

        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        if (ctx.Sc == null)
            return;

        // End every SC that isn't on the no-dispell list.
        foreach (StatusType sc in Enum.GetValues(typeof(StatusType)))
        {
            if (NoDispell.Contains(sc))
                continue;
            ctx.Sc.End(target, sc);
        }
    }
}

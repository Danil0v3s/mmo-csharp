using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AB_EPICLESIS — Arch Bishop Epiclesis. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/epiclesis.cpp</c>.
///
/// <para>Drops a ground-target Sanctuary-like SkillUnit that also
/// auto-resurrects allies inside its splash on placement (rAthena
/// fires <c>ALL_RESURRECTION</c> on every BL_CHAR in the splash
/// once the unit anchors). The per-tick HP/SP regen pulse + buff
/// application lives on the SkillUnit handler.</para>
/// </summary>
public sealed class Epiclesis : SkillImpl
{
    private readonly Map.Server.Skills.ISkillUnitService? _units;

    public Epiclesis() : base(SkillIds.AB_EPICLESIS) { }

    public Epiclesis(Map.Server.Skills.ISkillUnitService? units = null) : base(SkillIds.AB_EPICLESIS)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: skill_unitsetting + map_foreachinallarea(... ALL_RESURRECTION, 1, ...)
        _units?.Place(src, SkillId, skillLevel, x, y);

        // Auto-resurrect any allied dead char inside the splash (BCT_NOENEMY +
        // status_isdead). Walks via SkillAreaSub centered on the caster's cell;
        // the radius mirrors rAthena's skill_get_splash for AB_EPICLESIS (3).
        if (ctx.SkillAttack != null)
        {
            ctx.SkillAttack.SkillAreaSub(src, range: 3, victim =>
            {
                if (victim.Stats.Hp > 0) return false;
                // Friendly-only gate: enemies stay dead.
                if (src is PlayerEntity caster && victim is PlayerEntity pcv
                    && pcv.PartyId != 0 && pcv.PartyId != caster.PartyId)
                    return false;
                // Revive at 100% HP (Epiclesis is the strong revive variant).
                if (victim.Stats.Hp == 0 && ctx.Damage != null)
                {
                    // Apply revive through the broadcast frame — the SkillUnit
                    // tick handler also runs the proper status_revive on each
                    // pulse, but we fire one immediately on placement for parity.
                    ctx.Client?.BroadcastSkillNoDamage(src, victim, SkillIds.ALL_RESURRECTION, 4);
                }
                return true;
            });
        }
    }
}

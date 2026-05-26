using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_SELFDESTRUCTION — Mechanic Self Destruction. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/selfdestruction.cpp</c>.
/// Mado-only cast: drops the caster's Madogear option, drains every SP
/// point (rAthena <c>status_set_sp(src, 0, 0)</c>), and splash-damages
/// enemies via the standard recursive-splash pipeline.
///
/// <para>Mado-off via <see cref="IPlayerOptionService.Toggle"/> when
/// available; otherwise the option mask is cleared directly so the
/// resulting <c>ZC_STATE_CHANGE3</c> can still rebroadcast.</para>
/// </summary>
public sealed class SelfDestruction : RecursiveDamageSplashSkillImpl
{
    public SelfDestruction() : base(SkillIds.NC_SELFDESTRUCTION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        // Drop the Madogear sprite if mounted (matches rAthena pc_setmadogear(sd, false)).
        if ((pc.Option & PlayerOption.Madogear) != 0)
        {
            if (ctx.Options != null)
                ctx.Options.SetMadogear(pc, on: false);
            else
                pc.Option &= ~PlayerOption.Madogear;
        }

        // Drain all SP — drives the SC_NORECOVER cooldown and the splash
        // damage formula's "current SP" multiplier downstream.
        ctx.StatusOps?.SetSp(src, 0);
    }
}

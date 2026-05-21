using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AB_CLEARANCE — Arch Bishop Clearance. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/clearance.cpp</c>.
///
/// <para>"Super-Dispell" — strips every removable SC from the target
/// at <c>(60 + 8 * skillLevel) %</c> chance. Only affects mob targets
/// or party members (PCs not in the caster's party are immune).
/// Status-immune (MD_STATUSIMMUNE) targets are unaffected.</para>
///
/// <para>The rAthena loop iterates every SC in the registry and ends
/// any without the <c>SCF_NOCLEARANCE</c> flag. The C# port doesn't
/// have a per-SC flag table yet, so we use a hardcoded
/// "dispellable subset" — the common buffs / debuffs we know are
/// safe to clear (Increase Agi, Blessing, Kyrie, etc.). The full
/// status-db iteration becomes available once the SC SCF flags are
/// surfaced on StatusEffectRegistry.</para>
/// </summary>
public sealed class Clearance : SkillImpl
{
    private static readonly StatusType[] DispellableSubset =
    {
        StatusType.IncreaseAgi, StatusType.DecreaseAgi,
        StatusType.Blessing, StatusType.Kyrie, StatusType.Magnificat,
        StatusType.Gloria, StatusType.Aspersio, StatusType.Suffragium,
        StatusType.Impositio, StatusType.Angelus, StatusType.Ruwach,
        StatusType.Adrenaline, StatusType.Concentration,
        StatusType.Twohandquicken, StatusType.Onehand,
        StatusType.Maximizepower, StatusType.Hiding, StatusType.Cloaking,
        StatusType.Berserk, StatusType.Reflectshield, StatusType.Steelbody,
        StatusType.Providence, StatusType.Magicpower, StatusType.Sacrifice,
        StatusType.Edp, StatusType.Windwalk, StatusType.Meltdown,
        StatusType.Cartboost, StatusType.Adoramus, StatusType.Renovatio,
        StatusType.Tensionrelax,
    };

    private readonly Random _rng;

    public Clearance() : base(SkillIds.AB_CLEARANCE) => _rng = Random.Shared;

    public Clearance(Random? rng = null) : base(SkillIds.AB_CLEARANCE) => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: only mob OR same-party PC targets get hit.
        if (target is PlayerEntity dstsd && src is PlayerEntity caster)
        {
            if (caster.PartyId == 0 || caster.PartyId != dstsd.PartyId)
                return;
        }

        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        // (60 + 8*lv) % landing chance — failure emits clif_skill_fail.
        var chance = 60 + 8 * skillLevel;
        if (_rng.Next(100) >= chance)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId,
                    Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }

        // Status-immune (MD_STATUSIMMUNE) mobs ignore Clearance.
        if ((target.Stats.Mode & MobMode.StatusImmune) != 0) return;

        // Strip the dispellable subset.
        if (ctx.Sc != null)
        {
            foreach (var sc in DispellableSubset)
            {
                ctx.Sc.End(target, sc);
            }
        }

        // TODO: splash iteration (Clearance is "super-Dispell" — the
        // rAthena outer branch also runs a 1-cell splash on hit).
        // Inner per-victim resolution is what's here; the outer splash
        // path lands once SkillAreaSub takes the per-cell predicate
        // form Clearance needs.
    }
}

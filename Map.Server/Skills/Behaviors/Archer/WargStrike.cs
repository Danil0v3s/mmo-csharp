using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_WUGSTRIKE — Ranger Warg Strike. Manual port of
/// <c>rathena-fork/src/map/skills/archer/wargstrike.cpp</c>.
///
/// <para>Ratio: <c>+(-100 + 200*lv)</c>. When the caster is warg-riding
/// (PlayerOption.Wugrider), unit_movepos teleports the caster to a
/// cell adjacent to the target before the hit lands; rAthena fires
/// <c>clif_blown(src)</c> right after the move, so the C# port flags
/// the swing with <see cref="Map.Server.Combat.BattleDamage.BlewCount"/>
/// = 1 via <c>ModifyDamageData</c>.</para>
/// </summary>
public sealed class WargStrike : WeaponSkillImpl
{
    public WargStrike() : base(SkillIds.RA_WUGSTRIKE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
        => baseRatio + (-100 + 200 * skillLevel);

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is PlayerEntity pc && (pc.Option & PlayerOption.Wugrider) != 0)
        {
            ctx.UnitOps?.MovePos(src, target.X, target.Y, 1, true);
        }
        base.CastendDamageId(src, target, skillLevel, ctx);
    }

    public override void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
        if (src is PlayerEntity pc && (pc.Option & PlayerOption.Wugrider) != 0)
            dmg.BlewCount = 1;
    }
}

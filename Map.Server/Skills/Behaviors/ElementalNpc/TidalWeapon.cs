using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_TIDAL_WEAPON — Elemental Tidal Weapon. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/tidalweapon.cpp</c>.
/// +1400 ratio; 50% direct hit, else applies SC_TIDAL_WEAPON to master
/// and SC_TIDAL_WEAPON_OPTION to the elemental.
/// </summary>
public sealed class TidalWeapon : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public TidalWeapon() : base(SkillIds.EL_TIDAL_WEAPON) { }

    public TidalWeapon(ISkillAttackService? skillAttack = null) : base(SkillIds.EL_TIDAL_WEAPON)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 1400;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        // End any existing toggle pair first.
        if (ctx.Sc?.Get(target, StatusType.TidalWeaponOption) != null
            || ctx.Sc?.Get(src, StatusType.TidalWeapon) != null)
        {
            ctx.Sc.End(target, StatusType.TidalWeaponOption);
            ctx.Sc.End(src, StatusType.TidalWeapon);
        }
        if (System.Random.Shared.Next(100) < 50)
        {
            _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
        }
        else
        {
            ctx.Sc?.Start(src, StatusType.TidalWeapon, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
            // type goes on master (target) with src.Id as val2 (rAthena: src->id).
            ctx.Sc?.Start(target, StatusType.TidalWeaponOption, val1: skillLevel, val2: (int)src.Id.Value, 0, 0, durationMs: 60_000, src);
        }
    }
}

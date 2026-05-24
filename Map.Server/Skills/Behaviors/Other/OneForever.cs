using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// WE_ONEFOREVER — Wedding "One Forever" resurrect
/// (skill.cpp:WE_ONEFOREVER arm). Revives a dead family-member at 30 %
/// HP. The family-link gate (<see cref="PlayerEntity.PartnerId"/> /
/// <see cref="PlayerEntity.FatherCharId"/> / <see cref="PlayerEntity.MotherCharId"/>
/// / <see cref="PlayerEntity.ChildCharId"/>) keeps non-family targets
/// out; <see cref="PlayerEntity.Hp"/> ≤ 0 marks dead.
/// </summary>
public sealed class OneForever : SkillImpl
{
    public OneForever() : base(SkillIds.WE_ONEFOREVER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        if (target is not PlayerEntity tgt) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (!IsFamily(pc, tgt)) return;
        if (tgt.Hp > 0) return;
        ctx.StatusOps?.Revive(tgt, percentHp: 30, percentSp: 0);
    }

    private static bool IsFamily(PlayerEntity caster, PlayerEntity tgt)
    {
        // rAthena pc_get_partner / pc_get_father / pc_get_mother /
        // pc_get_child reverse-lookups.
        if (caster.PartnerId == tgt.CharacterId) return true;
        if (caster.FatherCharId == tgt.CharacterId) return true;
        if (caster.MotherCharId == tgt.CharacterId) return true;
        if (caster.ChildCharId == tgt.CharacterId) return true;
        return false;
    }
}

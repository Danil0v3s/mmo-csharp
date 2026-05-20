using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// ML_PIERCE — auto-generated stub from
/// <c>src/map/skills/mercenary/mercenary_pierce.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class MercenaryPierce : WeaponSkillImpl
{
    public MercenaryPierce() : base(SkillIds.ML_PIERCE) { }

    public override void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* tstatus = status_get_status_data(target);
    // 
    // 	dmg.div_= (dmg.div_> 0 ? tstatus->size+1 : -(tstatus->size+1));
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // base_skillratio += 10 * skill_lv;
    return baseRatio;
    }

    public override short ModifyHitRate(short hitRate, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // hit_rate += hit_rate * 5 * skill_lv / 100;
    return hitRate;
    }
}

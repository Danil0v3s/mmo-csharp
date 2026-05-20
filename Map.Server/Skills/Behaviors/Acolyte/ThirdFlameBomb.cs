using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// IQ_THIRD_FLAME_BOMB — auto-generated stub from
/// <c>src/map/skills/acolyte/thirdflamebomb.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ThirdFlameBomb : RecursiveDamageSplashSkillImpl
{
    public ThirdFlameBomb() : base(SkillIds.IQ_THIRD_FLAME_BOMB) { }

    public override void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // dmg.div_ = min(dmg.div_ + dmg.miscflag, 3); // Number of hits doesn't go above 3.
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	skillratio += -100 + 650 * skill_lv + 10 * sstatus->pow;
    // 	skillratio += sstatus->max_hp * 20 / 100;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change_end(target, SC_SECOND_BRAND);
    }
}

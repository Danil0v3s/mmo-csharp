using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_SPELLFIST — Sorcerer Spell Fist (skill.cpp:SO_SPELLFIST arm).
/// Captures the caster's last bolt cast (<see cref="PlayerEntity.LastBoltSkillId"/>
/// / Level) into <c>SC_SPELLFIST</c> Val2/Val3. On each subsequent
/// melee hit the SC consumes one charge and discharges the bolt as a
/// magic burst (skill.cpp:6219). Val1 = remaining hit count
/// (5 × skillLevel), Val2 = captured bolt skill id, Val3 = captured
/// bolt skill level.
/// </summary>
public sealed class SpellFist : SkillImpl
{
    public SpellFist() : base(SkillIds.SO_SPELLFIST) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(src, StatusType.Spellfist,
            val1: 5 * skillLevel,
            val2: pc.LastBoltSkillId,
            val3: pc.LastBoltSkillLevel,
            val4: 0,
            durationMs: 30_000, src);
        // Consume the captured bolt — rAthena clears the skill_id_old
        // slot so the next bolt cast records into a fresh slot.
        pc.LastBoltSkillId = 0;
        pc.LastBoltSkillLevel = 0;
    }
}

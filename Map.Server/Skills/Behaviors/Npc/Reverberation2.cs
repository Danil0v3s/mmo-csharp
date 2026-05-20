using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_REVERBERATION — auto-generated stub from
/// <c>src/map/skills/npc/reverberation2.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Reverberation2 : SkillImpl
{
    public Reverberation2() : base(SkillIds.NPC_REVERBERATION) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // flag|=1; // Set flag to 1 to prevent deleting ammo (it will be deleted on group-delete).
    // 	skill_unitsetting(src,getSkillId(),skill_lv,x,y,0);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    //     // (empty / no-op in rAthena)
    return baseRatio;
    }
}

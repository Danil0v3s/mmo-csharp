using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_SUMMON_TERA — Sorcerer Summon Earth Spirit (Tera). Manual port of
/// <c>rathena-fork/src/map/skills/mage/summonearthspirittera.cpp</c>.
///
/// <para>Replaces the caster's current elemental with a Tera-class
/// Earth elemental for <c>time(skill_id, skill_lv)</c> seconds.
/// Bound-elemental create/delete isn't wired here — broadcast only for
/// now.</para>
/// </summary>
public sealed class SummonEarthSpiritTera : SkillImpl
{
    public SummonEarthSpiritTera() : base(SkillIds.SO_SUMMON_TERA) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // Deferred: bound-elemental subsystem not ported — elemental_delete + elemental_create
        // TERA-tier swap requires it.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}

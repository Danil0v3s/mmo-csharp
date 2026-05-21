using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AL_CRUCIS — Acolyte Signum Crucis. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/crucis.cpp</c>.
///
/// <para>Splash debuff that lowers Undead/Demon DEF on every enemy
/// in range. Per-target landing chance is level-scaled:</para>
///
/// <code>
///   chance% = 25 + skillLevel*4 + casterLevel − targetLevel
/// </code>
///
/// <para>rAthena's outer branch (no <c>flag &amp; 1</c>) iterates
/// every enemy in <c>skill_get_splash</c> and recursively invokes
/// itself per-victim with the bit set. The inner branch is what
/// actually applies the SC.</para>
///
/// <para>Duration: <c>30000 * skillLevel</c> ms per skill_db.</para>
/// </summary>
public sealed class Crucis : SkillImpl
{
    private readonly Random _rng;

    public Crucis() : base(SkillIds.AL_CRUCIS) => _rng = Random.Shared;

    public Crucis(Random? rng = null) : base(SkillIds.AL_CRUCIS) => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Always broadcast the cast visual (rAthena emits clif_skill_nodamage
        // in the outer branch). Splash range from skill_db: 7 cells.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        const short splashRange = 7;
        var duration = 30_000 * skillLevel;

        var victims = ctx.Entities.ForEachInRange(src.MapId, src.X, src.Y, splashRange,
            EntityType.Mob | EntityType.Pc)
            .Where(v => v.Id != src.Id);

        foreach (var v in victims)
        {
            // chance = 25 + skill_lv*4 + casterLv - targetLv
            var chance = 25 + skillLevel * 4 + src.Level - v.Level;
            if (chance <= 0) continue;
            if (_rng.Next(100) >= chance) continue;

            ctx.Sc?.Start(v, StatusType.Signumcrucis, val1: skillLevel,
                0, 0, 0, duration, src);
        }
    }
}

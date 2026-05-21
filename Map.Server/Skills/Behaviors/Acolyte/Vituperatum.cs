using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AB_VITUPERATUM — Arch Bishop Vituperatum. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/vituperatum.cpp</c>.
///
/// <para>AoE damage centered on the target. The fork uses
/// <c>StatusSkillImpl</c> as the base (its SC apply hook lives in
/// the SC handler since AB_VITUPERATUM doesn't have a standard
/// SC — the recursive splash is how it deals damage). The C# port
/// inlines the splash iteration + per-victim damage application
/// (Holy element from skill_db).</para>
/// </summary>
public sealed class Vituperatum : SkillImpl
{
    public Vituperatum() : base(SkillIds.AB_VITUPERATUM) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: map_foreachinrange(skill_area_sub, target, splash, BL_CHAR, src,
        //          getSkillId(), skill_lv, tick, flag|BCT_ENEMY|1, skill_castend_nodamage_id);
        // The recursive inner call applies StatusSkillImpl's standard SC apply,
        // which AB_VITUPERATUM uses as a damage delivery (the skill has no SC
        // attached, but the splash function still runs through it).
        const short splashRange = 3;
        var victims = ctx.Entities.ForEachInRange(target.MapId, target.X, target.Y,
            splashRange, EntityType.Mob | EntityType.Pc)
            .Where(v => v.Id != src.Id);

        // Per-target damage = base MATK * (300 + 100*lv) / 100 (skill_db ratio).
        // We use the magic-bolt baseline as the MATK source.
        var matk = (src.Stats.MatkMin + src.Stats.MatkMax) / 2;
        if (matk <= 0) matk = 1;
        var ratio = 300 + 100 * skillLevel;
        var perVictim = Math.Max(1, matk * ratio / 100);

        foreach (var v in victims)
        {
            ctx.Damage.ApplyDamage(v, perVictim, src);
        }

        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}

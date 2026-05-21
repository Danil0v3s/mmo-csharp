using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// PR_BENEDICTIO — Priest Benedictio Sanctissimi Sacramenti. Manual
/// port of <c>rathena-fork/src/map/skills/acolyte/bssacramenti.cpp</c>.
///
/// <para>Ground-target dual-effect:</para>
/// <list type="bullet">
///   <item>Living non-Demon targets in splash get
///         <see cref="StatusType.Benedictio"/> SC.</item>
///   <item>Undead-race or Demon-race targets take Holy magic damage.</item>
/// </list>
/// </summary>
public sealed class BenedictioSanctissimiSacramenti : SkillImpl
{
    private readonly Map.Server.Skills.ISkillAttackService? _skillAttack;

    public BenedictioSanctissimiSacramenti() : base(SkillIds.PR_BENEDICTIO) { }

    public BenedictioSanctissimiSacramenti(Map.Server.Skills.ISkillAttackService? skillAttack = null)
        : base(SkillIds.PR_BENEDICTIO)
    {
        _skillAttack = skillAttack;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Apply SC only when target is NOT undead/demon (the SC-side
        // benefit; undead targets get hit instead).
        if (target.Stats.Race == BattleRace.Undead ||
            target.Stats.DefenseElement == BattleElement.Undead ||
            target.Stats.Race == BattleRace.Demon) return;

        ctx.Sc?.Start(target, StatusType.Benedictio,
            val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target.Stats.Race != BattleRace.Undead &&
            target.Stats.DefenseElement != BattleElement.Undead &&
            target.Stats.Race != BattleRace.Demon) return;

        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Splash: PCs get buff, mobs/players in undead race get damage.
        const short splashRange = 5;
        var victims = ctx.Entities.ForEachInRange(src.MapId, x, y, splashRange,
            EntityType.Mob | EntityType.Pc);
        foreach (var v in victims)
        {
            if (v.Id == src.Id) continue;
            CastendNoDamageId(src, v, skillLevel, ctx);
            CastendDamageId(src, v, skillLevel, ctx);
        }
    }
}

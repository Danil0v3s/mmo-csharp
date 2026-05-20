using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AL_CRUCIS — Acolyte Signum Crucis. Mirrors
/// <c>rathena-fork/src/map/skills/acolyte/signumcrucis.cpp</c>.
///
/// Caster-centered AoE (radius 5). Applies SC_SIGNUMCRUCIS to every
/// Undead / Dark element enemy in range — Val1 = (10 + lv*4) % DEF
/// drop. Duration <c>30 * lv</c> seconds.
/// </summary>
public sealed class SignumCrucis : SkillImpl
{
    private const short Radius = 5;

    public SignumCrucis() : base(SkillIds.AL_CRUCIS) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return;
        var debuff = 10 + skillLevel * 4;
        var durationMs = 30_000 * skillLevel;
        var victims = ctx.Entities.ForEachInRange(src.MapId, src.X, src.Y, Radius,
            EntityType.Mob | EntityType.Pc);
        foreach (var v in victims)
        {
            if (v.Id == src.Id) continue;
            if (v.Stats.DefenseElement != BattleElement.Undead
                && v.Stats.DefenseElement != BattleElement.Dark) continue;
            ctx.Sc.Start(v, StatusType.Signumcrucis, val1: debuff, 0, 0, 0, durationMs, src);
        }
    }
}

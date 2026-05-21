using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AL_DECAGI — Decrease Agility. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/decagi.cpp</c>.
///
/// <para>Single-target debuff that lands with a chance scaled by
/// caster Level + INT:</para>
/// <code>
///   chance% = 50 + skillLevel*3 + (CasterLevel + CasterINT) / 5
/// </code>
///
/// <para>Duration follows the AL_DECAGI Duration1 ladder in
/// <c>db/re/skill_db.yml</c>: <c>(30 + 10*lv) * 1000</c> ms.</para>
///
/// <para>The clif frame's <c>result</c> byte mirrors whether the SC
/// actually landed (1 = applied, 0 = resisted) so the client renders
/// the right hit/miss animation — same as the rAthena one-liner
/// <c>clif_skill_nodamage(..., sc_start(...))</c>.</para>
/// </summary>
public sealed class DecreaseAgi : SkillImpl
{
    private readonly Random _rng;

    public DecreaseAgi() : base(SkillIds.AL_DECAGI) => _rng = Random.Shared;

    public DecreaseAgi(Random? rng = null) : base(SkillIds.AL_DECAGI) => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: chance = 50 + skill_lv*3 + (status_get_lv(src) + sstatus->int_) / 5
        var chance = 50 + skillLevel * 3 + (src.Level + src.Stats.IntStat) / 5;

        bool landed = false;
        if (ctx.Sc != null && _rng.Next(100) < chance)
        {
            var duration = (30 + 10 * skillLevel) * 1000;
            var sc = ctx.Sc.Start(target, StatusType.DecreaseAgi, val1: skillLevel, 0, 0, 0, duration, src);
            landed = sc != null;
        }

        // rAthena: clif_skill_nodamage(src, *bl, getSkillId(), skill_lv, sc_start(...))
        // The packet's success byte = whether the SC actually attached
        // (rAthena passes the bool return of sc_start as the 5th arg).
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel, landed);
    }
}

using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// IQ_EXPOSION_BLASTER — Inquisitor Explosion Blaster. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/explosionblaster.cpp</c>.
///
/// <para>Holy splash. Ratio: <c>-100 + 450 + 2600*lv + 10*POW</c>;
/// +<c>950*lv</c> bonus when target has SC_HOLY_OIL (Oleum Sanctum
/// combo). SC reader in CalculateSkillRatio not yet wired —
/// bonus deferred.</para>
/// </summary>
public sealed class ExplosionBlaster : RecursiveDamageSplashSkillImpl
{
    public ExplosionBlaster() : base(SkillIds.IQ_EXPOSION_BLASTER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        CastendDamageId(src, target, skillLevel, ctx);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + 450 + 2600 * skillLevel) + 10 * src.Stats.Pow;
    }
}

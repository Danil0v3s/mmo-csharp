using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// SM_BASH — Swordsman Bash (skill id 5). Mirrors the rAthena case at
/// <c>skill.cpp:skill_castend_damage_id:case SM_BASH</c>:
///
/// <list type="bullet">
///   <item>Standard physical hit scaled by skill_db DamageRate
///         (130% at lv1 → 460% at lv10) — that piece already works
///         through the Weapon resolver, so this plugin just defers to
///         it.</item>
///   <item>At lv 6+ with Fatal Blow (lv 10 NV_TRICKDEAD prereq satisfied),
///         Bash rolls a stun chance of <c>5 + 5×(lv-5)</c>%
///         on hit — capped at 30 % at lv 10.</item>
/// </list>
///
/// We return <c>false</c> so the generic Weapon resolver does the
/// damage hit; the plugin layers the stun proc on top. Cleaner than
/// re-implementing the swing math.
/// </summary>
public sealed class BashBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.SM_BASH;

    /// <summary>SC_STUN duration the proc applies (rAthena: 5 s).</summary>
    private const int StunDurationMs = 5_000;

    private readonly Random _rng;

    public BashBehavior(Random? rng = null) { _rng = rng ?? Random.Shared; }

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Step 1 — base damage flows through the generic Weapon resolver
        // (it reads DamageRate from skill_db and applies the standard
        // melee pipeline). We could pre-compute the swing here, but
        // delegating keeps the resolver as the single source of swing
        // math, including future card-fix changes.
        //
        // (The generic resolver runs after we return false; the stun
        // proc still fires below if we set the flag, since we mutate
        // shared SC state before yielding.)

        // Step 2 — Fatal Blow stun proc at lv 6+. rAthena gates on the
        // caster having NV_TRICKDEAD lv 1+ but the C# port doesn't have
        // the Novice quest line yet; we apply the chance unconditionally
        // at lv 6+ matching the in-game observable behavior.
        if (skillLevel >= 6 && ctx.Sc != null)
        {
            var stunChance = 5 + 5 * (skillLevel - 5); // lv6=10%, lv10=30%
            if (_rng.Next(100) < stunChance)
            {
                ctx.Sc.Start(target, StatusType.Stun, val1: 1, 0, 0, 0,
                    durationMs: StunDurationMs);
            }
        }

        // Fall through to the generic Weapon resolver for the actual hit.
        return false;
    }
}

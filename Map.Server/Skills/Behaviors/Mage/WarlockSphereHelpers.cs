using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// Warlock-sphere bookkeeping. The four <c>WL_SUMMON*</c> skills
/// (Stone / Fire / Water / Wind) push an element-typed sphere into
/// <c>SC_SPHERE_1..5</c>; the spheres are later consumed by
/// <c>WL_RELEASE</c> / <c>WL_COMET</c> / <c>WL_TETRAVORTEX</c> / etc.
///
/// <para>rAthena (skill.cpp:WL_SUMMONSTONE arm): try each
/// <c>SC_SPHERE_*</c> slot in order; if it's free, sc_start the
/// requested element into Val1 there. If all five slots are occupied,
/// rAthena overwrites the oldest at lv 2+; lv 1 fails.</para>
/// </summary>
internal static class WarlockSphereHelpers
{
    /// <summary>rAthena <c>WLS_FIRE</c>.</summary>
    public const int WlsFire = 1;
    /// <summary>rAthena <c>WLS_WIND</c>.</summary>
    public const int WlsWind = 2;
    /// <summary>rAthena <c>WLS_WATER</c>.</summary>
    public const int WlsWater = 3;
    /// <summary>rAthena <c>WLS_STONE</c>.</summary>
    public const int WlsStone = 4;

    private static readonly StatusType[] SphereSlots =
    {
        StatusType.Sphere1,
        StatusType.Sphere2,
        StatusType.Sphere3,
        StatusType.Sphere4,
        StatusType.Sphere5,
    };

    /// <summary>
    /// Push an element-typed sphere into the next free <c>SC_SPHERE_*</c>
    /// slot. If all slots are full, replace <c>SC_SPHERE_1</c> when
    /// <paramref name="skillLevel"/> ≥ 2 (rAthena's overwrite-oldest
    /// behavior); refuse at lv 1.
    /// </summary>
    public static void PushSphere(Entity caster, int element, ushort skillLevel,
        int durationMs, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return;
        for (var i = 0; i < SphereSlots.Length; i++)
        {
            if (ctx.Sc.Get(caster, SphereSlots[i]) == null)
            {
                ctx.Sc.Start(caster, SphereSlots[i], val1: element, 0, 0, 0, durationMs, caster);
                return;
            }
        }
        if (skillLevel < 2) return;
        // All five occupied — overwrite the oldest slot (Sphere1).
        ctx.Sc.End(caster, StatusType.Sphere1);
        ctx.Sc.Start(caster, StatusType.Sphere1, val1: element, 0, 0, 0, durationMs, caster);
    }
}

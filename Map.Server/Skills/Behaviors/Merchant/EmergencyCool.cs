using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_EMERGENCYCOOL — Mechanic Emergency Cool
/// (skill.cpp:NC_EMERGENCYCOOL arm). Reads the caster's inventory for
/// the three cooling-item tiers (rAthena skill_db Requires.Equipment):
/// <list type="bullet">
///   <item><c>Cooling_Device</c> → -45 overheat</item>
///   <item><c>High_Quality_Cooler</c> → -75 overheat</item>
///   <item><c>Special_Cooler</c> → -105 overheat</item>
/// </list>
/// Picks the highest tier whose item is present; defaults to -45
/// when no cooler item is found (rAthena's <c>min(i, 2)</c> clamp).
/// The overheat counter lives on <c>SC_OVERHEAT_LIMITPOINT.Val1</c>;
/// we End + restart at the reduced value, or End outright when the
/// new value clamps to ≤ 0.
/// </summary>
public sealed class EmergencyCool : SkillImpl
{
    /// <summary>rAthena skill_db cooling-item aegis names in
    /// least-cool → most-cool order (matches limit[] index 0..2).</summary>
    private static readonly (string Aegis, int Delta)[] CoolingTiers =
    {
        ("Cooling_Device",     -45),
        ("High_Quality_Cooler", -75),
        ("Special_Cooler",    -105),
    };

    public EmergencyCool() : base(SkillIds.NC_EMERGENCYCOOL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (src is not PlayerEntity pc) return;
        var session = ctx.Sessions?.TryGet(pc);
        var inv = session?.Inventory;
        var catalog = ctx.Catalog;
        var delta = -45; // default to weakest cooling (rAthena min(i, 2)).
        if (inv != null && catalog != null)
        {
            foreach (var (aegis, d) in CoolingTiers)
            {
                var catRow = catalog.GetByAegisName(aegis);
                if (catRow == null) continue;
                if (inv.Exists(r => r.NameId == catRow.Id && r.Amount > 0))
                {
                    delta = d; // walk all tiers — last match wins (strongest).
                }
            }
        }
        ApplyOverheat(pc, delta, ctx);
    }

    private static void ApplyOverheat(PlayerEntity pc, int delta, SkillBehaviorContext ctx)
    {
        var sc = ctx.Sc?.Get(pc, StatusType.OverheatLimitpoint);
        if (sc == null) return;
        var next = sc.Val1 + delta;
        if (next <= 0)
        {
            ctx.Sc?.End(pc, StatusType.OverheatLimitpoint);
            ctx.Sc?.End(pc, StatusType.Overheat);
            return;
        }
        // rAthena pc_overheat restarts the SC at the new value; the
        // SC engine handles the tier-down visual on the wire.
        var remaining = (int)Math.Max(1_000, sc.ExpiresAt - Environment.TickCount64);
        ctx.Sc?.End(pc, StatusType.OverheatLimitpoint);
        ctx.Sc?.Start(pc, StatusType.OverheatLimitpoint, val1: next, sc.Val2, sc.Val3, sc.Val4,
            durationMs: remaining, pc);
    }
}

using Map.Server.Entities;
using Map.Server.World;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MC_VENDING — Merchant Vending. Manual port of rAthena
/// <c>vending.cpp:vending_openvending</c> (skill arm). Pops the vending
/// UI request (max items = 2 + skill level) for the caster's cart.
///
/// <para>Gates (mirrors rAthena vending.cpp:308):
/// <list type="bullet">
///   <item>Caster must have MC_VENDING learned (skill_lv &gt; 0) — the
///         cast hook already enforces this via skill_db sp_cost.</item>
///   <item>Trade permission: rAthena <c>pc_can_give_items(sd)</c>. The
///         C# port doesn't track per-account GM-trade permission yet;
///         we honor the MapFlag.NoTrade / MapFlag.NoVending gates that
///         enforce the same "this player can't list a stall here"
///         behavior on the typical no-trade WoE / event maps.</item>
///   <item>Cart presence: vending without a cart equipped fails. The
///         cart state lives on session data; without a session bridge
///         the cast still emits — the actual stall open / persistence
///         happens through the vending shop subsystem when wired.</item>
/// </list></para>
/// </summary>
public sealed class Vending : SkillImpl
{
    public Vending() : base(SkillIds.MC_VENDING) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity sd) return;

        // Trade-gate: rAthena pc_can_give_items + map-no-trade refusal.
        if (!CanOpenVending(sd, ctx))
        {
            ctx.Client?.BroadcastSkillFail(sd, SkillId,
                Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }

    private static bool CanOpenVending(PlayerEntity sd, SkillBehaviorContext ctx)
    {
        // rAthena pc_can_give_items — gated by PC_PERM_TRADE /
        // PC_PERM_TRADE_UNCONDITIONAL. Default for typical accounts is
        // true; the C# port's PC permission system isn't wired yet, so
        // the per-account refusal is a no-op. We honor the map-level
        // gates that yield the same end-user outcome.
        if (ctx.MapFlags == null || ctx.World == null) return true;
        foreach (var m in ctx.World.All)
        {
            if ((uint)m.Name.GetHashCode() == sd.MapId)
            {
                if (ctx.MapFlags.IsSet(m.Name, MapFlag.NoVending)) return false;
                if (ctx.MapFlags.IsSet(m.Name, MapFlag.NoTrade)) return false;
                break;
            }
        }
        return true;
    }
}

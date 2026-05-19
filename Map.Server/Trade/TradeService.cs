using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Trade;

/// <summary>
/// Concrete <see cref="ITradeService"/>. Drives the trade state
/// machine — request, ack, add item/zeny, ok, commit, cancel —
/// against per-session <see cref="TradeState"/> blocks.
///
/// Atomic commit invariant: when both sides reach
/// <c>LockedStage == 2</c> we sanity-check (impossible_trade_check
/// equivalent: deal slots still reference valid inventory rows with
/// the right amount; both bags have room) and then perform the swap
/// in one go. Any failure throws the trade and leaves both inventories
/// untouched.
/// </summary>
public sealed class TradeService : ITradeService
{
    private readonly IEntityRegistry _entities;
    private readonly ISessionManagerAccessor _sessions;
    private readonly ILogger<TradeService> _logger;

    public TradeService(
        IEntityRegistry entities,
        ISessionManagerAccessor sessions,
        ILogger<TradeService> logger)
    {
        _entities = entities;
        _sessions = sessions;
        _logger = logger;
    }

    public TradeOpResult Request(PlayerEntity initiator, PlayerEntity target)
    {
        if (target == null || initiator.Id == target.Id) return TradeOpResult.TargetNotExist;

        // rAthena: cancel any prior partner before binding the new one.
        ClearTradeIfAny(initiator);

        var tSession = _sessions.GetByEntityId(target.Id);
        if (tSession?.Trade != null) return TradeOpResult.Failed;
        // Cross-map / out-of-range gate.
        if (initiator.MapId != target.MapId) return TradeOpResult.TargetTooFar;
        var dx = Math.Abs(initiator.X - target.X);
        var dy = Math.Abs(initiator.Y - target.Y);
        if (Math.Max(dx, dy) > ITradeService.TradeDistance) return TradeOpResult.TargetTooFar;

        var iSession = _sessions.GetByEntityId(initiator.Id);
        if (iSession == null || tSession == null) return TradeOpResult.Failed;

        iSession.Trade = new TradeState
        {
            SelfCharId = initiator.CharacterId,
            PartnerCharId = target.CharacterId,
            PartnerLevel = target.Level,
        };
        tSession.Trade = new TradeState
        {
            SelfCharId = target.CharacterId,
            PartnerCharId = initiator.CharacterId,
            PartnerLevel = initiator.Level,
        };
        // Target has to Acknowledge before the swap window opens.
        iSession.Trade.Accepted = false;
        tSession.Trade.Accepted = false;
        return TradeOpResult.Ok;
    }

    public TradeOpResult Acknowledge(PlayerEntity target, bool accept)
    {
        var tSession = _sessions.GetByEntityId(target.Id);
        if (tSession?.Trade is not { } tState) return TradeOpResult.NotInTrade;

        if (!accept)
        {
            ClearTradeIfAny(target);
            return TradeOpResult.Ok;
        }

        // Mark both sides as accepted — trade window can open.
        tState.Accepted = true;
        if (Other(tState) is { } iState) iState.Accepted = true;
        return TradeOpResult.Ok;
    }

    public TradeOpResult AddItem(PlayerEntity side, int inventoryIndex, int amount)
    {
        var session = _sessions.GetByEntityId(side.Id);
        if (session?.Trade is not { Accepted: true, LockedStage: 0 } state) return TradeOpResult.InvalidStage;
        if (session.Inventory is not { } inv) return TradeOpResult.Failed;
        if (inventoryIndex < 0 || inventoryIndex >= inv.Count) return TradeOpResult.SlotInvalid;
        if (amount < 1 || amount > inv[inventoryIndex].Amount) return TradeOpResult.SlotInvalid;
        if (state.Items.Count >= ITradeService.MaxDealItems) return TradeOpResult.SlotInvalid;

        // rAthena allows the same slot to appear multiple times only if
        // the total doesn't exceed available stack — collapse to a
        // single entry for the first slice.
        for (var i = 0; i < state.Items.Count; i++)
        {
            if (state.Items[i].InventoryIndex == inventoryIndex)
            {
                var combined = state.Items[i].Amount + amount;
                if (combined > inv[inventoryIndex].Amount) return TradeOpResult.SlotInvalid;
                state.Items[i] = new TradeItem(inventoryIndex, combined);
                return TradeOpResult.Ok;
            }
        }
        state.Items.Add(new TradeItem(inventoryIndex, amount));
        return TradeOpResult.Ok;
    }

    public TradeOpResult AddZeny(PlayerEntity side, long zeny)
    {
        var session = _sessions.GetByEntityId(side.Id);
        if (session?.Trade is not { Accepted: true, LockedStage: 0 } state) return TradeOpResult.InvalidStage;
        if (session.CharacterData == null) return TradeOpResult.Failed;
        if (zeny < 0) return TradeOpResult.Failed;
        if ((ulong)zeny > session.CharacterData.Zeny) return TradeOpResult.NotEnoughZeny;
        state.Zeny = zeny;
        return TradeOpResult.Ok;
    }

    public TradeOpResult Ok(PlayerEntity side)
    {
        var session = _sessions.GetByEntityId(side.Id);
        if (session?.Trade is not { Accepted: true } state) return TradeOpResult.NotInTrade;
        if (state.LockedStage > 0) return TradeOpResult.Ok; // idempotent
        state.LockedStage = 1;
        return TradeOpResult.Ok;
    }

    public TradeOpResult Commit(PlayerEntity side)
    {
        var iSession = _sessions.GetByEntityId(side.Id);
        if (iSession?.Trade is not { } iState) return TradeOpResult.NotInTrade;
        if (iState.LockedStage < 1) return TradeOpResult.InvalidStage;
        iState.LockedStage = 2;

        if (Other(iState) is not { LockedStage: 2 } tState)
        {
            // Other side hasn't pressed Trade yet — wait.
            return TradeOpResult.InvalidStage;
        }

        // Both committed — atomic swap.
        var tSession = _sessions.GetByEntityId(new EntityId(iState.PartnerCharId));
        if (tSession == null) { ClearBoth(iState); return TradeOpResult.Failed; }

        if (!ValidateBothSides(iSession, tSession, iState, tState))
        {
            ClearBoth(iState);
            return TradeOpResult.Failed;
        }

        SwapItems(iSession, tSession, iState, tState);
        SwapZeny(iSession, tSession, iState, tState);

        ClearBoth(iState);
        _logger.LogInformation(
            "Trade committed: char {A} ↔ char {B}",
            iState.SelfCharId, iState.PartnerCharId);
        return TradeOpResult.Ok;
    }

    public TradeOpResult Cancel(PlayerEntity side)
    {
        if (_sessions.GetByEntityId(side.Id)?.Trade is { } state) ClearBoth(state);
        return TradeOpResult.Ok;
    }

    // ---- internals ----

    private TradeState? Other(TradeState state)
    {
        var partnerSession = _sessions.GetByEntityId(new EntityId(state.PartnerCharId));
        return partnerSession?.Trade;
    }

    private void ClearTradeIfAny(PlayerEntity pc)
    {
        var session = _sessions.GetByEntityId(pc.Id);
        if (session?.Trade is { } state) ClearBoth(state);
    }

    private void ClearBoth(TradeState state)
    {
        var iSession = _sessions.GetByEntityId(new EntityId(state.SelfCharId));
        var tSession = _sessions.GetByEntityId(new EntityId(state.PartnerCharId));
        if (iSession != null) iSession.Trade = null;
        if (tSession != null) tSession.Trade = null;
    }

    /// <summary>
    /// Mirrors rAthena <c>impossible_trade_check</c> + <c>trade_check</c>:
    /// every offered slot still references a valid inventory row with at
    /// least the offered amount; each side has zeny to cover its offer.
    /// Inventory-room check is permissive at this slice — slots removed +
    /// added net out 1:1 so the room couldn't shrink below the start.
    /// </summary>
    private static bool ValidateBothSides(
        MapSessionData iSession, MapSessionData tSession,
        TradeState iState, TradeState tState)
    {
        if (!ValidateOneSide(iSession, iState)) return false;
        if (!ValidateOneSide(tSession, tState)) return false;
        return true;
    }

    private static bool ValidateOneSide(MapSessionData session, TradeState state)
    {
        if (session.Inventory is not { } inv) return state.Items.Count == 0;
        foreach (var it in state.Items)
        {
            if (it.InventoryIndex < 0 || it.InventoryIndex >= inv.Count) return false;
            if (inv[it.InventoryIndex].Amount < it.Amount) return false;
        }
        if (state.Zeny > 0 && (session.CharacterData?.Zeny ?? 0) < (ulong)state.Zeny) return false;
        return true;
    }

    private static void SwapItems(
        MapSessionData iSession, MapSessionData tSession,
        TradeState iState, TradeState tState)
    {
        TransferItems(iSession, tSession, iState);
        TransferItems(tSession, iSession, tState);
    }

    private static void TransferItems(MapSessionData from, MapSessionData to, TradeState fromState)
    {
        if (from.Inventory is not { } srcInv) return;
        to.Inventory ??= new List<InventoryItem>();
        var dstInv = to.Inventory;

        // Iterate offered slots in reverse-index order so RemoveAt doesn't
        // shift indices for entries we haven't processed yet.
        foreach (var deal in fromState.Items.OrderByDescending(d => d.InventoryIndex))
        {
            var src = srcInv[deal.InventoryIndex];
            // Stack into an existing mergeable slot, else append new.
            var merge = FindMergeable(dstInv, src);
            if (merge != null)
            {
                merge.Amount += (uint)deal.Amount;
            }
            else
            {
                dstInv.Add(new InventoryItem
                {
                    ServerIndex = dstInv.Count,
                    NameId = src.NameId,
                    Amount = (uint)deal.Amount,
                    Identified = src.Identified,
                    Refine = src.Refine,
                    Attribute = src.Attribute,
                    Card0 = src.Card0, Card1 = src.Card1, Card2 = src.Card2, Card3 = src.Card3,
                    ExpireTime = src.ExpireTime,
                    Bound = src.Bound,
                    UniqueId = src.UniqueId,
                    EnchantGrade = src.EnchantGrade,
                });
            }

            src.Amount -= (uint)deal.Amount;
            if (src.Amount == 0)
            {
                if (src.Id > 0) from.RemovedInventoryIds.Add(src.Id);
                srcInv.RemoveAt(deal.InventoryIndex);
            }
        }
    }

    private static void SwapZeny(
        MapSessionData iSession, MapSessionData tSession,
        TradeState iState, TradeState tState)
    {
        if (iState.Zeny > 0 && iSession.CharacterData != null && tSession.CharacterData != null)
        {
            iSession.CharacterData.Zeny -= (uint)iState.Zeny;
            tSession.CharacterData.Zeny += (uint)iState.Zeny;
        }
        if (tState.Zeny > 0 && iSession.CharacterData != null && tSession.CharacterData != null)
        {
            tSession.CharacterData.Zeny -= (uint)tState.Zeny;
            iSession.CharacterData.Zeny += (uint)tState.Zeny;
        }
    }

    private static InventoryItem? FindMergeable(List<InventoryItem> inv, InventoryItem src)
    {
        foreach (var i in inv)
        {
            if (i.NameId != src.NameId) continue;
            if (i.Refine != src.Refine || i.Identified != src.Identified) continue;
            if (i.Card0 != src.Card0 || i.Card1 != src.Card1 || i.Card2 != src.Card2 || i.Card3 != src.Card3) continue;
            return i;
        }
        return null;
    }
}

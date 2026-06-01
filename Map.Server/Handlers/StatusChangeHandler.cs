using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Status;

namespace Map.Server.Handlers;

/// <summary>
/// CZ_STATUS_CHANGE dispatcher — spend status points to raise a base
/// stat. rAthena <c>pc_statusup</c> (pc.cpp:8734). Validates max-stat
/// cap, point cost, and triggers a stat recalc so derived numbers
/// (hit / flee / batk / matk / etc.) refresh.
/// </summary>
[PacketHandler(PacketHeader.CZ_STATUS_CHANGE)]
public class StatusChangeHandler(
    IEntityRegistry registry,
    IStatusCalcService statusCalc,
    ILogger<StatusChangeHandler> logger
) : IPacketHandler<MapSessionData, CZ_STATUS_CHANGE>
{
    public Task HandleAsync(MapSessionData session, CZ_STATUS_CHANGE packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity player)
        {
            return Task.CompletedTask;
        }

        if (packet.Amount == 0)
        {
            return Task.CompletedTask;
        }

        // COMBAT-10: allocate into the persisted BASE stat (rAthena
        // sd->status.str), not the final battle stat. CalcPc re-layers
        // equip + job bonus on top from BaseParams.
        ref short field = ref Map(player.BaseParams, packet.StatusId);
        // ref-return on a non-existent slot returns the dummy; check id.
        if (!IsValidStatId(packet.StatusId))
        {
            logger.LogDebug("Invalid stat id {Id} from char {Char}", packet.StatusId, player.CharacterId);
            return Task.CompletedTask;
        }

        var current = field;
        if (current >= StatPointTable.MaxBaseStat) return Task.CompletedTask;

        // rAthena pc_statusup caps the requested amount to the maximum
        // increase possible with the current point pool.
        int possible = 0, pointsLeft = player.StatusPoints;
        while (current + possible < StatPointTable.MaxBaseStat && possible < packet.Amount)
        {
            var cost = StatPointTable.CostToIncrement(current + possible);
            if (cost > pointsLeft) break;
            pointsLeft -= cost;
            possible++;
        }
        if (possible == 0) return Task.CompletedTask;

        var totalCost = StatPointTable.TotalCost(current, possible);
        field = (short)(current + possible);
        player.StatusPoints -= totalCost;

        // Recalc the full stat block so hit/flee/batk/matk/maxhp/etc
        // pick up the new primary stat.
        statusCalc.CalcPc(player, BuildInputs(player));

        // Push the new values back to the client. SP_STR/AGI/VIT/INT/DEX/LUK
        // is the same id the request came in on; rAthena also emits
        // SP_USTR..SP_ULUK (the per-stat cost indicator) but those derive
        // cleanly from the new value via StatPointTable.CostToIncrement.
        session.EnqueuePacket(new ZC_PAR_CHANGE { VarId = packet.StatusId, Value = field });
        session.EnqueuePacket(new ZC_PAR_CHANGE { VarId = SpId.SP_STATUSPOINT, Value = player.StatusPoints });
        // Cost indicator for the next +1 — SP_USTR/UAGI/... at id+19.
        var costVarId = (ushort)(packet.StatusId + (SpId.SP_USTR - SpId.SP_STR));
        session.EnqueuePacket(new ZC_PAR_CHANGE
        {
            VarId = costVarId,
            Value = StatPointTable.CostToIncrement(field),
        });

        logger.LogInformation(
            "Char {Char} raised stat {Stat} by {Amount} (cost {Cost}; remaining {Left} points)",
            player.CharacterId, packet.StatusId, possible, totalCost, player.StatusPoints);
        return Task.CompletedTask;
    }

    private static bool IsValidStatId(ushort id)
        => id >= SpId.SP_STR && id <= SpId.SP_LUK;

    /// <summary>
    /// Resolve the writable BASE stat slot on <see cref="PcBaseParams"/>.
    /// Returns a ref to a throwaway field for unknown ids so the
    /// branch above can validate without crashing.
    /// </summary>
    private static ref short Map(PcBaseParams s, ushort id)
    {
        switch (id)
        {
            case SpId.SP_STR: return ref s.Str;
            case SpId.SP_AGI: return ref s.Agi;
            case SpId.SP_VIT: return ref s.Vit;
            case SpId.SP_INT: return ref s.IntStat;
            case SpId.SP_DEX: return ref s.Dex;
            case SpId.SP_LUK: return ref s.Luk;
            default: return ref _unused;
        }
    }
    private static short _unused;

    // COMBAT-10: base params from BaseParams; weapon/def from Stats; JobId/
    // WeaponType threaded so the calc folds the same job bonus as every other
    // recalc path (else the param-base snapshot would strip it next recalc).
    private static PcBaseInputs BuildInputs(PlayerEntity p)
        => new(
            BaseLevel: p.Level, JobLevel: p.JobLevel,
            Str: p.BaseParams.Str, Agi: p.BaseParams.Agi, Vit: p.BaseParams.Vit,
            Int: p.BaseParams.IntStat, Dex: p.BaseParams.Dex, Luk: p.BaseParams.Luk,
            Pow: p.BaseParams.Pow, Sta: p.BaseParams.Sta, Wis: p.BaseParams.Wis,
            Spl: p.BaseParams.Spl, Con: p.BaseParams.Con, Crt: p.BaseParams.Crt,
            WeaponAtkMin: p.Stats.WatkMin, WeaponAtkMax: p.Stats.WatkMax,
            EquipDef: p.Stats.Def, EquipMdef: p.Stats.Mdef,
            AttackRange: p.Stats.AttackRange,
            JobId: p.ClassId, WeaponType: p.WeaponType);
}

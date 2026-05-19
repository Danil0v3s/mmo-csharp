using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Status;
using Map.Server.Visibility;

namespace Map.Server.Handlers;

/// <summary>
/// "LoadEndAck" — the client tells us it's finished loading the map data
/// and is ready to render entities. rAthena <c>clif_parse_LoadEndAck</c>:
/// flips the session from auth-only to actually spawned, registers the
/// <see cref="PlayerEntity"/> in the spatial index, and emits the entry +
/// surroundings packets so the world appears around the player.
/// </summary>
[PacketHandler(PacketHeader.CZ_NOTIFY_ACTORINIT)]
public class NotifyActorInitHandler(
    IEntityRegistry registry,
    IVisibilityService visibility,
    StatusBroadcaster statusBroadcaster,
    IStatusCalcService statusCalc,
    Map.Server.Inventory.IInventoryService inventory,
    ILogger<NotifyActorInitHandler> logger
) : IPacketHandler<MapSessionData, CZ_NOTIFY_ACTORINIT>
{
    public Task HandleAsync(MapSessionData session, CZ_NOTIFY_ACTORINIT packet)
    {
        if (session.AuthState != MapAuthState.Authenticated)
        {
            logger.LogWarning(
                "CZ_NOTIFY_ACTORINIT on session {SessionId} in unexpected state {State} — ignoring",
                session.SessionId, session.AuthState);
            return Task.CompletedTask;
        }

        if (session.CharacterId is not { } charId
            || session.AccountId is not { } accountId
            || session.MapId is not { } mapId)
        {
            logger.LogError("Session {SessionId} reached actor-init without bound character info", session.SessionId);
            session.Disconnect(DisconnectReason.PacketHandlerError);
            return Task.CompletedTask;
        }

        // Defensive: if a stale entity exists for this char_id (crash recovery
        // / gRPC EnterMap placeholder), tear it down before re-spawning.
        if (registry.Get(new EntityId(charId)) != null)
        {
            registry.Remove(new EntityId(charId));
        }

        var player = new PlayerEntity(
            characterId: charId,
            accountId: accountId,
            name: session.CharacterName ?? string.Empty,
            sessionId: session.SessionId,
            mapId: mapId,
            x: session.SpawnX,
            y: session.SpawnY);
        player.Dir = session.SpawnDir;

        // Hydrate full BattleStats from the loaded char snapshot — mirrors
        // rAthena's status_calc_pc(SCO_FIRST) at chrif_authok (status.cpp:6160).
        // Inventory/equip layered in here would come from the InventoryService;
        // until equip parsing lands, pass the captured Novice weapon defaults so
        // batk/hit/flee match the wire baseline in RenewalFormulas.
        if (session.CharacterData is { } ch)
        {
            statusCalc.CalcPc(player, new PcBaseInputs(
                BaseLevel: (int)ch.BaseLevel,
                JobLevel: (int)ch.JobLevel,
                Str: (int)ch.Str,
                Agi: (int)ch.Agi,
                Vit: (int)ch.Vit,
                Int: (int)ch.IntStat,
                Dex: (int)ch.Dex,
                Luk: (int)ch.Luk,
                Pow: (int)ch.Pow,
                Sta: (int)ch.Sta,
                Wis: (int)ch.Wis,
                Spl: (int)ch.Spl,
                Con: (int)ch.Con,
                Crt: (int)ch.Crt,
                // Knife + Cotton Shirt baseline mirrors the rAthena capture
                // until inventory-driven equip processing lands.
                WeaponAtkMin: 17,
                WeaponAtkMax: 17,
                EquipDef: 10,
                EquipMdef: 0,
                AttackRange: 1));
            // Persisted current HP/SP from the snapshot wins over the calc-
            // derived max so partial-HP relog doesn't reset to full.
            player.Hp = (int)Math.Min(ch.Hp, (uint)player.MaxHp);
            player.Sp = (int)Math.Min(ch.Sp, (uint)player.MaxSp);
            // Persisted EXP + level + status/skill points — needed by
            // ExpService (pc_gainexp / pc_checkbaselevelup parity).
            player.BaseExp = (long)ch.BaseExp;
            player.JobExp = (long)ch.JobExp;
            player.JobLevel = (int)ch.JobLevel;
            player.StatusPoints = (int)ch.StatusPoint;
        }

        registry.Add(player);

        session.EntityId = player.Id;
        session.AuthState = MapAuthState.Spawned;

        // Tell other players in view about us; then tell us about them.
        visibility.NotifySpawnedToArea(player);
        visibility.SendCurrentViewToSelf(player);

        // LoadEndAck cascade — sprite changes, inventory, weight, map
        // property, self-spawn, skill info, hotkeys, exp, initial-status,
        // party/config/reputation. Mirrors clif_parse_LoadEndAck
        // (clif.cpp:10723+). Source data is the CharacterDataResponse
        // cached on the session by WantToConnectionHandler.
        if (session.CharacterData != null)
        {
            statusBroadcaster.BroadcastLoadEndAck(session, session.CharacterData, (uint)accountId);
        }

        // Inventory list — rAthena emits clif_inventorylist at the start
        // of LoadEndAck (clif.cpp:10760) so deleted items get filtered
        // before pc_checkitem would render them as "unknown item". We
        // emit it after BroadcastLoadEndAck for visual ordering; the
        // client is order-tolerant for the open-bag UI here.
        inventory.SendInventoryList(session);

        logger.LogInformation(
            "Player {Name} (char {CharId}) spawned at ({X},{Y}) on map 0x{MapId:X8}",
            player.Name, charId, player.X, player.Y, mapId);

        return Task.CompletedTask;
    }
}

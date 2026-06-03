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
    Map.Server.Combat.IPcDeathService pcDeath,
    Map.Server.Inventory.IInventoryService inventory,
    Map.Server.Items.IItemCatalog itemCatalog,
    Map.Server.Inventory.IItemHookDispatcher hookDispatcher,
    Map.Server.Inventory.IComboDispatcher comboDispatcher,
    Map.Server.Services.Intif.IIntifService intif,
    ILogger<NotifyActorInitHandler> logger
) : IPacketHandler<MapSessionData, CZ_NOTIFY_ACTORINIT>
{
    public async Task HandleAsync(MapSessionData session, CZ_NOTIFY_ACTORINIT packet)
    {
        if (session.AuthState != MapAuthState.Authenticated)
        {
            logger.LogWarning(
                "CZ_NOTIFY_ACTORINIT on session {SessionId} in unexpected state {State} — ignoring",
                session.SessionId, session.AuthState);
            return;
        }

        if (session.CharacterId is not { } charId
            || session.AccountId is not { } accountId
            || session.MapId is not { } mapId)
        {
            logger.LogError("Session {SessionId} reached actor-init without bound character info", session.SessionId);
            session.Disconnect(DisconnectReason.PacketHandlerError);
            return;
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
            // Equip-derived weapon/armor numbers (rAthena status_calc_pc
            // reads these from the equipped items at the same point in
            // the cascade). Fallback to the Novice + Knife baseline if
            // no items are equipped — keeps replay parity at first
            // login until starter items land.
            var equip = Map.Server.Inventory.EquipBonusAggregator
                .Aggregate(session.Inventory, itemCatalog);
            if (equip.WeaponAtkMin == 0) equip = equip with { WeaponAtkMin = 17, WeaponAtkMax = 17 };
            if (equip.EquipDef == 0) equip = equip with { EquipDef = 10 };

            // COMBAT-01: build the equip script-bonus bundle from worn gear
            // so card / flat bonuses (bHit, bCritical, bMaxHP, bAspd, ...)
            // apply on login — not only after a re-equip action. Mirrors
            // EquipService.TryRecalcStats' build; CalcPc below reads
            // player.EquipBonuses for the flat-derived folds. With no
            // bonus-script gear equipped the bundle stays empty (zero folds),
            // so the replay baseline is unaffected.
            Map.Server.Inventory.EquipBonusAggregator.BuildBundle(
                session.Inventory, itemCatalog, player.EquipBonuses, player, hookDispatcher);
            if (session.Inventory != null)
            {
                var equippedForCombo = new List<Map.Server.Inventory.InventoryItem>();
                for (var i = 0; i < session.Inventory.Count; i++)
                    if (session.Inventory[i].Equip != 0) equippedForCombo.Add(session.Inventory[i]);
                comboDispatcher.ApplyActiveCombos(equippedForCombo, player.EquipBonuses, player);
            }

            // COMBAT-09: persist the job class on the entity so the renewal
            // ASPD formula (and any later job-keyed lookup) resolves the right
            // job_aspd_db / job catalogs.
            player.ClassId = (int)ch.ClassId;

            // COMBAT-52: hydrate die_counter from the PC_DIE_COUNTER char register
            // (rAthena pc.cpp:2297) BEFORE the CalcPc below, so the Super Novice
            // never-died all-stat +10 gate folds with the persisted count in a single
            // recalc (no double-calc / HP-clamp glitch for a previously-died relogger).
            player.DieCounter = Map.Server.Persistence.DieCounterReg.Read(session.VarRegs);

            // COMBAT-16: resolve the right-hand weapon type from worn gear so the
            // size-fix penalty + renewal ASPD lookup are correct at login (not
            // stuck at 0/Fist).
            player.WeaponType = equip.WeaponType;

            // COMBAT-10: hydrate the persisted BASE allocated stats from the
            // char payload. These are the source for every later recalc-input
            // builder; CalcPc layers equip param + job bonus on top of them.
            player.BaseParams.Str = (short)ch.Str;
            player.BaseParams.Agi = (short)ch.Agi;
            player.BaseParams.Vit = (short)ch.Vit;
            player.BaseParams.IntStat = (short)ch.IntStat;
            player.BaseParams.Dex = (short)ch.Dex;
            player.BaseParams.Luk = (short)ch.Luk;
            player.BaseParams.Pow = (short)ch.Pow;
            player.BaseParams.Sta = (short)ch.Sta;
            player.BaseParams.Wis = (short)ch.Wis;
            player.BaseParams.Spl = (short)ch.Spl;
            player.BaseParams.Con = (short)ch.Con;
            player.BaseParams.Crt = (short)ch.Crt;

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
                WeaponAtkMin: equip.WeaponAtkMin,
                WeaponAtkMax: equip.WeaponAtkMax,
                EquipDef: equip.EquipDef,
                EquipMdef: equip.EquipMdef,
                AttackRange: equip.AttackRange,
                WeaponElement: equip.WeaponElement,
                // COMBAT-09: job + weapon type drive the renewal ASPD formula's
                // job_aspd_db base lookup. ClassId is set on the player just
                // above; WeaponType is computed from worn gear by the equip
                // helper (0 = bare-hand until first equip recalc).
                JobId: (int)ch.ClassId,
                WeaponType: player.WeaponType,
                WeaponLevel: equip.WeaponLevel,
                // COMBAT-18: off-hand (dual-wield) weapon from the equip summary.
                LeftWeaponAtkMin: equip.LeftWeaponAtkMin,
                LeftWeaponAtkMax: equip.LeftWeaponAtkMax,
                LeftWeaponLevel: equip.LeftWeaponLevel,
                LeftWeaponType: equip.LeftWeaponType,
                LeftWeaponElement: equip.LeftWeaponElement,
                HasShield: equip.HasShield));
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
            // Register the savepoint for pc_respawn. CharacterDataResponse
            // doesn't yet split save_point from last_point — use the last
            // map/pos as a proxy (first-slice; proto extension is queued).
            if (!string.IsNullOrEmpty(ch.MapName))
            {
                pcDeath.SetSavepoint(charId, ch.MapName, session.SpawnX, session.SpawnY);
            }
        }

        // COMBAT-48 — hydrate the Warp Portal memo points loaded from the
        // memo table (PlayerStateService.LoadAsync) onto the live entity so
        // AL_WARP destination resolution + pc_memo see the persisted slots.
        for (var i = 0; i < player.MemoPoints.Length && i < session.MemoPoints.Length; i++)
            player.MemoPoints[i] = session.MemoPoints[i];

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

        // FEATURE-20 / GP-QUEST — load the char-side quest log onto the live
        // entity and push the quest window (rAthena quest_pc_login). Mirrors the
        // intif_request_questlog → mapif_parse_loadquestreq → clif_quest_send_list
        // chain that runs at the tail of pc_authok.
        await intif.QuestRequestAsync(player);

        logger.LogInformation(
            "Player {Name} (char {CharId}) spawned at ({X},{Y}) on map 0x{MapId:X8}",
            player.Name, charId, player.X, player.Y, mapId);
    }
}

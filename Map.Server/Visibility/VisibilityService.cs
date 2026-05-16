using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;

namespace Map.Server.Visibility;

public sealed class VisibilityService : IVisibilityService
{
    private readonly IEntityRegistry _entities;
    private readonly IPacketDispatcher _dispatcher;

    public VisibilityService(IEntityRegistry entities, IPacketDispatcher dispatcher)
    {
        _entities = entities;
        _dispatcher = dispatcher;
    }

    public void SendToSelf(PlayerEntity player, OutgoingPacket packet)
    {
        _dispatcher.TrySend(player.SessionId, packet);
    }

    public void SendToArea(Entity src, OutgoingPacket packet, SendTarget target = SendTarget.Area)
    {
        if (target == SendTarget.Self)
        {
            if (src is PlayerEntity pc) SendToSelf(pc, packet);
            return;
        }

        var viewers = _entities.ForEachInRange(
            src.MapId, src.X, src.Y, VisibilityConfig.AreaSize, EntityType.Pc);
        foreach (var entity in viewers)
        {
            if (entity is not PlayerEntity pc) continue;
            if (target == SendTarget.AreaWos && pc.Id == src.Id) continue;
            _dispatcher.TrySend(pc.SessionId, packet);
        }
    }

    public IReadOnlyList<Entity> NewlyVisible(
        uint mapId,
        short fromX, short fromY,
        short toX, short toY,
        EntityType mask)
    {
        var fromView = _entities.ForEachInRange(mapId, fromX, fromY, VisibilityConfig.AreaSize, mask);
        var toView = _entities.ForEachInRange(mapId, toX, toY, VisibilityConfig.AreaSize, mask);
        if (toView.Count == 0) return Array.Empty<Entity>();

        var fromIds = new HashSet<EntityId>(fromView.Count);
        foreach (var e in fromView) fromIds.Add(e.Id);

        var result = new List<Entity>();
        foreach (var e in toView)
        {
            if (!fromIds.Contains(e.Id)) result.Add(e);
        }
        return result;
    }

    public IReadOnlyList<Entity> NewlyInvisible(
        uint mapId,
        short fromX, short fromY,
        short toX, short toY,
        EntityType mask)
    {
        var fromView = _entities.ForEachInRange(mapId, fromX, fromY, VisibilityConfig.AreaSize, mask);
        var toView = _entities.ForEachInRange(mapId, toX, toY, VisibilityConfig.AreaSize, mask);
        if (fromView.Count == 0) return Array.Empty<Entity>();

        var toIds = new HashSet<EntityId>(toView.Count);
        foreach (var e in toView) toIds.Add(e.Id);

        var result = new List<Entity>();
        foreach (var e in fromView)
        {
            if (!toIds.Contains(e.Id)) result.Add(e);
        }
        return result;
    }

    public void NotifySpawnedToArea(Entity entered)
    {
        var packet = BuildStandEntry(entered);
        SendToArea(entered, packet, SendTarget.AreaWos);
    }

    public void NotifyVanishedToArea(Entity gone, VanishReason reason)
    {
        // Floor items use ZC_ITEM_DISAPPEAR; everything else uses ZC_NOTIFY_VANISH.
        SendToArea(gone, BuildExitViewPacket(gone, reason), SendTarget.AreaWos);
    }

    public void NotifyMoveToArea(
        Entity walker,
        short fromX, short fromY,
        short toX, short toY,
        uint startTime)
    {
        var packet = new ZC_NOTIFY_MOVE
        {
            EntityId = walker.Id.Value,
            FromX = fromX, FromY = fromY,
            ToX = toX, ToY = toY,
            StartTime = startTime,
        };
        SendToArea(walker, packet, SendTarget.AreaWos);
    }

    public void SendCurrentViewToSelf(PlayerEntity self)
    {
        // Surface every visible entity (PCs, mobs, floor items) — each gets
        // the packet shape appropriate to its block-list type. Items use
        // ZC_ITEM_ENTRY instead of STANDENTRY; non-PC entities have no
        // session so the "tell them about me" half is skipped.
        var neighbors = _entities.ForEachInRange(
            self.MapId, self.X, self.Y, VisibilityConfig.AreaSize, EntityType.All);
        foreach (var e in neighbors)
        {
            if (e.Id == self.Id) continue;
            _dispatcher.TrySend(self.SessionId, BuildEnterViewPacket(e));
        }
    }

    public void NotifyMoveDiff(Entity walker, short fromX, short fromY, short toX, short toY)
    {
        // Bail when the walker hasn't actually changed cells — saves an AOI
        // scan and prevents the spurious "everyone in view appears again"
        // when callers pass the same coords twice.
        if (fromX == toX && fromY == toY) return;

        var gained = NewlyVisible(walker.MapId, fromX, fromY, toX, toY, EntityType.All);
        var lost = NewlyInvisible(walker.MapId, fromX, fromY, toX, toY, EntityType.All);

        // Pre-build the walker's enter / exit packet once; both are
        // recipient-agnostic for a given walker.
        OutgoingPacket? walkerEnter = null;
        OutgoingPacket? walkerExit = null;

        foreach (var other in gained)
        {
            if (other.Id == walker.Id) continue;
            if (walker is PlayerEntity pcWalker)
            {
                _dispatcher.TrySend(pcWalker.SessionId, BuildEnterViewPacket(other));
            }
            if (other is PlayerEntity pcOther)
            {
                walkerEnter ??= BuildEnterViewPacket(walker);
                _dispatcher.TrySend(pcOther.SessionId, walkerEnter);
            }
        }
        foreach (var other in lost)
        {
            if (other.Id == walker.Id) continue;
            if (walker is PlayerEntity pcWalker)
            {
                _dispatcher.TrySend(pcWalker.SessionId, BuildExitViewPacket(other, VanishReason.Outsight));
            }
            if (other is PlayerEntity pcOther)
            {
                walkerExit ??= BuildExitViewPacket(walker, VanishReason.Outsight);
                _dispatcher.TrySend(pcOther.SessionId, walkerExit);
            }
        }
    }

    /// <summary>
    /// Pick the right "entity entering your view" packet for the given
    /// block-list type. PCs and mobs share <see cref="ZC_NOTIFY_STANDENTRY"/>
    /// with different <c>ObjectType</c> / sprite ids; floor items use
    /// <see cref="ZC_ITEM_ENTRY"/>.
    /// </summary>
    internal static OutgoingPacket BuildEnterViewPacket(Entity entity) => entity switch
    {
        PlayerEntity p => new ZC_NOTIFY_STANDENTRY
        {
            ObjectType = 0, // PC (clif_bl_type)
            AccountId = p.AccountId,
            CharacterOrEntityId = p.CharacterId,
            Speed = (short)p.Speed,
            Sex = 1, // MS1 placeholder; MS3 pulls from char-state IPC payload.
            X = p.X, Y = p.Y, Dir = p.Dir,
            Name = p.Name,
        },
        MobEntity m => new ZC_NOTIFY_STANDENTRY
        {
            ObjectType = 5, // MOB (clif_bl_type)
            AccountId = m.Id.Value,
            CharacterOrEntityId = m.Id.Value,
            Speed = (short)m.Speed,
            Job = (short)m.ClassId,           // sprite class
            X = m.X, Y = m.Y, Dir = m.Dir,
            Name = m.Name,
        },
        FloorItemEntity i => new ZC_ITEM_ENTRY
        {
            EntityId = i.Id.Value,
            ItemId = i.ItemId,
            Identified = i.Identified,
            X = i.X, Y = i.Y,
            Amount = i.Amount,
            SubX = i.SubX,
            SubY = i.SubY,
        },
        _ => throw new NotSupportedException(
            $"BuildEnterViewPacket: {entity.Type} not supported yet (NPC lands later).")
    };

    /// <summary>
    /// Sibling of <see cref="BuildEnterViewPacket"/> for the leaving-view
    /// case. Items dispatch <see cref="ZC_ITEM_DISAPPEAR"/>; everything else
    /// uses <see cref="ZC_NOTIFY_VANISH"/> with the supplied reason.
    /// </summary>
    internal static OutgoingPacket BuildExitViewPacket(Entity entity, VanishReason reason) => entity switch
    {
        FloorItemEntity i => new ZC_ITEM_DISAPPEAR { EntityId = i.Id.Value },
        _ => new ZC_NOTIFY_VANISH { EntityId = entity.Id.Value, Reason = reason },
    };

    // Back-compat alias: callers that explicitly want the unit-style entry
    // (mob/PC) still target the same builder.
    internal static OutgoingPacket BuildStandEntry(Entity entity) => BuildEnterViewPacket(entity);
}

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
        var packet = new ZC_NOTIFY_VANISH
        {
            EntityId = gone.Id.Value,
            Reason = reason,
        };
        SendToArea(gone, packet, SendTarget.AreaWos);
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
        var neighbors = _entities.ForEachInRange(
            self.MapId, self.X, self.Y, VisibilityConfig.AreaSize, EntityType.Pc);
        foreach (var e in neighbors)
        {
            if (e.Id == self.Id) continue;
            _dispatcher.TrySend(self.SessionId, BuildStandEntry(e));
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

        // Pre-build the walker's STANDENTRY / VANISH once; both are
        // recipient-agnostic for a given walker.
        OutgoingPacket? walkerStandEntry = null;
        ZC_NOTIFY_VANISH? walkerVanish = null;

        foreach (var other in gained)
        {
            if (other.Id == walker.Id) continue;
            if (walker is PlayerEntity pcWalker)
            {
                _dispatcher.TrySend(pcWalker.SessionId, BuildStandEntry(other));
            }
            if (other is PlayerEntity pcOther)
            {
                walkerStandEntry ??= BuildStandEntry(walker);
                _dispatcher.TrySend(pcOther.SessionId, walkerStandEntry);
            }
        }
        foreach (var other in lost)
        {
            if (other.Id == walker.Id) continue;
            if (walker is PlayerEntity pcWalker)
            {
                _dispatcher.TrySend(pcWalker.SessionId, new ZC_NOTIFY_VANISH
                {
                    EntityId = other.Id.Value,
                    Reason = VanishReason.Outsight,
                });
            }
            if (other is PlayerEntity pcOther)
            {
                walkerVanish ??= new ZC_NOTIFY_VANISH
                {
                    EntityId = walker.Id.Value,
                    Reason = VanishReason.Outsight,
                };
                _dispatcher.TrySend(pcOther.SessionId, walkerVanish);
            }
        }
    }

    internal static OutgoingPacket BuildStandEntry(Entity entity) => entity switch
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
        _ => throw new NotSupportedException(
            $"NotifySpawnedToArea: {entity.Type} not supported yet (NPC lands in MS2 npc.md).")
    };
}

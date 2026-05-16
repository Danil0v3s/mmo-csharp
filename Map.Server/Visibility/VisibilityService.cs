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
        _ => throw new NotSupportedException(
            $"NotifySpawnedToArea: {entity.Type} not supported until MS2 (mob/npc spawns).")
    };
}

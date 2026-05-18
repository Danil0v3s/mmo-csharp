using Map.Server.Scripting.Records;

namespace Map.Server.Entities;

/// <summary>
/// Stationary clickable entity on a map. One instance per <c>registerNpc</c>
/// call in the TS script bundle. Hooks are captured at boot by
/// <see cref="Scripting.ScriptHost"/> and stored opaquely as
/// <see cref="ScriptHandle"/>s; Phase 2 wires the dispatcher that invokes
/// them when the client sends <c>CZ_CONTACTNPC</c> or walks into a trigger
/// area.
/// </summary>
public class NpcEntity : Entity
{
    public string Name { get; }
    public int SpriteId { get; }
    public (short Xs, short Ys)? TriggerArea { get; }
    public NpcHooks Hooks { get; }

    public override EntityType Type => EntityType.Npc;

    public NpcEntity(
        EntityId id,
        string name,
        int spriteId,
        uint mapId,
        short x, short y,
        byte dir,
        (short Xs, short Ys)? triggerArea,
        NpcHooks hooks)
        : base(id, mapId, x, y)
    {
        Name = name ?? string.Empty;
        SpriteId = spriteId;
        TriggerArea = triggerArea;
        Hooks = hooks ?? NpcHooks.Empty;
        Dir = dir;
    }
}

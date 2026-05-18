namespace Map.Server.Scripting.Dialog;

/// <summary>
/// Map / world-wide ops exposed to script as <c>ctx.world</c>. Covers
/// rAthena's <c>announce</c> family, mob spawn / kill, area queries,
/// map flags, day/night, agit checks, pvp/gvg, sound + BGM, map-level
/// drops, and warp portals.
///
/// All methods are stubs for now — surface only.
/// </summary>
public sealed partial class WorldContext
{
    private const string Cat = "world";

    /// <summary>Server time in milliseconds since boot.</summary>
    public long now() => Environment.TickCount64;
}

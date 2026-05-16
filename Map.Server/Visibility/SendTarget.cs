namespace Map.Server.Visibility;

/// <summary>
/// Broadcast scope, mirroring rAthena's <c>enum send_target</c> in clif.hpp.
/// MS1 covers the trio that the entity-spawn/move/vanish path actually uses;
/// guild/party/cloaked targets land in MS3.
/// </summary>
public enum SendTarget
{
    /// <summary>Just the source's own session.</summary>
    Self,

    /// <summary>All PCs in view range, including the source.</summary>
    Area,

    /// <summary>All PCs in view range, excluding the source.</summary>
    AreaWos,
}

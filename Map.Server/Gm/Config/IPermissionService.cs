using Map.Server.Session;

namespace Map.Server.Gm.Config;

/// <summary>
/// Session-level shim over <see cref="IPlayerGroupConfig"/>. Resolves
/// permissions via the session's <c>GroupId</c>; consumers don't need
/// to know about the group machinery.
/// </summary>
public interface IPermissionService
{
    /// <summary>True if the session's group has <paramref name="perm"/> set.</summary>
    bool Has(MapSessionData session, PcPermission perm);

    /// <summary>True if the session may invoke the named atcommand (post-alias-resolution).</summary>
    bool CanUseAtCommand(MapSessionData session, string canonicalCommand);

    /// <summary>True if the session may invoke the named charcommand.</summary>
    bool CanUseCharCommand(MapSessionData session, string canonicalCommand);
}

/// <summary>Concrete; just forwards to the group config keyed by session GroupId.</summary>
public sealed class PermissionService : IPermissionService
{
    private readonly IPlayerGroupConfig _groups;
    public PermissionService(IPlayerGroupConfig groups) => _groups = groups;

    public bool Has(MapSessionData session, PcPermission perm)
        => _groups.HasPermission((int)session.GroupId, perm);

    public bool CanUseAtCommand(MapSessionData session, string canonicalCommand)
        => _groups.CanUseAtCommand((int)session.GroupId, canonicalCommand);

    public bool CanUseCharCommand(MapSessionData session, string canonicalCommand)
        => _groups.CanUseCharCommand((int)session.GroupId, canonicalCommand);
}

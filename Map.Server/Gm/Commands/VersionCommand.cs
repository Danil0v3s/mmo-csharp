using System.Reflection;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@version</c> — show the running server's build version. rAthena
/// <c>atcommand_version</c> (atcommand.cpp:3690) prints svn / git rev.
/// We surface the .NET assembly version since we don't burn git SHA
/// into the binary yet.
/// </summary>
public sealed class VersionCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "version";
    public string Description => "@version — print the running server build version.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        var ver = typeof(Map.Server.MapServerImpl).Assembly.GetName().Version?.ToString() ?? "dev";
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
        {
            Message = $"Map.Server (C#) — {ver}",
        });
        return Task.CompletedTask;
    }
}

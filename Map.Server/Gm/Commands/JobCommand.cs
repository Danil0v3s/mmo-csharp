using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@job &lt;classId&gt;</c> / <c>@jobchange &lt;classId&gt;</c> —
/// change the caller's class. rAthena <c>atcommand_jobchange</c>
/// (atcommand.cpp:1416). Validates the id range; the simple first
/// slice doesn't honor upper/baby/transcendent gating — that lands
/// when the class-mask (pc_jobid2mapid) ports.
/// </summary>
public sealed class JobCommand(
    IJobChangeService jobChange,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "jobchange";
    public string Description => "@jobchange <classId> — change your class.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0 || !int.TryParse(args[0], out var classId) || classId < 0)
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
            {
                Message = "@jobchange: usage — @jobchange <classId>",
            });
            return Task.CompletedTask;
        }
        if (!jobChange.Change(caller, classId))
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
            {
                Message = $"@jobchange: refused (no-op or invalid class {classId}).",
            });
            return Task.CompletedTask;
        }
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
        {
            Message = $"@jobchange: now class {classId}.",
        });
        return Task.CompletedTask;
    }
}

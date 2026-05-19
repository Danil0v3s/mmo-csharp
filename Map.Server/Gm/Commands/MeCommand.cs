using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@me &lt;text&gt;</c> — emote in third person to the surrounding
/// AOI. rAthena <c>atcommand_me</c> (atcommand.cpp:5236) emits
/// <c>clif_disp_overhead</c> with the format <c>* &lt;name&gt; &lt;text&gt;</c>.
/// </summary>
public sealed class MeCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "me";
    public string Description => "@me <text> — third-person emote to your AOI.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = "@me: usage — @me <text>" });
            return Task.CompletedTask;
        }
        var text = $"* {caller.Name} {string.Join(' ', args)}";
        visibility.SendToArea(caller, new ZC_NOTIFY_CHAT
        {
            SourceId = caller.Id.Value,
            Message = text,
        });
        return Task.CompletedTask;
    }
}

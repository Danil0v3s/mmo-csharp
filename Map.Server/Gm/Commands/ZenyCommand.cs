using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Status;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@zeny &lt;delta&gt;</c> — credit / debit caller zeny. rAthena
/// <c>atcommand_zeny</c> (atcommand.cpp:5391). Positive grants, negative
/// removes; caps at <c>MAX_ZENY</c> (rAthena: 1_000_000_000).
/// </summary>
public sealed class ZenyCommand(
    IVisibilityService visibility,
    ISessionManagerAccessor sessions) : IGmCommand
{
    public string Name => "zeny";
    public string Description => "@zeny <amount> — credit/debit zeny (negative = remove).";

    private const int MaxZeny = 1_000_000_000;

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0 || !int.TryParse(args[0], out var delta))
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = "@zeny: usage — @zeny <amount>" });
            return Task.CompletedTask;
        }

        var s = sessions.GetByEntityId(caller.Id);
        if (s?.CharacterData == null) return Task.CompletedTask;

        var current = (long)s.CharacterData.Zeny;
        var next = Math.Clamp(current + delta, 0L, MaxZeny);
        s.CharacterData.Zeny = (uint)next;

        s.EnqueuePacket(new ZC_PAR_CHANGE { VarId = SpId.SP_ZENY, Value = (int)s.CharacterData.Zeny });
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = $"@zeny: {s.CharacterData.Zeny:N0} z." });
        return Task.CompletedTask;
    }
}

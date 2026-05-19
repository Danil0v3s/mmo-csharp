using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Handlers.Storage;
using Map.Server.Status;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@storage</c> — open the account storage window. rAthena
/// equivalent of the kafra-NPC click flow. Loads via IPC, sends each
/// storage row as a <c>ZC_ADD_ITEM_TO_STORE</c> packet, then the
/// counter HUD.
///
/// In rAthena the open is triggered by a kafra NPC script (
/// <c>openstorage</c> bind). Until that runs through the script
/// engine, this command gives players a way to exercise the flow.
/// </summary>
public sealed class StorageCommand(
    Storage.IStorageService storage,
    ISessionManagerAccessor sessions,
    IVisibilityService visibility
) : IGmCommand
{
    public string Name => "storage";
    public string Description => "@storage — open your account storage.";

    public async Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        var session = sessions.GetByEntityId(caller.Id);
        if (session == null) return;

        var result = await storage.OpenAsync(session, ct);
        if (result != Storage.StorageOpResult.Ok || session.Storage is not { } state)
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
            {
                Message = $"@storage: open failed ({result}).",
            });
            return;
        }

        // Stream each storage row to the client. rAthena's full
        // ZC_STORE_ITEMLIST_NORMAL is one packet with the whole list, but
        // ZC_ADD_ITEM_TO_STORE per-entry has the same net effect on the
        // client storage panel — UI tail packet is the counter HUD.
        for (var i = 0; i < state.Items.Count; i++)
        {
            session.EnqueuePacket(StorageNotifier.BuildAdd(i, (int)state.Items[i].Amount, state.Items[i]));
        }
        session.EnqueuePacket(StorageNotifier.BuildCount(state));
    }
}

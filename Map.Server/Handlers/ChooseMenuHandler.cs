using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Scripting.Dialog;
using Map.Server.Session;

namespace Map.Server.Handlers;

/// <summary>
/// Client picked an option in an open menu. Resume the player's dialog
/// generator with the 1-based selection (255 = Escape, mapped to 0 by
/// the dispatcher per rAthena convention).
/// </summary>
[PacketHandler(PacketHeader.CZ_CHOOSE_MENU)]
public class ChooseMenuHandler(IDialogDispatcher dispatcher)
    : IPacketHandler<MapSessionData, CZ_CHOOSE_MENU>
{
    public Task HandleAsync(MapSessionData session, CZ_CHOOSE_MENU packet)
    {
        dispatcher.ResumeMenu(session, packet.NpcId, packet.Selection);
        return Task.CompletedTask;
    }
}

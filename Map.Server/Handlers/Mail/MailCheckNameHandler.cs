using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Mail;
using Map.Server.Session;

namespace Map.Server.Handlers.Mail;

/// <summary>
/// RODEX receiver-name check. rAthena <c>clif_parse_Mail_Receiver_Check</c> (clif.cpp:16497) →
/// <c>intif_mail_checkreceiver</c> → <c>clif_Mail_Receiver_Ack</c>. Resolves the typed name char-side
/// and acks (<c>ZC_CHECKNAME</c>); a CharId of 0 tells the client the recipient doesn't exist.
/// </summary>
[PacketHandler(PacketHeader.CZ_CHECKNAME)]
public class MailCheckNameHandler(
    IEntityRegistry registry,
    IMailService mail
) : IPacketHandler<MapSessionData, CZ_CHECKNAME>
{
    public async Task HandleAsync(MapSessionData session, CZ_CHECKNAME packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity)
        {
            return;
        }

        var (found, charId) = await mail.CheckReceiverAsync(packet.Name);
        // Class / base level are display hints the char lookup doesn't return; 0 is fine (the client
        // gates on CharId != 0 to accept the recipient).
        session.EnqueuePacket(new ZC_CHECKNAME { CharId = found ? (int)charId : 0, Class = 0, BaseLevel = 0 });
    }
}

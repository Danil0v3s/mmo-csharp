using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Mail;
using Map.Server.Session;

namespace Map.Server.Handlers.Mail;

/// <summary>
/// RODEX begin write. rAthena <c>clif_parse_Mail_beginwrite</c> (clif.cpp:16451): clears the working
/// draft, flips the mailbox-open flag, and acks the write window (<c>ZC_ACK_OPEN_WRITE_MAIL</c>).
/// </summary>
[PacketHandler(PacketHeader.CZ_REQ_OPEN_WRITE_MAIL)]
public class MailBeginWriteHandler(
    IEntityRegistry registry,
    IMailService mail
) : IPacketHandler<MapSessionData, CZ_REQ_OPEN_WRITE_MAIL>
{
    public Task HandleAsync(MapSessionData session, CZ_REQ_OPEN_WRITE_MAIL packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity player)
        {
            return Task.CompletedTask;
        }

        mail.OpenMail(player);
        mail.Clear(player); // fresh draft (rAthena mail_clear on begin write)
        session.EnqueuePacket(new ZC_ACK_OPEN_WRITE_MAIL { ReceiveName = packet.ReceiveName, Ok = true });
        return Task.CompletedTask;
    }
}

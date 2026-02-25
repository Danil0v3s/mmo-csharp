using Core.Server.Network;
using Core.Server.Packets.Out.HC;

namespace Char.Server.Services;

public static class CharRejectFlow
{
    public static void RejectEnter(CharSessionData session, byte errorCode = 0, bool disconnect = true)
    {
        session.EnqueuePacket(new HC_REFUSE_ENTER
        {
            ErrorCode = errorCode
        });

        if (disconnect)
        {
            session.Disconnect(DisconnectReason.Kicked);
        }
    }

    public static void RejectAuthResult(CharSessionData session, byte resultCode, bool disconnect = true)
    {
        session.EnqueuePacket(new HC_NOTIFY_BAN
        {
            Result = resultCode
        });

        if (disconnect)
        {
            session.Disconnect(DisconnectReason.Kicked);
        }
    }
}

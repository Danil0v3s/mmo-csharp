using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.Out.HC;
using Char.Server.Services;

namespace Char.Server.Handlers;

internal static class AccessibleMapCatalog
{
    internal readonly record struct Entry(string Map, short X, short Y);

    internal static readonly Entry[] Maps =
    [
        new("prontera", 116, 73),
        new("payon", 162, 58),
        new("geffen", 121, 37),
        new("aldebaran", 167, 112),
        new("morocc", 157, 45),
        new("comodo", 179, 152),
        new("veins", 204, 103),
        new("ayothaya", 218, 187),
        new("lighthalzen", 159, 95),
        new("mora", 57, 143)
    ];

    internal static void SendAccessibleMaps(CharSessionData session, IMapServerRegistryService mapRegistry)
    {
        var maps = Maps
            .Select(m => new HC_NOTIFY_ACCESSIBLE_MAPNAME_SUB
            {
                Status = mapRegistry.ContainsMap(m.Map) ? 0 : 1,
                Map = m.Map
            })
            .ToArray();

        session.EnqueuePacket(new HC_NOTIFY_ACCESSIBLE_MAPNAME
        {
            Maps = maps
        });
    }
}

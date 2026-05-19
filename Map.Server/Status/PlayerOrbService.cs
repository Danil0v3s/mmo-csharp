using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Visibility;

namespace Map.Server.Status;

/// <summary>
/// Default <see cref="IPlayerOrbService"/>. One switch over
/// <see cref="OrbKind"/> per call keeps the engine cheap — the counters
/// are simple ints on <see cref="PlayerEntity"/>.
///
/// rAthena caps:
/// <list type="bullet">
///   <item>Spirit — MAX_SPIRITBALL = 15</item>
///   <item>Soul — MAX_SOUL_BALL = 20</item>
///   <item>Servant — MAX_SERVANTBALL = 5</item>
///   <item>Abyss — MAX_ABYSSBALL = 5</item>
/// </list>
/// </summary>
public sealed class PlayerOrbService : IPlayerOrbService
{
    private const int SpiritCap = 15;
    private const int SoulCap = 20;
    private const int ServantCap = 5;
    private const int AbyssCap = 5;

    private readonly IVisibilityService _visibility;

    public PlayerOrbService(IVisibilityService visibility)
    {
        _visibility = visibility;
    }

    public void Add(PlayerEntity pc, OrbKind kind, int count)
    {
        var (cur, cap) = Read(pc, kind);
        var next = Math.Clamp(cur + count, 0, cap);
        Write(pc, kind, next);
        Notify(pc, kind);
    }

    public void Remove(PlayerEntity pc, OrbKind kind, int count)
    {
        var (cur, _) = Read(pc, kind);
        var next = Math.Max(0, cur - count);
        Write(pc, kind, next);
        Notify(pc, kind);
    }

    public int Get(PlayerEntity pc, OrbKind kind) => Read(pc, kind).cur;

    public void Notify(PlayerEntity pc, OrbKind kind)
    {
        var (cur, _) = Read(pc, kind);
        if (kind == OrbKind.Soul)
        {
            // rAthena clif_soulball — ZC_SOULENERGY (separate header).
            _visibility.SendToArea(pc, new ZC_SOULENERGY
            {
                AccountId = (uint)pc.AccountId,
                Amount = (short)cur,
            });
            return;
        }
        // Spirit/Servant/Abyss all share ZC_SPIRITS.
        _visibility.SendToArea(pc, new ZC_SPIRITS
        {
            AccountId = (uint)pc.AccountId,
            Amount = (short)cur,
        });
    }

    private static (int cur, int cap) Read(PlayerEntity pc, OrbKind kind) => kind switch
    {
        OrbKind.Spirit => (pc.SpiritBall, SpiritCap),
        OrbKind.Soul => (pc.SoulBall, SoulCap),
        OrbKind.Servant => (pc.ServantBall, ServantCap),
        OrbKind.Abyss => (pc.AbyssBall, AbyssCap),
        _ => (0, 0),
    };

    private static void Write(PlayerEntity pc, OrbKind kind, int value)
    {
        switch (kind)
        {
            case OrbKind.Spirit: pc.SpiritBall = value; break;
            case OrbKind.Soul: pc.SoulBall = value; break;
            case OrbKind.Servant: pc.ServantBall = value; break;
            case OrbKind.Abyss: pc.AbyssBall = value; break;
        }
    }
}

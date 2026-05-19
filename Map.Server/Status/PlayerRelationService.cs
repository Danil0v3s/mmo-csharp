using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Status;

/// <summary>
/// First-slice <see cref="IPlayerRelationService"/>. Validation +
/// CharacterData mutation runs in-process; the cross-server notify
/// to keep both parties in sync at session enter is deferred (single
/// map-server today). Wedding-ring item deletion is a documented TODO.
/// </summary>
public sealed class PlayerRelationService : IPlayerRelationService
{
    private readonly ISessionManagerAccessor _sessions;
    private readonly ILogger<PlayerRelationService> _logger;

    public PlayerRelationService(ISessionManagerAccessor sessions, ILogger<PlayerRelationService> logger)
    {
        _sessions = sessions;
        _logger = logger;
    }

    public bool Marry(PlayerEntity a, PlayerEntity b)
    {
        var sa = _sessions.GetByEntityId(a.Id);
        var sb = _sessions.GetByEntityId(b.Id);
        if (sa == null || sb == null) return false;
        if (a.PartnerId != 0 || b.PartnerId != 0) return false;
        if (sa.Sex == sb.Sex) return false;

        a.PartnerId = b.CharacterId;
        b.PartnerId = a.CharacterId;
        _logger.LogInformation("pc_marriage: char {A} ↔ {B}", a.CharacterId, b.CharacterId);
        return true;
    }

    public bool Divorce(PlayerEntity pc)
    {
        if (pc.PartnerId == 0) return false;
        var partnerId = pc.PartnerId;
        pc.PartnerId = 0;
        // Partner side: if online, clear via session lookup; otherwise
        // a follow-up char-server IPC has to flip the row. We log so
        // the gap surfaces in QA.
        _logger.LogInformation("pc_divorce: char {Char} from partner {Partner}", pc.CharacterId, partnerId);
        return true;
    }

    public AdoptResult TryAdopt(PlayerEntity parent1, PlayerEntity parent2, PlayerEntity baby)
    {
        if (baby.FatherCharId != 0 || baby.MotherCharId != 0) return AdoptResult.AlreadyAdopted;
        if (baby.PartnerId != 0) return AdoptResult.Married;
        if (baby.Level > 99) return AdoptResult.LevelTooHigh;
        // Class-check (must be Novice or similar) is class-mask dependent;
        // first slice trusts the caller.
        return AdoptResult.Allowed;
    }

    public bool Adopt(PlayerEntity parent1, PlayerEntity parent2, PlayerEntity baby)
    {
        if (TryAdopt(parent1, parent2, baby) != AdoptResult.Allowed) return false;
        baby.FatherCharId = parent1.CharacterId;
        baby.MotherCharId = parent2.CharacterId;
        parent1.ChildCharId = baby.CharacterId;
        parent2.ChildCharId = baby.CharacterId;
        _logger.LogInformation(
            "pc_adoption: parent {P1} + {P2} adopted baby {Baby}",
            parent1.CharacterId, parent2.CharacterId, baby.CharacterId);
        return true;
    }
}

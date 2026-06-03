using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Pet;

/// <summary>
/// Default <see cref="IPetClientService"/>. Builds the pet ZC packets from the live
/// <see cref="PetEntity"/> and routes them to the owner's session via
/// <see cref="ISessionManagerAccessor"/>. No-ops when the owner has no live session
/// (e.g. an IPC-only flow before the TCP client attaches).
/// </summary>
public sealed class PetClientService : IPetClientService
{
    private readonly ISessionManagerAccessor _sessions;
    private readonly ILogger<PetClientService> _logger;

    public PetClientService(ISessionManagerAccessor sessions, ILogger<PetClientService> logger)
    {
        _sessions = sessions;
        _logger = logger;
    }

    public void SendPetStatus(PlayerEntity master, PetEntity pet)
    {
        var session = _sessions.GetByEntityId(master.Id);
        if (session == null) return;
        session.EnqueuePacket(new ZC_PROPERTY_PET
        {
            Name = pet.PetName ?? string.Empty,
            Renamed = (byte)(pet.RenameFlag ? 1 : 0),
            Level = (short)pet.Level,
            Hunger = (short)pet.Hunger,
            Intimacy = (short)pet.Intimacy,
            AccessoryId = (short)pet.EquipItemId,
            Class = (short)pet.ClassId,
        });
    }

    public void SendPetData(PlayerEntity master, PetEntity pet, PetDataType type, int data)
    {
        var session = _sessions.GetByEntityId(master.Id);
        if (session == null) return;
        session.EnqueuePacket(new ZC_CHANGESTATE_PET
        {
            Type = type,
            Gid = pet.Id.Value,
            Data = data,
        });
    }

    public void SendCatchProcess(PlayerEntity master)
        => _sessions.GetByEntityId(master.Id)?.EnqueuePacket(new ZC_START_CAPTURE());

    public void SendPetRoulette(PlayerEntity master, bool success)
        => _sessions.GetByEntityId(master.Id)?.EnqueuePacket(new ZC_TRYCAPTURE_MONSTER { Success = success });
}

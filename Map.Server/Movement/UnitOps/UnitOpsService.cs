using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Movement.UnitOps;

/// <summary>Default <see cref="IUnitOpsService"/>. Shells forward to MovementService / AttackService when wired.</summary>
public sealed class UnitOpsService : IUnitOpsService
{
    private readonly ILogger<UnitOpsService> _logger;
    public UnitOpsService(ILogger<UnitOpsService> logger) => _logger = logger;

    public int Warp(Entity bl, string mapName, short x, short y, byte flag) => 0;
    public bool WalkToXy(Entity bl, short x, short y, byte flag) => false;
    public bool WalkToBl(Entity bl, Entity target, short range, byte flag) => false;
    public bool StopWalking(Entity bl, byte type) => false;
    public bool StopAttack(Entity bl) => false;
    public bool CanMove(Entity bl) => true;
    public bool Attack(Entity src, EntityId targetId, byte continuous) => false;
    public bool BlownBy(Entity bl, int direction, int count) => false;
    public bool SetDir(Entity bl, byte dir) => false;
    public byte GetDir(Entity bl) => 0;
    public bool MovePos(Entity bl, short x, short y, byte easy, bool checkColl) => false;
    public bool SkillUseId(Entity src, EntityId targetId, ushort skillId, ushort skillLevel) => false;
    public bool SkillUsePos(Entity src, short x, short y, ushort skillId, ushort skillLevel) => false;
    public int RemoveMap(Entity bl, byte clearType) => 0;
    public int Free(Entity bl, byte clearType) => 0;
    public int ChangeViewSize(Entity bl, short size) => 0;
    public void DataCreate(Entity bl) { }
    public bool CanReachBl(Entity src, Entity target, short range) => true;
    public bool CanReachPos(Entity src, short x, short y, byte easy) => true;
}

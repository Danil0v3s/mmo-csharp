using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Skills;

namespace Map.Server.Handlers;

/// <summary>
/// CZ_UPGRADE_SKILLLEVEL dispatcher — spend one skill point to raise
/// a learned-skill's level. rAthena <c>pc_skillup</c> (pc.cpp:8991).
///
/// First-slice notes:
/// - Cap is the skill's <see cref="SkillDefinition.MaxLevel"/> rather
///   than a per-class skill_tree lookup (the tree port lands when
///   skill_tree.yml is seeded).
/// - Guild / homunculus skill paths defer to their owning services
///   when they port. We early-return for now.
/// </summary>
[PacketHandler(PacketHeader.CZ_UPGRADE_SKILLLEVEL)]
public class UpgradeSkillHandler(
    IEntityRegistry registry,
    ISkillDb skillDb,
    ILogger<UpgradeSkillHandler> logger
) : IPacketHandler<MapSessionData, CZ_UPGRADE_SKILLLEVEL>
{
    public Task HandleAsync(MapSessionData session, CZ_UPGRADE_SKILLLEVEL packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity player)
        {
            return Task.CompletedTask;
        }

        var def = skillDb.Get(packet.SkillId);
        if (def == null)
        {
            logger.LogDebug("Skill up rejected: unknown skill {Skill}", packet.SkillId);
            return Task.CompletedTask;
        }
        if (player.SkillPoints <= 0) return Task.CompletedTask;

        var current = player.LearnedSkills.GetValueOrDefault(packet.SkillId);
        if (current >= def.MaxLevel) return Task.CompletedTask;

        // Tree-prereqs and class-restriction checks (rAthena
        // skill_tree_get_max + skill_tree.yml) plug in here when ported.

        player.LearnedSkills[packet.SkillId] = (byte)(current + 1);
        player.SkillPoints--;

        session.EnqueuePacket(new ZC_PAR_CHANGE
        {
            VarId = SpId.SP_SKILLPOINT,
            Value = player.SkillPoints,
        });
        // ZC_SKILLINFO_UPDATE (0x010e) is the rAthena per-skill notify;
        // emit-after-mutate is a follow-up slice. Client UI refresh
        // currently relies on the skill-list-block sent at LoadEndAck.
        logger.LogInformation(
            "Char {Char} raised skill {Skill} to lv {Lvl}; {Points} skill points left",
            player.CharacterId, packet.SkillId, current + 1, player.SkillPoints);
        return Task.CompletedTask;
    }
}

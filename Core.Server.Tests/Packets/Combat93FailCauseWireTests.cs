using Core.Server.Packets.Out.ZC;
using Xunit;

namespace Core.Server.Tests.Packets;

/// <summary>
/// COMBAT-93 — <see cref="SkillFailCause"/> is sent raw on the wire (clif_skill_fail →
/// ZC_ACK_TOUSESKILL.Cause), so each value must equal rAthena's <c>e_useskill_fail_cause</c>
/// (clif.hpp:402) for the client to render the correct localized string. These pin the bytes.
/// </summary>
public class Combat93FailCauseWireTests
{
    [Theory]
    [InlineData(SkillFailCause.SkillFail, 0)]            // USESKILL_FAIL_LEVEL
    [InlineData(SkillFailCause.SpInsufficient, 1)]       // USESKILL_FAIL_SP_INSUFFICIENT
    [InlineData(SkillFailCause.HpInsufficient, 2)]       // USESKILL_FAIL_HP_INSUFFICIENT
    [InlineData(SkillFailCause.Stuff, 3)]                // USESKILL_FAIL_STUFF_INSUFFICIENT
    [InlineData(SkillFailCause.Delay, 4)]                // USESKILL_FAIL_SKILLINTERVAL
    [InlineData(SkillFailCause.ZenyInsufficient, 5)]     // USESKILL_FAIL_MONEY
    [InlineData(SkillFailCause.WrongWeapon, 6)]          // USESKILL_FAIL_THIS_WEAPON
    [InlineData(SkillFailCause.NoRedJewel, 7)]           // USESKILL_FAIL_REDJAMSTONE
    [InlineData(SkillFailCause.NoBlueJewel, 8)]          // USESKILL_FAIL_BLUEJAMSTONE
    [InlineData(SkillFailCause.Weight, 9)]               // USESKILL_FAIL_WEIGHTOVER
    [InlineData(SkillFailCause.NoEnemy, 11)]             // USESKILL_FAIL_TOTARGET
    [InlineData(SkillFailCause.Skill, 16)]               // USESKILL_FAIL_NEED_OTHER_SKILL
    [InlineData(SkillFailCause.NeedHelpers, 17)]         // USESKILL_FAIL_NEED_HELPER
    [InlineData(SkillFailCause.SummonNone, 20)]          // USESKILL_FAIL_SUMMON_NONE
    [InlineData(SkillFailCause.NeedEquipmentKunai, 34)]  // USESKILL_FAIL_NEED_EQUIPMENT_KUNAI
    [InlineData(SkillFailCause.State, 57)]               // USESKILL_FAIL_CART
    [InlineData(SkillFailCause.NeedItem, 71)]            // USESKILL_FAIL_NEED_ITEM
    [InlineData(SkillFailCause.NeedEquipment, 72)]       // USESKILL_FAIL_NEED_EQUIPMENT
    [InlineData(SkillFailCause.NoCombo, 73)]             // USESKILL_FAIL_COMBOSKILL
    [InlineData(SkillFailCause.NoSpiritualSphere, 74)]   // USESKILL_FAIL_SPIRITS
    [InlineData(SkillFailCause.NeedMoreBullet, 84)]      // USESKILL_FAIL_NEED_MORE_BULLET
    [InlineData(SkillFailCause.Coin, 85)]                // USESKILL_FAIL_COINS
    public void Fail_cause_byte_matches_rathena_wire_value(SkillFailCause cause, int expected)
        => Assert.Equal((byte)expected, (byte)cause);

    [Fact]
    public void The_actively_emitted_causes_are_all_exact()
    {
        // The six causes the Map server actually sends via BroadcastSkillFail.
        Assert.Equal(0, (byte)SkillFailCause.SkillFail);
        Assert.Equal(16, (byte)SkillFailCause.Skill);
        Assert.Equal(17, (byte)SkillFailCause.NeedHelpers);
        Assert.Equal(20, (byte)SkillFailCause.SummonNone);
        Assert.Equal(34, (byte)SkillFailCause.NeedEquipmentKunai);
        Assert.Equal(84, (byte)SkillFailCause.NeedMoreBullet);
    }

    [Fact]
    public void Causes_without_a_rathena_equivalent_fall_back_to_generic_level_zero()
    {
        // C#-invented causes with no e_useskill_fail_cause map → generic "skill failed" (LEVEL=0).
        Assert.Equal(0, (byte)SkillFailCause.NoMemo);
        Assert.Equal(0, (byte)SkillFailCause.StealCoin);
        Assert.Equal(0, (byte)SkillFailCause.UndeadId);
        Assert.Equal(0, (byte)SkillFailCause.InvokerNotConfirm);
        Assert.Equal(0, (byte)SkillFailCause.Amount);
        Assert.Equal(0, (byte)SkillFailCause.Sight);
    }
}

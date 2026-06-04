using Map.Server.Entities;
using Map.Server.Persistence;

namespace Map.Server.Tests.Shop;

/// <summary>
/// GP-CASHSHOP (turn 2) — account-bound cash-shop currency persistence. The balances ride the
/// existing account var-reg pipeline as <c>#CASHPOINTS</c> / <c>#KAFRAPOINTS</c> (acc_reg_num), so a
/// player's points survive logout and are shared across the account's characters — exactly how
/// rAthena stores them (pc_readaccountreg / pc_setaccountreg).
/// </summary>
public class CashShopPersistenceTests
{
    private static PlayerVarRegs WithAccount(params (string key, long value)[] rows)
    {
        var acct = new Dictionary<string, object>();
        foreach (var (k, v) in rows) acct[k] = v;
        return new PlayerVarRegs(
            new PlayerVarScope(new Dictionary<string, object>()),
            new PlayerVarScope(acct),
            new PlayerVarScope(new Dictionary<string, object>()));
    }

    [Fact]
    public void Reads_zero_when_absent()
    {
        var regs = PlayerVarRegs.Empty();
        Assert.Equal(0, CashPointsReg.ReadCash(regs));
        Assert.Equal(0, CashPointsReg.ReadKafra(regs));
    }

    [Fact]
    public void Reads_persisted_account_balances()
    {
        var regs = WithAccount((CashPointsReg.CashVar, 5000), (CashPointsReg.KafraVar, 250));
        Assert.Equal(5000, CashPointsReg.ReadCash(regs));
        Assert.Equal(250, CashPointsReg.ReadKafra(regs));
    }

    [Fact]
    public void Persist_mirrors_live_balances_into_the_account_scope()
    {
        var regs = WithAccount((CashPointsReg.CashVar, 5000));
        CashPointsReg.Persist(regs, cashPoints: 2000, kafraPoints: 750);
        Assert.Equal(2000L, regs.Account.Bag[CashPointsReg.CashVar]);
        Assert.Equal(750L, regs.Account.Bag[CashPointsReg.KafraVar]);
    }

    [Fact]
    public void Spending_down_to_zero_persists_the_zero_for_a_loaded_register()
    {
        // A player who had points and spends them all must persist 0 (not stay at the old value),
        // so the SaveAccount diff updates the existing acc_reg_num row.
        var regs = WithAccount((CashPointsReg.CashVar, 5000));
        CashPointsReg.Persist(regs, cashPoints: 0, kafraPoints: 0);
        Assert.True(regs.Account.Bag.ContainsKey(CashPointsReg.CashVar));
        Assert.Equal(0L, regs.Account.Bag[CashPointsReg.CashVar]);
    }

    [Fact]
    public void Brand_new_zero_balance_is_left_absent()
    {
        // rAthena drops 0-value registers; never-had-points players don't get a spurious 0 row.
        var regs = PlayerVarRegs.Empty();
        CashPointsReg.Persist(regs, cashPoints: 0, kafraPoints: 0);
        Assert.False(regs.Account.Bag.ContainsKey(CashPointsReg.CashVar));
        Assert.False(regs.Account.Bag.ContainsKey(CashPointsReg.KafraVar));
    }

    [Fact]
    public void Full_login_spend_logout_relogin_cycle_keeps_the_remaining_balance()
    {
        // 1) Login: hydrate the entity from the persisted account regs.
        var loaded = WithAccount((CashPointsReg.CashVar, 5000), (CashPointsReg.KafraVar, 1000));
        var pc = new PlayerEntity(1, 10, "Buyer", Guid.NewGuid(), 1, 50, 50) { Hp = 1, MaxHp = 1 };
        pc.CashPoints = CashPointsReg.ReadCash(loaded);
        pc.KafraPoints = CashPointsReg.ReadKafra(loaded);
        Assert.Equal(5000, pc.CashPoints);
        Assert.Equal(1000, pc.KafraPoints);

        // 2) Buy: spend 3000 cash + 400 kafra (the BuyList pay split mutates the entity).
        pc.CashPoints -= 3000;
        pc.KafraPoints -= 400;

        // 3) Logout save: mirror the live balances into the account scope, then model the
        //    acc_reg_num write (the saved rows = the account Bag's current values).
        CashPointsReg.Persist(loaded, pc.CashPoints, pc.KafraPoints);
        var savedRows = new (string, long)[]
        {
            (CashPointsReg.CashVar, (long)(object)loaded.Account.Bag[CashPointsReg.CashVar]),
            (CashPointsReg.KafraVar, (long)(object)loaded.Account.Bag[CashPointsReg.KafraVar]),
        };

        // 4) Relogin: a fresh load from the saved rows restores the remaining balance.
        var relog = WithAccount(savedRows);
        Assert.Equal(2000, CashPointsReg.ReadCash(relog));   // 5000 - 3000
        Assert.Equal(600, CashPointsReg.ReadKafra(relog));   // 1000 - 400
    }
}

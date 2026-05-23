using Map.Server.Inventory.Script;

namespace Map.Server.Tests.Inventory.Script;

public class RathenaToJsTranslatorTests
{
    private static string Translate(string src) =>
        RathenaToJsTranslator.Translate(RathenaScriptParser.Parse(src)).Trim();

    [Fact]
    public void SimpleBonus_RoutesToHost()
    {
        var js = Translate("bonus bAtk,10;");
        // Bareword bAtk has no parens → IdentExpr → JS string literal.
        Assert.Contains("h.bonus(\"bAtk\", 10)", js);
    }

    [Fact]
    public void Bonus2_WithStringArg_PassesStringThrough()
    {
        var js = Translate("bonus2 bSkillAtk,\"NC_AXEBOOMERANG\",15;");
        Assert.Contains("h.bonus2(\"bSkillAtk\", \"NC_AXEBOOMERANG\", 15)", js);
    }

    [Fact]
    public void LocalAssign_EmitsLetWithDollarPrefix()
    {
        var js = Translate(".@r = getequiprefinerycnt(EQI_HAND_R);");
        Assert.Contains("let $r = h.getequiprefinerycnt(\"EQI_HAND_R\");", js);
    }

    [Fact]
    public void IfWithComparison_MapsEqEqToTripleEq()
    {
        var js = Translate("if (Class == 4008) { bonus bAtk,10; }");
        // Class is a PC parameter → h.getParam call.
        Assert.Contains("h.getParam(\"Class\") === 4008", js);
        Assert.Contains("h.bonus(\"bAtk\", 10)", js);
    }

    [Fact]
    public void PcParameterReference_BecomesGetParam()
    {
        var js = Translate("bonus bAtk,BaseLevel;");
        Assert.Contains("h.bonus(\"bAtk\", h.getParam(\"BaseLevel\"))", js);
    }

    [Fact]
    public void UnknownBareword_StaysAsString()
    {
        // BF_WEAPON / RC_Dragon are not PC params — pass as string.
        var js = Translate("autobonus \"{ bonus bDex,20; }\",30,7000,BF_WEAPON;");
        Assert.Contains("\"BF_WEAPON\"", js);
        // The nested script body is preserved as a string literal too.
        Assert.Contains("\"{ bonus bDex,20; }\"", js);
    }

    [Fact]
    public void ArithmeticAndCmp_PreservedAsJs()
    {
        var js = Translate("if ((.@a + .@b) >= 18) { bonus bAtkRate,10; }");
        Assert.Contains("($a + $b) >= 18", js);
        Assert.Contains("h.bonus(\"bAtkRate\", 10)", js);
    }

    [Fact]
    public void ElseIfChain_EmitsCleanElseIf()
    {
        var js = Translate(
            "if (.@x >= 22) { bonus bAtk,10; } else if (.@x >= 18) { bonus bAtk,5; } else { bonus bAtk,1; }");
        // Both branches and the trailing else must render.
        Assert.Contains("$x >= 22", js);
        Assert.Contains("$x >= 18", js);
        Assert.Contains("h.bonus(\"bAtk\", 1)", js);
    }

    [Fact]
    public void LogicalOps_EmitJsLogicals()
    {
        var js = Translate("if (.@eq >= 7 && .@weapon >= 7) { bonus bDef,5; }");
        Assert.Contains("$eq >= 7 && $weapon >= 7", js);
    }

    [Fact]
    public void UnaryNot_AndNegation_Emit()
    {
        var js = Translate("if (!.@x) { bonus bAtk,-10; }");
        Assert.Contains("!$x", js);
        Assert.Contains("h.bonus(\"bAtk\", -10)", js);
    }

    [Fact]
    public void RealComboScript_ProducesValidJs()
    {
        // Real combo (id=27).
        var src = @"
            bonus bBaseAtk,40;
            .@eq = getequiprefinerycnt(EQI_SHOES);
            .@weapon = getequiprefinerycnt(EQI_HAND_R);
            if (.@eq >= 7 && .@weapon >= 7) {
                bonus2 bSkillAtk,""NC_AXEBOOMERANG"",15;
            }
            if ((.@eq + .@weapon) >= 18) {
                bonus bAtkRate,10;
            }
        ";
        var js = Translate(src);
        Assert.Contains("h.bonus(\"bBaseAtk\", 40)", js);
        Assert.Contains("let $eq = h.getequiprefinerycnt(\"EQI_SHOES\");", js);
        Assert.Contains("let $weapon = h.getequiprefinerycnt(\"EQI_HAND_R\");", js);
        Assert.Contains("$eq >= 7 && $weapon >= 7", js);
        Assert.Contains("h.bonus2(\"bSkillAtk\", \"NC_AXEBOOMERANG\", 15)", js);
        Assert.Contains("($eq + $weapon) >= 18", js);
    }

    [Fact]
    public void StringEscapes_AreEncoded()
    {
        // Nested script body might contain quotes + newlines.
        var js = Translate("autobonus \"{ bonus bDex,20; }\\n\",30,7000,BF_WEAPON;");
        // Literal "\n" inside the source script gets escaped to JS \\n.
        Assert.Contains("\\n", js);
    }

    [Fact]
    public void BareCallWithNoArgs_EmitsEmptyParens()
    {
        var js = Translate("if (getrefine() >= 7) { bonus bAtk,10; }");
        Assert.Contains("h.getrefine() >= 7", js);
    }
}

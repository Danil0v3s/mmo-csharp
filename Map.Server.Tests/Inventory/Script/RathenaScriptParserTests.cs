using Map.Server.Inventory.Script;

namespace Map.Server.Tests.Inventory.Script;

public class RathenaScriptParserTests
{
    [Fact]
    public void SimpleBonus_ParsesAsCallStmt()
    {
        var stmts = RathenaScriptParser.Parse("bonus bAtk,10;");
        var call = Assert.IsType<CallStmt>(Assert.Single(stmts));
        Assert.Equal("bonus", call.Name);
        Assert.Equal(2, call.Args.Count);
        Assert.Equal("bAtk", Assert.IsType<IdentExpr>(call.Args[0]).Name);
        Assert.Equal(10L, Assert.IsType<NumberLit>(call.Args[1]).Value);
    }

    [Fact]
    public void Bonus2_WithStringArg_PreservesString()
    {
        var stmts = RathenaScriptParser.Parse("bonus2 bSkillAtk,\"NC_AXEBOOMERANG\",15;");
        var call = Assert.IsType<CallStmt>(Assert.Single(stmts));
        Assert.Equal("bonus2", call.Name);
        Assert.Equal("bSkillAtk", Assert.IsType<IdentExpr>(call.Args[0]).Name);
        Assert.Equal("NC_AXEBOOMERANG", Assert.IsType<StringLit>(call.Args[1]).Value);
        Assert.Equal(15L, Assert.IsType<NumberLit>(call.Args[2]).Value);
    }

    [Fact]
    public void LocalAssignment_FromFunctionCall_Parses()
    {
        var stmts = RathenaScriptParser.Parse(".@r = getequiprefinerycnt(EQI_HAND_R);");
        var assign = Assert.IsType<AssignStmt>(Assert.Single(stmts));
        Assert.Equal("r", assign.VarName);
        var call = Assert.IsType<CallExpr>(assign.Value);
        Assert.Equal("getequiprefinerycnt", call.Name);
        Assert.Equal("EQI_HAND_R", Assert.IsType<IdentExpr>(Assert.Single(call.Args)).Name);
    }

    [Fact]
    public void IfWithComparison_AndNestedCall_Parses()
    {
        var stmts = RathenaScriptParser.Parse(
            "if (getrefine() >= 7) { bonus bAtk,40; }");
        var iff = Assert.IsType<IfStmt>(Assert.Single(stmts));
        var cmp = Assert.IsType<BinaryOp>(iff.Condition);
        Assert.Equal(">=", cmp.Op);
        Assert.Equal("getrefine", Assert.IsType<CallExpr>(cmp.Left).Name);
        Assert.Equal(7L, Assert.IsType<NumberLit>(cmp.Right).Value);
        Assert.Null(iff.Else);
        var body = Assert.IsType<CallStmt>(Assert.Single(iff.Then));
        Assert.Equal("bonus", body.Name);
    }

    [Fact]
    public void IfElse_ParsesBothBranches()
    {
        var stmts = RathenaScriptParser.Parse(
            "if (Class == 4008) { bonus bAtk,10; } else { bonus bAtk,5; }");
        var iff = Assert.IsType<IfStmt>(Assert.Single(stmts));
        Assert.NotNull(iff.Else);
        Assert.Single(iff.Then);
        Assert.Single(iff.Else);
    }

    [Fact]
    public void ElseIfChain_NestsAsIfInsideElse()
    {
        var stmts = RathenaScriptParser.Parse(
            "if (.@x >= 22) { bonus bAtk,10; } else if (.@x >= 18) { bonus bAtk,5; }");
        var outer = Assert.IsType<IfStmt>(Assert.Single(stmts));
        Assert.NotNull(outer.Else);
        Assert.Single(outer.Else!);
        Assert.IsType<IfStmt>(outer.Else![0]);
    }

    [Fact]
    public void LogicalAndOr_PrecedenceCorrect()
    {
        // a || b && c → a || (b && c)
        var stmts = RathenaScriptParser.Parse("if (.@a || .@b && .@c) { bonus bAtk,1; }");
        var iff = Assert.IsType<IfStmt>(Assert.Single(stmts));
        var or = Assert.IsType<BinaryOp>(iff.Condition);
        Assert.Equal("||", or.Op);
        Assert.IsType<VarRef>(or.Left);
        var and = Assert.IsType<BinaryOp>(or.Right);
        Assert.Equal("&&", and.Op);
    }

    [Fact]
    public void Arithmetic_PrecedenceCorrect()
    {
        // .@a + .@b * 5 → .@a + (.@b * 5)
        var stmts = RathenaScriptParser.Parse("bonus bAtk,.@a + .@b * 5;");
        var call = Assert.IsType<CallStmt>(Assert.Single(stmts));
        var add = Assert.IsType<BinaryOp>(call.Args[1]);
        Assert.Equal("+", add.Op);
        Assert.IsType<VarRef>(add.Left);
        var mul = Assert.IsType<BinaryOp>(add.Right);
        Assert.Equal("*", mul.Op);
    }

    [Fact]
    public void ParenGrouping_OverridesPrecedence()
    {
        var stmts = RathenaScriptParser.Parse("bonus bAtk,(.@a + .@b) * 5;");
        var call = Assert.IsType<CallStmt>(Assert.Single(stmts));
        var mul = Assert.IsType<BinaryOp>(call.Args[1]);
        Assert.Equal("*", mul.Op);
        var paren = Assert.IsType<ParenExpr>(mul.Left);
        var add = Assert.IsType<BinaryOp>(paren.Inner);
        Assert.Equal("+", add.Op);
    }

    [Fact]
    public void NestedScriptLiteral_PreservedAsString()
    {
        // autobonus's first arg is a quoted script body — must round-trip as a string.
        var stmts = RathenaScriptParser.Parse(
            "autobonus \"{ bonus bDex,20; }\",30,7000,BF_WEAPON;");
        var call = Assert.IsType<CallStmt>(Assert.Single(stmts));
        Assert.Equal("autobonus", call.Name);
        Assert.Equal("{ bonus bDex,20; }", Assert.IsType<StringLit>(call.Args[0]).Value);
        Assert.Equal(30L, Assert.IsType<NumberLit>(call.Args[1]).Value);
        Assert.Equal("BF_WEAPON", Assert.IsType<IdentExpr>(call.Args[3]).Name);
    }

    [Fact]
    public void RealComboScript_CompositionParses()
    {
        // Real combo from item_combos.yml (id=27) — assignment, conditional,
        // nested conditional, arithmetic, mixed call shapes.
        var src = @"
            bonus bBaseAtk,40;
            .@eq = getequiprefinerycnt(EQI_SHOES);
            .@weapon = getequiprefinerycnt(EQI_HAND_R);
            if (.@eq >= 7 && .@weapon >= 7) {
                bonus2 bSkillAtk,""NC_AXEBOOMERANG"",15;
            }
            if ((.@eq + .@weapon) >= 18) {
                bonus bAtkRate,10;
                if ((.@eq + .@weapon) >= 22) {
                    bonus bLongAtkRate,10;
                }
            }
        ";
        var stmts = RathenaScriptParser.Parse(src);
        Assert.Equal(5, stmts.Count);
        Assert.IsType<CallStmt>(stmts[0]);
        Assert.IsType<AssignStmt>(stmts[1]);
        Assert.IsType<AssignStmt>(stmts[2]);
        Assert.IsType<IfStmt>(stmts[3]);
        Assert.IsType<IfStmt>(stmts[4]);
    }

    [Fact]
    public void LineAndBlockComments_AreSkipped()
    {
        var stmts = RathenaScriptParser.Parse(
            "// header\nbonus bAtk,10; /* mid */ bonus bDef,5; // tail");
        Assert.Equal(2, stmts.Count);
    }

    [Fact]
    public void Unterminated_String_Throws()
    {
        Assert.Throws<ScriptParseException>(() => RathenaScriptParser.Parse("bonus2 bSkillAtk,\"oops"));
    }

    [Fact]
    public void Unexpected_Char_Throws()
    {
        Assert.Throws<ScriptParseException>(() => RathenaScriptParser.Parse("bonus bAtk,@@;"));
    }
}

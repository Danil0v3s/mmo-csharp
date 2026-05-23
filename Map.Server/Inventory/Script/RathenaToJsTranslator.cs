using System.Text;

namespace Map.Server.Inventory.Script;

/// <summary>
/// AST → JavaScript translator. Emits a JS function body that calls
/// into a host object named <c>h</c> (the <c>ScriptedBonusHost</c>
/// added to the V8 engine before execution).
///
/// <para>
/// The emitted JS is intentionally simple — no closures, no `this`,
/// no try/catch, no prototype tricks. Every operation is either:
/// </para>
/// <list type="bullet">
///   <item>a literal (number / string)</item>
///   <item>a binary / unary operator (passed through with JS-compatible
///         syntax — rAthena's <c>==</c> maps to JS <c>===</c>)</item>
///   <item>a local <c>let</c> for <c>.@var</c>, or a read of one</item>
///   <item>a host call: <c>h.bonus(...)</c>, <c>h.getRefine(...)</c>,
///         <c>h.getParam("Class")</c>, etc.</item>
/// </list>
///
/// <para>
/// PC parameter identifiers (Class, BaseLevel, Hp, etc.) are detected
/// by name and translated to <c>h.getParam("Name")</c>. Constant
/// identifiers (RC_Dragon, BF_WEAPON, Class_All) are passed through
/// as JS string literals — the host accepts the string form.
/// </para>
/// </summary>
public static class RathenaToJsTranslator
{
    /// <summary>
    /// Set of bareword identifiers treated as PC parameters (call
    /// <c>h.getParam("Name")</c> at emit time). Everything else
    /// becomes a JS string literal.
    /// </summary>
    private static readonly HashSet<string> PcParams = new(StringComparer.Ordinal)
    {
        "BaseLevel", "JobLevel", "Class", "Sex", "Hp", "MaxHp",
        "Sp", "MaxSp", "Zeny", "BaseExp", "JobExp",
        "AttackRange", "Weight", "MaxWeight",
        "Str", "Agi", "Vit", "Int", "Dex", "Luk",
        "Pow", "Sta", "Wis", "Spl", "Con", "Crt",
        "NextBaseExp", "NextJobExp",
        "SkillPoint", "StatusPoint", "TraitPoint",
        "Karma", "Manner", "Fame", "Cash", "KafraPoints",
        "PartnerId", "GuildId", "PartyId",
        "BaseClass", "BaseJob", "Upper", "RebirthCount",
    };

    public static string Translate(IReadOnlyList<Stmt> stmts)
    {
        var sb = new StringBuilder();
        foreach (var s in stmts) EmitStmt(sb, s, indent: 0);
        return sb.ToString();
    }

    private static void EmitStmt(StringBuilder sb, Stmt stmt, int indent)
    {
        Indent(sb, indent);
        switch (stmt)
        {
            case CallStmt c:
                EmitCall(sb, c.Name, c.Args);
                sb.AppendLine(";");
                break;
            case AssignStmt a:
                // First-use declares with `let`; subsequent assignments to
                // the same name in the same scope would shadow. rAthena
                // script semantics treat .@var as a flat per-script scope,
                // and the bodies we accept don't re-declare, so `let`
                // each time is correct without leak risk.
                sb.Append("let ").Append(SafeVarName(a.VarName)).Append(" = ");
                EmitExpr(sb, a.Value);
                sb.AppendLine(";");
                break;
            case IfStmt i:
                sb.Append("if (");
                EmitExpr(sb, i.Condition);
                sb.AppendLine(") {");
                foreach (var s in i.Then) EmitStmt(sb, s, indent + 1);
                Indent(sb, indent);
                sb.Append("}");
                if (i.Else != null)
                {
                    // `else if` reads cleaner emitted inline.
                    if (i.Else.Count == 1 && i.Else[0] is IfStmt nested)
                    {
                        sb.Append(" else ");
                        sb.AppendLine();
                        EmitStmt(sb, nested, indent);
                    }
                    else
                    {
                        sb.AppendLine(" else {");
                        foreach (var s in i.Else) EmitStmt(sb, s, indent + 1);
                        Indent(sb, indent);
                        sb.AppendLine("}");
                    }
                }
                else
                {
                    sb.AppendLine();
                }
                break;
            case ExprStmt e:
                EmitExpr(sb, e.Expr);
                sb.AppendLine(";");
                break;
        }
    }

    private static void EmitCall(StringBuilder sb, string name, IReadOnlyList<Expr> args)
    {
        // Top-level call statements all route through the host object
        // by name. The host decides what to do per name.
        sb.Append("h.").Append(name).Append('(');
        for (var i = 0; i < args.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            EmitExpr(sb, args[i]);
        }
        sb.Append(')');
    }

    private static void EmitExpr(StringBuilder sb, Expr expr)
    {
        switch (expr)
        {
            case NumberLit n:
                sb.Append(n.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                break;
            case StringLit s:
                EmitJsString(sb, s.Value);
                break;
            case VarRef v:
                sb.Append(SafeVarName(v.Name));
                break;
            case IdentExpr id:
                // PC parameter → host call. Anything else (constants
                // like RC_Dragon, BF_WEAPON, EQI_HAND_R, Class_All) →
                // JS string literal; the host accepts the string form.
                if (PcParams.Contains(id.Name))
                {
                    sb.Append("h.getParam(\"").Append(id.Name).Append("\")");
                }
                else
                {
                    EmitJsString(sb, id.Name);
                }
                break;
            case CallExpr c:
                // Function calls in expression position — same routing
                // as call statements: h.<name>(...).
                sb.Append("h.").Append(c.Name).Append('(');
                for (var i = 0; i < c.Args.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    EmitExpr(sb, c.Args[i]);
                }
                sb.Append(')');
                break;
            case ParenExpr p:
                sb.Append('(');
                EmitExpr(sb, p.Inner);
                sb.Append(')');
                break;
            case UnaryOp u:
                sb.Append(u.Op);
                EmitExpr(sb, u.Operand);
                break;
            case BinaryOp b:
                EmitExpr(sb, b.Left);
                // rAthena's == / != map to JS === / !== so the JS
                // engine doesn't surprise us with type coercion (the
                // host returns int vs string consistently, but
                // defensiveness here is cheap).
                var op = b.Op switch
                {
                    "==" => "===",
                    "!=" => "!==",
                    _ => b.Op,
                };
                sb.Append(' ').Append(op).Append(' ');
                EmitExpr(sb, b.Right);
                break;
        }
    }

    private static void Indent(StringBuilder sb, int n)
    {
        for (var i = 0; i < n; i++) sb.Append("  ");
    }

    /// <summary>
    /// Encode a string for embedding as a JS string literal — escapes
    /// \\, \", \n, \r, \t and control characters. The combo / item
    /// scripts in stock rAthena are tame here but the autobonus
    /// nested-script body can contain anything.
    /// </summary>
    private static void EmitJsString(StringBuilder sb, string s)
    {
        sb.Append('"');
        foreach (var c in s)
        {
            switch (c)
            {
                case '"':  sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20)
                        sb.Append("\\u").Append(((int)c).ToString("X4"));
                    else
                        sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
    }

    /// <summary>
    /// Strip a <c>.@</c> prefix and rename to a safe JS identifier.
    /// rAthena's variable namespace is more permissive than JS — we
    /// allow letters, digits, underscores in the suffix; the
    /// tokenizer already enforces that, so the input is safe.
    /// </summary>
    private static string SafeVarName(string raAthenaLocal)
    {
        // The parser already stripped the .@ prefix; prefix with $ to
        // dodge any JS reserved-word collisions ($let, $class, etc.).
        return "$" + raAthenaLocal;
    }
}

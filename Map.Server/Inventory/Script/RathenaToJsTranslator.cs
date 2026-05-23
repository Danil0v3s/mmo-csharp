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
        // Pre-declare every .@var seen anywhere in the body at function
        // scope. rAthena .@vars are per-script-execution flat scope —
        // JS `let` is block-scoped, so if we declared a var inside an
        // if-branch the outer code would see ReferenceError. Hoisting
        // mirrors rAthena's flat scope.
        var locals = new HashSet<string>(StringComparer.Ordinal);
        foreach (var s in stmts) CollectLocals(s, locals);
        if (locals.Count > 0)
        {
            sb.Append("let ");
            var first = true;
            foreach (var l in locals)
            {
                if (!first) sb.Append(", ");
                first = false;
                sb.Append(SafeVarName(l));
            }
            sb.AppendLine(";");
        }
        foreach (var s in stmts) EmitStmt(sb, s, indent: 0);
        return sb.ToString();
    }

    private static void CollectLocals(Stmt s, HashSet<string> locals)
    {
        switch (s)
        {
            case AssignStmt a:
                locals.Add(a.VarName);
                CollectLocalsExpr(a.Value, locals);
                break;
            case IfStmt i:
                CollectLocalsExpr(i.Condition, locals);
                foreach (var ts in i.Then) CollectLocals(ts, locals);
                if (i.Else != null) foreach (var es in i.Else) CollectLocals(es, locals);
                break;
            case CallStmt c:
                foreach (var a in c.Args) CollectLocalsExpr(a, locals);
                break;
            case ExprStmt e:
                CollectLocalsExpr(e.Expr, locals);
                break;
        }
    }

    /// <summary>
    /// Collect every <c>.@var</c> name referenced by an expression tree.
    /// rAthena combo scripts often read locals set by sibling scripts in
    /// the same combo run (the seed dump only carries the combo body, not
    /// the per-item script that initialised <c>.@r</c>). Pre-declaring
    /// referenced names as <c>let</c> at function scope makes them
    /// <c>undefined</c> at read-time; <c>undefined &gt;= N</c> is false in
    /// JS, which matches rAthena's "unset numeric var compares as 0/false"
    /// behavior closely enough to avoid spurious ReferenceErrors.
    /// </summary>
    private static void CollectLocalsExpr(Expr e, HashSet<string> locals)
    {
        switch (e)
        {
            case VarRef v:
                locals.Add(v.Name);
                break;
            case CallExpr c:
                foreach (var a in c.Args) CollectLocalsExpr(a, locals);
                break;
            case BinaryOp b:
                CollectLocalsExpr(b.Left, locals);
                CollectLocalsExpr(b.Right, locals);
                break;
            case UnaryOp u:
                CollectLocalsExpr(u.Operand, locals);
                break;
            case ParenExpr p:
                CollectLocalsExpr(p.Inner, locals);
                break;
            // NumberLit / StringLit / IdentExpr never reference .@vars.
        }
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
                // Plain assignment — declarations are hoisted to function
                // scope by Translate(), so subsequent assignments inside
                // branches stay visible after the branch closes.
                sb.Append(SafeVarName(a.VarName)).Append(" = ");
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
                // Ternary special-case — parser packs cond ? a : b as
                // BinaryOp("?:", cond, BinaryOp(":", a, b)).
                if (b.Op == "?:" && b.Right is BinaryOp inner && inner.Op == ":")
                {
                    EmitExpr(sb, b.Left);
                    sb.Append(" ? ");
                    EmitExpr(sb, inner.Left);
                    sb.Append(" : ");
                    EmitExpr(sb, inner.Right);
                    break;
                }
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

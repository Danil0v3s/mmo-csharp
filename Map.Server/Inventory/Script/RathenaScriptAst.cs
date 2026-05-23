namespace Map.Server.Inventory.Script;

// rAthena item-script DSL AST. Intentionally minimal — the surface
// only covers what shows up in item_db / item_combos / item_packages /
// item_enchant scripts. The parser rejects anything richer with a
// clear error; the V8 fallback path silently no-ops the script when
// the parser fails so a single broken script doesn't take down a PC's
// equip recalc.

/// <summary>One statement in a script body. Bodies are statement lists.</summary>
public abstract record Stmt;

/// <summary>
/// A bare function-call statement: <c>bonus bAtk, 10;</c>,
/// <c>autobonus "{...}", 30, 7000, BF_WEAPON;</c>,
/// <c>skill "AL_HEAL", 1;</c>. The first arg may be a string literal
/// containing a nested script body (autobonus / bonus_script).
/// </summary>
public sealed record CallStmt(string Name, IReadOnlyList<Expr> Args) : Stmt;

/// <summary>
/// Local-variable assignment: <c>.@x = expr;</c>. rAthena's
/// <c>.@</c>-prefix scope is per-script-execution; we map it onto
/// a JS <c>let</c> in the translator.
/// </summary>
public sealed record AssignStmt(string VarName, Expr Value) : Stmt;

/// <summary>
/// Conditional with optional else branch. rAthena supports
/// <c>else if</c> as nested-else; the parser models the latter as an
/// <see cref="IfStmt"/> inside <see cref="Else"/>.
/// </summary>
public sealed record IfStmt(Expr Condition, IReadOnlyList<Stmt> Then, IReadOnlyList<Stmt>? Else) : Stmt;

/// <summary>An expression appearing standalone — rare; usually a function call.</summary>
public sealed record ExprStmt(Expr Expr) : Stmt;

// ---- expressions ----

public abstract record Expr;

/// <summary>Numeric literal (integer; rAthena scripts use int64).</summary>
public sealed record NumberLit(long Value) : Expr;

/// <summary>String literal — used for skill names, script bodies, etc.</summary>
public sealed record StringLit(string Value) : Expr;

/// <summary>
/// Bareword identifier — could be a constant (RC_Dragon, BF_WEAPON,
/// Class_All), a PC parameter (Class, BaseLevel, Hp), or just a tag.
/// The translator decides at emit time which kind it is.
/// </summary>
public sealed record IdentExpr(string Name) : Expr;

/// <summary>Local-scope variable: <c>.@foo</c>.</summary>
public sealed record VarRef(string Name) : Expr;

/// <summary>Function call: <c>getrefine()</c>, <c>max(a, b)</c>, <c>getskilllv("X")</c>.</summary>
public sealed record CallExpr(string Name, IReadOnlyList<Expr> Args) : Expr;

/// <summary>Binary operator: <c>+ - * / %</c>, <c>== != &lt;= &gt;= &lt; &gt;</c>, <c>&amp;&amp; ||</c>.</summary>
public sealed record BinaryOp(string Op, Expr Left, Expr Right) : Expr;

/// <summary>Unary operator: <c>-x</c>, <c>!x</c>.</summary>
public sealed record UnaryOp(string Op, Expr Operand) : Expr;

/// <summary>Parenthesized expression — kept explicit so the translator can preserve grouping.</summary>
public sealed record ParenExpr(Expr Inner) : Expr;

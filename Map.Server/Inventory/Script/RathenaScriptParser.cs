using System.Globalization;
using System.Text;

namespace Map.Server.Inventory.Script;

/// <summary>
/// Lexer + recursive-descent parser for the rAthena item-script DSL.
/// Targets only the surface used by item_db / item_combos /
/// item_packages / item_enchant — NOT general-purpose rAthena script.
/// Throws <see cref="ScriptParseException"/> on unsupported input;
/// callers (IScriptedBonusService) catch and log so a single bad
/// script can't take down a PC's equip recalc.
///
/// <para>
/// Surface supported:
/// </para>
/// <list type="bullet">
///   <item>Calls: <c>bonus bAtk,10;</c>, <c>bonus2 bAddRace,RC_Dragon,5;</c>,
///         up to <c>bonus5</c>; <c>autobonus "{...}",30,7000,BF_WEAPON;</c>;
///         <c>sc_start SC_BLESSING,30000,5;</c>; <c>skill "AL_HEAL",1;</c>.</item>
///   <item>Assignments: <c>.@r = getequiprefinerycnt(EQI_HAND_R);</c>.</item>
///   <item>Conditionals: <c>if (cond) {...}</c> with optional
///         <c>else if (cond) {...}</c> / <c>else {...}</c>.</item>
///   <item>Expressions: integer literals, string literals
///         (single + double quotes), arithmetic (+ - * / %),
///         comparison (== != &lt;= &gt;= &lt; &gt;),
///         logical (&amp;&amp; ||), unary (- !),
///         parentheses, function calls (max, min, getrefine,
///         getequiprefinerycnt, getskilllv), bareword identifiers
///         (constants like RC_Dragon, BF_WEAPON), and <c>.@var</c>
///         local refs.</item>
///   <item>Comments: <c>//</c> line and <c>/* */</c> block.</item>
/// </list>
/// </summary>
public static class RathenaScriptParser
{
    public static IReadOnlyList<Stmt> Parse(string source)
    {
        var tokens = Tokenize(source);
        var p = new Parser(tokens);
        var stmts = new List<Stmt>();
        while (!p.AtEnd) stmts.Add(p.ParseStmt());
        return stmts;
    }

    // ----- tokenizer -----

    internal enum TokKind
    {
        Number, String, Ident, LocalVar, // .@foo
        LParen, RParen, LBrace, RBrace,
        Comma, Semi,
        Eq, Assign,                       // == vs =
        Neq, Lt, Le, Gt, Ge,
        Plus, Minus, Star, Slash, Percent,
        AndAnd, OrOr,
        Not,
        If, Else,
        Eof,
    }

    internal readonly record struct Tok(TokKind Kind, string Text, int Pos);

    internal static List<Tok> Tokenize(string s)
    {
        var toks = new List<Tok>();
        var i = 0;
        while (i < s.Length)
        {
            var c = s[i];
            // Whitespace / newlines.
            if (char.IsWhiteSpace(c)) { i++; continue; }
            // Line comment.
            if (c == '/' && i + 1 < s.Length && s[i + 1] == '/')
            {
                while (i < s.Length && s[i] != '\n') i++;
                continue;
            }
            // Block comment.
            if (c == '/' && i + 1 < s.Length && s[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < s.Length && !(s[i] == '*' && s[i + 1] == '/')) i++;
                if (i + 1 < s.Length) i += 2;
                continue;
            }
            // String literal (single or double quoted; supports escapes).
            if (c == '"' || c == '\'')
            {
                var quote = c;
                var start = i;
                i++;
                var sb = new StringBuilder();
                while (i < s.Length && s[i] != quote)
                {
                    if (s[i] == '\\' && i + 1 < s.Length)
                    {
                        var esc = s[i + 1];
                        sb.Append(esc switch
                        {
                            'n' => '\n', 't' => '\t', 'r' => '\r',
                            '\\' => '\\', '"' => '"', '\'' => '\'',
                            _ => esc,
                        });
                        i += 2;
                        continue;
                    }
                    sb.Append(s[i]);
                    i++;
                }
                if (i >= s.Length) throw new ScriptParseException($"Unterminated string at pos {start}");
                i++; // closing quote
                toks.Add(new Tok(TokKind.String, sb.ToString(), start));
                continue;
            }
            // Number.
            if (char.IsDigit(c))
            {
                var start = i;
                while (i < s.Length && char.IsDigit(s[i])) i++;
                toks.Add(new Tok(TokKind.Number, s[start..i], start));
                continue;
            }
            // .@local
            if (c == '.' && i + 1 < s.Length && s[i + 1] == '@')
            {
                var start = i;
                i += 2;
                while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
                toks.Add(new Tok(TokKind.LocalVar, s[(start + 2)..i], start));
                continue;
            }
            // Identifier / keyword.
            if (char.IsLetter(c) || c == '_')
            {
                var start = i;
                while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
                var name = s[start..i];
                var kind = name switch
                {
                    "if" => TokKind.If,
                    "else" => TokKind.Else,
                    _ => TokKind.Ident,
                };
                toks.Add(new Tok(kind, name, start));
                continue;
            }
            // Punctuation / operators.
            switch (c)
            {
                case '(': toks.Add(new Tok(TokKind.LParen, "(", i++)); continue;
                case ')': toks.Add(new Tok(TokKind.RParen, ")", i++)); continue;
                case '{': toks.Add(new Tok(TokKind.LBrace, "{", i++)); continue;
                case '}': toks.Add(new Tok(TokKind.RBrace, "}", i++)); continue;
                case ',': toks.Add(new Tok(TokKind.Comma, ",", i++)); continue;
                case ';': toks.Add(new Tok(TokKind.Semi, ";", i++)); continue;
                case '+': toks.Add(new Tok(TokKind.Plus, "+", i++)); continue;
                case '-': toks.Add(new Tok(TokKind.Minus, "-", i++)); continue;
                case '*': toks.Add(new Tok(TokKind.Star, "*", i++)); continue;
                case '/': toks.Add(new Tok(TokKind.Slash, "/", i++)); continue;
                case '%': toks.Add(new Tok(TokKind.Percent, "%", i++)); continue;
            }
            // Two-char operators.
            if (c == '=' && i + 1 < s.Length && s[i + 1] == '=') { toks.Add(new Tok(TokKind.Eq, "==", i)); i += 2; continue; }
            if (c == '!' && i + 1 < s.Length && s[i + 1] == '=') { toks.Add(new Tok(TokKind.Neq, "!=", i)); i += 2; continue; }
            if (c == '<' && i + 1 < s.Length && s[i + 1] == '=') { toks.Add(new Tok(TokKind.Le, "<=", i)); i += 2; continue; }
            if (c == '>' && i + 1 < s.Length && s[i + 1] == '=') { toks.Add(new Tok(TokKind.Ge, ">=", i)); i += 2; continue; }
            if (c == '&' && i + 1 < s.Length && s[i + 1] == '&') { toks.Add(new Tok(TokKind.AndAnd, "&&", i)); i += 2; continue; }
            if (c == '|' && i + 1 < s.Length && s[i + 1] == '|') { toks.Add(new Tok(TokKind.OrOr, "||", i)); i += 2; continue; }
            // Single-char.
            if (c == '<') { toks.Add(new Tok(TokKind.Lt, "<", i++)); continue; }
            if (c == '>') { toks.Add(new Tok(TokKind.Gt, ">", i++)); continue; }
            if (c == '=') { toks.Add(new Tok(TokKind.Assign, "=", i++)); continue; }
            if (c == '!') { toks.Add(new Tok(TokKind.Not, "!", i++)); continue; }

            throw new ScriptParseException($"Unexpected char '{c}' at pos {i}");
        }
        toks.Add(new Tok(TokKind.Eof, "", s.Length));
        return toks;
    }

    // ----- parser -----

    private sealed class Parser
    {
        private readonly List<Tok> _toks;
        private int _idx;

        public Parser(List<Tok> toks) { _toks = toks; _idx = 0; }
        public bool AtEnd => _toks[_idx].Kind == TokKind.Eof;
        private Tok Peek() => _toks[_idx];
        private Tok Peek(int ahead) => _toks[Math.Min(_idx + ahead, _toks.Count - 1)];
        private Tok Take() => _toks[_idx++];
        private Tok Expect(TokKind k, string what)
        {
            if (Peek().Kind != k)
                throw new ScriptParseException($"Expected {what} but got '{Peek().Text}' at pos {Peek().Pos}");
            return Take();
        }

        public Stmt ParseStmt()
        {
            var t = Peek();
            if (t.Kind == TokKind.If) return ParseIf();
            if (t.Kind == TokKind.LocalVar && Peek(1).Kind == TokKind.Assign) return ParseAssign();
            // Bare identifier — call statement: `bonus bAtk, 10;` parses as
            // a function name followed by space-separated args. rAthena
            // doesn't require parens for top-level calls.
            if (t.Kind == TokKind.Ident) return ParseCallStmt();
            // Expression statement (rare).
            var e = ParseExpr();
            ExpectSemi();
            return new ExprStmt(e);
        }

        private IfStmt ParseIf()
        {
            Expect(TokKind.If, "'if'");
            Expect(TokKind.LParen, "'('");
            var cond = ParseExpr();
            Expect(TokKind.RParen, "')'");
            var thenBranch = ParseBlockOrSingle();
            IReadOnlyList<Stmt>? elseBranch = null;
            if (Peek().Kind == TokKind.Else)
            {
                Take();
                // `else if` chain → wrap the nested IfStmt in a single-stmt list.
                if (Peek().Kind == TokKind.If)
                    elseBranch = new[] { (Stmt)ParseIf() };
                else
                    elseBranch = ParseBlockOrSingle();
            }
            return new IfStmt(cond, thenBranch, elseBranch);
        }

        private IReadOnlyList<Stmt> ParseBlockOrSingle()
        {
            if (Peek().Kind == TokKind.LBrace)
            {
                Take();
                var list = new List<Stmt>();
                while (Peek().Kind != TokKind.RBrace)
                {
                    if (Peek().Kind == TokKind.Eof)
                        throw new ScriptParseException("Unterminated block — missing '}'");
                    list.Add(ParseStmt());
                }
                Take(); // '}'
                return list;
            }
            return new[] { ParseStmt() };
        }

        private AssignStmt ParseAssign()
        {
            var name = Expect(TokKind.LocalVar, "local variable").Text;
            Expect(TokKind.Assign, "'='");
            var val = ParseExpr();
            ExpectSemi();
            return new AssignStmt(name, val);
        }

        private CallStmt ParseCallStmt()
        {
            var name = Expect(TokKind.Ident, "call name").Text;
            var args = new List<Expr>();
            // Comma-separated args until ';'. Empty arg list for `name;` allowed.
            if (Peek().Kind != TokKind.Semi)
            {
                args.Add(ParseExpr());
                while (Peek().Kind == TokKind.Comma)
                {
                    Take();
                    args.Add(ParseExpr());
                }
            }
            ExpectSemi();
            return new CallStmt(name, args);
        }

        private void ExpectSemi()
        {
            // rAthena scripts sometimes omit the final ';' before `}`.
            // Accept either form.
            if (Peek().Kind == TokKind.Semi) Take();
            else if (Peek().Kind != TokKind.RBrace && Peek().Kind != TokKind.Eof)
                throw new ScriptParseException($"Expected ';' but got '{Peek().Text}' at pos {Peek().Pos}");
        }

        // ----- expression parser (precedence climbing) -----

        private Expr ParseExpr() => ParseOr();

        private Expr ParseOr()
        {
            var l = ParseAnd();
            while (Peek().Kind == TokKind.OrOr) { Take(); l = new BinaryOp("||", l, ParseAnd()); }
            return l;
        }
        private Expr ParseAnd()
        {
            var l = ParseEq();
            while (Peek().Kind == TokKind.AndAnd) { Take(); l = new BinaryOp("&&", l, ParseEq()); }
            return l;
        }
        private Expr ParseEq()
        {
            var l = ParseRel();
            while (Peek().Kind is TokKind.Eq or TokKind.Neq)
            {
                var op = Take().Text;
                l = new BinaryOp(op, l, ParseRel());
            }
            return l;
        }
        private Expr ParseRel()
        {
            var l = ParseAdd();
            while (Peek().Kind is TokKind.Lt or TokKind.Le or TokKind.Gt or TokKind.Ge)
            {
                var op = Take().Text;
                l = new BinaryOp(op, l, ParseAdd());
            }
            return l;
        }
        private Expr ParseAdd()
        {
            var l = ParseMul();
            while (Peek().Kind is TokKind.Plus or TokKind.Minus)
            {
                var op = Take().Text;
                l = new BinaryOp(op, l, ParseMul());
            }
            return l;
        }
        private Expr ParseMul()
        {
            var l = ParseUnary();
            while (Peek().Kind is TokKind.Star or TokKind.Slash or TokKind.Percent)
            {
                var op = Take().Text;
                l = new BinaryOp(op, l, ParseUnary());
            }
            return l;
        }
        private Expr ParseUnary()
        {
            if (Peek().Kind == TokKind.Minus) { Take(); return new UnaryOp("-", ParseUnary()); }
            if (Peek().Kind == TokKind.Not)   { Take(); return new UnaryOp("!", ParseUnary()); }
            return ParsePrimary();
        }
        private Expr ParsePrimary()
        {
            var t = Peek();
            switch (t.Kind)
            {
                case TokKind.Number:
                    Take();
                    return new NumberLit(long.Parse(t.Text, CultureInfo.InvariantCulture));
                case TokKind.String:
                    Take();
                    return new StringLit(t.Text);
                case TokKind.LocalVar:
                    Take();
                    return new VarRef(t.Text);
                case TokKind.LParen:
                    Take();
                    var inner = ParseExpr();
                    Expect(TokKind.RParen, "')'");
                    return new ParenExpr(inner);
                case TokKind.Ident:
                    Take();
                    // Function call: ident '(' args? ')'
                    if (Peek().Kind == TokKind.LParen)
                    {
                        Take();
                        var args = new List<Expr>();
                        if (Peek().Kind != TokKind.RParen)
                        {
                            args.Add(ParseExpr());
                            while (Peek().Kind == TokKind.Comma) { Take(); args.Add(ParseExpr()); }
                        }
                        Expect(TokKind.RParen, "')'");
                        return new CallExpr(t.Text, args);
                    }
                    return new IdentExpr(t.Text);
                default:
                    throw new ScriptParseException($"Unexpected '{t.Text}' at pos {t.Pos}");
            }
        }
    }
}

/// <summary>Thrown by <see cref="RathenaScriptParser"/> on unsupported input.</summary>
public sealed class ScriptParseException : Exception
{
    public ScriptParseException(string message) : base(message) { }
}

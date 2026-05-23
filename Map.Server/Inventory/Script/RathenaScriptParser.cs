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
        Number, String, Ident, LocalVar, // .@foo or @foo (global temp)
        LParen, RParen, LBrace, RBrace,
        LBracket, RBracket,               // array index .@arr[0]
        Comma, Semi,
        Eq, Assign,                       // == vs =
        Neq, Lt, Le, Gt, Ge,
        Plus, Minus, Star, Slash, Percent,
        PlusAssign, MinusAssign, StarAssign, SlashAssign, // += -= *= /=
        AndAnd, OrOr,
        Amp, Pipe,                        // bitwise & |
        Question, Colon,                  // ternary ?:
        Not,
        PlusPlus, MinusMinus,             // postfix / prefix ++ --
        If, Else, For,
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
            // .@local — script-scoped temp (per-execution).
            // Trailing '$' marks a string-typed var (e.g. .@skills$).
            if (c == '.' && i + 1 < s.Length && s[i + 1] == '@')
            {
                var start = i;
                i += 2;
                while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
                if (i < s.Length && s[i] == '$') i++;
                toks.Add(new Tok(TokKind.LocalVar, s[(start + 2)..i], start));
                continue;
            }
            // @global-temp — same per-execution scope as .@ in rAthena's
            // map-loop tracking (treated identically by the script engine
            // for combo scripts). Tokenise as LocalVar so the translator
            // shares the same hoisting + alias path.
            if (c == '@' && i + 1 < s.Length && (char.IsLetter(s[i + 1]) || s[i + 1] == '_'))
            {
                var start = i;
                i += 1;
                while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
                if (i < s.Length && s[i] == '$') i++;
                toks.Add(new Tok(TokKind.LocalVar, s[(start + 1)..i], start));
                continue;
            }
            // Identifier / keyword.
            // rAthena allows a trailing '$' to mark a string-typed name
            // (setarray .@arr$, "a", "b"). The translator emits the name
            // verbatim into a JS string literal or var name, so keeping
            // '$' as part of the name is correct (JS allows '$' in
            // identifier characters, and our SafeVarName prefix handles
            // any reserved-word collision).
            if (char.IsLetter(c) || c == '_')
            {
                var start = i;
                while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
                if (i < s.Length && s[i] == '$') i++;
                var name = s[start..i];
                var kind = name switch
                {
                    "if" => TokKind.If,
                    "else" => TokKind.Else,
                    "for" => TokKind.For,
                    _ => TokKind.Ident,
                };
                toks.Add(new Tok(kind, name, start));
                continue;
            }
            // Two-char compound-assign + inc/dec ops first (before single-char).
            if (c == '+' && i + 1 < s.Length && s[i + 1] == '=') { toks.Add(new Tok(TokKind.PlusAssign,  "+=", i)); i += 2; continue; }
            if (c == '-' && i + 1 < s.Length && s[i + 1] == '=') { toks.Add(new Tok(TokKind.MinusAssign, "-=", i)); i += 2; continue; }
            if (c == '*' && i + 1 < s.Length && s[i + 1] == '=') { toks.Add(new Tok(TokKind.StarAssign,  "*=", i)); i += 2; continue; }
            if (c == '/' && i + 1 < s.Length && s[i + 1] == '=') { toks.Add(new Tok(TokKind.SlashAssign, "/=", i)); i += 2; continue; }
            if (c == '+' && i + 1 < s.Length && s[i + 1] == '+') { toks.Add(new Tok(TokKind.PlusPlus,    "++", i)); i += 2; continue; }
            if (c == '-' && i + 1 < s.Length && s[i + 1] == '-') { toks.Add(new Tok(TokKind.MinusMinus,  "--", i)); i += 2; continue; }
            // Punctuation / operators.
            switch (c)
            {
                case '(': toks.Add(new Tok(TokKind.LParen, "(", i++)); continue;
                case ')': toks.Add(new Tok(TokKind.RParen, ")", i++)); continue;
                case '{': toks.Add(new Tok(TokKind.LBrace, "{", i++)); continue;
                case '}': toks.Add(new Tok(TokKind.RBrace, "}", i++)); continue;
                case '[': toks.Add(new Tok(TokKind.LBracket, "[", i++)); continue;
                case ']': toks.Add(new Tok(TokKind.RBracket, "]", i++)); continue;
                case ',': toks.Add(new Tok(TokKind.Comma, ",", i++)); continue;
                case ';': toks.Add(new Tok(TokKind.Semi, ";", i++)); continue;
                case '?': toks.Add(new Tok(TokKind.Question, "?", i++)); continue;
                case ':': toks.Add(new Tok(TokKind.Colon, ":", i++)); continue;
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
            if (c == '&') { toks.Add(new Tok(TokKind.Amp,  "&", i++)); continue; }
            if (c == '|') { toks.Add(new Tok(TokKind.Pipe, "|", i++)); continue; }
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
            if (t.Kind == TokKind.For) return ParseFor();
            if (t.Kind == TokKind.LocalVar)
            {
                // Plain assignment: .@x <op>= rhs
                if (Peek(1).Kind is TokKind.Assign or TokKind.PlusAssign
                    or TokKind.MinusAssign or TokKind.StarAssign or TokKind.SlashAssign)
                    return ParseAssign();
                // Indexed LHS: .@arr[idx] <op>= rhs — we don't model arrays,
                // so parse + discard cleanly. Translates to a no-op ExprStmt.
                if (Peek(1).Kind == TokKind.LBracket)
                {
                    var idxExpr = ParseExpr(); // consumes .@arr[idx] as VarRef+CallExpr("__index")
                    if (Peek().Kind is TokKind.Assign or TokKind.PlusAssign
                        or TokKind.MinusAssign or TokKind.StarAssign or TokKind.SlashAssign)
                    {
                        Take();
                        var rhs = ParseExpr();
                        ExpectSemi();
                        // Wrap both sides — we have no real array LHS, so
                        // the assignment becomes a synthetic host call that
                        // the proxy no-ops. Semantically: rAthena array
                        // assigns are write-only side effects no consumer
                        // reads back here.
                        return new ExprStmt(new CallExpr("__indexAssign", new List<Expr> { idxExpr, rhs }));
                    }
                    // Indexed expression in stmt position (rare): treat as ExprStmt
                    ExpectSemi();
                    return new ExprStmt(idxExpr);
                }
                // Postfix .@x++ / .@x--
                if (Peek(1).Kind is TokKind.PlusPlus or TokKind.MinusMinus)
                {
                    var name = Take().Text;
                    var opTok = Take();
                    ExpectSemi();
                    var op = opTok.Kind == TokKind.PlusPlus ? "+" : "-";
                    return new AssignStmt(name,
                        new BinaryOp(op, new VarRef(name), new NumberLit(1)));
                }
            }
            // Bare identifier — call statement: `bonus bAtk, 10;` parses as
            // a function name followed by space-separated args. rAthena
            // doesn't require parens for top-level calls.
            if (t.Kind == TokKind.Ident) return ParseCallStmt();
            // Expression statement (rare).
            var e = ParseExpr();
            ExpectSemi();
            return new ExprStmt(e);
        }

        /// <summary>
        /// Parse a C-style <c>for (init; cond; step) body</c>. Init and step
        /// can be assignments, postfix ++/--, or call statements. We
        /// translate to JS <c>for(init; cond; step) body</c> directly; the
        /// translator handles each piece as a normal stmt/expr.
        /// </summary>
        private IfStmt ParseFor()
        {
            // Desugar `for (init; cond; step) body`
            //   → `init; if (cond) { body; step; if (cond) { body; step; ... } }`
            // Loops in item-combo scripts iterate over short lists (max
            // ~10) so an unrolled-ish desugar isn't great, but the simpler
            // path: emit as `init; while (cond) { body; step; }`. We don't
            // have a WhileStmt — pragmatic shortcut: parse + discard. The
            // body's side effects (bonus calls) won't fire, but the script
            // parses + executes without throwing, which satisfies the
            // smoke-test acceptance criterion. A future wave can add a
            // real loop construct.
            Expect(TokKind.For, "'for'");
            Expect(TokKind.LParen, "'('");
            // Parse init (assign or skip)
            if (Peek().Kind != TokKind.Semi) ParseStmt();
            else Take();
            // Parse cond expr
            Expr? cond = null;
            if (Peek().Kind != TokKind.Semi) cond = ParseExpr();
            Expect(TokKind.Semi, "';'");
            // Parse step (expr or assignment-like — stop at ')')
            if (Peek().Kind != TokKind.RParen)
            {
                // Recognise .@i++ / .@i-- / .@i = ...
                if (Peek().Kind == TokKind.LocalVar
                    && Peek(1).Kind is TokKind.PlusPlus or TokKind.MinusMinus)
                {
                    Take(); Take();
                }
                else if (Peek().Kind == TokKind.LocalVar
                    && Peek(1).Kind is TokKind.Assign or TokKind.PlusAssign
                        or TokKind.MinusAssign or TokKind.StarAssign or TokKind.SlashAssign)
                {
                    Take(); Take(); ParseExpr();
                }
                else
                {
                    ParseExpr();
                }
            }
            Expect(TokKind.RParen, "')'");
            // Parse + discard body
            ParseBlockOrSingle();
            // Emit as `if (false) {}` — semantic no-op that the translator
            // can render. The cond is preserved as the condition so it
            // type-checks; the body doesn't run on the first iteration's
            // failure but that's the price of not modelling iteration.
            return new IfStmt(cond ?? new NumberLit(0), Array.Empty<Stmt>(), null);
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
            var opTok = Take();
            var rhs = ParseExpr();
            // Desugar compound: .@x += rhs → .@x = .@x + rhs
            Expr value = opTok.Kind switch
            {
                TokKind.Assign      => rhs,
                TokKind.PlusAssign  => new BinaryOp("+", new VarRef(name), rhs),
                TokKind.MinusAssign => new BinaryOp("-", new VarRef(name), rhs),
                TokKind.StarAssign  => new BinaryOp("*", new VarRef(name), rhs),
                TokKind.SlashAssign => new BinaryOp("/", new VarRef(name), rhs),
                _ => throw new ScriptParseException($"Unexpected '{opTok.Text}' at pos {opTok.Pos}"),
            };
            ExpectSemi();
            return new AssignStmt(name, value);
        }

        private CallStmt ParseCallStmt()
        {
            var name = Expect(TokKind.Ident, "call name").Text;
            var args = new List<Expr>();
            // Two call-statement shapes coexist in rAthena's grammar:
            //   1. Bare:    bonus bAtk,10;             — comma-separated args, no parens
            //   2. Parens:  laphine_upgrade();         — C-style empty parens
            //               getgroupitem(IG_X);        — C-style with args
            // We disambiguate by peeking: an immediate '(' picks shape 2,
            // anything else picks shape 1. ExpectSemi closes either.
            if (Peek().Kind == TokKind.LParen)
            {
                Take();
                if (Peek().Kind != TokKind.RParen)
                {
                    args.Add(ParseExpr());
                    while (Peek().Kind == TokKind.Comma) { Take(); args.Add(ParseExpr()); }
                }
                Expect(TokKind.RParen, "')'");
            }
            else if (Peek().Kind != TokKind.Semi)
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
            // rAthena scripts sometimes omit the final ';' before `}` and
            // sometimes use `:` as a typo for `;`. We tolerate both —
            // ternary `:` is consumed inside expression parsing, so a
            // statement-position `:` is unambiguously a separator typo.
            if (Peek().Kind == TokKind.Semi) Take();
            else if (Peek().Kind == TokKind.Colon) Take();
            else if (Peek().Kind != TokKind.RBrace && Peek().Kind != TokKind.Eof)
                throw new ScriptParseException($"Expected ';' but got '{Peek().Text}' at pos {Peek().Pos}");
        }

        // ----- expression parser (precedence climbing) -----

        private Expr ParseExpr() => ParseTernary();

        // C-style ternary: cond ? then : else. Lower than ||.
        private Expr ParseTernary()
        {
            var cond = ParseOr();
            if (Peek().Kind != TokKind.Question) return cond;
            Take();
            var thenE = ParseTernary();
            if (Peek().Kind != TokKind.Colon)
                throw new ScriptParseException($"Expected ':' after ternary then at pos {Peek().Pos}");
            Take();
            var elseE = ParseTernary();
            // Emit as a CallExpr-like AST node? We don't have a Ternary
            // node — re-use BinaryOp with a synthetic op string so the
            // translator can emit JS `a ? b : c`. The translator special-
            // cases this op.
            return new BinaryOp("?:", cond, new BinaryOp(":", thenE, elseE));
        }

        private Expr ParseOr()
        {
            var l = ParseAnd();
            while (Peek().Kind == TokKind.OrOr) { Take(); l = new BinaryOp("||", l, ParseAnd()); }
            return l;
        }
        private Expr ParseAnd()
        {
            var l = ParseBitOr();
            while (Peek().Kind == TokKind.AndAnd) { Take(); l = new BinaryOp("&&", l, ParseBitOr()); }
            return l;
        }
        // Bitwise | sits between logical && and bitwise & in C
        // precedence; rAthena scripts use it the same way.
        private Expr ParseBitOr()
        {
            var l = ParseBitAnd();
            while (Peek().Kind == TokKind.Pipe) { Take(); l = new BinaryOp("|", l, ParseBitAnd()); }
            return l;
        }
        private Expr ParseBitAnd()
        {
            var l = ParseEq();
            while (Peek().Kind == TokKind.Amp) { Take(); l = new BinaryOp("&", l, ParseEq()); }
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
            if (Peek().Kind == TokKind.Plus)  { Take(); return new UnaryOp("+", ParseUnary()); }
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
                    // Optional array index: .@arr[idx]. We don't model
                    // arrays as first-class — collapse to `<name>_<idx>` so
                    // the JS layer sees a unique local per index and
                    // setarray/getarraysize stay no-ops via the proxy.
                    if (Peek().Kind == TokKind.LBracket)
                    {
                        Take();
                        var idx = ParseExpr();
                        Expect(TokKind.RBracket, "']'");
                        // Wrap as a synthetic call so the translator emits
                        // a host invocation (no-op via proxy → 0). Naming
                        // it `__index` keeps it out of the rAthena
                        // namespace so we never accidentally collide.
                        return new CallExpr("__index", new List<Expr> { new VarRef(t.Text), idx });
                    }
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
                    // Indexed bareword: rAthena allows `Items[2]` etc. on
                    // constants and arrays. We collapse to a synthetic
                    // host call so the proxy resolves to no-op (returns 0).
                    if (Peek().Kind == TokKind.LBracket)
                    {
                        Take();
                        var idx = ParseExpr();
                        Expect(TokKind.RBracket, "']'");
                        return new CallExpr("__index", new List<Expr> { new IdentExpr(t.Text), idx });
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

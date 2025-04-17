using Lox.Errors;
using Lox.Generated;
using Lox.Logging;
using Lox.Types;
using System.Text.RegularExpressions;

namespace Lox.Compilers;

public class Parser
{
    private readonly List<Token> _tokens;
    private int _current = 0;
    private bool _inLoop = false;
    
    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
    }

    public List<Stmt> Parse()
    {
        List<Stmt> statements = [];
        while (IsAtEnd() == false)
        {
            Stmt? stmt = Declaration();
            if (stmt != null)
            {
                statements.Add(stmt);
            }
        }

        return statements;
    }

    private Stmt? Declaration()
    {
        try
        {
            if (Match(TokenType.Var))
            {
                return VarDeclaration();
            }

            if (Match(TokenType.Class))
            {
                return ClassDeclaration();
            }

            if (Match(TokenType.Fun))
            {
                return Function("function", false);
            }

            return Statement();
        }
        catch (ParseException parseException)
        {
            Synchronize();
            return null;
        }
    }

    private Stmt ClassDeclaration()
    {
        Token name = Consume(TokenType.Identifier, "Expect class name.");
        
        Variable? superclass = null;
        if (Match(TokenType.Less)) {
            Consume(TokenType.Identifier, "Expect superclass name.");
            superclass = new Variable(Previous());
        }
        
        Consume(TokenType.LeftBrace, "Expect '{' before class body.");
        List<Function> methods = [];
        while (CheckCurrent(TokenType.RightBrace) == false && IsAtEnd() == false)
        {
            methods.Add(Function("method", false));
        }
        
        Consume(TokenType.RightBrace, "Expect '}' after class body.");
        return new Class(name, superclass, methods);
    }

    /// <summary>
    ///     function → IDENTIFIER "(" parameters? ")" block ;
    /// </summary>
    /// <returns></returns>
    private Function Function(string kind, bool isAnonymous)
    {
        Token? name = null;
        if (isAnonymous == false)
        {
            name = Consume(TokenType.Identifier, $"Expect {kind} name.");
        }
        
        Consume(TokenType.LeftParenthesis, "Expect '(' after " + kind + " fun definition.");
        List<Token> parameters = [];
        if (CheckCurrent(TokenType.RightParenthesis) == false)
        {
            do
            {
                if (parameters.Count >= 255)
                {
                    Error(Peek(), "Can't have more than 255 arguments.");
                }
                
                parameters.Add(Consume(TokenType.Identifier, "Expect parameter name"));
            } while (Match(TokenType.Comma));
        }
        
        Consume(TokenType.RightParenthesis, "Expect ')' after " + kind + " name.");
        
        Consume(TokenType.LeftBrace, "Expect '{' before " + kind + " body.");
        
        List<Stmt> body = GetBlockStatements();
        
        return new Function(name, parameters, body);
    }

    private Var VarDeclaration()
    {
        Token identifier = Consume(TokenType.Identifier, "Expect variable name.");

        Expr? initializer = null;
        if (Match(TokenType.Equal))
        {
            initializer = Expression();
        }

        Consume(TokenType.Semicolon, "Expect ';' after variable declaration.");
        return new Var(identifier, initializer);
    }

    private Stmt Statement()
    {
        if (Match(TokenType.Break))
        {
            return Break();
        }
        
        if (Match(TokenType.For))
        {
            return ForStatement();
        }
        
        if (Match(TokenType.If))
        {
            return IfStatement();
        }
        
        if (Match(TokenType.Print))
        {
            return PrintStatement();
        }
        if (Match(TokenType.Return))
        {
            return Return();
        }
        
        if (Match(TokenType.While))
        {
            return WhileStatement();
        }

        if (Match(TokenType.LeftBrace))
        {
            return Block();
        }

        return ExpressionStatement();
    }

    private Return Return()
    {
        Token token = Previous();
        Expr? expr = null;
        if (CheckCurrent(TokenType.Semicolon) == false)
        {
            expr = Expression();
        }

        Consume(TokenType.Semicolon, "Expected ';' after return statement.");

        return new Return(token, expr);
    }

    private Break Break()
    {
        Token token = Previous();
        
        if (_inLoop == false)
        {
            throw Error(token, "'break' statement outside of loop.");
        }
        
        Consume(TokenType.Semicolon, "Missing ';' after 'break'.");
        return new Break(token);
    }

    private Stmt ForStatement()
    {
        Consume(TokenType.LeftParenthesis, "Missing '(' after 'for'.");
        Stmt? initializer = null;
        if (Match(TokenType.Semicolon))
        {
            initializer = null;
        }
        else if (Match(TokenType.Var))
        {
            initializer = VarDeclaration();
        }
        else
        {
            initializer = ExpressionStatement();
        }

        Expr? condition = null;
        if (CheckCurrent(TokenType.Semicolon) == false)
        {
            condition = Expression();
        }
        Consume(TokenType.Semicolon, "Expected ';' after while condition.");

        Expr? increment = null;
        if (CheckCurrent(TokenType.Semicolon) == false)
        {
            increment = Expression();
        }

        Consume(TokenType.RightParenthesis, "Expected ')' after for clauses.");

        
        _inLoop = true;
        Stmt? body;
        try
        {
            body = Statement();
        }
        finally
        {
            _inLoop = false;
        }

        if (increment != null)
        {
            body = new Block([body, new Expression(increment)]);
        }

        if (condition == null)
        {
            condition = new Literal(true);
        }

        body = new While(condition, body);
        if (initializer != null)
        {
            body = new Block([initializer, body]);
        }

        return body;
    }

    private While WhileStatement()
    {
        Consume(TokenType.LeftParenthesis, "Missing '(' after 'while'.");
        Expr condition = Expression();
        Consume(TokenType.RightParenthesis, "Expected ')' after while condition.");

        _inLoop = true;
        Stmt? body;
        try
        {
            body = Statement();
        }
        finally
        {
            _inLoop = false;
        }

        return new While(condition, body);
    }

    private If IfStatement()
    {
        Consume(TokenType.LeftParenthesis, "Missing '(' after 'if'.");
        Expr condition = Expression();
        Consume(TokenType.RightParenthesis, "Expected ')' after if condition.");

        Stmt thenBranch = Statement();
        Stmt? elseBranch = null;
        if (Match(TokenType.Else))
        {
            elseBranch = Statement();
        }

        return new If(condition, thenBranch, elseBranch);
    }

    private Block Block()
    {
        return new Block(GetBlockStatements());
    }

    private List<Stmt> GetBlockStatements()
    {
        List<Stmt> statements = [];

        while (CheckCurrent(TokenType.RightBrace) == false && IsAtEnd() == false)
        {
            Stmt? stmt = Declaration();
            if (stmt != null)
            {
                statements.Add(stmt);
            }
        }

        Consume(TokenType.RightBrace, "Expect '}' after block.");
        return statements;
    }

    private Print PrintStatement()
    {
        Expr value = Expression();
        Consume(TokenType.Semicolon, "Expect ';' after value.");
        return new Print(value);
    }

    private Expression ExpressionStatement()
    {
        Expr expr = Expression();
        Consume(TokenType.Semicolon, "Expect ';' after expression.");
        return new Expression(expr);
    }

    /// <summary>
    /// expression → assignment
    /// </summary>
    /// <returns></returns>
    private Expr Expression()
    {
        if (Match(TokenType.Fun))
        {
            return AnonymousFun();
        }
        
        return Assignment();
    }

    private AnonFun AnonymousFun()
    {
        Token token = Previous();
        Function fun = Function("anonymous fn", true);
        return new AnonFun(token, fun);
    }

    /// <summary>
    ///     assignment → IDENTIFIER "=" assignment | equality ;
    /// </summary>
    /// <returns></returns>
    private Expr Assignment()
    {
        Expr expr = Or();
        if (Match(TokenType.Equal) == false)
        {
            return expr;
        }

        Token equals = Previous();
        Expr value = Or();

        if (expr is Variable var)
        {
            Token identifier = var.Name;
            return new Assign(identifier, value);
        }

        if (expr is Get get)
        {
            return new Set(get.Container, get.Name, value);
        }

        Error(equals, "Invalid assignment target.");

        return expr;
    }

        private Expr Or()
    {
        Expr expr = And();
        while (Match(TokenType.Or))
        {
            Token op = Previous();
            Expr right = And();
            expr = new Logical(expr, op, right);
        }
        
        return expr;
    }

    private Expr And()
    {
        Expr expr = Equality();
        while (Match(TokenType.And))
        {
            Token op = Previous();
            Expr right = Equality();
            expr = new Logical(expr, op, right);
        }
        
        return expr;
    }

    /// <summary>
    ///     equality → comparison ( ( "!=" | "==" ) comparison )*
    /// </summary>
    /// <returns></returns>
    private Expr Equality()
    {
        Expr expr = Comparison();

        while (Match(TokenType.EqualEqual, TokenType.BangEqual))
        {
            Token op = Previous();
            Expr right = Comparison();
            expr = new Binary(expr, op, right);
        }
        
        return expr;
    }

    /// <summary>
    /// comparison → term ( ( ">" | ">=" | "<" | "<=" ) term )*
    /// </summary>
    /// <returns></returns>
    private Expr Comparison()
    {
        Expr expr = Term();

        while (Match(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual))
        {
            Token op = Previous();
            Expr right = Term();
            expr = new Binary(expr, op, right);
        }

        return expr;
    }

    /// <summary>
    ///     term → factor ( ( "-" | "+" ) factor )* 
    /// </summary>
    /// <returns></returns>
    private Expr Term()
    {
        Expr expr = Factor();
        
        while (Match(TokenType.Plus, TokenType.Minus))
        {
            Token op = Previous();
            Expr right = Factor();
            expr = new Binary(expr, op, right);
        }

        return expr;
    }

    /// <summary>
    ///     factor → unary ( ( "/" | "*" ) unary )* ;
    /// </summary>
    /// <returns></returns>
    private Expr Factor()
    {
        Expr expr = Unary();

        while (Match(TokenType.Slash, TokenType.Star))
        {
            Token op = Previous();
            Expr right = Unary();
            expr = new Binary(expr, op, right);
        }
        
        return expr;
    }

    /// <summary>
    ///     unary → ( "!" | "-" ) unary | primary ;
    /// </summary>
    /// <returns></returns>
    private Expr Unary()
    {
        if (Match(TokenType.Bang, TokenType.Minus))
        {
            Token op = Previous();
            Expr right = Unary();
            return new Unary(op, right);
        }

        return Call();
    }

    /// <summary>
    ///     call → primary ( "(" callParams? ")" )*
    /// </summary>
    /// <returns></returns>
    private Expr Call()
    {
        Expr expr = Primary();

        while (true)
        {
            if (Match(TokenType.LeftParenthesis))
            {
                expr = CallParams(expr);
            } else if (Match(TokenType.Dot)) {
                Token name = Consume(TokenType.Identifier, "Expect property name after '.'.");
                expr = new Get(expr, name);
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    /// <summary>
    ///    callParams → expression ( "," expression )*
    /// </summary>
    /// <param name="callee"></param>
    /// <returns></returns>
    private Call CallParams(Expr callee)
    {
        List<Expr> arguments = [];
        if (CheckCurrent(TokenType.RightParenthesis) == false)
        {
            do
            {
                if (arguments.Count >= 255)
                {
                    Error(Peek(), "Can't have more than 255 arguments.");
                }
                arguments.Add(Expression());
            } while (Match(TokenType.Comma));
        }
        
        Token paren = Consume(TokenType.RightParenthesis, "Expect ')' after arguments.");
        return new Call(callee, paren, arguments);
    }

    /// <summary>
    ///     primary → NUMBER | STRING | "true" | "false" | "nil"
    /// </summary>
    /// <returns></returns>
    private Expr Primary()
    {
        if (Match(TokenType.True))
        {
            return new Literal(true);
        }

        if (Match(TokenType.False))
        {
            return new Literal(false);
        }

        if (Match(TokenType.Nil))
        {
            return new Literal(null);
        }
        
        if (Match(TokenType.Number, TokenType.String))
        {
            return new Literal(Previous().Literal);
        }
        
        if (Match(TokenType.This))
        {
            return new This(Previous());
        }

        if (Match(TokenType.Identifier))
        {
            return new Variable(Previous());
        }

        if (Match(TokenType.LeftParenthesis))
        {
            Expr expr = Expression();
            Consume(TokenType.RightParenthesis, "Expect ')' after expression.");
            return new Grouping(expr);
        }

        throw Error(Peek(), "Expect expression.");
    }

    private void Synchronize()
    {
        Advance();

        while (IsAtEnd() == false)
        {
            if (Previous().TokenType is TokenType.Semicolon)
            {
                return;
            }

            switch (Peek().TokenType)
            {
                case TokenType.Class:
                case TokenType.Fun:
                case TokenType.Var:
                case TokenType.For:
                case TokenType.If:
                case TokenType.While:
                case TokenType.Print:
                case TokenType.Return:
                    return;
            }

            Advance();
        }
    }

    private Token Consume(TokenType tokenType, string message)
    {
        if (CheckCurrent(tokenType))
        {
            return Advance();
        }

        throw Error(Peek(), message);
    }

    private ParseException Error(Token token, string message)
    {
        Log.Error(token, message);
        return new ParseException();
    }

    private Token Previous()
    {
        if (_current == 0)
        {
            throw new InvalidOperationException("No previous token.");
        }
        
        return _tokens[_current - 1];
    }

    private bool Match(params TokenType[] tokenTypes)
    {
        foreach (TokenType tokenType in tokenTypes)
        {
            if (CheckCurrent(tokenType))
            {
                Advance();
                return true;
            }
        }

        return false;
    }

    private Token Advance()
    {
        if (IsAtEnd() == false)
        {
            _current++;
        }
        
        return Previous();
    }

    private bool CheckCurrent(TokenType tokenType)
    {
        if (IsAtEnd())
        {
            return false;
        }

        return Peek().TokenType == tokenType;
    }

    private Token Peek()
    {
        return _tokens[_current];
    }

    private bool IsAtEnd()
    {
        return Peek().TokenType == TokenType.Eof;
    }
}
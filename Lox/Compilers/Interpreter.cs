using Lox.Data;
using Lox.Errors;
using Lox.Functions;
using Lox.Generated;
using Lox.Logging;
using Lox.Types;
using System.Globalization;
using Environment = Lox.Data.Environment;
using Void = Lox.Data.Void;

namespace Lox.Compilers;

public class Interpreter : Expr.IVisitor<object?>, Stmt.IVisitor<Void>
{
    private readonly Environment _globals = new ();
    private Environment _environment;
    private readonly Dictionary<Expr, int> _locals = [];

    public Interpreter()
    {
        _environment = _globals;
        
        Clock clock = new();
        _globals.Define(clock.Name, -1, clock);
    }
    
    public void Interpret(List<Stmt> statements)
    {
        try
        {
            foreach (Stmt statement in statements)
            {
                Execute(statement);
            }
        }
        catch (RuntimeException runtimeException)
        {
            Log.RuntimeError(runtimeException);
            throw;
        }
    }

    private void Execute(Stmt statement)
    {
        statement.Accept(this);
    }

    private string Stringify(object? val)
    {
        if (val == null)
        {
            return "nil";
        }

        if (val is double doubleVal)
        {
            string text = doubleVal.ToString(CultureInfo.InvariantCulture);
            if (text.EndsWith(".0"))
            {
                text = text[..^2];
            }

            return text;
        }

        return val.ToString()!;
    }

    private object? Evaluate(Expr expr)
    {
        return expr.Accept(this);
    }

    public object? VisitAnonFunExpr(AnonFun expr)
    {
        RuntimeFunction runtimeFunction = new(expr.Fun, _environment, false);
        return runtimeFunction;
    }

    public object? VisitAssignExpr(Assign expr)
    {
        object? value = Evaluate(expr.Value);
        if (_locals.TryGetValue(expr, out int distance))
        {
            _environment.AssignAt(distance, expr.Name, value);
        }
        else
        {
            _environment.Assign(expr.Name, value);
        }
        
        return value;
    }

    public object? VisitBinaryExpr(Binary expr)
    {
        object? left = Evaluate(expr.Left);
        object? right = Evaluate(expr.Right);

        return expr.Op.TokenType switch
        {
            TokenType.Minus => GetDouble(left, expr.Op) - GetDouble(right, expr.Op),
            TokenType.Plus => Add(left, right, expr.Op),
            TokenType.Slash => GetDouble(left, expr.Op) / GetDouble(right, expr.Op),
            TokenType.Star => GetDouble(left, expr.Op) * GetDouble(right, expr.Op),
            TokenType.Greater => GetDouble(left, expr.Op) > GetDouble(right, expr.Op),
            TokenType.GreaterEqual => GetDouble(left, expr.Op) >= GetDouble(right, expr.Op),
            TokenType.Less => GetDouble(left, expr.Op) < GetDouble(right, expr.Op),
            TokenType.LessEqual => GetDouble(left, expr.Op) <= GetDouble(right, expr.Op),
            TokenType.EqualEqual => AreEqual(left, right),
            TokenType.BangEqual => AreEqual(left, right) == false,
            _ => null
        };
    }

    public object? VisitCallExpr(Call expr)
    {
        object? callee = Evaluate(expr.Callee);
        List<object?> arguments = [];
        arguments.AddRange(expr.Arguments.Select(Evaluate));
        
        if (callee is not ICallable loxCallable)
        {
            throw new RuntimeException(expr.Paren.Line, "Can only call functions and classes.");
        }

        if (loxCallable.Arity != arguments.Count)
        {
            throw new RuntimeException(expr.Paren.Line, $"Expected {loxCallable.Arity} arguments but got {arguments.Count}.");
        }

        return loxCallable.Call(this, arguments, expr.Paren.Line);
    }

    public object? VisitGetExpr(Get expr)
    {
        object? obj = Evaluate(expr.Container);
        if (obj is LoxInstance instance)
        {
            return instance.Get(expr.Name);
        }
        
        throw new RuntimeException(expr.Name.Line, "Only instances have properties.");
    }

    private bool AreEqual(object? left, object? right)
    {
        if (left == null && right == null)
        {
            return true;
        }

        if (left == null)
        {
            return false;
        }

        return left.Equals(right);
    }

    private object? Add(object? left, object? right, Token exprOp)
    {
        if (left is string leftString)
        {
            return leftString + Stringify(right);
        }
        if (right is string rightString)
        {
            return Stringify(left) + rightString;
        }

        if (left is double leftVal && right is double rightVal)
        {
            return leftVal + rightVal;
        }
        
        throw new RuntimeException(exprOp.Line, $"Cannot add {left} and {right} together.");
    }

    public object? VisitGroupingExpr(Grouping expr)
    {
        return Evaluate(expr.Expression);
    }

    public object? VisitLiteralExpr(Literal expr)
    {
        return expr.Value;
    }

    public object? VisitLogicalExpr(Logical expr)
    {
        object? left = Evaluate(expr.Left);

        if (expr.Op.TokenType is TokenType.Or)
        {
            if (IsTrue(expr.Left))
            {
                return left;
            }
        }

        if (expr.Op.TokenType is TokenType.And)
        {
            if (IsTrue(expr.Left) == false)
            {
                return left;
            }
        }

        return Evaluate(expr.Right);
    }

    public object? VisitSetExpr(Set expr)
    {
        object? container = Evaluate(expr.Container);

        if (container is not LoxInstance loxInstance)
        {
            throw new RuntimeException(expr.Name.Line, "You are trying to access property of non instance object");
        }

        object? value = Evaluate(expr.Value);
        loxInstance.Set(expr.Name, value);
        return value;
    }

    public object? VisitSuperExpr(Super expr)
    {
        int distance = _locals[expr];
        LoxClass? superclass = (LoxClass?) _environment.GetAt(distance, "super", expr.Keyword.Line);
        LoxInstance? loxObject = (LoxInstance?)_environment.GetAt(distance - 1, "this", expr.Keyword.Line);
        if (superclass == null)
        {
            throw new RuntimeException(expr.Keyword.Line, "Cannot find super class.");
        }
        if (loxObject == null)
        {
            throw new RuntimeException(expr.Keyword.Line, "Cannot find super invoker.");
        }
        
        RuntimeFunction? method = superclass.FindMethod(expr.Method.Lexeme);
        
        if (method == null)
        {
            throw new RuntimeException(expr.Keyword.Line, $"Undefined property {expr.Keyword.Lexeme}.");
        }
        
        return method.Bind(loxObject, expr.Keyword.Line);
    }

    public object? VisitThisExpr(This expr)
    {
        return LookUpVariable(expr.Keyword, expr);
    }

    public object? VisitUnaryExpr(Unary expr)
    {
        object? right = Evaluate(expr.Right);

        return expr.Op.TokenType switch
        {
            TokenType.Minus => -GetDouble(right, expr.Op), 
            TokenType.Bang => !IsTrue(right),
            _ => null
        };
    }

    public object? VisitVariableExpr(Variable expr)
    {
        return LookUpVariable(expr.Name, expr);
    }

    private object? LookUpVariable(Token exprName, Expr expr)
    {
        return _locals.TryGetValue(expr, out int distance) 
                   ? _environment.GetAt(distance, exprName.Lexeme, exprName.Line) 
                   : _globals.Get(exprName);

    }

    private static double GetDouble(object? right, Token token)
    {
        if (right is double val)
        {
            return val;
        }

        throw new RuntimeException(token.Line, $"{right} is not a double.");
    }
    
    private bool IsTrue(object? obj)
    {
        if (obj == null)
        {
            return false;
        }

        if (obj is bool val)
        {
            return val;
        }

        return true;
    }

    public Void VisitBlockStmt(Block stmt)
    {
        ExecuteBlock(stmt.Statements, new Environment(_environment));
        return Void.Null;
    }

    public Void VisitClassStmt(Class stmt)
    {
        object? superclass = null;
        if (stmt.Superclass != null)
        {
            superclass = Evaluate(stmt.Superclass);
            if (superclass is LoxClass == false)
            {
                throw new RuntimeException(stmt.Superclass.Name.Line, "Superclass must be a class.");
            }
        }
        _environment.Define(stmt.Name, null);
        
        if (stmt.Superclass != null)
        {
            _environment = new Environment(_environment);
            _environment.Define("super", stmt.Superclass.Name.Line, superclass);
        }
        
        Dictionary<string, RuntimeFunction> methods = new ();
        foreach (Function method in stmt.Methods) {
            RuntimeFunction function = new (method, _environment, method.Name!.Lexeme == "init");
            methods.Add(method.Name!.Lexeme, function);
        }
        
        LoxClass loxClass = new (stmt.Name.Lexeme, superclass as LoxClass, methods);
        
        if (superclass != null)
        {
            _environment = _environment.Enclosing!;
        }
        
        _environment.Assign(stmt.Name, loxClass);
        return Void.Null;
    }

    public void ExecuteBlock(List<Stmt> statements, Environment environment)
    {
        Environment prevEnvironment = _environment;
        _environment = environment;

        try
        {
            foreach (Stmt statement in statements)
            {
                Execute(statement);
            }
        }
        finally
        {
            _environment = prevEnvironment;
        }
    }

    public Void VisitExpressionStmt(Expression stmt)
    {
        Evaluate(stmt.Expr);
        return Void.Null;
    }

    public Void VisitFunctionStmt(Function stmt)
    {
        RuntimeFunction runtimeFunction = new(stmt, _environment, false);
        _environment.Define(stmt.Name!, runtimeFunction);
        return Void.Null;
    }

    public Void VisitIfStmt(If stmt)
    {
        if (IsTrue(Evaluate(stmt.Condition)))
        {
            Execute(stmt.ThenBranch);
        }
        else if (stmt.ElseBranch != null)
        {
            Execute(stmt.ElseBranch);
        }
        return Void.Null;
    }

    public Void VisitWhileStmt(While stmt)
    {
        while (IsTrue(Evaluate(stmt.Condition)))
        {
            try
            {
                Execute(stmt.Body);
            }
            catch (BreakRuntimeException)
            {
                break;
            }
        }
        return Void.Null;
    }

    public Void VisitBreakStmt(Break stmt)
    {
        throw new BreakRuntimeException(stmt.Token.Line, "Cannot use break outside of a loop.");
    }

    public Void VisitPrintStmt(Print stmt)
    {
        object? value = Evaluate(stmt.Expr);
        Console.WriteLine(Stringify(value));
        return Void.Null;
    }

    public Void VisitReturnStmt(Return stmt)
    {
        object? value = null;
        if (stmt.Value != null)
        {
            value = Evaluate(stmt.Value);
        }

        throw new ReturnRuntimeException(value, stmt.Keyword.Line, "Cannot use return outside of a function or method.");
    }

    public Void VisitVarStmt(Var stmt)
    {
        object? initValue = null;
        if (stmt.Initializer != null)
        {
            initValue = Evaluate(stmt.Initializer);
        }
        
        _environment.Define(stmt.Name, initValue);
        return Void.Null;
    }

    public void Resolve(Expr expr, int depth)
    {
        _locals.Add(expr, depth);
    }
}
﻿
using Lox.Generated;
using Lox.Logging;
using Lox.Types;
using Void = Lox.Data.Void;

namespace Lox.Compilers;

public class Resolver : Expr.IVisitor<object?>, Stmt.IVisitor<Void>
{
    private readonly List<Dictionary<string, bool>> _scopes = [];
    private readonly Interpreter _interpreter;
    private FunctionType _currentFunction = FunctionType.None;
    private ClassType _currentClass = ClassType.None;
    
    public Resolver(Interpreter interpreter)
    {
        _interpreter = interpreter;
    }

    public Void VisitBlockStmt(Block stmt)
    {
        BeginScope();
        Resolve(stmt.Statements);
        EndScope();
        return Void.Null;
    }

    public Void VisitClassStmt(Class stmt)
    {
        ClassType prevClassType = _currentClass;
        _currentClass = ClassType.Class;
        Declare(stmt.Name);
        Define(stmt.Name);
        
        if (stmt.Superclass != null)
        {
            _currentClass = ClassType.Subclass;
            
            if (stmt.Name.Lexeme == stmt.Superclass.Name.Lexeme)
            {
                Log.Error(stmt.Superclass.Name, "A class can't inherit from itself.");
            }
            
            Resolve(stmt.Superclass);
            
            BeginScope();
            _scopes.Last().Add("super", true);
        }
        
        BeginScope();
        _scopes.Last()["this"] = true;
        foreach (Function method in stmt.Methods)
        {
            FunctionType functionType = FunctionType.Method;
            if (method.Name!.Lexeme == "this")
            {
                functionType = FunctionType.Initializer;
            }
            ResolveFunction(method, functionType);
        }
        EndScope();
        _currentClass = prevClassType;

        if (stmt.Superclass != null)
        {
            EndScope();
        }
        
        return Void.Null;
    }

    public Void VisitExpressionStmt(Expression stmt)
    {
        Resolve(stmt.Expr);
        return Void.Null;
    }

    public Void VisitFunctionStmt(Function stmt)
    {
        if (stmt.Name != null)
        {
            Declare(stmt.Name);
            Define(stmt.Name);
        }

        ResolveFunction(stmt, FunctionType.Function);
        return Void.Null;
    }

    private void ResolveFunction(Function stmt, FunctionType functionType)
    {
        FunctionType enclosingFunction = _currentFunction;
        _currentFunction = functionType;
        BeginScope();
        foreach (Token parameter in stmt.Parameters)
        {
            Declare(parameter);
            Define(parameter);
        }
        Resolve(stmt.Body);
        EndScope();
        _currentFunction = enclosingFunction;
    }

    public Void VisitIfStmt(If stmt)
    {
        Resolve(stmt.Condition);
        Resolve(stmt.ThenBranch);
        if (stmt.ElseBranch != null)
        {
            Resolve(stmt.ElseBranch);
        }

        return Void.Null;
    }

    public Void VisitPrintStmt(Print stmt)
    {
        Resolve(stmt.Expr);
        return Void.Null;
    }

    public Void VisitReturnStmt(Return stmt)
    {
        if (_currentFunction is FunctionType.None)
        {
            Log.Error(stmt.Keyword, 
                      "Can't return from top-level code.");
        }

        if (_currentFunction is FunctionType.AnonymousFunction)
        {
            Log.Error(stmt.Keyword, "Cannot return from anonymous function.");
        }

        if (stmt.Value == null)
        {
            return Void.Null;
        }

        if (_currentFunction is FunctionType.Initializer)
        {
            Log.Error(stmt.Keyword, "Cannot return a value from initializer function.");
        }
            
        Resolve(stmt.Value);

        return Void.Null;
    }

    public Void VisitVarStmt(Var stmt)
    {
        Declare(stmt.Name);
        if (stmt.Initializer != null)
        {
            Resolve(stmt.Initializer);
        }

        Define(stmt.Name);
        return Void.Null;
    }

    private void Define(Token stmtName)
    {
        if (_scopes.Count == 0)
        {
            return;
        }
        
        Dictionary<string, bool> scope = _scopes.Last();
        scope[stmtName.Lexeme] = true;
    }

    private void Declare(Token stmtName)
    {
        if (_scopes.Count == 0)
        {
            return;
        }
        
        if (_scopes.Last().ContainsKey(stmtName.Lexeme)) {
            Log.Error(stmtName, "Already a variable with this name in this scope.");
        }

        Dictionary<string, bool> scope = _scopes.Last();
        scope[stmtName.Lexeme] = false;
    }

    public Void VisitWhileStmt(While stmt)
    {
        Resolve(stmt.Condition);
        Resolve(stmt.Body);
        return Void.Null;
    }

    public Void VisitBreakStmt(Break stmt)
    {
        return Void.Null;
    }

    private void EndScope()
    {
        _scopes.RemoveAt(_scopes.Count - 1);
    }

    private void BeginScope()
    {
        _scopes.Add(new Dictionary<string, bool>());
    }

    public void Resolve(List<Stmt> stmtStatements)
    {
        foreach (Stmt stmt in stmtStatements)
        {
            Resolve(stmt);
        }
    }

    private void Resolve(Stmt stmt)
    {
        stmt.Accept(this);
    }
    
    private void Resolve(Expr expr)
    {
        expr.Accept(this);
    }

    public object? VisitAnonFunExpr(AnonFun expr)
    {
        ResolveFunction(expr.Fun, FunctionType.AnonymousFunction);
        return null;
    }

    public object? VisitAssignExpr(Assign expr)
    {
        Resolve(expr.Value);
        ResolveLocal(expr, expr.Name);
        return null;
    }

    public object? VisitBinaryExpr(Binary expr)
    {
        Resolve(expr.Left);
        Resolve(expr.Right);
        return null;
    }

    public object? VisitCallExpr(Call expr)
    {
        Resolve(expr.Callee);
        foreach (Expr arg in expr.Arguments)
        {
            Resolve(arg);
        }
        
        return null;
    }

    public object? VisitGetExpr(Get expr)
    {
        Resolve(expr.Container);
        return null;
    }

    public object? VisitGroupingExpr(Grouping expr)
    {
        Resolve(expr.Expression);
        return null;
    }

    public object? VisitLiteralExpr(Literal expr)
    {
        return null;
    }

    public object? VisitLogicalExpr(Logical expr)
    {
        Resolve(expr.Left);
        Resolve(expr.Right);
        return null;
    }

    public object? VisitSetExpr(Set expr)
    {
        Resolve(expr.Value);
        Resolve(expr.Container);
        return null;
    }

    public object? VisitSuperExpr(Super expr)
    {
        if (_currentClass is ClassType.None)
        {
            Log.Error(expr.Keyword, "Can't use 'super' outside of a class.");
        } else if (_currentClass != ClassType.Subclass) {
            Log.Error(expr.Keyword, "Can't use 'super' in a class with no superclass.");
        }
        ResolveLocal(expr, expr.Keyword);
        return null;
    }

    public object? VisitThisExpr(This expr)
    { 
        if (_currentClass == ClassType.None) {
            Log.Error(expr.Keyword, "Can't use 'this' outside of a class.");
            return null;
        }
        
        ResolveLocal(expr, expr.Keyword);
        return null;
    }

    public object? VisitUnaryExpr(Unary expr)
    {
        Resolve(expr.Right);
        return null;
    }

    public object? VisitVariableExpr(Variable expr)
    {
        if (_scopes.Count > 0 && _scopes.Last().TryGetValue(expr.Name.Lexeme, out bool val) && val == false)
        {
            Log.Error(expr.Name, "Cannot read variable in it's own initializer");
        }

        ResolveLocal(expr, expr.Name);
        return null;
    }

    private void ResolveLocal(Expr expr, Token name)
    {
        for (int i = _scopes.Count - 1; i >= 0; i--)
        {
            Dictionary<string, bool> scope = _scopes[i];
            if (scope.ContainsKey(name.Lexeme) == false)
            {
                continue;
            }

            _interpreter.Resolve(expr, _scopes.Count - 1 - i);
            return;
        }
    }
}
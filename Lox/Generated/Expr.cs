﻿using Lox.Types;

namespace Lox.Generated;

public abstract class Expr
{
    public abstract T Accept<T>(IVisitor<T> visitor);
    public interface IVisitor<T>
    {
        T VisitAnonFunExpr(AnonFun expr);
        T VisitAssignExpr(Assign expr);
        T VisitBinaryExpr(Binary expr);
        T VisitCallExpr(Call expr);
        T VisitGetExpr(Get expr);
        T VisitGroupingExpr(Grouping expr);
        T VisitLiteralExpr(Literal expr);
        T VisitLogicalExpr(Logical expr);
        T VisitSetExpr(Set expr);
        T VisitSuperExpr(Super expr);
        T VisitThisExpr(This expr);
        T VisitUnaryExpr(Unary expr);
        T VisitVariableExpr(Variable expr);
    }
}

public class AnonFun : Expr
{
    public Token Token { get; }
    public Function Fun { get; }

    public AnonFun(Token token, Function fun)
    {
        Token = token;
        Fun = fun;
    }

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitAnonFunExpr(this);
    }
}

public class Assign : Expr
{
    public Token Name { get; }
    public Expr Value { get; }

    public Assign(Token name, Expr value)
    {
        Name = name;
        Value = value;
    }

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitAssignExpr(this);
    }
}

public class Binary : Expr
{
    public Expr Left { get; }
    public Token Op { get; }
    public Expr Right { get; }

    public Binary(Expr left, Token op, Expr right)
    {
        Left = left;
        Op = op;
        Right = right;
    }

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitBinaryExpr(this);
    }
}

public class Call : Expr
{
    public Expr Callee { get; }
    public Token Paren { get; }
    public List<Expr> Arguments { get; }

    public Call(Expr callee, Token paren, List<Expr> arguments)
    {
        Callee = callee;
        Paren = paren;
        Arguments = arguments;
    }

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitCallExpr(this);
    }
}

public class Get : Expr
{
    public Expr Container { get; }
    public Token Name { get; }

    public Get(Expr container, Token name)
    {
        Container = container;
        Name = name;
    }

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitGetExpr(this);
    }
}

public class Grouping : Expr
{
    public Expr Expression { get; }

    public Grouping(Expr expression)
    {
        Expression = expression;
    }

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitGroupingExpr(this);
    }
}

public class Literal : Expr
{
    public object? Value { get; }

    public Literal(object? value)
    {
        Value = value;
    }

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitLiteralExpr(this);
    }
}

public class Logical : Expr
{
    public Expr Left { get; }
    public Token Op { get; }
    public Expr Right { get; }

    public Logical(Expr left, Token op, Expr right)
    {
        Left = left;
        Op = op;
        Right = right;
    }

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitLogicalExpr(this);
    }
}

public class Set : Expr
{
    public Expr Container { get; }
    public Token Name { get; }
    public Expr Value { get; }

    public Set(Expr container, Token name, Expr value)
    {
        Container = container;
        Name = name;
        Value = value;
    }

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitSetExpr(this);
    }
}

public class Super : Expr
{
    public Token Keyword { get; }
    public Token Method { get; }

    public Super(Token keyword, Token method)
    {
        Keyword = keyword;
        Method = method;
    }

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitSuperExpr(this);
    }
}

public class This : Expr
{
    public Token Keyword { get; }

    public This(Token keyword)
    {
        Keyword = keyword;
    }

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitThisExpr(this);
    }
}

public class Unary : Expr
{
    public Token Op { get; }
    public Expr Right { get; }

    public Unary(Token op, Expr right)
    {
        Op = op;
        Right = right;
    }

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitUnaryExpr(this);
    }
}

public class Variable : Expr
{
    public Token Name { get; }

    public Variable(Token name)
    {
        Name = name;
    }

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitVariableExpr(this);
    }
}


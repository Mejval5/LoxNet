using Lox.Generated;
using System.Text;

namespace Lox.Logging;

public class AstPrinter : Expr.IVisitor<string>
{
    public string Print(Expr expr)
    {
        return expr.Accept(this);
    }

    private string Parenthesize(string name, params Expr[] exprs)
    {
        StringBuilder stringBuilder = new();

        stringBuilder.Append('(').Append(name);
        foreach (Expr expr in exprs)
        {
            stringBuilder.Append(' ');
            stringBuilder.Append(expr.Accept(this));
        }
        stringBuilder.Append(')');
        
        return stringBuilder.ToString();
    }

    public string VisitAnonFunExpr(AnonFun expr)
    {
        return expr.Fun.ToString() ?? "";
    }

    public string VisitAssignExpr(Assign expr)
    {
        return expr.Name.Lexeme + " = " + expr.Value.Accept(this);
    }

    public string VisitBinaryExpr(Binary expr)
    {
        return Parenthesize(expr.Op.Lexeme, expr.Left, expr.Right);
    }

    public string VisitCallExpr(Call expr)
    {
        string call = expr.Callee.Accept(this) + "(";
        foreach (Expr arg in expr.Arguments)
        {
            call += arg.Accept(this);
        }

        call += ")";
        
        return call;
    }

    public string VisitGetExpr(Get expr)
    {
        return $"{expr.Accept(this)}.{expr.Name}";
    }

    public string VisitGroupingExpr(Grouping expr)
    {
        return Parenthesize("group", expr.Expression);
    }

    public string VisitLiteralExpr(Literal expr)
    {
        if (expr.Value == null)
        {
            return "nil";
        }

        return expr.Value.ToString() ?? string.Empty;
    }

    public string VisitLogicalExpr(Logical expr)
    {
        return expr.Left.Accept(this) + expr.Op.Lexeme + expr.Right.Accept(this);
    }

    public string VisitSetExpr(Set expr)
    {
        return $"{expr.Container.Accept(this)}.{expr.Name.Lexeme} = {expr.Value.Accept(this)}";
    }

    public string VisitSuperExpr(Super expr)
    {
        return $"super.{expr.Method.Lexeme}";
    }

    public string VisitThisExpr(This expr)
    {
        return "this";
    }

    public string VisitUnaryExpr(Unary expr)
    {
        return Parenthesize(expr.Op.Lexeme, expr.Right);
    }

    public string VisitVariableExpr(Variable expr)
    {
        return expr.Name.Lexeme;
    }
}
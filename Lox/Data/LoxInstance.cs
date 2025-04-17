using Lox.Errors;
using Lox.Functions;
using Lox.Types;

namespace Lox.Data;

public class LoxInstance
{
    private readonly LoxClass _loxClass;
    private readonly Dictionary<string, object?> Properties = new();
    
    public LoxInstance(LoxClass loxClass)
    {
        _loxClass = loxClass;
    }
    
    public override string ToString()
    {
        return _loxClass.Name + " instance";
    }

    public object? Get(Token exprName)
    {
        if (Properties.TryGetValue(exprName.Lexeme, out object? value))
        {
            return value;
        }
        
        RuntimeFunction? method = _loxClass.FindMethod(exprName.Lexeme);
        if (method != null)
        {
            return method.Bind(this, exprName.Line);
        }

        throw new RuntimeException(exprName.Line, $"Invalid property name {exprName.Lexeme}.");
    }

    public void Set(Token exprName, object? value)
    {
        Properties[exprName.Lexeme] = value;
    }
}
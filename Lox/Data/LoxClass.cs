using Lox.Compilers;
using Lox.Functions;

namespace Lox.Data;

public class LoxClass : ICallable
{
    public int Arity
    {
        get
        {
            RuntimeFunction? initializer = FindMethod("init");
            return initializer?.Arity ?? 0;
        }
    }
    public string Name { get; }
    private Dictionary<string, RuntimeFunction> _methods;
    
    public object? Call(Interpreter interpreter, List<object?> arguments, int line)
    {
        LoxInstance instance = new(this);
        
        RuntimeFunction? initializer = FindMethod("init");
        initializer?.Bind(instance, line).Call(interpreter, arguments, line);
        
        return instance;
    }

    public LoxClass(string name, Dictionary<string, RuntimeFunction> methods)
    {
        Name = name;
        _methods = methods;
    }
    
    public override string ToString()
    {
        return Name;
    }

    public RuntimeFunction? FindMethod(string exprNameLexeme)
    {
        _methods.TryGetValue(exprNameLexeme, out RuntimeFunction? value);
        return value;
    }
}
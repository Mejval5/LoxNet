using Lox.Compilers;
using Lox.Functions;

namespace Lox.Data;

public class LoxClass : ICallable
{
    public string Name { get; }
    private LoxClass? Superclass { get; }
    private Dictionary<string, RuntimeFunction> Methods { get; }

    public int Arity
    {
        get
        {
            RuntimeFunction? initializer = FindMethod("init");
            return initializer?.Arity ?? 0;
        }
    }
    
    public object? Call(Interpreter interpreter, List<object?> arguments, int line)
    {
        LoxInstance instance = new(this);
        
        RuntimeFunction? initializer = FindMethod("init");
        initializer?.Bind(instance, line).Call(interpreter, arguments, line);
        
        return instance;
    }

    public LoxClass(string name, LoxClass? superclass, Dictionary<string, RuntimeFunction> methods)
    {
        Name = name;
        Superclass = superclass;
        Methods = methods;
    }
    
    public override string ToString()
    {
        return Name;
    }

    public RuntimeFunction? FindMethod(string methodName)
    {
        if (Methods.TryGetValue(methodName, out RuntimeFunction? value))
        {
            return value;
        }
        
        return Superclass?.FindMethod(methodName);
    }
}
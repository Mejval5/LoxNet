using Lox.Compilers;
using Lox.Data;
using Lox.Errors;
using Lox.Generated;
using Lox.Types;
using Environment = Lox.Data.Environment;

namespace Lox.Functions;

public class RuntimeFunction : ICallable
{
    private Function Declaration { get; }
    private Environment Closure;
    
    public string Name => Declaration.Name?.Lexeme ?? "";
    public int Arity => Declaration.Parameters.Count;

    public RuntimeFunction(Function declaration, Environment closure)
    {
        Declaration = declaration;
        Closure = closure;
    }
    
    public object? Call(Interpreter interpreter, List<object?> arguments)
    {
        Environment environment = new (Closure);
        for (int index = 0; index < Declaration.Parameters.Count; index++)
        {
            Token param = Declaration.Parameters[index];
            object? arg = arguments[index];
            environment.Define(param, arg);
        }

        try
        {
            interpreter.ExecuteBlock(Declaration.Body, environment);
        }
        catch (ReturnRuntimeException returnRuntimeException)
        {
            return returnRuntimeException.Value;
        }
        
        return null;
    }

    public override string ToString()
    {
        return $"<fn {Name}>";
    }
}
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
    private readonly Environment Closure;
    private readonly bool IsInitializer;
    
    public string Name => Declaration.Name?.Lexeme ?? "";
    public int Arity => Declaration.Parameters.Count;

    public RuntimeFunction(Function declaration, Environment closure, bool isInitializer)
    {
        Declaration = declaration;
        Closure = closure;
        IsInitializer = isInitializer;
    }
    
    public object? Call(Interpreter interpreter, List<object?> arguments, int line)
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
            return IsInitializer ? Closure.GetAt(0, "this", line) : returnRuntimeException.Value;
        }

        return IsInitializer ? Closure.GetAt(0, "this", line) : null;
    }

    public override string ToString()
    {
        return $"<fn {Name}>";
    }

    public ICallable Bind(LoxInstance loxInstance, int line)
    {
        Environment environment = new (Closure);
        environment.Define("this", line, loxInstance);
        return new RuntimeFunction(Declaration, environment, IsInitializer);
    }
}
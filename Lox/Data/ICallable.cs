using Lox.Compilers;

namespace Lox.Data;

public interface ICallable
{
    int Arity { get; }
    string Name { get; }
    object? Call(Interpreter interpreter, List<object?> arguments, int line);
}
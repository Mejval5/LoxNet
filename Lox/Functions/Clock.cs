using Lox.Compilers;
using Lox.Data;

namespace Lox.Functions;

public class Clock : ICallable
{
    public string Name => nameof(Clock).ToLower();
    public int Arity => 0;
    
    public object? Call(Interpreter interpreter, List<object?> arguments)
    {
        return DateTime.Now.Ticks / (double)TimeSpan.TicksPerSecond;
    }

    public override string ToString()
    {
        return $"<native {Name} fn>";
    }
}
using Lox.Types;

namespace Lox.Errors;

public class ParseException : Exception
{
    
}

public class RuntimeException : Exception
{
    public int ErrorLine { get; }

    public RuntimeException(int errorLine, string message) : base(message)
    {
        ErrorLine = errorLine;
    }
}

public class BreakRuntimeException : RuntimeException
{
    public BreakRuntimeException(int errorLine, string message) : base(errorLine, message)
    {
    }
}

public class ReturnRuntimeException : RuntimeException
{
    public object? Value { get; }
    
    public ReturnRuntimeException(object? value, int errorLine, string message) : base(errorLine, message)
    {
        Value = value;
    }
}


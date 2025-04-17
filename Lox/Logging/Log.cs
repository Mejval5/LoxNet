using Lox.Compilers;
using Lox.Errors;
using Lox.Types;

namespace Lox.Logging;

public static class Log
{
    public static void Error(Token token, string message)
    {
        if (token.TokenType is TokenType.Eof)
        {
            ReportError(token.Line, " at end", message);
        }
        else
        {
            ReportError(token.Line, $"at '{token.Lexeme}'", message);
        }
    }
    
    public static void Error(int line, string message)
    {
        ReportError(line, "", message);
    }
    
    private static void ReportError(int line, string where, string message)
    {
        Console.WriteLine($"[Line {line}] Error {where}: {message}");
        CompileRunner.HadError = true;
    }

    public static void RuntimeError(RuntimeException runtimeException)
    {
        Console.WriteLine($"{runtimeException.Message}\n[Line {runtimeException.ErrorLine}]");
        CompileRunner.HadRuntimeError = true;
    }
}
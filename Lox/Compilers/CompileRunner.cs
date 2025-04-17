using Lox.Errors;
using Lox.Generated;
using Lox.Logging;
using Lox.Types;
using Lox.Utils;

namespace Lox.Compilers;

public static class CompileRunner
{
    private static readonly Interpreter Interpreter = new();
        
    public static bool ShouldVerboseLog { get; set; }
    public static bool HadError { get; set; }
    public static bool HadRuntimeError { get; set; }

    public static void RunFile(string filePath)
    {
        if (File.Exists(filePath) == false)
        {
            filePath = Path.Combine(PathUtils.GetRootLoxPath(), filePath);
        }
        
        byte[] bytes = File.ReadAllBytes(filePath);
        if (bytes.Length == 0)
        {
            Console.WriteLine($"Error reading file: {filePath}");
            return;
        }
        // make sure to remove bom
            
        string code = System.Text.Encoding.UTF8.GetString(bytes);
        code = code.TrimStart('\uFEFF');
        Run(code);
        
        if (HadError)
        {
            Environment.Exit(65);
        }

        if (HadRuntimeError)
        {
            Environment.Exit(70);
        }
    }

    private static void Run(string code)
    {
        Scanner scanner = new(code);
        List<Token> tokens = scanner.ScanTokens();

        if (ShouldVerboseLog)
        {
            foreach (Token token in tokens)
            {
                Console.WriteLine(token);
            }
        }
            
        Parser parser = new (tokens);
        List<Stmt> statements = parser.Parse();

        if (ShouldVerboseLog)
        {
            foreach (Stmt? statement in statements)
            {
                if (statement is Expression expr)
                {
                    Console.WriteLine(new AstPrinter().Print(expr.Expr));
                }
            }
        }

        if (HadError)
        {
            return;
        }
            
        Resolver resolver = new (Interpreter);
        resolver.Resolve(statements);
        
        if (HadError)
        {
            return;
        }
        
        try
        {
            Interpreter.Interpret(statements);
        }
        catch (RuntimeException)
        {
        }
    }

    public static void RunPrompt()
    {
        while (true) { 
            Console.Write("> ");
            string? line = Console.ReadLine();
            if (line == null)
            {
                break;
            }

            Run(line);

            HadError = false;
            HadRuntimeError = false;
        }
    }
}
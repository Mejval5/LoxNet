using Lox.Errors;
using Lox.Types;

namespace Lox.Data;

public class Environment
{
    private readonly Environment? Enclosing;
    private Dictionary<string, object?> Values { get; } = new();

    public Environment(Environment? enclosing = null)
    {
        Enclosing = enclosing;
    }
    
    public void Define(string identifier, int line, object? value)
    {
        if (Values.TryAdd(identifier, value) == false)
        {
            throw new RuntimeException(line, $"Redefining existing variable '{identifier}' is not allowed.");
        }
    }

    public void Define(Token identifier, object? value)
    {
        if (Values.TryAdd(identifier.Lexeme, value) == false)
        {
            throw new RuntimeException(identifier.Line, $"Redefining existing variable '{identifier.Lexeme}' is not allowed.");
        }
    }

    public void Assign(Token identifier, object? value)
    {
        if (Values.ContainsKey(identifier.Lexeme))
        {
            Values[identifier.Lexeme] = value;
            return;
        }

        if (Enclosing != null)
        {
            Enclosing.Assign(identifier, value);
            return;
        }

        throw new RuntimeException(identifier.Line, $"Undefined variable '{identifier.Lexeme}'.");
    }
    
    public object? Get(Token identifier)
    {
        if (Values.TryGetValue(identifier.Lexeme, out object? value))
        {
            return value;
        }

        if (Enclosing != null)
        {
            return Enclosing.Get(identifier);
        }

        throw new RuntimeException(identifier.Line, $"Undefined variable '{identifier.Lexeme}'.");
    }

    public object? GetAt(int distance, Token exprName)
    {
        Environment? targetEnv = Ancestor(distance);
        if (targetEnv == null)
        {
            throw new RuntimeException(exprName.Line, $"Could not find scope of variable '{exprName.Lexeme}'.");
        }

        if (targetEnv.Values.TryGetValue(exprName.Lexeme, out object? value))
        {
            return value;
        }
        
        throw new RuntimeException(exprName.Line, $"Undefined variable '{exprName.Lexeme}'.");
    }

    private Environment? Ancestor(int distance)
    {
        Environment? env = this;
        for (int i = 0; i < distance; i++)
        {
            env = env?.Enclosing;
        }

        return env;
    }

    public void AssignAt(int distance, Token exprName, object? value)
    {
        Environment? targetEnv = Ancestor(distance);
        if (targetEnv == null)
        {
            throw new RuntimeException(exprName.Line, $"Could not find scope of assigning variable '{exprName.Lexeme}'.");
        }
        
        if (targetEnv.Values.TryAdd(exprName.Lexeme, value))
        {
            return;
        }
        
        throw new RuntimeException(exprName.Line, $"Could not assign variable '{exprName.Lexeme}'.");
    }
}
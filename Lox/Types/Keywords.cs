namespace Lox.Types;

public static class Keywords
{
    public static readonly Dictionary<string, TokenType> Reserved = new()
    {
        { "and", TokenType.And },
        { "break", TokenType.Break },
        { "class", TokenType.Class },
        { "else", TokenType.Else },
        { "false", TokenType.False },
        { "fun", TokenType.Fun },
        { "for", TokenType.For },
        { "if", TokenType.If },
        { "nil", TokenType.Nil },
        { "or", TokenType.Or },
        { "print", TokenType.Print },
        { "return", TokenType.Return },
        { "super", TokenType.Super },
        { "this", TokenType.This },
        { "true", TokenType.True },
        { "var", TokenType.Var },
        { "while", TokenType.While }
    };
}
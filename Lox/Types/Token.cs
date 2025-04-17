namespace Lox.Types;

public class Token
{
    public TokenType TokenType { get; set; }
    public string Lexeme;
    public object? Literal;
    public int Line;
    
    public Token (TokenType tokenType, string lexeme, object? literal, int line)
    {
        TokenType = tokenType;
        Lexeme = lexeme;
        Literal = literal;
        Line = line;
    }
    
    public override string ToString()
    {
        return $"{TokenType} {Lexeme} {Literal}";
    }
}
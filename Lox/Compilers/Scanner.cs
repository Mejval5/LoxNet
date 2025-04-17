using Lox.Logging;
using Lox.Types;

namespace Lox.Compilers;

public class Scanner
{
    private readonly string _code;
    private readonly List<Token> _tokens;
    
    private int _start = 0;
    private int _current = 0;
    private int _line = 1;

    public bool IsAtEnd => _current >= _code.Length;
    
    public Scanner(string code)
    {
        _code = code;
        _tokens = [];
    }

    public List<Token> ScanTokens()
    {
        while (IsAtEnd == false)
        {
            _start = _current;
            ScanNewToken();
        }

        _tokens.Add(new Token(TokenType.Eof, "", null, _line));
        return _tokens;
    }

    private void ScanNewToken()
    {
        char ch = Advance();
        try
        {
            ProcessNextCharacterIntoToken(ch);
        }
        catch (Exception exception)
        {
            Log.Error(_line, exception.Message);
        }
    }

    private void ProcessNextCharacterIntoToken(char ch)
    {
        switch (ch)
        {
            case '(':
                AddToken(TokenType.LeftParenthesis);
                break;

            case ')':
                AddToken(TokenType.RightParenthesis);
                break;
            
            case '{':
                AddToken(TokenType.LeftBrace);
                break;
            
            case '}':
                AddToken(TokenType.RightBrace);
                break;
            
            case ',':
                AddToken(TokenType.Comma);
                break;
            
            case '.':
                AddToken(TokenType.Dot);
                break;
            
            case '-':
                AddToken(TokenType.Minus);
                break;
            
            case '+':
                AddToken(TokenType.Plus);
                break;
            
            case ';':
                AddToken(TokenType.Semicolon);
                break;
            
            case '*':
                AddToken(TokenType.Star);
                break;

            case '!':
                AddToken(Match('=') ?TokenType.BangEqual : TokenType.Bang);
                break;

            case '=':
                AddToken(Match('=') ? TokenType.EqualEqual : TokenType.Equal);
                break;

            case '<':
                AddToken(Match('=') ? TokenType.LessEqual : TokenType.Less);
                break;
            
            case '>':
                AddToken(Match('=') ? TokenType.GreaterEqual : TokenType.Greater);
                break;
            
            case '/':
                if (Match('/'))
                {
                    ReadComment();
                }
                else
                {
                    AddToken(TokenType.Slash);
                }
                break;

            case ' ':
            case '\r':
            case '\t':
                break;

            case '\n':
                NextLine();
                break;

            case '"':
                ReadString();
                break;

            default:
                if (IsDigit(ch))
                {
                    ReadNumber();
                    break;
                }
                
                if (IsAlpha(ch))
                {
                    ReadIdentifier();
                    break;
                }

                throw new Exception($"Unexpected character: {ch}");
        }
    }

    private void ReadIdentifier()
    {
        while (IsAlpha(Peek()) || IsDigit(Peek()))
        {
            Advance();
        }

        string text = _code.Substring(_start, _current - _start);
        TokenType tokenType = Keywords.Reserved.GetValueOrDefault(text, TokenType.Identifier);
        AddToken(tokenType);
    }

    private bool IsAlpha(char ch)
    {
        return (ch >= 'a' && ch <= 'z') ||
               (ch >= 'A' && ch <= 'Z') ||
               ch == '_';
    }

    private void ReadNumber()
    {
        while (IsDigit(Peek()))
        {
            Advance();
        }

        if (Peek() == '.' && IsDigit(Peek(1)))
        {
            Advance();
            
            while (IsDigit(Peek()))
            {
                Advance();
            }
        }
        
        string value = _code.Substring(_start, _current - _start);
        double number = double.Parse(value);
        AddToken(TokenType.Number, number);
    }

    private bool IsDigit(char ch)
    {
        return ch >= '0' && ch <= '9';
    }

    private void ReadString()
    {
        while (Peek() != '"' && IsAtEnd == false)
        {
            if (Peek() == '\n')
            {
                NextLine();
            }
            Advance();
        }
        
        if (IsAtEnd)
        {
            throw new Exception("Unterminated string.");
        }
        
        Advance();
        string value = _code.Substring(_start + 1, _current - _start - 2);
        AddToken(TokenType.String, value);
    }

    private void NextLine()
    {
        _line++;
    }

    private void ReadComment()
    {
        while (Peek() != '\n' && IsAtEnd == false)
        {
            Advance();
        }
    }

    private char Peek(int i = 0)
    {
        if (_current + i >= _code.Length)
        {
            return '\0';
        }
        
        return _code[_current + i];
    }

    private bool Match(char charToMatch)
    {
        if (IsAtEnd)
        {
            return false;
        }
        
        char ch = _code[_current];
        if (ch != charToMatch)
        {
            return false;
        }
        
        _current++;
        return true;
    }

    private void AddToken(TokenType tokenType, object literal = null)
    {
        string text = _code.Substring(_start, _current - _start);
        _tokens.Add(new Token(tokenType, text, literal, _line));
    }

    private char Advance()
    {
        char ch = _code[_current];
        _current++;
        return ch;
    }
}
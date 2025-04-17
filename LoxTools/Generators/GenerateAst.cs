using LoxTools.Extensions;
using System.Text;

namespace LoxTools.Generators;

public static class GenerateAst
{
    private const string LoxFolder = "Lox";
    private const string GeneratedFolder = "Generated";
    private const string CsExtension = ".cs";

    public static void Generate()
    {
        string outputDir = Directory.GetParent(Environment.CurrentDirectory)!.Parent!.Parent!.Parent!.FullName;

        outputDir = Path.Combine(outputDir, LoxFolder, GeneratedFolder);
        if (Directory.Exists(outputDir) == false)
        {
            Directory.CreateDirectory(outputDir);
        }
        
        DefineAst(outputDir,
                  "Expr",
                  [
                      "AnonFun  : Token token, Function fun",
                      "Assign   : Token name, Expr value",
                      "Binary   : Expr left, Token op, Expr right",
                      "Call     : Expr callee, Token paren, List<Expr> arguments",
                      "Grouping : Expr expression",
                      "Literal  : object? value",
                      "Logical  : Expr left, Token op, Expr right",
                      "Unary    : Token op, Expr right",
                      "Variable : Token name"
                  ]);
        
        DefineAst(outputDir,
                  "Stmt",
                  [
                      "Block      : List<Stmt> statements",
                      "Expression : Expr expr",
                      "Function   : Token? name, List<Token> parameters, List<Stmt> body",
                      "If         : Expr condition, Stmt thenBranch, Stmt? elseBranch",
                      "Print      : Expr expr",
                      "Return     : Token keyword, Expr? value",
                      "Var        : Token name, Expr? initializer",
                      "While      : Expr condition, Stmt body",
                      "Break      : Token token"
                  ]);
    }

    private static void DefineAst(string outputDir, string baseName, List<string> types)
    {
        string fileName = Path.Combine(outputDir, baseName + CsExtension);
        StreamWriter writer = new (fileName, false, Encoding.UTF8);
        writer.WriteLine("using Lox.Types;");
        writer.WriteLine();
        writer.WriteLine("namespace Lox.Generated;");
        writer.WriteLine();
        writer.WriteLine($"public abstract class {baseName}");
        writer.WriteLine("{");
        writer.WriteLine("    public abstract T Accept<T>(IVisitor<T> visitor);");
        DefineVisitor(writer, baseName, types);
        writer.WriteLine("}");
        foreach (string type in types)
        {
            writer.WriteLine();
            string[] split = type.Split(":");
            string className = split[0].Trim();
            string fields = split[1].Trim();
            DefineType(writer, baseName, className, fields);
        }
        
        writer.WriteLine();

        writer.Close();
    }

    private static void DefineVisitor(StreamWriter writer, string baseName, List<string> types)
    {
        writer.WriteLine("    public interface IVisitor<T>");
        writer.WriteLine("    {");
        foreach (string type in types)
        {
            string typeName = type.Split(":")[0].Trim();
            writer.WriteLine($"        T Visit{typeName}{baseName}({typeName} {baseName.ToLower()});");
        }
        writer.WriteLine("    }");
    }

    private static void DefineType(StreamWriter writer, string baseName, string className, string fields)
    {
        string[] fieldsArray = fields.Split(", ").Where(field => string.IsNullOrWhiteSpace(field) == false).ToArray();
        
        writer.WriteLine($"public class {className} : {baseName}");
        writer.WriteLine("{");
        foreach (string field in fieldsArray)
        {
            string type = field.Split(" ")[0];
            string name = field.Split(" ")[1];
            writer.WriteLine($"    public {type} {name.FirstCharToUpper()} {{ get; }}");
        }
        writer.WriteLine();
        writer.WriteLine($"    public {className}({fields})");
        writer.WriteLine("    {");
        foreach (string field in fieldsArray)
        {
            string name = field.Split(" ")[1];
            writer.WriteLine($"        {name.FirstCharToUpper()} = {name};");
        }
        
        writer.WriteLine("    }");
        writer.WriteLine();
        writer.WriteLine($"    public override T Accept<T>(IVisitor<T> visitor)");
        writer.WriteLine("    {");
        writer.WriteLine($"        return visitor.Visit{className}{baseName}(this);");
        writer.WriteLine("    }");
        writer.WriteLine("}");
    }
}
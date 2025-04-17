namespace Lox.Utils;

public static class PathUtils
{
    public static string GetRootLoxPath()
    {
        return Directory.GetParent(Environment.CurrentDirectory)!.Parent!.Parent!.FullName;
    }
}
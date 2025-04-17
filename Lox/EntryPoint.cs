
using Lox.Compilers;

List<int> parsedOptionalArgsIndices = [];
if (args.Length > 0)
{
    for (int index = 0; index < args.Length; index++)
    {
        string arg = args[index];
        bool consumeArg = false;
        if (arg == "-v")
        {
            CompileRunner.ShouldVerboseLog = true;
            consumeArg = true;
        }

        if (consumeArg)
        {
            parsedOptionalArgsIndices.Add(index);
        }
    }
}

int remainingArguments = args.Length - parsedOptionalArgsIndices.Count;
if (remainingArguments > 1)
{
    Console.WriteLine("Usage: Lox [script]");
    Console.WriteLine("Optional arguments:");
    Console.WriteLine("-v : Enables verbose logging");
    Environment.Exit(64);
}

if (remainingArguments == 1)
{
    int remainingIndex = Enumerable
                         .Range(0, args.Length)
                         .First(argIndex => parsedOptionalArgsIndices.Contains(argIndex) == false);
    CompileRunner.RunFile(args[remainingIndex]);
}
else
{
    CompileRunner.RunPrompt();
}
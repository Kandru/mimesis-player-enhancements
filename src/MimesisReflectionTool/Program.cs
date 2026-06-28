using System.Reflection;
using MimesisReflectionTool;

return CommandLine.Run(args);

internal static class CommandLine
{
    public static int Run(string[] args)
    {
        string? melonLoaderPath = null;
        string? gamePath = null;
        string? assemblyName = null;
        var commandArgs = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--melonloader":
                    melonLoaderPath = RequireValue(args, ref i, "--melonloader");
                    break;
                case "--game":
                    gamePath = RequireValue(args, ref i, "--game");
                    break;
                case "--assembly":
                    assemblyName = RequireValue(args, ref i, "--assembly");
                    break;
                case "-h":
                case "--help":
                    PrintHelp();
                    return 0;
                default:
                    commandArgs.Add(args[i]);
                    break;
            }
        }

        if (commandArgs.Count == 0)
        {
            PrintHelp();
            return 1;
        }

        try
        {
            string melonLoader = MelonLoaderAssemblyPaths.Resolve(melonLoaderPath, gamePath);
            Console.WriteLine($"Using MelonLoader assemblies: {melonLoader}");
            Console.WriteLine();

            var context = new MelonLoaderReflectionContext(melonLoader);
            Assembly assembly = string.IsNullOrWhiteSpace(assemblyName)
                ? context.MelonLoader
                : context.LoadAssemblyByName(assemblyName);

            Console.WriteLine($"Reflecting assembly: {assembly.GetName().Name}");
            Console.WriteLine();

            return Execute(context, assembly, commandArgs);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static int Execute(MelonLoaderReflectionContext context, Assembly assembly, List<string> commandArgs)
    {
        string command = commandArgs[0];

        switch (command)
        {
            case "types":
                return RunTypes(assembly, commandArgs);
            case "type":
                return RunType(context, assembly, commandArgs);
            case "properties":
                return RunProperties(context, assembly, commandArgs);
            case "methods":
                return RunMethods(context, assembly, commandArgs);
            case "fields":
                return RunFields(context, assembly, commandArgs);
            case "constants":
                return RunConstants(context, assembly, commandArgs);
            case "member":
                return RunMember(context, assembly, commandArgs);
            default:
                Console.Error.WriteLine($"Unknown command: {command}");
                PrintHelp();
                return 1;
        }
    }

    private static int RunTypes(Assembly assembly, List<string> args)
    {
        string? filter = args.Count > 1 ? args[1] : null;
        IEnumerable<Type> types = SafeGetTypes(assembly);
        if (!string.IsNullOrWhiteSpace(filter))
        {
            types = types.Where(t =>
                t.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || (t.FullName?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        ReflectionPrinter.PrintTypes(types);
        return 0;
    }

    private static int RunType(MelonLoaderReflectionContext context, Assembly assembly, List<string> args)
    {
        if (args.Count < 2)
            throw new InvalidOperationException("Usage: type <TypeName>");

        ReflectionPrinter.PrintTypeSummary(context.RequireType(assembly, args[1]));
        return 0;
    }

    private static int RunProperties(MelonLoaderReflectionContext context, Assembly assembly, List<string> args)
    {
        if (args.Count < 2)
            throw new InvalidOperationException("Usage: properties <TypeName> [nameFilter]");

        string? filter = args.Count > 2 ? args[2] : null;
        ReflectionPrinter.PrintProperties(context.RequireType(assembly, args[1]), filter);
        return 0;
    }

    private static int RunMethods(MelonLoaderReflectionContext context, Assembly assembly, List<string> args)
    {
        if (args.Count < 2)
            throw new InvalidOperationException("Usage: methods <TypeName> [nameFilter]");

        string? filter = args.Count > 2 ? args[2] : null;
        ReflectionPrinter.PrintMethods(context.RequireType(assembly, args[1]), filter);
        return 0;
    }

    private static int RunFields(MelonLoaderReflectionContext context, Assembly assembly, List<string> args)
    {
        if (args.Count < 2)
            throw new InvalidOperationException("Usage: fields <TypeName> [nameFilter]");

        string? filter = args.Count > 2 ? args[2] : null;
        ReflectionPrinter.PrintFields(context.RequireType(assembly, args[1]), filter);
        return 0;
    }

    private static int RunConstants(MelonLoaderReflectionContext context, Assembly assembly, List<string> args)
    {
        if (args.Count < 2)
            throw new InvalidOperationException("Usage: constants <TypeName>");

        ReflectionPrinter.PrintConstants(context.RequireType(assembly, args[1]));
        return 0;
    }

    private static int RunMember(MelonLoaderReflectionContext context, Assembly assembly, List<string> args)
    {
        if (args.Count < 3)
            throw new InvalidOperationException("Usage: member <TypeName> <MemberName>");

        ReflectionPrinter.PrintMember(context.RequireType(assembly, args[1]), args[2]);
        return 0;
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null)!;
        }
    }

    private static string RequireValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
            throw new InvalidOperationException($"Missing value for {option}.");

        index++;
        return args[index];
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            MimesisReflectionTool — runtime reflection inspector for MelonLoader assemblies

            Usage:
              dotnet run --project src/MimesisReflectionTool -- [options] <command> [args]

            Options:
              --melonloader <path>   Path to MelonLoader/net35
              --game <path>          MIMESIS install root (uses <path>/MelonLoader/net35)
              --assembly <name>      Assembly file name without path (default: MelonLoader)
              -h, --help             Show this help

            Assembly resolution order:
              1. --melonloader
              2. --game
              3. MIMESIS_PATH environment variable
              4. deps/reference/MelonLoader/net35 after ./scripts/bootstrap-deps.sh

            Commands:
              types [filter]                    List types whose name/full name contains filter
              type <TypeName>                   Summary of one type
              properties <TypeName> [filter]    List properties
              methods <TypeName> [filter]       List methods
              fields <TypeName> [filter]        List fields
              constants <TypeName>              List const/static literal fields
              member <TypeName> <Member>        Show one method/property/field

            Examples:
              dotnet run --project src/MimesisReflectionTool -- properties MelonLoader.Logging.ColorARGB Green
              dotnet run --project src/MimesisReflectionTool -- type MelonLoader.MelonMod
              dotnet run --project src/MimesisReflectionTool -- types Logger
            """);
    }
}

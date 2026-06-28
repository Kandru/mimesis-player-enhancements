using System.Reflection;
using MimesisInspectionTool;

return CommandLine.Run(args);

internal static class CommandLine
{
    public static int Run(string[] args)
    {
        string? managedPath = null;
        string? gamePath = null;
        List<string> commandArgs = [];

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--managed":
                    managedPath = RequireValue(args, ref i, "--managed");
                    break;
                case "--game":
                    gamePath = RequireValue(args, ref i, "--game");
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
            string managed = ManagedAssemblyPaths.Resolve(managedPath, gamePath);
            Console.WriteLine($"Using managed assemblies: {managed}");
            Console.WriteLine();

            using MimesisMetadataContext context = new(managed);
            return Execute(context, commandArgs);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static int Execute(MimesisMetadataContext context, List<string> commandArgs)
    {
        string command = commandArgs[0];

        switch (command)
        {
            case "types":
                return RunTypes(context, commandArgs);
            case "type":
                return RunType(context, commandArgs);
            case "methods":
                return RunMethods(context, commandArgs);
            case "fields":
                return RunFields(context, commandArgs);
            case "constants":
                return RunConstants(context, commandArgs);
            case "member":
                return RunMember(context, commandArgs);
            case "scan":
                return RunScan(context, commandArgs);
            default:
                Console.Error.WriteLine($"Unknown command: {command}");
                PrintHelp();
                return 1;
        }
    }

    private static int RunTypes(MimesisMetadataContext context, List<string> args)
    {
        string? filter = args.Count > 1 ? args[1] : null;
        IEnumerable<Type> types = context.AssemblyCSharp.GetTypes();
        if (!string.IsNullOrWhiteSpace(filter))
        {
            types = types.Where(t =>
                t.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || (t.FullName?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        InspectionPrinter.PrintTypes(types);
        return 0;
    }

    private static int RunType(MimesisMetadataContext context, List<string> args)
    {
        if (args.Count < 2)
        {
            throw new InvalidOperationException("Usage: type <TypeName>");
        }

        InspectionPrinter.PrintTypeSummary(context.RequireType(args[1]));
        return 0;
    }

    private static int RunMethods(MimesisMetadataContext context, List<string> args)
    {
        if (args.Count < 2)
        {
            throw new InvalidOperationException("Usage: methods <TypeName> [nameFilter]");
        }

        string? filter = args.Count > 2 ? args[2] : null;
        InspectionPrinter.PrintMethods(context.RequireType(args[1]), filter);
        return 0;
    }

    private static int RunFields(MimesisMetadataContext context, List<string> args)
    {
        if (args.Count < 2)
        {
            throw new InvalidOperationException("Usage: fields <TypeName> [nameFilter]");
        }

        string? filter = args.Count > 2 ? args[2] : null;
        InspectionPrinter.PrintFields(context.RequireType(args[1]), filter);
        return 0;
    }

    private static int RunConstants(MimesisMetadataContext context, List<string> args)
    {
        if (args.Count < 2)
        {
            throw new InvalidOperationException("Usage: constants <TypeName>");
        }

        InspectionPrinter.PrintConstants(context.RequireType(args[1]));
        return 0;
    }

    private static int RunMember(MimesisMetadataContext context, List<string> args)
    {
        if (args.Count < 3)
        {
            throw new InvalidOperationException("Usage: member <TypeName> <MemberName>");
        }

        InspectionPrinter.PrintMember(context.RequireType(args[1]), args[2]);
        return 0;
    }

    private static int RunScan(MimesisMetadataContext context, List<string> args)
    {
        if (args.Count < 2)
        {
            throw new InvalidOperationException("Usage: scan <memberNameFilter>");
        }

        string filter = args[1];
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        Type[] types;
        try
        {
            types = context.AssemblyCSharp.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t != null).Cast<Type>().ToArray();
        }

        foreach (Type type in types.OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            MethodInfo[] methods;
            FieldInfo[] fields;
            try
            {
                methods = type.GetMethods(flags);
                fields = type.GetFields(flags);
            }
            catch
            {
                continue;
            }

            foreach (MethodInfo method in methods)
            {
                if (method.IsSpecialName || !method.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Console.WriteLine($"{type.FullName}.{method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))}) -> {method.ReturnType.Name}");
            }

            foreach (FieldInfo field in fields)
            {
                if (!field.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Console.WriteLine($"{type.FullName}.{field.Name} : {field.FieldType.Name}");
            }
        }

        return 0;
    }

    private static string RequireValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
        {
            throw new InvalidOperationException($"Missing value for {option}.");
        }

        index++;
        return args[index];
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            MimesisInspectionTool — read-only metadata inspector for MIMESIS assemblies

            Usage:
              dotnet run --project src/MimesisInspectionTool -- [options] <command> [args]

            Options:
              --managed <path>   Path to MIMESIS_Data/Managed
              --game <path>      MIMESIS install root (uses <path>/MIMESIS_Data/Managed)
              -h, --help         Show this help

            Assembly resolution order:
              1. --managed
              2. --game
              3. MIMESIS_PATH environment variable
              4. deps/reference/Managed after ./scripts/bootstrap-deps.sh

            Commands:
              types [filter]                 List types whose name/full name contains filter
              type <TypeName>                Summary of one type
              methods <TypeName> [filter]    List methods
              fields <TypeName> [filter]     List fields
              constants <TypeName>           List const/static literal fields
              member <TypeName> <Member>     Show one method/property/field

            Examples:
              dotnet run --project src/MimesisInspectionTool -- constants MMSaveGameData
              dotnet run --project src/MimesisInspectionTool -- type VWorld
              dotnet run --project src/MimesisInspectionTool -- methods MMSaveGameData SaveSlot
              dotnet run --project src/MimesisInspectionTool -- member MMSaveGameData CheckSaveSlotID
            """);
    }
}

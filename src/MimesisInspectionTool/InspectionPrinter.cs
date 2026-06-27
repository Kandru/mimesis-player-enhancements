using System.Reflection;
using System.Text;

namespace MimesisInspectionTool;

internal static class InspectionPrinter
{
    public static void PrintTypes(IEnumerable<Type> types)
    {
        foreach (Type type in types.OrderBy(t => t.FullName, StringComparer.Ordinal))
            Console.WriteLine(FormatTypeHeader(type));
    }

    public static void PrintTypeSummary(Type type)
    {
        Console.WriteLine(FormatTypeHeader(type));
        Console.WriteLine();

        Console.WriteLine("Constants / static fields:");
        bool anyConstants = false;
        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        {
            if (!field.IsLiteral)
                continue;

            anyConstants = true;
            Console.WriteLine($"  {field.FieldType.Name} {field.Name} = {FormatConstant(field)}");
        }

        if (!anyConstants)
            Console.WriteLine("  (none)");

        Console.WriteLine();
        Console.WriteLine("Properties:");
        PrintMembers(type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static), "  (none)");

        Console.WriteLine();
        Console.WriteLine("Methods:");
        try
        {
            PrintMembers(
                type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Where(m => !m.IsSpecialName),
                "  (none)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  (partial list — {ex.Message})");
        }
    }

    public static void PrintMethods(Type type, string? filter)
    {
        IEnumerable<MethodInfo> methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Where(m => !m.IsSpecialName);

        if (!string.IsNullOrWhiteSpace(filter))
        {
            methods = methods.Where(m => m.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        PrintMembers(methods, "No methods matched.");
    }

    public static void PrintFields(Type type, string? filter)
    {
        IEnumerable<FieldInfo> fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        if (!string.IsNullOrWhiteSpace(filter))
            fields = fields.Where(f => f.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));

        foreach (FieldInfo field in fields.OrderBy(f => f.Name, StringComparer.Ordinal))
        {
            string flags = field.IsStatic ? "static" : "instance";
            if (field.IsLiteral)
                flags += " const";
            Console.WriteLine($"{FormatField(field)} [{flags}]");
        }
    }

    public static void PrintConstants(Type type)
    {
        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        {
            if (!field.IsLiteral)
                continue;

            Console.WriteLine($"{field.FieldType.Name} {field.Name} = {FormatConstant(field)}");
        }
    }

    public static void PrintMember(Type type, string memberName)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        MethodInfo? method = type.GetMethods(flags)
            .FirstOrDefault(m => string.Equals(m.Name, memberName, StringComparison.Ordinal));
        if (method != null)
        {
            Console.WriteLine(FormatMethod(method));
            return;
        }

        PropertyInfo? property = type.GetProperty(memberName, flags);
        if (property != null)
        {
            Console.WriteLine(FormatProperty(property));
            return;
        }

        FieldInfo? field = type.GetField(memberName, flags);
        if (field != null)
        {
            Console.WriteLine(FormatField(field));
            return;
        }

        throw new InvalidOperationException($"Member not found on {type.FullName}: {memberName}");
    }

    private static void PrintMembers(IEnumerable<MemberInfo> members, string emptyMessage)
    {
        bool any = false;
        foreach (MemberInfo member in members.OrderBy(m => m.Name, StringComparer.Ordinal))
        {
            any = true;
            Console.WriteLine("  " + member switch
            {
                MethodInfo method => FormatMethod(method),
                PropertyInfo property => FormatProperty(property),
                _ => member.Name,
            });
        }

        if (!any)
            Console.WriteLine(emptyMessage);
    }

    private static string FormatTypeHeader(Type type)
    {
        string kind = type.IsClass ? "class" : type.IsValueType ? "struct" : "type";
        string visibility = type.IsPublic ? "public" : "non-public";
        return $"{type.FullName} ({visibility} {kind})";
    }

    private static string FormatMethod(MethodInfo method)
    {
        var sb = new StringBuilder();
        if (method.IsStatic)
            sb.Append("static ");
        sb.Append(method.ReturnType.Name);
        sb.Append(' ');
        sb.Append(method.Name);
        sb.Append('(');
        sb.Append(string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}")));
        sb.Append(')');
        return sb.ToString();
    }

    private static string FormatProperty(PropertyInfo property)
    {
        var accessors = new List<string>();
        if (property.CanRead)
            accessors.Add("get");
        if (property.CanWrite)
            accessors.Add("set");
        return $"{property.PropertyType.Name} {property.Name} {{ {string.Join("; ", accessors)} }}";
    }

    private static string FormatField(FieldInfo field) =>
        $"{field.FieldType.Name} {field.Name}";

    private static string FormatConstant(FieldInfo field)
    {
        try
        {
            object? value = field.GetRawConstantValue();
            return value?.ToString() ?? "null";
        }
        catch
        {
            return "?";
        }
    }
}

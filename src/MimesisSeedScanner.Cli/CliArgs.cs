namespace MimesisSeedScanner.Cli
{
    internal static class CliArgs
    {
        internal static string? Get(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        internal static int GetInt(string[] args, string name, int defaultValue)
        {
            string? value = Get(args, name);
            return int.TryParse(value, out int parsed) ? parsed : defaultValue;
        }

        internal static int? TryGetInt(string[] args, string name)
        {
            string? value = Get(args, name);
            return int.TryParse(value, out int parsed) ? parsed : null;
        }

        internal static TimeSpan? GetTimeBudget(string[] args, string name)
        {
            string? value = Get(args, name);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            value = value.Trim().ToLowerInvariant();
            if (value.EndsWith('h'))
            {
                return TimeSpan.FromHours(double.Parse(value[..^1], System.Globalization.CultureInfo.InvariantCulture));
            }

            if (value.EndsWith('m'))
            {
                return TimeSpan.FromMinutes(double.Parse(value[..^1], System.Globalization.CultureInfo.InvariantCulture));
            }

            if (value.EndsWith('s'))
            {
                return TimeSpan.FromSeconds(double.Parse(value[..^1], System.Globalization.CultureInfo.InvariantCulture));
            }

            return TimeSpan.FromSeconds(double.Parse(value, System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}

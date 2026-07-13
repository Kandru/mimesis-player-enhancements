using System.Collections.Specialized;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal sealed class WebDashboardSseSubscriptions
    {
        internal bool Snapshot { get; private set; }
        internal bool Minimap { get; private set; }

        internal static WebDashboardSseSubscriptions Parse(NameValueCollection? query)
        {
            WebDashboardSseSubscriptions subscriptions = new();
            string? channels = query?["channels"];
            if (string.IsNullOrWhiteSpace(channels))
            {
                subscriptions.Snapshot = true;
                return subscriptions;
            }

            string[] parts = channels.Split(',');
            foreach (string rawPart in parts)
            {
                string part = rawPart.Trim();
                if (part.Length == 0)
                {
                    continue;
                }
                if (part.Equals("snapshot", StringComparison.OrdinalIgnoreCase))
                {
                    subscriptions.Snapshot = true;
                }
                else if (part.Equals("minimap", StringComparison.OrdinalIgnoreCase))
                {
                    subscriptions.Minimap = true;
                }
            }

            if (!subscriptions.Snapshot && !subscriptions.Minimap)
            {
                subscriptions.Snapshot = true;
            }

            return subscriptions;
        }

        internal bool Wants(string eventName)
        {
            return eventName switch
            {
                "snapshot" => Snapshot,
                "minimap" => Minimap,
                _ => false,
            };
        }
    }
}

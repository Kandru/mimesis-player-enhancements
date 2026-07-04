namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal static class FunnyTramMenuLabels
    {
        private static readonly string[] Labels =
        [
            "I like trains",
            "Choo choo choose a save",
            "All aboard the pain train",
            "Thomas has seen things",
            "Trammy McTramface",
            "Press F to pay tramfare",
            "Hop on the hype tram",
            "This tram runs on screams",
            "Next stop: regret station",
            "Mind the gap between the saves",
            "Tickets to trauma, please",
            "Express service to doom",
            "Whistle while you panic",
            "No refunds on this ride",
            "Schedule: delayed indefinitely",
            "Powered by friendship and fear",
            "Tram of Theseus",
            "Beware the closing doors (and mimics)",
            "All stations to suffering",
            "Ding ding, despair incoming",
            "Final destination: probably not",
            "Soul tram, no brakes",
            "Screaming optional, dying mandatory",
            "Not the conductor you deserve",
            "Derailed but determined",
            "Your save, our collective trauma",
            "Please hold onto your loot",
            "Leaving the station of good ideas",
            "Ghost tram go brr",
            "Ticket punched, fate sealed",
        ];

        internal static string PickRandom()
        {
            return Labels[UnityEngine.Random.Range(0, Labels.Length)];
        }
    }
}

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardSessionScene
    {
        internal static string Resolve(GameMainBase? main)
        {
            return main switch
            {
                InTramWaitingScene => "tram",
                MaintenanceScene => "maintenance",
                GamePlayScene => "dungeon",
                DeathMatchScene => "death_match",
                _ => "",
            };
        }

        internal static bool IsBlindModeScene(GameMainBase? main) =>
            main is GamePlayScene or DeathMatchScene;
    }
}

using MimesisPlayerEnhancement.Features.UserInterface.CustomLoadingScreen;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.UserInterface
{
    public sealed class CustomLoadingScreenApplierTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void PredictContextFromSceneName_returns_null_for_blank(string? sceneName)
        {
            CustomLoadingScreenContext? context = CustomLoadingScreenApplier.PredictContextFromSceneName(
                sceneName,
                previousTransition: null,
                sessionLive: true,
                tramWaitingPresent: true);

            Assert.Null(context);
        }

        [Theory]
        [InlineData("InTramWaiting")]
        [InlineData("scene_tram_hub")]
        public void PredictContextFromSceneName_tram_without_dungeon_previous_is_TramScene(string sceneName)
        {
            CustomLoadingScreenContext? context = CustomLoadingScreenApplier.PredictContextFromSceneName(
                sceneName,
                previousTransition: CustomLoadingScreenContext.Maintenance,
                sessionLive: true,
                tramWaitingPresent: false);

            Assert.Equal(CustomLoadingScreenContext.TramScene, context);
        }

        [Fact]
        public void PredictContextFromSceneName_tram_after_dungeon_is_DungeonEnd()
        {
            CustomLoadingScreenContext? context = CustomLoadingScreenApplier.PredictContextFromSceneName(
                "InTramWaiting",
                previousTransition: CustomLoadingScreenContext.DungeonStart,
                sessionLive: true,
                tramWaitingPresent: false);

            Assert.Equal(CustomLoadingScreenContext.DungeonEnd, context);
        }

        [Fact]
        public void PredictContextFromSceneName_maintenance_is_Maintenance()
        {
            CustomLoadingScreenContext? context = CustomLoadingScreenApplier.PredictContextFromSceneName(
                "MaintenanceRoom",
                previousTransition: null,
                sessionLive: true,
                tramWaitingPresent: true);

            Assert.Equal(CustomLoadingScreenContext.Maintenance, context);
        }

        [Theory]
        [InlineData("MainMenu")]
        [InlineData("QuitGame")]
        public void PredictContextFromSceneName_menu_or_quit_is_null(string sceneName)
        {
            CustomLoadingScreenContext? context = CustomLoadingScreenApplier.PredictContextFromSceneName(
                sceneName,
                previousTransition: CustomLoadingScreenContext.DungeonStart,
                sessionLive: true,
                tramWaitingPresent: true);

            Assert.Null(context);
        }

        [Fact]
        public void PredictContextFromSceneName_live_session_from_tram_is_DungeonStart()
        {
            CustomLoadingScreenContext? context = CustomLoadingScreenApplier.PredictContextFromSceneName(
                "Dungeon_Forest",
                previousTransition: CustomLoadingScreenContext.TramScene,
                sessionLive: true,
                tramWaitingPresent: true);

            Assert.Equal(CustomLoadingScreenContext.DungeonStart, context);
        }

        [Fact]
        public void PredictContextFromSceneName_without_live_session_is_null()
        {
            CustomLoadingScreenContext? context = CustomLoadingScreenApplier.PredictContextFromSceneName(
                "Dungeon_Forest",
                previousTransition: CustomLoadingScreenContext.TramScene,
                sessionLive: false,
                tramWaitingPresent: true);

            Assert.Null(context);
        }

        [Theory]
        [InlineData("FirstEnter", null, (int)CustomLoadingScreenContext.FirstEnter)]
        [InlineData("Maintenance", null, (int)CustomLoadingScreenContext.Maintenance)]
        [InlineData("DeathMatch", null, (int)CustomLoadingScreenContext.DeathMatch)]
        [InlineData("InTramWaiting", null, (int)CustomLoadingScreenContext.TramScene)]
        [InlineData("InTramWaiting", (int)CustomLoadingScreenContext.DungeonStart, (int)CustomLoadingScreenContext.DungeonEnd)]
        [InlineData("Dungeon_Forest", null, (int)CustomLoadingScreenContext.DungeonStart)]
        [InlineData("Default", null, (int)CustomLoadingScreenContext.DungeonStart)]
        public void FromLoadingSceneKey_maps_keys(string? key, int? previous, int expected)
        {
            CustomLoadingScreenContext? previousContext = previous.HasValue
                ? (CustomLoadingScreenContext)previous.Value
                : null;

            CustomLoadingScreenContext actual = CustomLoadingScreenContextUtil.FromLoadingSceneKey(
                key,
                previousContext);

            Assert.Equal((CustomLoadingScreenContext)expected, actual);
        }

        [Theory]
        [InlineData((int)CustomLoadingScreenContext.FirstEnter, "FirstEnter")]
        [InlineData((int)CustomLoadingScreenContext.Maintenance, "Maintenance")]
        [InlineData((int)CustomLoadingScreenContext.TramScene, "TramScene")]
        [InlineData((int)CustomLoadingScreenContext.DungeonStart, "DungeonStart")]
        [InlineData((int)CustomLoadingScreenContext.DungeonEnd, "DungeonEnd")]
        [InlineData((int)CustomLoadingScreenContext.DeathMatch, "DeathMatch")]
        public void ToFolderName_matches_context(int context, string folder)
        {
            Assert.Equal(
                folder,
                CustomLoadingScreenContextUtil.ToFolderName((CustomLoadingScreenContext)context));
        }
    }
}

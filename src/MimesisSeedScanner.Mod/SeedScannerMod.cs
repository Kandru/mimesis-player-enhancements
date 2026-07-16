[assembly: MelonInfo(typeof(MimesisSeedScanner.Mod.SeedScannerMod), "Mimesis Seed Scanner", "1.0.0", "Mimesis")]
[assembly: MelonGame("ReLUGames", "MIMESIS")]

namespace MimesisSeedScanner.Mod
{
    internal sealed class SeedScannerMod : MelonMod
    {
        private static bool _exportRunning;
        private HarmonyLib.Harmony? _harmony;

        public override void OnInitializeMelon()
        {
            _harmony = new HarmonyLib.Harmony("MimesisSeedScanner.Mod");
            _harmony.PatchAll(typeof(SeedScannerMod).Assembly);
        }

        public override void OnDeinitializeMelon()
        {
            _harmony?.UnpatchSelf();
        }

        public override void OnUpdate()
        {
            if (!SeedScannerInput.WasScanTriggerPressedThisFrame() || _exportRunning)
            {
                return;
            }

            MelonCoroutines.Start(RunCatalogExportCoroutine());
        }

        private static IEnumerator RunCatalogExportCoroutine()
        {
            _exportRunning = true;
            MelonLogger.Msg("Catalog export started — press F10 only.");

            if (!FlowCatalog.TryGetFlows(out IReadOnlyList<FlowCatalogEntry> flows))
            {
                MelonLogger.Msg("Resolving dungeon flow assets — this may take a few seconds...");
                yield return FlowCatalog.BootstrapCoroutine();
                if (!FlowCatalog.TryGetFlows(out flows))
                {
                    MelonLogger.Error(
                        "Catalog export refused — could not load dungeon flow assets. Stay on the main menu and try again.");
                    _exportRunning = false;
                    yield break;
                }
            }

            List<FlowCatalogEntry> validFlows = FilterProductionFlows(flows);
            if (validFlows.Count == 0)
            {
                MelonLogger.Error("Catalog export refused — no production dungeon flows found.");
                _exportRunning = false;
                yield break;
            }

            MelonLogger.Msg("Exporting tile/flow catalogs from Unity assets...");
            yield return null;

            ScanCatalog catalog;
            try
            {
                catalog = CatalogExporter.Export(validFlows);
                CatalogExportStore.Save(catalog);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Catalog export failed — {ex}");
                _exportRunning = false;
                yield break;
            }

            MelonLogger.Msg($"Catalog export complete — wrote {CatalogExportStore.OutputPath}.");
            MelonLogger.Msg(
                "Run: dotnet run --project src/MimesisSeedScanner.Cli -- scan "
                + $"--catalog \"{CatalogExportStore.OutputPath}\"");
            _exportRunning = false;
        }

        private static List<FlowCatalogEntry> FilterProductionFlows(IReadOnlyList<FlowCatalogEntry> flows)
        {
            List<FlowCatalogEntry> validFlows = flows
                .Where(entry => entry.Flow != null)
                .ToList();

            foreach (FlowCatalogEntry entry in flows)
            {
                if (entry.Flow == null)
                {
                    MelonLogger.Warning($"Skipping flow '{entry.FlowId}' — asset reference is missing.");
                }
            }

            HashSet<string> productionFlowIds = FlowCatalog.GetProductionFlowIds();
            if (productionFlowIds.Count > 0)
            {
                int beforeCount = validFlows.Count;
                validFlows = validFlows
                    .Where(entry => productionFlowIds.Contains(entry.FlowId))
                    .ToList();
                MelonLogger.Msg(
                    $"Using {validFlows.Count} base production flow(s) (skipped {beforeCount - validFlows.Count} unreferenced or variant flows).");
            }
            else
            {
                int beforeCount = validFlows.Count;
                validFlows = validFlows
                    .Where(entry => FlowCatalog.ShouldScanFlow(entry.FlowId))
                    .ToList();
                MelonLogger.Msg(
                    $"Using {validFlows.Count} base flow(s) (skipped {beforeCount - validFlows.Count} jackpot/DL/devtools variants).");
            }

            return validFlows;
        }
    }
}

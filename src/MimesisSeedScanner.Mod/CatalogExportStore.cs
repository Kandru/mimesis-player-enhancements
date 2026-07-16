using MimesisSeedScanner;
using Newtonsoft.Json;

namespace MimesisSeedScanner.Mod
{
    internal static class CatalogExportStore
    {
        internal static string OutputPath =>
            Path.Combine(MelonEnvironment.UserDataDirectory, "scan-catalog.json");

        internal static void Save(ScanCatalog catalog)
        {
            var document = new ScanCatalogDocument
            {
                ExportedAt = DateTime.UtcNow,
                Catalog = catalog,
            };
            string json = JsonConvert.SerializeObject(document, Formatting.Indented);
            string tempPath = OutputPath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Copy(tempPath, OutputPath, overwrite: true);
            File.Delete(tempPath);
        }
    }
}

using System.Reflection;
using MimesisPlayerEnhancement.Tests.Infrastructure;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.ModVersionDisplay
{
    public sealed class ModVersionDisplayPatchContractTests
    {
        private const BindingFlags InstanceMember =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [Theory]
        [InlineData("UIPrefab_MainMenu")]
        [InlineData("UIPrefab_InGameMenu")]
        public void SetVersionText_exists(string typeName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType(typeName);

            MethodInfo? method = type.GetMethod("SetVersionText", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Empty(method.GetParameters());
        }

        [Theory]
        [InlineData("UIPrefab_MainMenu")]
        [InlineData("UIPrefab_InGameMenu")]
        public void UE_versionText_property_exists(string typeName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType(typeName);

            PropertyInfo? property = type.GetProperty("UE_versionText", InstanceMember);

            Assert.NotNull(property);
            Assert.NotNull(property.GetGetMethod(nonPublic: true));
        }

        private static MimesisMetadataContext CreateContext()
        {
            string managedPath = ManagedAssemblyPaths.Resolve();
            return new MimesisMetadataContext(managedPath);
        }
    }
}

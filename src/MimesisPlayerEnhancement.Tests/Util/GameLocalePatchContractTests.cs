using System.Reflection;
using MimesisPlayerEnhancement.Tests.Infrastructure;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Util
{
    public sealed class GameLocalePatchContractTests
    {
        private const BindingFlags InstanceMember =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [Fact]
        public void L10NManager_ChangeLanguage_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("L10NManager");

            MethodInfo? method = type.GetMethod("ChangeLanguage", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
            ParameterInfo[] parameters = method.GetParameters();
            Assert.Single(parameters);
            Assert.Equal("String", parameters[0].ParameterType.Name);
        }

        [Fact]
        public void L10NManager_language_property_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("L10NManager");

            PropertyInfo? property = type.GetProperty("language", InstanceMember);

            Assert.NotNull(property);
            Assert.Equal("String", property.PropertyType.Name);
            Assert.NotNull(property.GetMethod);
        }

        [Fact]
        public void Hub_lcman_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("Hub");

            FieldInfo? field = type.GetField("lcman", InstanceMember);

            Assert.NotNull(field);
            Assert.False(field.IsStatic);
            Assert.Equal("L10NManager", field.FieldType.Name);
        }

        private static MimesisMetadataContext CreateContext()
        {
            string managedPath = ManagedAssemblyPaths.Resolve();
            return new MimesisMetadataContext(managedPath);
        }
    }
}

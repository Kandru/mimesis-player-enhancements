using System.Reflection;
using MimesisPlayerEnhancement.Tests.Infrastructure;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.WebDashboard
{
    public sealed class WebDashboardModerationContractTests
    {
        private const BindingFlags InstanceMember =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [Fact]
        public void SessionManager_HandleKickPlayerReq_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SessionManager");

            MethodInfo? method = type.GetMethod("HandleKickPlayerReq", InstanceMember);

            Assert.NotNull(method);
            ParameterInfo[] parameters = method.GetParameters();
            Assert.Equal(3, parameters.Length);
            Assert.Equal("VPlayer", parameters[0].ParameterType.Name);
            Assert.Equal("Int64", parameters[1].ParameterType.Name);
            Assert.Equal("Int32", parameters[2].ParameterType.Name);
            Assert.Equal("MsgErrorCode", method.ReturnType.Name);
        }

        [Fact]
        public void SessionManager_RemoveInternal_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SessionManager");

            MethodInfo? method = type.GetMethod("RemoveInternal", InstanceMember);

            Assert.NotNull(method);
            ParameterInfo[] parameters = method.GetParameters();
            Assert.Equal(2, parameters.Length);
            Assert.Equal("Int64", parameters[0].ParameterType.Name);
            Assert.Equal("DisconnectReason", parameters[1].ParameterType.Name);
        }

        [Theory]
        [InlineData("_commandExecutor")]
        [InlineData("_dormantSnapshots")]
        [InlineData("_bannedSteamIDs")]
        public void SessionManager_moderation_fields_exist(string fieldName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SessionManager");

            FieldInfo? field = type.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
        }

        private static MimesisMetadataContext CreateContext()
        {
            string managedPath = ManagedAssemblyPaths.Resolve();
            return new MimesisMetadataContext(managedPath);
        }
    }
}

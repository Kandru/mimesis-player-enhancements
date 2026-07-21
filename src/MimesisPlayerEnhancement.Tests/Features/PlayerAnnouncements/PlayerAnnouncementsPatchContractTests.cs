using System.Reflection;
using MimesisPlayerEnhancement.Tests.Infrastructure;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.PlayerAnnouncements
{
    public sealed class PlayerAnnouncementsPatchContractTests
    {
        private const BindingFlags InstanceMember =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [Fact]
        public void DungeonRoom_OnAllMemberEntered_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type dungeonRoom = context.RequireType("DungeonRoom");

            MethodInfo? method = dungeonRoom.GetMethod("OnAllMemberEntered", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Empty(method.GetParameters());
        }

        [Fact]
        public void DungeonRoom_OnActorEnter_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type dungeonRoom = context.RequireType("DungeonRoom");

            MethodInfo? method = dungeonRoom.GetMethods(InstanceMember)
                .FirstOrDefault(candidate =>
                    candidate.Name == "OnActorEnter"
                    && candidate.GetParameters().Length == 1
                    && candidate.GetParameters()[0].ParameterType.Name == "VActor");

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void GameMainBase_OnPlayerDeath_accepts_ProtoActor()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("GameMainBase");

            MethodInfo? method = type.GetMethods(InstanceMember)
                .FirstOrDefault(candidate =>
                    candidate.Name == "OnPlayerDeath"
                    && candidate.GetParameters().Length >= 1
                    && candidate.GetParameters()[0].ParameterType.Name == "ProtoActor");

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void VActor_MasterID_is_Int32()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VActor");

            MemberInfo? member = type.GetProperty("MasterID", InstanceMember) as MemberInfo
                ?? type.GetField("MasterID", InstanceMember);

            Assert.NotNull(member);
            Type memberType = member switch
            {
                PropertyInfo property => property.PropertyType,
                FieldInfo field => field.FieldType,
                _ => throw new InvalidOperationException("Unexpected member type."),
            };
            Assert.Equal("Int32", memberType.Name);
        }

        [Fact]
        public void VActor_ActorType_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VActor");

            MemberInfo? member = type.GetProperty("ActorType", InstanceMember) as MemberInfo
                ?? type.GetField("ActorType", InstanceMember);

            Assert.NotNull(member);
            Type memberType = member switch
            {
                PropertyInfo property => property.PropertyType,
                FieldInfo field => field.FieldType,
                _ => throw new InvalidOperationException("Unexpected member type."),
            };
            Assert.Equal("ActorType", memberType.Name);
        }

        [Fact]
        public void VMonster_inherits_VActor()
        {
            using MimesisMetadataContext context = CreateContext();
            Type vMonster = context.RequireType("VMonster");
            Type vActor = context.RequireType("VActor");

            Assert.True(vActor.IsAssignableFrom(vMonster));
        }

        [Fact]
        public void ActorType_has_Monster_constant()
        {
            using MimesisMetadataContext context = CreateContext();
            Type actorType = context.RequireType("ReluProtocol.Enum.ActorType");

            FieldInfo? monsterField = actorType.GetField("Monster", BindingFlags.Public | BindingFlags.Static);

            Assert.NotNull(monsterField);
            Assert.Equal("ActorType", monsterField.FieldType.Name);
        }

        private static MimesisMetadataContext CreateContext()
        {
            string managedPath = ManagedAssemblyPaths.Resolve();
            return new MimesisMetadataContext(managedPath);
        }
    }
}

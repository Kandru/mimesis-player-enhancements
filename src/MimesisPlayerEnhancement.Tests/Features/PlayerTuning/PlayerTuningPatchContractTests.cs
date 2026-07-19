using System.Reflection;
using MimesisPlayerEnhancement.Tests.Infrastructure;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.PlayerTuning
{
    public sealed class PlayerTuningPatchContractTests
    {
        private const BindingFlags InstanceMember =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [Fact]
        public void MappedStats_LoadBaseStats_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("MappedStats");

            MethodInfo? method = type.GetMethod("LoadBaseStats", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Boolean", method.ReturnType.Name);
            ParameterInfo[] parameters = method.GetParameters();
            Assert.Equal(3, parameters.Length);
            Assert.Equal("ActorType", parameters[0].ParameterType.Name);
            Assert.Equal("Int32", parameters[1].ParameterType.Name);
            Assert.Equal("Int32", parameters[2].ParameterType.Name);
        }

        [Fact]
        public void InventoryController_OnChangeInventory_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("InventoryController");

            MethodInfo? method = type.GetMethod("OnChangeInventory", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Empty(method.GetParameters());
        }

        [Fact]
        public void ProtoActor_SetControlMode_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ProtoActor");

            MethodInfo? method = type.GetMethod("SetControlMode", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Single(method.GetParameters());
        }

        [Fact]
        public void ProtoActor_SetAsOtherPlayer_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ProtoActor");

            MethodInfo? method = type.GetMethod("SetAsOtherPlayer", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void ProtoActor_SetAsMonster_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ProtoActor");

            MethodInfo? method = type.GetMethod("SetAsMonster", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void ProtoActor_SetupMonsterCapsuleCollider_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ProtoActor");

            MethodInfo? method = type.GetMethod("SetupMonsterCapsuleCollider", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Single(method.GetParameters());
            Assert.Equal("Int32", method.GetParameters()[0].ParameterType.Name);
        }

        [Fact]
        public void ProtoActor_OnActorRevive_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ProtoActor");

            MethodInfo? method = type.GetMethod("OnActorRevive", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Theory]
        [InlineData("C_RunStaminaConsumeValue")]
        [InlineData("C_StaminaRegenValue")]
        [InlineData("C_StaminaRegenDelayEmpty")]
        [InlineData("C_StaminaRegenDelayRemain")]
        public void DataConsts_stamina_fields_are_Int64(string fieldName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("DataConsts");

            FieldInfo? field = type.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
            Assert.Equal("Int64", field.FieldType.Name);
        }

        [Fact]
        public void DataConsts_C_MaxCarryWeight_is_Int32()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("DataConsts");

            FieldInfo? field = type.GetField("C_MaxCarryWeight", InstanceMember);

            Assert.NotNull(field);
            Assert.Equal("Int32", field.FieldType.Name);
        }

        [Fact]
        public void InventoryController_self_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("InventoryController");

            FieldInfo? field = type.GetField("_self", InstanceMember);

            Assert.NotNull(field);
        }

        [Fact]
        public void IVroom_vPlayerDict_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("IVroom");

            FieldInfo? field = type.GetField("_vPlayerDict", InstanceMember);

            Assert.NotNull(field);
        }

        [Fact]
        public void StatController_StatManager_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("StatController");

            FieldInfo? field = type.GetField("StatManager", InstanceMember);

            Assert.NotNull(field);
        }

        [Fact]
        public void VRoomManager_vrooms_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VRoomManager");

            FieldInfo? field = type.GetField("_vrooms", InstanceMember);

            Assert.NotNull(field);
        }

        [Fact]
        public void ProtoActor_GetComponent_accepts_Type()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ProtoActor");

            MethodInfo? method = type.GetMethods(InstanceMember)
                .FirstOrDefault(candidate =>
                    candidate.Name == "GetComponent"
                    && candidate.GetParameters().Length == 1
                    && candidate.GetParameters()[0].ParameterType.Name == "Type");

            Assert.NotNull(method);
        }

        [Fact]
        public void CapsuleCollider_enabled_property_exists_on_Collider_base()
        {
            string managedPath = ManagedAssemblyPaths.Resolve();
            string modulePath = Path.Combine(managedPath, "UnityEngine.dll");
            if (!File.Exists(modulePath))
            {
                // Bootstrap reference libs may omit this Unity module; verified on full game install.
                return;
            }

            List<string> assemblyPaths = [];
            HashSet<string> seenNames = new(StringComparer.OrdinalIgnoreCase);
            foreach (string path in Directory.EnumerateFiles(managedPath, "*.dll"))
            {
                string name = Path.GetFileName(path);
                if (seenNames.Add(name))
                {
                    assemblyPaths.Add(path);
                }
            }

            string runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)
                                  ?? throw new InvalidOperationException("Could not locate runtime assemblies.");
            foreach (string runtimeAssembly in Directory.EnumerateFiles(runtimeDir, "*.dll"))
            {
                string name = Path.GetFileName(runtimeAssembly);
                if (seenNames.Add(name))
                {
                    assemblyPaths.Add(runtimeAssembly);
                }
            }

            var resolver = new PathAssemblyResolver(assemblyPaths);
            using var loadContext = new MetadataLoadContext(resolver, typeof(object).Assembly.GetName().Name);
            Assembly module = loadContext.LoadFromAssemblyPath(modulePath);
            Type? capsuleCollider = module.GetType("UnityEngine.CapsuleCollider", throwOnError: false);

            Assert.NotNull(capsuleCollider);
            Type? colliderBase = capsuleCollider.BaseType;
            Assert.NotNull(colliderBase);
            Assert.Equal("Collider", colliderBase.Name);

            PropertyInfo? property = colliderBase.GetProperty("enabled", InstanceMember);
            Assert.NotNull(property);
            Assert.Equal("Boolean", property.PropertyType.Name);
        }

        private static MimesisMetadataContext CreateContext()
        {
            string managedPath = ManagedAssemblyPaths.Resolve();
            return new MimesisMetadataContext(managedPath);
        }
    }
}

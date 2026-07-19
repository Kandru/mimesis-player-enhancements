using System.Reflection;
using MimesisPlayerEnhancement.Tests.Infrastructure;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.UserInterface
{
    public sealed class UserInterfacePatchContractTests
    {
        private const BindingFlags InstanceMember =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private const BindingFlags StaticMember =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [Theory]
        [InlineData("Start")]
        [InlineData("UpdatePlayerListView")]
        public void UIPrefab_Spectator_PlayerListView_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_Spectator_PlayerListView");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void UIPrefabScript_Hide_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefabScript");

            MethodInfo? method = type.GetMethod("Hide", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void UIPrefabScript_Show_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefabScript");

            MethodInfo? method = type.GetMethod("Show", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void UIPrefabScript_Cor_Hide_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefabScript");

            MethodInfo? method = type.GetMethod("Cor_Hide", InstanceMember);

            Assert.NotNull(method);
        }

        [Theory]
        [InlineData("OnEnable")]
        [InlineData("OnDisable")]
        public void UIPrefab_InGameMenu_player_list_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_InGameMenu");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Theory]
        [InlineData("SetLoadingScene")]
        [InlineData("SetLoadingText")]
        public void UIPrefab_Scene_Loading_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_Scene_Loading");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void UIPrefab_SurvivalResult_OnEnable_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_SurvivalResult");

            MethodInfo? method = type.GetMethod("OnEnable", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void UIPrefab_SurvivalResult_PatchParameter_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_SurvivalResult");

            MethodInfo? method = type.GetMethod("PatchParameter", InstanceMember);

            Assert.NotNull(method);
        }

        [Theory]
        [InlineData("Start")]
        [InlineData("OnShow")]
        [InlineData("OnHpChanged")]
        [InlineData("OnContaChanged")]
        [InlineData("SetVisibleOxyGauge")]
        public void UIPrefab_InGame_fps_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_InGame");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
        }

        [Fact]
        public void UIPrefab_Inventory_UpdateSlot_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_Inventory");

            MethodInfo? method = type.GetMethod("UpdateSlot", InstanceMember);

            Assert.NotNull(method);
        }

        [Theory]
        [InlineData("InitCommonUIValue")]
        [InlineData("OnPlayerSpawn")]
        [InlineData("OnDestroy")]
        [InlineData("EndSceneLoading")]
        public void GameMainBase_ui_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("GameMainBase");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
        }

        [Theory]
        [InlineData("DestroyActor")]
        [InlineData("OnActorDeath")]
        [InlineData("UpdateHp")]
        [InlineData("ResolvePacket_HitTargetSig")]
        public void ProtoActor_world_overlay_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ProtoActor");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
        }

        [Fact]
        public void ProtoActor_OnPacket_ActorDamagedSig_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ProtoActor");

            MethodInfo? method = FindMethodWithSingleParameter(type, "OnPacket", "ActorDamagedSig");

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void GameMainBase_OnPacket_FieldHitTargetSig_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("GameMainBase");

            MethodInfo? method = FindMethodWithSingleParameter(type, "OnPacket", "FieldHitTargetSig");

            Assert.NotNull(method);
        }

        [Fact]
        public void GameMainBase_OnPacket_ProjectileHitTargetSig_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("GameMainBase");

            MethodInfo? method = FindMethodWithSingleParameter(type, "OnPacket", "ProjectileHitTargetSig");

            Assert.NotNull(method);
        }

        [Fact]
        public void AudioManager_PlaySfx_string_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = RequireAudioManagerType(context);

            MethodInfo? method = type.GetMethods(InstanceMember)
                .FirstOrDefault(candidate =>
                    candidate.Name == "PlaySfx"
                    && candidate.GetParameters().Length == 1
                    && candidate.GetParameters()[0].ParameterType.Name == "String");

            Assert.NotNull(method);
        }

        [Fact]
        public void AudioManager_PlaySfxTransform_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = RequireAudioManagerType(context);

            MethodInfo? method = type.GetMethods(InstanceMember)
                .FirstOrDefault(candidate =>
                    candidate.Name == "PlaySfxTransform"
                    && candidate.GetParameters().Length == 2
                    && candidate.GetParameters()[0].ParameterType.Name == "String"
                    && candidate.GetParameters()[1].ParameterType.Name == "Transform");

            Assert.NotNull(method);
        }

        [Fact]
        public void AudioPlayer_Start_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("AudioPlayer");

            MethodInfo? method = type.GetMethod("Start", InstanceMember);

            Assert.NotNull(method);
        }

        [Fact]
        public void GamePlayScene_Start_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("GamePlayScene");

            MethodInfo? method = type.GetMethod("Start", InstanceMember);

            Assert.NotNull(method);
        }

        [Fact]
        public void ModHelper_InvokeTimingCallback_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ModHelper");

            MethodInfo? method = type.GetMethod("InvokeTimingCallback", StaticMember);

            Assert.NotNull(method);
        }

        [Fact]
        public void Hub_LoadScene_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("Hub");

            MethodInfo? method = type.GetMethod("LoadScene", StaticMember);

            Assert.NotNull(method);
            Assert.True(method.IsStatic);
        }

        [Fact]
        public void UIManager_FadeOut_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIManager");

            MethodInfo? method = type.GetMethod("FadeOut", InstanceMember);

            Assert.NotNull(method);
        }

        [Fact]
        public void NewTramLeverLevelObject_OnChangeLevelObjectStateSig_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("NewTramLeverLevelObject");

            MethodInfo? method = type.GetMethod("OnChangeLevelObjectStateSig", InstanceMember);

            Assert.NotNull(method);
        }

        [Theory]
        [InlineData("Start")]
        [InlineData("OnEnable")]
        [InlineData("SetVersionText")]
        public void UIPrefab_MainMenu_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_MainMenu");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
        }

        [Theory]
        [InlineData("Start")]
        [InlineData("OnEnable")]
        [InlineData("SetVersionText")]
        public void UIPrefab_InGameMenu_menu_mirror_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_InGameMenu");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
        }

        [Theory]
        [InlineData("ingameui")]
        [InlineData("inventoryui")]
        [InlineData("spectatorui")]
        public void GameMainBase_ui_fields_exist(string fieldName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("GameMainBase");

            FieldInfo? field = type.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
        }

        [Theory]
        [InlineData("UE_Currency")]
        [InlineData("UE_KillCount")]
        [InlineData("oxyGauge")]
        public void UIPrefab_InGame_wiring_members_exist(string memberName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_InGame");

            MemberInfo? member = type.GetField(memberName, InstanceMember) as MemberInfo
                ?? type.GetProperty(memberName, InstanceMember);

            Assert.NotNull(member);
        }

        [Theory]
        [InlineData("UE_rootNode")]
        [InlineData("UE_InvenFrame1")]
        [InlineData("UE_InvenFrame2")]
        [InlineData("UE_InvenFrame3")]
        [InlineData("UE_InvenFrame4")]
        [InlineData("UE_Weight")]
        public void UIPrefab_Inventory_wiring_members_exist(string memberName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_Inventory");

            MemberInfo? member = type.GetField(memberName, InstanceMember) as MemberInfo
                ?? type.GetProperty(memberName, InstanceMember);

            Assert.NotNull(member);
        }

        [Theory]
        [InlineData("playerListView")]
        [InlineData("IsShow")]
        public void UIPrefab_Spectator_wiring_members_exist(string memberName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_Spectator");

            MemberInfo? member = type.GetField(memberName, InstanceMember) as MemberInfo
                ?? type.GetProperty(memberName, InstanceMember);

            Assert.NotNull(member);
        }

        [Theory]
        [InlineData("liveColor")]
        [InlineData("deadColor")]
        public void UIPrefab_Spectator_PlayerListView_color_fields_exist(string fieldName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_Spectator_PlayerListView");

            FieldInfo? field = type.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
        }

        [Theory]
        [InlineData("UE_Name_Text")]
        [InlineData("SpriteChangeAnimation")]
        [InlineData("IsPossessor")]
        public void UIPrefab_Spectator_PlayerListViewItem_wiring_members_exist(string memberName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_Spectator_PlayerListViewItem");

            MemberInfo? member = type.GetField(memberName, InstanceMember) as MemberInfo
                ?? type.GetProperty(memberName, InstanceMember);

            Assert.NotNull(member);
        }

        [Fact]
        public void AudioPlayer_sfxId_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("AudioPlayer");

            FieldInfo? field = type.GetField("sfxId", InstanceMember);

            Assert.NotNull(field);
        }

        [Theory]
        [InlineData("loadingSceneDataList")]
        [InlineData("currentLoadingSceneData")]
        public void UIPrefab_Scene_Loading_fields_exist(string fieldName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_Scene_Loading");

            FieldInfo? field = type.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
        }

        [Theory]
        [InlineData("fadeInImage")]
        [InlineData("videoPlayLayer")]
        [InlineData("currentTimerDialog")]
        public void UIManager_wiring_fields_exist(string fieldName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIManager");

            FieldInfo? field = type.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
        }

        [Theory]
        [InlineData("UIPrefab_MainMenu")]
        [InlineData("UIPrefab_InGameMenu")]
        public void Version_text_property_exists(string typeName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType(typeName);

            PropertyInfo? property = type.GetProperty("UE_versionText", InstanceMember);

            Assert.NotNull(property);
        }

        private static Type RequireAudioManagerType(MimesisMetadataContext context)
        {
            return context.FindType("Mimic.Audio.AudioManager")
                ?? context.RequireType("AudioManager");
        }

        private static MethodInfo? FindMethodWithSingleParameter(Type type, string methodName, string parameterTypeName)
        {
            return type.GetMethods(InstanceMember)
                .FirstOrDefault(candidate =>
                    candidate.Name == methodName
                    && candidate.GetParameters().Length == 1
                    && candidate.GetParameters()[0].ParameterType.Name == parameterTypeName);
        }

        private static MimesisMetadataContext CreateContext()
        {
            string managedPath = ManagedAssemblyPaths.Resolve();
            return new MimesisMetadataContext(managedPath);
        }
    }
}

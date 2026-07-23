using System.Reflection;
using HarmonyLib;
using MimesisPlayerEnhancement.Features.MimicTuning.MimicVoiceTuning;
using MimesisPlayerEnhancement.Features.MimicTuning.Patches;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.MimicTuning
{
    public sealed class MimicTuningPatchAccessToolsTests
    {
        [Fact]
        public void RollPossessionDurationMs_transpiler_target_resolves_two_arg_overload()
        {
            MethodInfo? method = MimicPossessionPatchSupport.RollPossessionDurationMsMethod;

            Assert.NotNull(method);
            Assert.Equal(2, method.GetParameters().Length);
            Assert.Equal(typeof(long), method.GetParameters()[0].ParameterType);
            Assert.Equal(typeof(int), method.GetParameters()[1].ParameterType);
        }

        [Fact]
        public void ScalePossessionCooltimeMs_transpiler_target_resolves_one_arg_overload()
        {
            MethodInfo? method = MimicPossessionPatchSupport.ScalePossessionCooltimeMsMethod;

            Assert.NotNull(method);
            Assert.Single(method.GetParameters());
            Assert.Equal(typeof(long), method.GetParameters()[0].ParameterType);
        }

        [Fact]
        public void GetResponseMaxDistance_transpiler_target_resolves_parameterless_overload()
        {
            MethodInfo? method = AccessTools.Method(
                typeof(MimicVoiceTuningResolver),
                nameof(MimicVoiceTuningResolver.GetResponseMaxDistance),
                Type.EmptyTypes);

            Assert.NotNull(method);
            Assert.Empty(method.GetParameters());
            Assert.Equal(typeof(float), method.ReturnType);
        }
    }
}

using System.Reflection;
using MelonLoader;
using MelonLoader.Logging;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Logging
{
    public sealed class LoggingReflectionContractTests
    {
        [Fact]
        public void MelonLogger_PassLogMsg_exists_with_expected_signature()
        {
            MethodInfo? method = typeof(MelonLogger).GetMethod(
                "PassLogMsg",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                binder: null,
                types:
                [
                    typeof(ColorARGB),
                    typeof(string),
                    typeof(ColorARGB),
                    typeof(string),
                    typeof(string),
                ],
                modifiers: null);

            Assert.NotNull(method);
            Assert.True(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);

            ParameterInfo[] parameters = method.GetParameters();
            Assert.Equal(5, parameters.Length);
            Assert.Equal("ColorARGB", parameters[0].ParameterType.Name);
            Assert.Equal("String", parameters[1].ParameterType.Name);
            Assert.Equal("ColorARGB", parameters[2].ParameterType.Name);
            Assert.Equal("String", parameters[3].ParameterType.Name);
            Assert.Equal("String", parameters[4].ParameterType.Name);
        }
    }
}

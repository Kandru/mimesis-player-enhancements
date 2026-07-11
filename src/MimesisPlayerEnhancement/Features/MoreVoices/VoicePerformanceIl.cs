using System.Reflection;
using System.Reflection.Emit;

namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    internal static class VoicePerformanceIl
    {
        internal static IEnumerable<CodeInstruction> ReplaceSpeechEventFind(
            IEnumerable<CodeInstruction> instructions,
            FieldInfo eventsField,
            MethodInfo replacement)
        {
            List<CodeInstruction> codes = [.. instructions];

            for (int i = 0; i < codes.Count; i++)
            {
                if (!IsSpeechEventFindCall(codes[i]))
                {
                    continue;
                }

                int eventsLoadIndex = FindEventsFieldLoadIndex(codes, i, eventsField);
                if (eventsLoadIndex < 0)
                {
                    continue;
                }

                int blockStart = eventsLoadIndex > 0 && codes[eventsLoadIndex - 1].opcode == OpCodes.Ldarg_0
                    ? eventsLoadIndex - 1
                    : eventsLoadIndex;
                int removeCount = i - blockStart + 1;

                codes.RemoveRange(blockStart, removeCount);
                codes.Insert(blockStart, new CodeInstruction(OpCodes.Ldarg_0));
                codes.Insert(blockStart + 1, new CodeInstruction(OpCodes.Ldarg_1));
                codes.Insert(blockStart + 2, new CodeInstruction(OpCodes.Call, replacement));
                i = blockStart + 2;
            }

            return codes;
        }

        private static bool IsSpeechEventFindCall(CodeInstruction instruction)
        {
            if (instruction.opcode != OpCodes.Call && instruction.opcode != OpCodes.Callvirt)
            {
                return false;
            }

            if (instruction.operand is not MethodInfo method)
            {
                return false;
            }

            if (!string.Equals(method.Name, "Find", StringComparison.Ordinal))
            {
                return false;
            }

            ParameterInfo[] parameters = method.GetParameters();
            return parameters.Length == 1 && parameters[0].ParameterType == typeof(Predicate<SpeechEvent>);
        }

        private static int FindEventsFieldLoadIndex(List<CodeInstruction> codes, int findCallIndex, FieldInfo eventsField)
        {
            for (int i = findCallIndex - 1; i >= 0; i--)
            {
                if (codes[i].opcode == OpCodes.Ldfld && ReferenceEquals(codes[i].operand, eventsField))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}

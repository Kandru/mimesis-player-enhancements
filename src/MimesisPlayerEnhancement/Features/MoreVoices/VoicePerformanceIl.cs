using System.Reflection;
using System.Reflection.Emit;

namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    internal static class VoicePerformanceIl
    {
        internal static IEnumerable<CodeInstruction> ReplaceSpeechEventFind(
            IEnumerable<CodeInstruction> instructions,
            FieldInfo eventsField,
            MethodInfo replacementMethod)
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

                List<CodeInstruction> removed = codes.GetRange(blockStart, removeCount);
                CodeInstruction[] replacementInstructions =
                [
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Call, replacementMethod),
                ];

                // brfalse after voiceman null-check lands on the first removed instruction — keep labels valid.
                foreach (Label label in removed[0].labels)
                {
                    replacementInstructions[0].labels.Add(label);
                }

                foreach (ExceptionBlock block in removed[0].blocks)
                {
                    replacementInstructions[0].blocks.Add(block);
                }

                codes.RemoveRange(blockStart, removeCount);
                codes.InsertRange(blockStart, replacementInstructions);
                i = blockStart + replacementInstructions.Length - 1;
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
                if (codes[i].opcode == OpCodes.Ldfld && IsEventsFieldLoad(codes[i], eventsField))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool IsEventsFieldLoad(CodeInstruction instruction, FieldInfo eventsField)
        {
            if (instruction.operand is not FieldInfo field)
            {
                return false;
            }

            return ReferenceEquals(field, eventsField)
                || (field.Name == eventsField.Name && field.DeclaringType == eventsField.DeclaringType);
        }
    }
}

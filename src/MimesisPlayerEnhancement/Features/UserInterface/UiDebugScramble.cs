using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface
{
    internal static class UiDebugScramble
    {
        internal static bool[] ScrambleTrueFlags(
            int count,
            float trueRatio,
            bool ensureMix,
            System.Random? random = null)
        {
            random ??= new System.Random();
            bool[] flags = new bool[count];
            if (count == 0)
            {
                return flags;
            }

            int trueCount = Mathf.Clamp(Mathf.RoundToInt(count * trueRatio), 0, count);
            if (ensureMix && count >= 2)
            {
                trueCount = Mathf.Clamp(trueCount, 1, count - 1);
            }

            for (int index = 0; index < trueCount; index++)
            {
                flags[index] = true;
            }

            for (int index = count - 1; index > 0; index--)
            {
                int swapIndex = random.Next(index + 1);
                (flags[index], flags[swapIndex]) = (flags[swapIndex], flags[index]);
            }

            return flags;
        }
    }
}

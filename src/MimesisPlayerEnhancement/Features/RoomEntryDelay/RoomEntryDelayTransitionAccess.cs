using System;
using System.Reflection;
using HarmonyLib;

namespace MimesisPlayerEnhancement.Features.RoomEntryDelay
{
    internal static class RoomEntryDelayTransitionAccess
    {
        private static readonly FieldInfo? CurrentTransitionField =
            AccessTools.Field(typeof(StaticLevelObject), "currentTransition");

        private static readonly FieldInfo? FromStateField;
        private static readonly FieldInfo? ToStateField;

        static RoomEntryDelayTransitionAccess()
        {
            Type? transitionContextType = AccessTools.Inner(typeof(StaticLevelObject), "TransitionContext");
            if (transitionContextType == null)
            {
                return;
            }

            FromStateField = AccessTools.Field(transitionContextType, "FromState");
            ToStateField = AccessTools.Field(transitionContextType, "ToState");
        }

        internal static bool TryGetCurrentTransition(StaticLevelObject levelObject, out int fromState, out int toState)
        {
            fromState = 0;
            toState = 0;

            if (CurrentTransitionField == null
                || FromStateField == null
                || ToStateField == null)
            {
                return false;
            }

            if (CurrentTransitionField.GetValue(levelObject) is not object transitionContext)
            {
                return false;
            }

            fromState = (int)FromStateField.GetValue(transitionContext)!;
            toState = (int)ToStateField.GetValue(transitionContext)!;
            return true;
        }
    }
}

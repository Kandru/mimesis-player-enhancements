namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal static class ExtendedSaveSlotsRuntime
    {
        internal static void RefreshFromConfig()
        {
            if (TramSavePickerController.IsActive)
            {
                TramSavePickerController.ApplyExtendedMode();
            }
            else
            {
                TramSavePickerController.ApplyVanillaMode();
            }
        }
    }
}

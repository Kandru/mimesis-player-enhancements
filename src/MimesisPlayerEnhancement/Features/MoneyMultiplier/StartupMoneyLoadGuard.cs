using ReluProtocol;

namespace MimesisPlayerEnhancement.Features.MoneyMultiplier;

internal static class StartupMoneyLoadGuard
{
    private static int _depth;

    internal static bool IsActive => _depth > 0;

    internal static bool TryEnterForSaveSlot(int saveSlotId)
    {
        if (saveSlotId == -1)
            return false;

        try
        {
            if (!MonoSingleton<PlatformMgr>.Instance.IsSaveFileExist(MMSaveGameData.GetSaveFileName(saveSlotId)))
                return false;
        }
        catch
        {
            return false;
        }

        _depth++;
        return true;
    }

    internal static void Exit()
    {
        if (_depth > 0)
            _depth--;
    }
}

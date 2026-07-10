using System.Diagnostics;
using System.Runtime.InteropServices;
using MelonLoader;
using MelonLoader.InternalUtils;

namespace MimesisPlayerEnhancement
{
    internal static class NativeMessageBox
    {
        private const uint MbOk = 0x00000000;
        private const uint MbIconWarning = 0x00000030;
        private const uint MbTopmost = 0x00040000;
        private const uint MbSetForeground = 0x00010000;
        private const uint MbSystemModal = 0x00001000;

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr FindWindowW(string? lpClassName, string? lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        internal static bool TryShowWarning(string caption, string message)
        {
            if (!CanShowNativeDialog())
            {
                return false;
            }

            try
            {
                IntPtr owner = ResolveOwnerWindow();
                TryPromoteToForeground(owner);

                uint flags = MbOk | MbIconWarning | MbTopmost | MbSetForeground | MbSystemModal;
                MessageBoxW(owner, message, caption, flags);
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Warn("Startup", $"Native message box failed — {ex.Message}");
                return false;
            }
        }

        private static IntPtr ResolveOwnerWindow()
        {
            string? gameName = UnityInformationHandler.GameName;
            if (!string.IsNullOrWhiteSpace(gameName))
            {
                IntPtr gameWindow = FindWindowW(null, gameName);
                if (gameWindow != IntPtr.Zero)
                {
                    return gameWindow;
                }
            }

            IntPtr processWindow = Process.GetCurrentProcess().MainWindowHandle;
            if (processWindow != IntPtr.Zero)
            {
                return processWindow;
            }

            IntPtr foreground = GetForegroundWindow();
            return foreground != IntPtr.Zero ? foreground : IntPtr.Zero;
        }

        private static void TryPromoteToForeground(IntPtr window)
        {
            if (window == IntPtr.Zero)
            {
                return;
            }

            IntPtr foreground = GetForegroundWindow();
            if (foreground == window)
            {
                return;
            }

            uint foregroundThread = GetWindowThreadProcessId(foreground, out _);
            uint currentThread = GetCurrentThreadId();
            bool attached = foreground != IntPtr.Zero
                && foregroundThread != currentThread
                && AttachThreadInput(currentThread, foregroundThread, true);

            try
            {
                BringWindowToTop(window);
                SetForegroundWindow(window);
            }
            finally
            {
                if (attached)
                {
                    AttachThreadInput(currentThread, foregroundThread, false);
                }
            }
        }

        private static bool CanShowNativeDialog()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return true;
            }

            return MelonUtils.IsUnderWineOrSteamProton();
        }
    }
}

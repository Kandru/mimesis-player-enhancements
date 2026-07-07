using System.Threading.Tasks;

namespace MimesisPlayerEnhancement.Util
{
    /// <summary>
    /// Bounded synchronous wait for background tasks (30s cap) with per-caller log wording.
    /// </summary>
    internal static class TaskWaitHelper
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

        /// <param name="waitLabel">Prefix for the failure log, e.g. "Background save".</param>
        /// <param name="timeoutMessage">Optional warning when the wait times out; silent when null.</param>
        internal static void WaitSync(Task? task, string logFeature, string waitLabel, string? timeoutMessage = null)
        {
            if (task == null || task.IsCompleted)
            {
                return;
            }

            try
            {
                if (!task.Wait(Timeout) && timeoutMessage != null)
                {
                    ModLog.Warn(logFeature, timeoutMessage);
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(logFeature, $"{waitLabel} wait failed: {ex.Message}");
            }
        }
    }
}

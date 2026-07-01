using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace MimesisPlayerEnhancement.Util
{
    internal enum FileWriteKind
    {
        Text,
        Bytes,
        Delete,
    }

    internal sealed class FileWritePayload
    {
        internal FileWriteKind Kind;
        internal string? Text;
        internal byte[]? Bytes;
    }

    /// <summary>
    /// Coalescing background file write queue. Callers prepare payloads on the game thread;
    /// disk I/O runs on thread-pool workers.
    /// </summary>
    internal static class BackgroundFileWriteQueue
    {
        private static readonly ConcurrentDictionary<string, PendingWrite> InFlight = new(StringComparer.OrdinalIgnoreCase);

        private sealed class PendingWrite
        {
            internal FileWritePayload? Latest;
            internal string LogFeature = "FileIO";
            internal Task? WriteTask;
        }

        internal static void EnqueueText(
            string path,
            string text,
            string logFeature = "FileIO",
            bool waitForCompletion = false)
        {
            Enqueue(path, new FileWritePayload { Kind = FileWriteKind.Text, Text = text }, logFeature, waitForCompletion);
        }

        internal static void EnqueueBytes(
            string path,
            byte[] bytes,
            string logFeature = "FileIO",
            bool waitForCompletion = false)
        {
            Enqueue(path, new FileWritePayload { Kind = FileWriteKind.Bytes, Bytes = bytes }, logFeature, waitForCompletion);
        }

        internal static void EnqueueDelete(
            string path,
            string logFeature = "FileIO",
            bool waitForCompletion = false)
        {
            Enqueue(path, new FileWritePayload { Kind = FileWriteKind.Delete }, logFeature, waitForCompletion);
        }

        internal static void FlushAllSync()
        {
            WaitForInFlightWrites();
        }

        private static void Enqueue(
            string path,
            FileWritePayload payload,
            string logFeature,
            bool waitForCompletion)
        {
            PendingWrite pending = InFlight.GetOrAdd(path, static _ => new PendingWrite());
            lock (pending)
            {
                pending.Latest = payload;
                pending.LogFeature = logFeature;

                if (pending.WriteTask is { IsCompleted: false })
                {
                    if (!waitForCompletion)
                    {
                        return;
                    }
                }
                else
                {
                    StartWriteTask(pending, path, payload, logFeature);
                    if (!waitForCompletion)
                    {
                        return;
                    }
                }
            }

            if (waitForCompletion)
            {
                WaitForPendingWrites(path, pending);
            }
        }

        private static void StartWriteTask(
            PendingWrite pending,
            string path,
            FileWritePayload payload,
            string logFeature)
        {
            FileWritePayload snapshot = payload;
            string feature = logFeature;
            pending.WriteTask = Task.Run(() => WriteToDisk(path, snapshot, feature));
        }

        private static void WriteToDisk(string path, FileWritePayload payload, string logFeature)
        {
            try
            {
                switch (payload.Kind)
                {
                    case FileWriteKind.Text:
                        EnsureDirectory(path);
                        AtomicFileIO.WriteText(path, payload.Text ?? string.Empty, logFeature);
                        break;
                    case FileWriteKind.Bytes:
                        EnsureDirectory(path);
                        AtomicFileIO.WriteBytes(path, payload.Bytes ?? [], logFeature);
                        break;
                    case FileWriteKind.Delete:
                        AtomicFileIO.Delete(path, logFeature);
                        break;
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(logFeature, $"Background file write failed ({Path.GetFileName(path)}): {ex.Message}");
            }

            CompleteWrite(path, payload);
        }

        private static void EnsureDirectory(string filePath)
        {
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                _ = Directory.CreateDirectory(directory);
            }
        }

        private static void CompleteWrite(string path, FileWritePayload written)
        {
            if (!InFlight.TryGetValue(path, out PendingWrite? pending))
            {
                return;
            }

            lock (pending)
            {
                if (pending.Latest != null && !PayloadsEqual(pending.Latest, written))
                {
                    FileWritePayload newer = pending.Latest;
                    string feature = pending.LogFeature;
                    pending.Latest = null;
                    pending.WriteTask = Task.Run(() => WriteToDisk(path, newer, feature));
                    return;
                }

                pending.Latest = null;
                _ = InFlight.TryRemove(path, out _);
            }
        }

        private static bool PayloadsEqual(FileWritePayload a, FileWritePayload b)
        {
            if (a.Kind != b.Kind)
            {
                return false;
            }

            return a.Kind switch
            {
                FileWriteKind.Text => a.Text == b.Text,
                FileWriteKind.Bytes => ReferenceEquals(a.Bytes, b.Bytes)
                    || (a.Bytes != null && b.Bytes != null && a.Bytes.AsSpan().SequenceEqual(b.Bytes)),
                FileWriteKind.Delete => true,
                _ => false,
            };
        }

        private static void WaitForInFlightWrites()
        {
            foreach (KeyValuePair<string, PendingWrite> kvp in InFlight.ToArray())
            {
                WaitForPendingWrites(kvp.Key, kvp.Value);
            }
        }

        private static void WaitForPendingWrites(string path, PendingWrite pending)
        {
            while (true)
            {
                Task? task;
                lock (pending)
                {
                    task = pending.WriteTask is { IsCompleted: false } ? pending.WriteTask : null;
                    if (task == null && pending.Latest != null)
                    {
                        FileWritePayload next = pending.Latest;
                        string feature = pending.LogFeature;
                        StartWriteTask(pending, path, next, feature);
                        task = pending.WriteTask is { IsCompleted: false } ? pending.WriteTask : null;
                    }
                }

                if (task == null)
                {
                    return;
                }

                WaitForTask(task);
            }
        }

        private static void WaitForTask(Task? task)
        {
            if (task == null || task.IsCompleted)
            {
                return;
            }

            try
            {
                if (!task.Wait(TimeSpan.FromSeconds(30)))
                {
                    ModLog.Warn("FileIO", "Background file write timed out after 30 seconds.");
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn("FileIO", $"Background file write wait failed: {ex.Message}");
            }
        }
    }
}

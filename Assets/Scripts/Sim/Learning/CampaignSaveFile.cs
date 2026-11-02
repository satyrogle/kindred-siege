using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Threading;

namespace KindredSiege.Sim.Learning
{
    public static partial class CampaignSave
    {
        private const string FileHeader = "kindred-siege-safe-file";
        public const string BackupLoadedNote = "The latest save could not be read. Your last good backup has been opened.";

        /// Windows local storage: flush a sibling temp, validate it, then replace with a backup.
        /// Failure before replacement leaves the existing save intact. No delete-then-move fallback.
        /// The caller owns its save path and when to save. Use TryReadFile for backup recovery.
        public static void WriteFile(string path, Campaign campaign)
        {
            WriteFileSnapshot(path, CaptureFile(campaign));
        }

        /// Capture on the campaign's owning thread. The returned value has no live campaign state.
        public static FileSnapshot CaptureFile(Campaign campaign)
        {
            if (campaign == null) throw new ArgumentNullException(nameof(campaign));
            return new FileSnapshot(Write(campaign), campaign.Tuning.Clone());
        }

        public sealed class FileSnapshot
        {
            internal readonly string Payload;
            internal readonly CampaignTuning Tuning;
            internal FileSnapshot(string payload, CampaignTuning tuning) { Payload = payload; Tuning = tuning; }
        }

        /// The same durable writer, accepting a previously captured immutable snapshot on any thread.
        public static void WriteFileSnapshot(string path, FileSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            path = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            // A persistent empty lock file avoids races caused by unlinking another writer's lock.
            using (var guard = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                string payload = snapshot.Payload;
                string sealedText = Seal(payload);
                string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    byte[] bytes = new UTF8Encoding(false, true).GetBytes(sealedText);
                    using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        stream.Write(bytes, 0, bytes.Length);
                        stream.Flush(true);
                    }
                    string written = File.ReadAllText(temp, Encoding.UTF8);
                    // Validate the exact bytes we produced. Replaying a live battle and decoding every
                    // historical event on every order is unnecessary; compatibility is checked on load.
                    if (written != sealedText || !Unseal(written, out _, out _))
                        throw new IOException("The new save failed validation; the previous save was kept.");

                    if (File.Exists(path))
                    {
                        string previous = File.ReadAllText(path, Encoding.UTF8);
                        if (FutureVersion(previous)) throw new IOException("This save belongs to a newer game version; it was not overwritten.");
                        bool sealedFile = previous.StartsWith(FileHeader + "\t", StringComparison.Ordinal);
                        bool readable, complete;
                        if (sealedFile)
                        {
                            // Our envelope attests the exact payload previously written. Do not replay
                            // old fights just to rotate its backup. Legacy files still get a full read.
                            readable = Unseal(previous, out _, out _);
                            complete = readable;
                        }
                        else
                        {
                            readable = TryRead(previous, snapshot.Tuning, out Campaign old, out _);
                            complete = readable && string.IsNullOrEmpty(old.LoadNote);
                        }
                        // A damaged primary must never displace a usable backup.
                        string backup = complete || (readable && !File.Exists(path + ".bak")) ? path + ".bak" : null;
                        File.Replace(temp, path, backup);
                    }
                    else File.Move(temp, path);
                }
                finally
                {
                    // Only this invocation's temp. Orphans from a process crash are never treated as committed saves.
                    if (File.Exists(temp)) File.Delete(temp);
                }
            }
        }

        /// One ordered disk writer. Only adjacent pending snapshots for the same path are coalesced;
        /// an in-flight write is always allowed to finish before the next one starts.
        public sealed class FileWriter : IDisposable
        {
            sealed class Pending
            {
                public string Path;
                public FileSnapshot Snapshot;
            }
            readonly object _gate = new object();
            readonly List<Pending> _pending = new List<Pending>();
            bool _running, _disposed;
            string _error;

            public void Submit(string path, FileSnapshot snapshot)
            {
                if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
                path = System.IO.Path.GetFullPath(path);
                lock (_gate)
                {
                    if (_disposed) throw new ObjectDisposedException(nameof(FileWriter));
                    var request = new Pending { Path = path, Snapshot = snapshot };
                    int last = _pending.Count - 1;
                    if (last >= 0 && string.Equals(_pending[last].Path, path, StringComparison.OrdinalIgnoreCase))
                        _pending[last] = request;
                    else _pending.Add(request);
                    if (_running) return;
                    _running = true;
                    ThreadPool.QueueUserWorkItem(_ => Drain());
                }
            }

            void Drain()
            {
                while (true)
                {
                    Pending next;
                    lock (_gate)
                    {
                        if (_pending.Count == 0)
                        {
                            _running = false;
                            Monitor.PulseAll(_gate);
                            return;
                        }
                        next = _pending[0]; _pending.RemoveAt(0);
                    }
                    try { WriteFileSnapshot(next.Path, next.Snapshot); }
                    catch (Exception e) { lock (_gate) _error = "Saving failed: " + e.Message; }
                }
            }

            // Call on load, pause and shutdown boundaries. Failures are reported on the caller's thread.
            public void Flush() { lock (_gate) while (_running) Monitor.Wait(_gate); }
            public string TakeError() { lock (_gate) { var error = _error; _error = null; return error; } }
            public void Dispose()
            {
                lock (_gate) { _disposed = true; while (_running) Monitor.Wait(_gate); }
            }
        }

        /// Does not rewrite either candidate. A newer-format primary is an explicit error, not a downgrade.
        public static bool TryReadFile(string path, CampaignTuning tuning, out Campaign campaign, out string failure)
        {
            campaign = null; failure = string.Empty;
            try
            {
                if (File.Exists(path))
                {
                    string text = File.ReadAllText(path, Encoding.UTF8);
                    if (FutureVersion(text)) return Fail("This save needs a newer game version.", out failure);
                    if (TryRead(text, tuning, out campaign, out failure)) return true;
                }
                else failure = "The save file is missing.";
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                failure = "The save could not be opened: " + e.Message;
            }
            string primaryFailure = failure;
            try
            {
                if (File.Exists(path + ".bak") && TryRead(File.ReadAllText(path + ".bak", Encoding.UTF8), tuning,
                    out campaign, out failure))
                {
                    campaign.LoadNote = BackupLoadedNote + (string.IsNullOrEmpty(campaign.LoadNote) ? "" : " " + campaign.LoadNote);
                    failure = string.Empty;
                    return true;
                }
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                failure = "The backup could not be opened: " + e.Message;
            }
            campaign = null;
            failure = primaryFailure + " No readable backup was found. " + failure;
            return false;
        }

        private static string Seal(string text) => FileHeader + "\t1\t" + Digest(text) + "\n" + text;

        private static bool Unseal(string text, out string payload, out string failure)
        {
            payload = text; failure = string.Empty;
            if (text == null || !text.StartsWith(FileHeader, StringComparison.Ordinal)) return true;
            int end = text.IndexOf('\n');
            if (end < 0) return Fail("The save file header is incomplete.", out failure);
            string[] fields = text.Substring(0, end).TrimEnd('\r').Split('\t');
            if (fields.Length != 3 || fields[0] != FileHeader || fields[1] != "1")
                return Fail("Unsupported safe save file format.", out failure);
            payload = text.Substring(end + 1);
            if (fields[2] != Digest(payload)) return Fail("The save file checksum does not match.", out failure);
            return true;
        }

        private static bool FutureVersion(string text)
        {
            // Inspect the declared format even if its checksum is damaged; never downgrade it automatically.
            if (text.StartsWith(FileHeader, StringComparison.Ordinal))
            {
                int end = text.IndexOf('\n');
                if (end < 0) return false;
                var envelope = text.Substring(0, end).TrimEnd('\r').Split('\t');
                if (envelope.Length > 1 && int.TryParse(envelope[1], out int fileVersion) && fileVersion > 1) return true;
                text = text.Substring(end + 1);
            }
            var header = text.Split('\n')[0].TrimEnd('\r').Split('\t');
            return header.Length == 2 && header[0] == Header && int.TryParse(header[1], out int version) && version > Version;
        }

        private static string Digest(string text)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "");
        }
    }
}

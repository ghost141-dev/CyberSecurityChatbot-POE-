// ============================================================
// AudioHelper.cs
// ------------------------------------------------------------
// Manages audio playback for the chatbot's voice greeting.
//
// Design decisions
// ----------------
// 1. Background thread — WPF's UI thread must never be blocked.
//    SoundPlayer.PlaySync() blocks until the WAV finishes, so it
//    runs on a ThreadPool thread. The UI remains responsive.
//
// 2. Failure tolerance — audio is a bonus feature. Any exception
//    (missing file, corrupt WAV, device error) is silently caught
//    so the application continues normally without showing an error.
//
// 3. File search order:
//    a. Same directory as the .exe  (AppDomain.CurrentDomain.BaseDirectory)
//    b. Current working directory   (plain filename as fallback)
//    If neither path exists, playback is skipped.
//
// 4. IDisposable — SoundPlayer implements IDisposable, so it is
//    wrapped in a using block to release the file handle immediately
//    after playback, preventing file-lock issues.
// ============================================================

using System;
using System.IO;
using System.Media;
using System.Threading;

namespace CybersecurityChatbotWPF
{
    /// <summary>
    /// Plays the WAV greeting file on a background thread.
    /// Failure-tolerant: a missing or unplayable file is ignored silently.
    /// </summary>
    public class AudioHelper
    {
        // ── Constants ─────────────────────────────────────────────────

        /// <summary>
        /// File name of the WAV greeting, relative to the exe directory.
        /// Change this constant if the file is renamed.
        /// </summary>
        private const string WavFileName = "greeting.wav";

        // ── Public methods ────────────────────────────────────────────

        /// <summary>
        /// Queues WAV playback on a ThreadPool background thread.
        /// Returns immediately so the calling UI thread is never blocked.
        /// </summary>
        public void PlayGreetingAsync()
        {
            // QueueUserWorkItem submits work to the .NET thread pool.
            // The underscore parameter (_) is the optional state object — unused here.
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    // ── Step 1: Resolve the WAV path ──────────────────
                    // Check next to the executable first (the most reliable location
                    // after publishing / running the built exe).
                    string exeDir  = AppDomain.CurrentDomain.BaseDirectory;
                    string wavPath = Path.Combine(exeDir, WavFileName);

                    // If not found beside the exe, fall back to the current directory
                    // (useful when running with 'dotnet run' from the project folder).
                    if (!File.Exists(wavPath))
                        wavPath = WavFileName;

                    // ── Step 2: Play if the file exists ───────────────
                    if (File.Exists(wavPath))
                    {
                        // 'using' ensures SoundPlayer.Dispose() is called after
                        // playback, releasing the file handle.
                        using (SoundPlayer player = new SoundPlayer(wavPath))
                        {
                            // PlaySync blocks this background thread (not the UI thread)
                            // until the audio finishes — clean and simple.
                            player.PlaySync();
                        }
                    }
                    // If the file still isn't found, we do nothing.
                    // WPF apps should not write to the console — the voice greeting
                    // is a bonus feature, not a critical requirement.
                }
                catch
                {
                    // Catch-all: audio failure must never crash the chatbot.
                    // Possible exceptions: FileNotFoundException, InvalidOperationException,
                    // UnauthorizedAccessException — all safely swallowed here.
                }
            });
        }
    }
}

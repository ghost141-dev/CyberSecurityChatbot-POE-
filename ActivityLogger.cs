// ============================================================
// ActivityLogger.cs
// ------------------------------------------------------------
// Records everything the chatbot does (tasks added, reminders set,
// quiz activity, NLP-interpreted commands) and exposes a recent
// summary for the "Show activity log" feature.
//
// Design notes
// ------------
// - Backed by a List<ActivityLogEntry> (generic collection, per brief).
// - Newest entries are added at the END of the list; GetRecent()
//   reads from the end backwards so the most recent action is shown first.
// - Default view caps at 8 entries (within the brief's 5–10 range).
//   GetAll() supports the optional "Show more" feature.
// ============================================================

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CybersecurityChatbotWPF
{
    /// <summary>
    /// In-memory activity log for the current session.
    /// Thread-unsafe by design — all calls happen on the WPF UI thread.
    /// </summary>
    public class ActivityLogger
    {
        /// <summary>
        /// Default number of entries shown by GetRecentFormatted()
        /// before the user requests "show more". Kept within the
        /// brief's required 5–10 range.
        /// </summary>
        private const int DefaultDisplayCount = 8;

        /// <summary>The full chronological list of logged actions.</summary>
        private readonly List<ActivityLogEntry> _entries = new List<ActivityLogEntry>();

        /// <summary>Total number of entries logged this session.</summary>
        public int Count => _entries.Count;

        /// <summary>
        /// Adds a new entry to the log. Called by the Task Assistant,
        /// Quiz, and NLP handlers whenever a significant action occurs.
        /// </summary>
        public void Log(string description, string category)
        {
            _entries.Add(new ActivityLogEntry(description, category));
        }

        /// <summary>
        /// Returns the most recent entries, newest first, up to `count`.
        /// </summary>
        public List<ActivityLogEntry> GetRecent(int count = DefaultDisplayCount)
        {
            return _entries
                .AsEnumerable()
                .Reverse()
                .Take(count)
                .ToList();
        }

        /// <summary>Returns every entry ever logged this session, newest first.</summary>
        public List<ActivityLogEntry> GetAll()
        {
            return _entries.AsEnumerable().Reverse().ToList();
        }

        /// <summary>
        /// Builds a chat-friendly formatted string of the most recent entries,
        /// matching the example format in the brief:
        ///   "Here's a summary of recent actions:
        ///    1. Task added: '...'
        ///    2. Quiz started..."
        /// </summary>
        /// <param name="showAll">
        /// If true, includes every entry (the "Show more" option);
        /// otherwise limited to DefaultDisplayCount.
        /// </param>
        public string GetFormattedSummary(bool showAll = false)
        {
            if (_entries.Count == 0)
                return "No actions have been logged yet this session.";

            List<ActivityLogEntry> toShow = showAll ? GetAll() : GetRecent();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Here's a summary of recent actions:");
            sb.AppendLine();

            int number = 1;
            foreach (ActivityLogEntry entry in toShow)
            {
                sb.AppendLine($"{number}. {entry}");
                number++;
            }

            if (!showAll && _entries.Count > DefaultDisplayCount)
                sb.AppendLine("\nType 'show more' to see the full history.");

            return sb.ToString().Trim();
        }
    }
}

// ============================================================
// ActivityLogEntry.cs
// ------------------------------------------------------------
// A single recorded action in the chatbot's activity log
// (e.g. "Task added", "Quiz started", "Reminder set").
// ============================================================

using System;

namespace CybersecurityChatbotWPF
{
    /// <summary>
    /// Represents one entry in the activity log: what happened and when.
    /// </summary>
    public class ActivityLogEntry
    {
        /// <summary>When the action occurred (local time).</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>Short human-readable description, e.g. "Task added: 'Enable 2FA'".</summary>
        public string Description { get; set; }

        /// <summary>
        /// Broad category of the action: "Task", "Reminder", "Quiz", "NLP".
        /// Lets the UI group or icon-decorate entries later if desired.
        /// </summary>
        public string Category { get; set; }

        public ActivityLogEntry(string description, string category)
        {
            Timestamp   = DateTime.Now;
            Description = description;
            Category    = category;
        }

        /// <summary>
        /// Formats the entry for display: "[14:32] Task added: 'Enable 2FA'".
        /// </summary>
        public override string ToString() => $"[{Timestamp:HH:mm}] {Description}";
    }
}

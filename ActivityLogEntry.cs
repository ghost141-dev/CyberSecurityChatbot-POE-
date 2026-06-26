// ============================================================
// ActivityLogEntry.cs
// ------------------------------------------------------------
// A single recorded action in the chatbot's activity log
// (e.g. "Task added", "Quiz started", "Reminder set").
// ============================================================

using System;

namespace CybersecurityChatbotWPF
{

    // Represents one entry in the activity log: what happened and when.

    public class ActivityLogEntry
    {
        // When the action occurred (local time).
        public DateTime Timestamp { get; set; }

        //Short human-readable description, e.g. "Task added: 'Enable 2FA'".
        public string Description { get; set; }

        // Broad category of the action: "Task", "Reminder", "Quiz", "NLP".
        // Lets the UI group or icon-decorate entries later if desired.
        public string Category { get; set; }

        public ActivityLogEntry(string description, string category)
        {
            Timestamp   = DateTime.Now;
            Description = description;
            Category    = category;
        }


        // Formats the entry for display: "[14:32] Task added: 'Enable 2FA'".
        // </summary>
        public override string ToString() => $"[{Timestamp:HH:mm}] {Description}";
    }
}

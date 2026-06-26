// ============================================================
// TaskItem.cs
// ------------------------------------------------------------
// Represents a single cybersecurity task the user is tracking
// (e.g. "Enable two-factor authentication"), as required by
// Part 3 Task 1. Mirrors columns in the MySQL `tasks` table —
// DatabaseHelper maps rows to/from this model.
// ============================================================

using System;

namespace CybersecurityChatbotWPF
{

    // A cybersecurity task with an optional reminder, persisted to MySQL.

    public class TaskItem
    {
        // Primary key from the database. 0 for a task not yet saved.
        public int Id { get; set; }

        // Short task title, e.g. "Enable two-factor authentication".
        public string Title { get; set; }

        // Longer description shown in the task list.
        public string Description { get; set; }

        // The date the reminder should fire, or null if the user
        // did not request a reminder for this task.
        public DateTime? ReminderDate { get; set; }

        // True once the user has marked this task as done.
        public bool IsCompleted { get; set; }

        /// <summary>When the task was first created.</summary>
        public DateTime CreatedAt { get; set; }

        public TaskItem() { }

        public TaskItem(string title, string description, DateTime? reminderDate = null)
        {
            Title        = title;
            Description  = description;
            ReminderDate = reminderDate;
            IsCompleted  = false;
            CreatedAt    = DateTime.Now;
        }

        // Human-friendly one-line summary, used in chat replies and logs.
        // e.g. "Enable two-factor authentication (Reminder: 2026-07-01)"
        public override string ToString()
        {
            string reminderPart = ReminderDate.HasValue
                ? $" (Reminder: {ReminderDate.Value:dd MMM yyyy})"
                : " (no reminder set)";
            string status = IsCompleted ? "[Done] " : "";
            return $"{status}{Title}{reminderPart}";
        }
    }
}

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
    /// <summary>
    /// A cybersecurity task with an optional reminder, persisted to MySQL.
    /// </summary>
    public class TaskItem
    {
        /// <summary>
        /// Primary key from the database. 0 for a task not yet saved.
        /// </summary>
        public int Id { get; set; }

        /// <summary>Short task title, e.g. "Enable two-factor authentication".</summary>
        public string Title { get; set; }

        /// <summary>Longer description shown in the task list.</summary>
        public string Description { get; set; }

        /// <summary>
        /// The date the reminder should fire, or null if the user
        /// did not request a reminder for this task.
        /// </summary>
        public DateTime? ReminderDate { get; set; }

        /// <summary>True once the user has marked this task as done.</summary>
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

        /// <summary>
        /// Human-friendly one-line summary, used in chat replies and logs.
        /// e.g. "Enable two-factor authentication (Reminder: 2026-07-01)"
        /// </summary>
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

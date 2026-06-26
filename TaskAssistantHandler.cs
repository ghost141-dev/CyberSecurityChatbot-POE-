// ============================================================
// TaskAssistantHandler.cs
// ------------------------------------------------------------
// Orchestrates the Task Assistant feature: takes high-level
// requests (from either the GUI panel or NLP-detected chat intents),
// talks to DatabaseHelper for persistence, and returns chat-style
// confirmation text matching the brief's example interactions.
//
// This class deliberately knows nothing about WPF controls or the
// chat textbox — it only deals with TaskItem/DatabaseHelper and
// returns plain strings. MainWindow decides where those strings go
// (chat bubble, panel refresh, etc.). Same separation-of-concerns
// pattern as ChatbotEngine -> ResponseEngine from Part 2.
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CybersecurityChatbotWPF
{
    /// <summary>
    /// Coordinates task creation, reminders, completion, and deletion,
    /// logging every significant action to the ActivityLogger.
    /// </summary>
    public class TaskAssistantHandler
    {
        private readonly DatabaseHelper _db;
        private readonly ActivityLogger _logger;
        private readonly NlpHelper _nlp;

        public TaskAssistantHandler(DatabaseHelper db, ActivityLogger logger, NlpHelper nlp)
        {
            _db     = db;
            _logger = logger;
            _nlp    = nlp;
        }

        /// <summary>
        /// Adds a new task with an optional reminder date, persists it,
        /// logs the action, and returns a chat-style confirmation.
        /// </summary>
        public async Task<(bool Success, string Message, TaskItem CreatedTask)> AddTaskAsync(
            string title, string description, DateTime? reminderDate)
        {
            if (string.IsNullOrWhiteSpace(title))
                return (false, "I need a short title for the task before I can add it.", null);

            if (!_db.IsAvailable)
                return (false, "I can't reach the task database right now, so I can't save that task. Please check your MySQL connection.", null);

            try
            {
                var task = new TaskItem(title.Trim(), string.IsNullOrWhiteSpace(description) ? title.Trim() : description.Trim(), reminderDate);
                task = await _db.AddTaskAsync(task);

                string reminderPart = reminderDate.HasValue
                    ? $" I'll remind you on {reminderDate.Value:dd MMM yyyy}."
                    : " Would you like a reminder for this task?";

                _logger.Log($"Task added: '{task.Title}'" + (reminderDate.HasValue ? $" (Reminder set for {reminderDate.Value:dd MMM yyyy})" : " (no reminder set)"), "Task");

                return (true, $"Task added: \"{task.Description}\".{reminderPart}", task);
            }
            catch (DatabaseException ex)
            {
                return (false, $"Something went wrong saving that task: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Sets or updates the reminder date on an existing task by Id.
        /// </summary>
        public async Task<(bool Success, string Message)> SetReminderAsync(int taskId, DateTime reminderDate, string taskTitle)
        {
            if (!_db.IsAvailable)
                return (false, "I can't reach the task database right now to set that reminder.");

            try
            {
                await _db.SetReminderAsync(taskId, reminderDate);
                _logger.Log($"Reminder set: '{taskTitle}' on {reminderDate:dd MMM yyyy}", "Reminder");
                return (true, $"Got it! I'll remind you about \"{taskTitle}\" on {reminderDate:dd MMM yyyy}.");
            }
            catch (DatabaseException ex)
            {
                return (false, $"I couldn't set that reminder: {ex.Message}");
            }
        }

        /// <summary>
        /// Convenience overload: parses a natural-language timeframe
        /// (e.g. "in 3 days") via NlpHelper, then sets the reminder.
        /// Used when the request came from chat rather than the GUI panel.
        /// </summary>
        public async Task<(bool Success, string Message)> SetReminderFromPhraseAsync(int taskId, string taskTitle, string timeframePhrase)
        {
            TimeSpan? offset = _nlp.ExtractTimeframe(timeframePhrase);
            DateTime reminderDate = DateTime.Now.Add(offset ?? TimeSpan.FromDays(3)); // default: 3 days if unclear
            return await SetReminderAsync(taskId, reminderDate, taskTitle);
        }

        /// <summary>
        /// Retrieves all tasks for display in the Task Assistant panel.
        /// </summary>
        public async Task<List<TaskItem>> GetAllTasksAsync()
        {
            if (!_db.IsAvailable) return new List<TaskItem>();
            try
            {
                return await _db.GetAllTasksAsync();
            }
            catch (DatabaseException)
            {
                return new List<TaskItem>();
            }
        }

        /// <summary>
        /// Marks a task complete and logs the action.
        /// </summary>
        public async Task<(bool Success, string Message)> CompleteTaskAsync(int taskId, string taskTitle)
        {
            if (!_db.IsAvailable)
                return (false, "I can't reach the task database right now.");

            try
            {
                await _db.MarkCompletedAsync(taskId, true);
                _logger.Log($"Task completed: '{taskTitle}'", "Task");
                return (true, $"Nice work! \"{taskTitle}\" is marked as complete.");
            }
            catch (DatabaseException ex)
            {
                return (false, $"I couldn't update that task: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a task and logs the action.
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteTaskAsync(int taskId, string taskTitle)
        {
            if (!_db.IsAvailable)
                return (false, "I can't reach the task database right now.");

            try
            {
                await _db.DeleteTaskAsync(taskId);
                _logger.Log($"Task deleted: '{taskTitle}'", "Task");
                return (true, $"Done — I've removed \"{taskTitle}\" from your task list.");
            }
            catch (DatabaseException ex)
            {
                return (false, $"I couldn't delete that task: {ex.Message}");
            }
        }

        /// <summary>
        /// Finds the best-matching task by a (possibly partial) title,
        /// used when NLP detects a complete/delete intent from free text
        /// like "mark task password as complete". Case-insensitive
        /// substring match against current titles.
        /// </summary>
        public async Task<TaskItem> FindTaskByTitleFragmentAsync(string fragment)
        {
            if (string.IsNullOrWhiteSpace(fragment)) return null;
            List<TaskItem> tasks = await GetAllTasksAsync();
            return tasks.FirstOrDefault(t =>
                t.Title.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// Formats the full task list for a chat-style "view tasks" reply.
        /// </summary>
        public string FormatTaskListForChat(List<TaskItem> tasks)
        {
            if (tasks == null || tasks.Count == 0)
                return "You don't have any cybersecurity tasks yet. Try saying \"add a task to enable 2FA\" to get started.";

            var lines = tasks.Select((t, i) => $"{i + 1}. {t}");
            return "Here are your current tasks:\n\n" + string.Join("\n", lines);
        }
    }
}

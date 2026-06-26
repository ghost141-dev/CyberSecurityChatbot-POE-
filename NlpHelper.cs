// ============================================================
// NlpHelper.cs
// ------------------------------------------------------------
// Simulates Natural Language Processing using basic string
// manipulation, as the brief explicitly directs:
//   "Use simple string manipulation methods (such as string.Contains()
//    in C#) to detect keywords and understand the user's intent."
//
// Strategy
// --------
// Rather than one fixed phrase per intent, each intent is matched
// against a LIST of trigger phrases/keywords (a generic collection,
// consistent with the rest of the project). This lets the bot
// recognise meaningfully different phrasings of the same request,
// e.g. all of these map to AddTask:
//   "add a task to enable 2FA"
//   "can you remind me to update my password"   (-> SetReminder, see below)
//   "I need to add a task"
//   "create a task for reviewing privacy settings"
//
// Reminders vs tasks: the brief's own example treats "remind me to X"
// as functionally a task+reminder shortcut ("Reminder set for 'Update
// my password' on tomorrow's date."). So SetReminder is detected
// separately, and TaskAssistantHandler treats it as "add a task whose
// title is X, with a reminder already attached".
//
// Detail extraction uses simple substring slicing after the matched
// trigger phrase — deliberately simple, per the brief's guidance that
// this is a *simulation*, not a real NLP/ML pipeline.
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace CybersecurityChatbotWPF
{

    // Detects user intent from free-text chat input using keyword and
    // phrase matching. Mirrors the "simple string manipulation" approach
    // the brief explicitly asks for.
    // </summary>
    public class NlpHelper
    {
        // ── Trigger phrase banks ─────────────────────────────────────────
        // Lists, not single strings, so multiple phrasings are recognised.
        // Ordered roughly from most-specific to least-specific within each list.

        private readonly List<string> _addTaskTriggers = new List<string>
        {
            "add a task", "add task", "create a task", "new task",
            "add a to-do", "add to do", "i need to add a task"
        };

        private readonly List<string> _reminderTriggers = new List<string>
        {
            "remind me to", "remind me about", "set a reminder", "set reminder",
            "can you remind me"
        };

        private readonly List<string> _viewTasksTriggers = new List<string>
        {
            "show my tasks", "view tasks", "list my tasks", "what are my tasks",
            "show tasks", "see my tasks", "my task list"
        };

        private readonly List<string> _completeTaskTriggers = new List<string>
        {
            "mark task", "mark as complete", "complete task", "i finished",
            "i've done", "i have done", "mark complete"
        };

        private readonly List<string> _deleteTaskTriggers = new List<string>
        {
            "delete task", "remove task", "delete the task", "cancel task"
        };

        private readonly List<string> _quizTriggers = new List<string>
        {
            "start quiz", "start the quiz", "play quiz", "take the quiz",
            "quiz me", "begin quiz", "let's do the quiz"
        };

        private readonly List<string> _activityLogTriggers = new List<string>
        {
            "show activity log", "activity log", "what have you done for me",
            "show me the log", "show log", "recent actions"
        };

        private readonly List<string> _showMoreTriggers = new List<string>
        {
            "show more", "see more", "full history", "show all"
        };

        // ── Public API ────────────────────────────────────────────────────

        // Analyses the user's input and returns the best-matching intent,
        // along with any extracted detail text. Returns NlpResult.NoMatch
        // if nothing recognisable was found — the caller should then fall
        // back to ResponseEngine's existing keyword/sentiment handling.
        public NlpResult Detect(string rawInput)
        {
            if (string.IsNullOrWhiteSpace(rawInput)) return NlpResult.NoMatch;

            string input = rawInput.Trim().ToLower();

            // Order matters: check the most specific/contextual intents
            // before the more generic ones, so e.g. "remind me to update
            // my password" is captured as SetReminder before AddTask logic
            // would otherwise need to disambiguate.

            if (ContainsAny(input, _showMoreTriggers))
                return new NlpResult(NlpIntent.ShowMoreActivityLog);

            if (ContainsAny(input, _activityLogTriggers))
                return new NlpResult(NlpIntent.ShowActivityLog);

            if (ContainsAny(input, _quizTriggers))
                return new NlpResult(NlpIntent.StartQuiz);

            if (ContainsAny(input, _completeTaskTriggers))
                return new NlpResult(NlpIntent.CompleteTask, ExtractDetail(input, _completeTaskTriggers));

            if (ContainsAny(input, _deleteTaskTriggers))
                return new NlpResult(NlpIntent.DeleteTask, ExtractDetail(input, _deleteTaskTriggers));

            if (ContainsAny(input, _viewTasksTriggers))
                return new NlpResult(NlpIntent.ViewTasks);

            // "remind me to update my password" -> SetReminder, detail = "update my password"
            if (ContainsAny(input, _reminderTriggers))
                return new NlpResult(NlpIntent.SetReminder, ExtractDetail(input, _reminderTriggers));

            // "add a task to enable 2FA" -> AddTask, detail = "enable 2FA"
            if (ContainsAny(input, _addTaskTriggers))
                return new NlpResult(NlpIntent.AddTask, ExtractDetail(input, _addTaskTriggers));

            return NlpResult.NoMatch;
        }

        // ── Private helpers ───────────────────────────────────────────────

        // True if the input contains ANY phrase from the supplied trigger list.
        // This is the "simple string manipulation" (string.Contains) the
        // brief explicitly calls for.
        private bool ContainsAny(string input, List<string> triggers)
        {
            return triggers.Any(trigger => input.Contains(trigger));
        }

        // Extracts the free-text detail following whichever trigger phrase
        // matched, then strips common filler words/punctuation so the
        // remaining text reads as a clean task title.
        // Example: "add a task to enable 2fa" -> "enable 2fa"
        //          "remind me to update my password tomorrow" -> "update my password tomorrow"
        private string ExtractDetail(string input, List<string> triggers)
        {
            // Find which trigger actually matched and where
            string matchedTrigger = triggers.FirstOrDefault(t => input.Contains(t));
            if (matchedTrigger == null) return string.Empty;

            int idx = input.IndexOf(matchedTrigger, StringComparison.OrdinalIgnoreCase);
            string remainder = input.Substring(idx + matchedTrigger.Length).Trim();

            // Strip a leading filler word like "to" that often follows the trigger
            // e.g. "add a task" + " to enable 2fa" -> "enable 2fa"
            string[] fillerStarts = { "to ", "for ", "about ", "- " };
            foreach (string filler in fillerStarts)
            {
                if (remainder.StartsWith(filler, StringComparison.OrdinalIgnoreCase))
                {
                    remainder = remainder.Substring(filler.Length).Trim();
                    break;
                }
            }

            // Strip trailing punctuation
            remainder = remainder.TrimEnd('.', '!', '?').Trim();

            // Capitalise the first letter for a cleaner task title
            if (remainder.Length > 0)
                remainder = char.ToUpper(remainder[0]) + remainder.Substring(1);

            return remainder;
        }

        // Attempts to pull a simple timeframe phrase out of free text,
        // e.g. "in 3 days", "tomorrow", "in a week". Used by the Task
        // Assistant to set a reminder date from natural phrasing.
        // Returns null if no recognisable timeframe is found.
        public TimeSpan? ExtractTimeframe(string input)
        {
            input = input.ToLower();

            if (input.Contains("tomorrow")) return TimeSpan.FromDays(1);
            if (input.Contains("today")) return TimeSpan.FromDays(0);
            if (input.Contains("next week") || input.Contains("a week")) return TimeSpan.FromDays(7);

            // Pattern: "in N day(s)"
            var match = System.Text.RegularExpressions.Regex.Match(input, @"in (\d+)\s*day");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int days))
                return TimeSpan.FromDays(days);

            return null;
        }
    }
}

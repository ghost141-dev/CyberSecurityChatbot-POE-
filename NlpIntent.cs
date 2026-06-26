// ============================================================
// NlpIntent.cs
// ------------------------------------------------------------
// Defines the set of "intents" the NLP simulation can recognise,
// and a small result DTO carrying the intent plus any extracted
// details (e.g. the task description pulled out of free text).
// ============================================================

namespace CybersecurityChatbotWPF
{

    // The set of user intents the simulated NLP layer can detect.
    // "None" means no recognised intent — falls through to the
    // existing keyword/sentiment/fallback handling in ResponseEngine.
    public enum NlpIntent
    {
        None,
        AddTask,
        SetReminder,
        ViewTasks,
        CompleteTask,
        DeleteTask,
        StartQuiz,
        ShowActivityLog,
        ShowMoreActivityLog
    }

    // Result of running NlpHelper.Detect() on a user message:
    // which intent was recognised, plus any free-text detail
    // extracted from the sentence (e.g. the task title).
    public class NlpResult
    {
        public NlpIntent Intent { get; set; }

        // Free-text detail extracted from the message — e.g. for
        // AddTask, this is the task description; for SetReminder,
        // the timeframe phrase (e.g. "in 3 days", "tomorrow").
        public string Detail { get; set; }

        public NlpResult(NlpIntent intent, string detail = null)
        {
            Intent = intent;
            Detail = detail;
        }

        public static NlpResult NoMatch => new NlpResult(NlpIntent.None);
    }
}

// ============================================================
// QuizQuestion.cs
// ------------------------------------------------------------
// Represents a single question in the cybersecurity quiz.
//
// Supports two question types via the same shape:
//   - Multiple choice : Options has 4 entries, CorrectIndex 0-3.
//   - True/False       : Options has 2 entries ("True","False").
//
// Keeping both types on one model (rather than two subclasses)
// keeps QuizEngine simple — it can treat every question identically
// when picking, displaying, and marking answers.
// ============================================================

using System.Collections.Generic;

namespace CybersecurityChatbotWPF
{

    // A single quiz question with its answer options, the correct
    // answer, and an explanation shown after the user answers.
    public class QuizQuestion
    {
        // The question text shown to the user.
        public string Text { get; set; }    


        // The list of selectable answers.
        // 4 entries for multiple-choice, 2 ("True"/"False") for T/F.
        public List<string> Options { get; set; }

        // Zero-based index into Options that is the correct answer.
        public int CorrectIndex { get; set; }

        // A short explanation shown after the user answers, reinforcing
        // the cybersecurity concept regardless of whether they were right.
        public string Explanation { get; set; }

        // The broad cybersecurity topic this question belongs to
        // (e.g. "phishing", "password"). Useful for activity logging
        // and for varying feedback by topic.
        public string Topic { get; set; }

        public QuizQuestion(string text, List<string> options, int correctIndex,
                             string explanation, string topic)
        {
            Text          = text;
            Options       = options;
            CorrectIndex  = correctIndex;
            Explanation   = explanation;
            Topic         = topic;
        }

        // Returns true if the supplied option index matches the correct answer.
        public bool IsCorrect(int selectedIndex) => selectedIndex == CorrectIndex;
    }
}   

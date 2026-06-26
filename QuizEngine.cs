// ============================================================
// QuizEngine.cs
// ------------------------------------------------------------
// Owns the cybersecurity quiz: the question bank, the current
// position within a quiz attempt, and the running score.
//
// Design notes
// ------------
// - Uses the SAME random-selection idea as ResponseEngine (shuffle
//   on StartNewQuiz so question order varies between attempts),
//   keeping with the "use lists/dictionaries" instruction in the brief.
// - Holds 12 questions (brief requires "more than 10"), mixing
//   multiple-choice and true/false as required.
// - Exposes a small, deliberate state machine:
//      StartNewQuiz()   -> shuffles, resets score/index
//      GetCurrentQuestion() -> the question to display now
//      SubmitAnswer(i)  -> marks right/wrong, advances index, returns feedback
//      IsFinished       -> true once all questions have been answered
//      ScoreSummary()   -> final score + a tiered feedback message
// MainWindow only talks to this class; it never touches the
// question list directly. This mirrors the ChatbotEngine/ResponseEngine
// split already used elsewhere in the project.
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace CybersecurityChatbotWPF
{
    /// <summary>
    /// Manages a single quiz attempt: question order, current question,
    /// score tracking, and feedback messages.
    /// </summary>
    public class QuizEngine
    {
        // ── Private fields ─────────────────────────────────────────────

        private readonly Random _random = new Random();

        /// <summary>
        /// The full question bank. Built once in the constructor.
        /// 12 questions covering phishing, password safety, safe browsing,
        /// social engineering, malware, 2FA, scams, and mobile security.
        /// </summary>
        private readonly List<QuizQuestion> _questionBank;

        /// <summary>
        /// The shuffled question order for the CURRENT attempt.
        /// Re-created every time StartNewQuiz() is called.
        /// </summary>
        private List<QuizQuestion> _activeQuestions;

        // ── Public session state ───────────────────────────────────────

        /// <summary>Index of the question currently being shown (0-based).</summary>
        public int CurrentIndex { get; private set; }

        /// <summary>Number of questions answered correctly so far.</summary>
        public int Score { get; private set; }

        /// <summary>Total number of questions in this attempt.</summary>
        public int TotalQuestions => _activeQuestions?.Count ?? 0;

        /// <summary>True once every question has been answered.</summary>
        public bool IsFinished => _activeQuestions != null && CurrentIndex >= _activeQuestions.Count;

        /// <summary>True once StartNewQuiz() has been called at least once.</summary>
        public bool HasStarted => _activeQuestions != null;

        // ── Constructor ────────────────────────────────────────────────

        public QuizEngine()
        {
            _questionBank = BuildQuestionBank();
        }

        // ── Public methods ──────────────────────────────────────────────

        /// <summary>
        /// Begins a fresh quiz attempt: shuffles the question bank order,
        /// resets the score and index to zero.
        /// </summary>
        public void StartNewQuiz()
        {
            // OrderBy with a random key is a simple, idiomatic LINQ shuffle.
            _activeQuestions = _questionBank.OrderBy(_ => _random.Next()).ToList();
            CurrentIndex = 0;
            Score = 0;
        }

        /// <summary>
        /// Returns the question currently due to be displayed, or null
        /// if the quiz has not started or has already finished.
        /// </summary>
        public QuizQuestion GetCurrentQuestion()
        {
            if (_activeQuestions == null || IsFinished) return null;
            return _activeQuestions[CurrentIndex];
        }

        /// <summary>
        /// Records the user's answer for the current question, updates the
        /// score, and advances to the next question.
        /// </summary>
        /// <param name="selectedIndex">The option index the user chose.</param>
        /// <returns>
        /// A tuple: (wasCorrect, explanation) so the GUI can show immediate
        /// feedback before moving on to the next question.
        /// </returns>
        public (bool WasCorrect, string Explanation) SubmitAnswer(int selectedIndex)
        {
            QuizQuestion current = GetCurrentQuestion();
            if (current == null) return (false, string.Empty);

            bool correct = current.IsCorrect(selectedIndex);
            if (correct) Score++;

            CurrentIndex++;  // Advance regardless of right/wrong — one attempt per question

            return (correct, current.Explanation);
        }

        /// <summary>
        /// Builds a tiered feedback message based on the final score,
        /// as required by the brief ("Great job!" vs "Keep learning").
        /// </summary>
        public string GetScoreSummary()
        {
            double percentage = TotalQuestions == 0 ? 0 : (double)Score / TotalQuestions * 100;

            string tierMessage;
            if (percentage >= 80)
                tierMessage = "Great job! You're a cybersecurity pro! 🛡️";
            else if (percentage >= 50)
                tierMessage = "Good effort! Keep learning to stay even safer online. 📚";
            else
                tierMessage = "Keep learning to stay safe online — review the tips and try again! 💡";

            return $"Quiz complete! You scored {Score}/{TotalQuestions} ({percentage:F0}%).\n\n{tierMessage}";
        }

        // ── Question bank ───────────────────────────────────────────────

        /// <summary>
        /// Builds the fixed bank of 12 cybersecurity questions.
        /// Mix of multiple-choice (4 options) and true/false (2 options),
        /// matching the brief's requirement for variety.
        /// </summary>
        private List<QuizQuestion> BuildQuestionBank()
        {
            return new List<QuizQuestion>
            {
                new QuizQuestion(
                    "What should you do if you receive an email asking for your password?",
                    new List<string> { "Reply with your password", "Delete the email", "Report the email as phishing", "Ignore it" },
                    2,
                    "Correct! Reporting phishing emails helps prevent scams and protects others too.",
                    "phishing"),

                new QuizQuestion(
                    "True or False: It is safe to reuse the same password across multiple accounts.",
                    new List<string> { "True", "False" },
                    1,
                    "False. Reusing passwords means one breach can compromise all your accounts.",
                    "password"),

                new QuizQuestion(
                    "Which of these is the strongest password?",
                    new List<string> { "password123", "MyDog2020", "Tr@il#Sunset!92Kite", "12345678" },
                    2,
                    "Long, random passphrases with mixed characters are far harder to crack than short or predictable ones.",
                    "password"),

                new QuizQuestion(
                    "True or False: Two-factor authentication (2FA) can block most automated account attacks.",
                    new List<string> { "True", "False" },
                    0,
                    "True. 2FA blocks over 99% of automated attacks even if your password is stolen.",
                    "2fa"),

                new QuizQuestion(
                    "What is a sign that a website might not be safe to enter your details into?",
                    new List<string> { "It uses HTTPS", "It has a padlock icon", "It lacks HTTPS and has a misspelled domain", "It loads quickly" },
                    2,
                    "Missing HTTPS and odd, misspelled domains (typosquatting) are red flags for unsafe sites.",
                    "browsing"),

                new QuizQuestion(
                    "True or False: Public Wi-Fi is just as safe as your home network for online banking.",
                    new List<string> { "True", "False" },
                    1,
                    "False. Public Wi-Fi can expose your data to attackers; use a VPN or avoid sensitive logins on it.",
                    "browsing"),

                new QuizQuestion(
                    "Social engineering attacks primarily exploit:",
                    new List<string> { "Software bugs", "Human psychology and trust", "Hardware failures", "Network bandwidth" },
                    1,
                    "Social engineering manipulates people directly — through urgency, authority, or trust — rather than exploiting code.",
                    "social"),

                new QuizQuestion(
                    "True or False: Ransomware encrypts your files and demands payment to unlock them.",
                    new List<string> { "True", "False" },
                    0,
                    "True. Regular backups are one of the best defences against ransomware.",
                    "malware"),

                new QuizQuestion(
                    "Which of these is recommended for setting up 2FA?",
                    new List<string> { "Sharing your code with a friend", "Using an authenticator app", "Disabling it for convenience", "Using your birth date as the code" },
                    1,
                    "Authenticator apps (e.g. Google Authenticator) are more secure than SMS-based codes.",
                    "2fa"),

                new QuizQuestion(
                    "True or False: If a deal or prize offer seems too good to be true, it usually is a scam.",
                    new List<string> { "True", "False" },
                    0,
                    "True. Unrealistic offers are a classic scam red flag — always verify independently.",
                    "scam"),

                new QuizQuestion(
                    "What should you check before clicking a shortened link (e.g. bit.ly)?",
                    new List<string> { "Nothing, they're always safe", "Hover or preview to see the real destination URL", "The link's font colour", "How many times it's been shared" },
                    1,
                    "Always preview shortened links before clicking — they can hide malicious destinations.",
                    "links"),

                new QuizQuestion(
                    "True or False: You should only install mobile apps from official app stores.",
                    new List<string> { "True", "False" },
                    0,
                    "True. Official stores like Google Play and the Apple App Store vet apps for malware far more than third-party sources.",
                    "mobile")
            };
        }
    }
}

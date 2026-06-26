// ============================================================
// ChatbotEngine.cs
// ------------------------------------------------------------
// Session orchestrator — the middle layer between the UI (MainWindow)
// and the knowledge base (ResponseEngine).
//
// Responsibilities
// ----------------
// 1. Hold the active UserProfile for the session.
// 2. Initialise a new user (Initialise).
// 3. Produce the welcome message shown when the chat screen opens.
// 4. Route all user input through ResponseEngine (ProcessInput).
// 5. Expose a formatted memory summary for the sidebar panel.
// 6. Expose read-only session properties (UserName, MessageCount, IsReady).
//
// MainWindow only calls methods on ChatbotEngine — it never touches
// ResponseEngine or UserProfile directly. This keeps the UI decoupled
// from the engine internals, which makes both easier to test and change.
// ============================================================

using System.Collections.Generic;
using System.Text;

namespace CybersecurityChatbotWPF
{

    // Orchestrates the chat session: creates the user profile,
    // delegates input processing to ResponseEngine, and surfaces
   // session state to the UI.
    // </summary>
    public class ChatbotEngine
    {
        // ── Private fields ─────────────────────────────────────────────

     
        // The response engine that owns the knowledge base.
        // Marked readonly because it is created once in the constructor
        // and never replaced.
       
        private readonly ResponseEngine _responseEngine;

        
        // The current user's profile. Null until Initialise() is called.
        // All public properties guard against a null profile.
        private UserProfile _currentUser;

        // ── Public session properties ──────────────────────────────────

        // The user's display name, or null if Initialise() has not been called.
        // Using a null-conditional operator (?.) avoids a NullReferenceException
        // if MainWindow somehow reads this before the name screen is submitted.
        public string UserName => _currentUser?.Name;

        // ── Public session properties ──────────────────────────────────
        // Total messages sent this session, or 0 before initialisation.
        // The null-coalescing operator (??) provides the default of 0.
        public int MessageCount => _currentUser?.MessageCount ?? 0;

        // True once Initialise() has been called with a valid name.
        // MainWindow can use this to guard against premature input.
        public bool IsReady => _currentUser != null;

        // ── Constructor ────────────────────────────────────────────────

        // ── Constructor ────────────────────────────────────────────────

        // Creates the engine and its internal ResponseEngine.
        // The ResponseEngine constructor builds the full knowledge base,
        // so this constructor is intentionally called once at window startup.
        public ChatbotEngine()
        {
            // ResponseEngine is created here and reused for the entire session.
            // It is readonly because the knowledge base does not change at runtime.
            _responseEngine = new ResponseEngine();
        }

        // ── Session lifecycle ──────────────────────────────────────────

        // Creates a new UserProfile for the session.
        // Must be called before ProcessInput() or GetWelcomeMessage().
        // </summary>
        // <param name="userName">The name submitted on the name screen.</param>
        public void Initialise(string userName)
        {
            // UserProfile's constructor handles trimming and null/whitespace guards
            _currentUser = new UserProfile(userName.Trim());
        }

        // ── Message production ─────────────────────────────────────────

        // Builds and returns the welcome message shown at the start of the chat.
        // Includes the user's name and a formatted topic menu.
        // </summary>
        // <returns>A multi-line welcome string, or a generic prompt if not initialised.</returns>
        public string GetWelcomeMessage()
        {
            // Guard: if called before Initialise(), return a safe fallback
            if (_currentUser == null) return "Welcome! Please enter your name to begin.";

            // String concatenation with \n for readability in the chat bubble.
            // Each topic is shown with a matching emoji for quick visual scanning.
            return $"Welcome, {_currentUser.Name}! Great to meet you 😊\n\n" +
                   "I am here to help you stay safe online.\n\n" +
                   "You can ask me about any of these topics:\n\n" +
                   "  🔑 password    🎣 phishing    🌐 browsing    🔒 privacy\n" +
                   "  🐛 malware     ⚠️  social      🔐 2fa         🛡️  scam\n" +
                   "  🔗 links       📱 mobile\n\n" +
                   "Or click any topic button on the left panel.\n" +
                   "Type 'help' to see the full list at any time.";
        }

        // Routes the user's input to ResponseEngine and returns the engine's reply.
        // Enforces a 500-character input limit to prevent extremely long inputs.
       
        // <param name="userInput">Raw text from the chat input box.</param>
        // <returns>An EngineResponse DTO containing the reply text and metadata.</returns>
        public EngineResponse ProcessInput(string userInput)
        {
            // Guard: if called before name entry, return a polite prompt
            if (_currentUser == null)
                return new EngineResponse("Please enter your name to begin.");

            // Clamp input length: very long strings could slow down Contains() checks
            // across the knowledge base dictionary.
            if (userInput.Length > 500)
                userInput = userInput.Substring(0, 500);

            // Delegate all matching logic to ResponseEngine, passing the user profile
            // so the engine can read and write session memory (LastTopic, FavouriteTopic, etc.)
            return _responseEngine.GetResponse(userInput, _currentUser);
        }

        // Increments the user's message counter.
        // Called by MainWindow after each successful send.
        // The null-conditional ?. means this is a no-op if called before Initialise().
        public void IncrementMessages() => _currentUser?.IncrementMessages();

        // ── Memory panel ───────────────────────────────────────────────

        // Builds a formatted multi-line string for the sidebar memory panel.
        // Shows: name, favourite topic, last topic, last mood, and any custom memory entries.
        // Returns an empty string if the session is not yet initialised.
        // </summary>
        // <returns>A newline-separated summary, or an empty string.</returns>
        public string GetMemorySummary()
        {
            // Not initialised yet — return empty so the panel shows the default hint text
            if (_currentUser == null) return string.Empty;

            // StringBuilder is more efficient than repeated string concatenation
            // when appending multiple lines in a loop.
            StringBuilder sb = new StringBuilder();

            // Always show the name so the panel is never completely empty after login
            if (!string.IsNullOrEmpty(_currentUser.Name))
                sb.AppendLine($"👤 Name: {_currentUser.Name}");

            // Show the topic the user said they were interested in, if captured
            if (!string.IsNullOrEmpty(_currentUser.FavouriteTopic))
                sb.AppendLine($"❤️ Interested in: {_currentUser.FavouriteTopic}");

            // Show the most recently matched topic keyword
            if (!string.IsNullOrEmpty(_currentUser.LastTopic))
                sb.AppendLine($"🔍 Last topic: {_currentUser.LastTopic}");

            // Show the most recently detected sentiment word
            if (!string.IsNullOrEmpty(_currentUser.LastSentiment))
                sb.AppendLine($"💭 Mood: {_currentUser.LastSentiment}");

            // Iterate the general-purpose Memory dictionary for any additional entries
            // (e.g. "context" captured from "I work as a …" phrases).
            // Skip "interest" because it is already shown via FavouriteTopic above.
            foreach (KeyValuePair<string, string> kv in _currentUser.Memory)
            {
                if (kv.Key == "interest") continue;  // avoid duplicate display
                sb.AppendLine($"📌 {kv.Key}: {kv.Value}");
            }

            // Trim trailing newline added by the last AppendLine call
            return sb.ToString().Trim();
        }
    }
}

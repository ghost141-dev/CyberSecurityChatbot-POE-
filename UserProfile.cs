// ============================================================
// UserProfile.cs
// ------------------------------------------------------------
// Represents a single chat session's user data.
//
// This class is instantiated by ChatbotEngine.Initialise() when
// the user submits their name. It persists for the entire session.
//
// Automatic properties are used throughout so the compiler
// generates the private backing fields — this keeps the class
// concise and idiomatic C#.
//
// The Memory dictionary acts as a general-purpose key-value store:
//   Key "interest"  → topic the user said they care about
//   Key "context"   → snippet from a sentence like "I work as …"
//
// ResponseEngine reads this dictionary via Recall() to personalise
// its responses (e.g. appending a reminder about the user's interest).
// ============================================================

using System.Collections.Generic;

namespace CybersecurityChatbotWPF
{

    // Stores all per-user state for the current chat session.
    // One instance is created per session and shared between
    // ChatbotEngine and ResponseEngine.
    public class UserProfile
    {
        // ── Auto-properties ───────────────────────────────────────────

        // The user's display name, entered on the name screen.
        // Defaults to "User" if the submitted name is blank.
        public string Name { get; set; }

        // Running total of messages the user has sent this session.
        // Incremented via IncrementMessages() after each send.
        public int MessageCount { get; set; }

        // The keyword of the most recent topic that matched in ResponseEngine
        // (e.g. "phishing", "password"). Used by the follow-up handler so
        // "tell me more" continues the same topic.
        public string LastTopic { get; set; }

        // The topic the user has explicitly expressed interest in
        // (e.g. "I'm interested in privacy" → FavouriteTopic = "privacy").
        // Used by PersonaliseResponse() to append a helpful reminder.
        public string FavouriteTopic { get; set; }

        // The most recently detected sentiment word
        // (e.g. "worried", "frustrated"). Displayed in the memory panel
        // as the user's current mood.
        public string LastSentiment { get; set; }

        // General-purpose key-value memory store for the session.
        // Keys are short strings like "interest" or "context".
        // Values are strings extracted from the user's messages.
        public Dictionary<string, string> Memory { get; set; }

        // ── Constructor ───────────────────────────────────────────────

        // Initialises a new user profile for the session.
        // <param name="name">
        //   The name the user entered. Whitespace-only names are replaced
        //   with the default "User" to avoid empty display names.
        // </param>
        public UserProfile(string name)
        {
            // Guard: if the user submitted spaces or nothing, use a default name
            Name           = string.IsNullOrWhiteSpace(name) ? "User" : name.Trim();
            MessageCount   = 0;
            LastTopic      = string.Empty;
            FavouriteTopic = string.Empty;
            LastSentiment  = string.Empty;

            // Initialise the memory dictionary — it starts empty each session
            Memory = new Dictionary<string, string>();
        }

        // ── Methods ───────────────────────────────────────────────────

        // Increments the message counter by one.
        // Called by MainWindow after every successful send.
        // Expression-bodied for brevity (C# 6+ feature).
        public void IncrementMessages() => MessageCount++;

        // Stores or overwrites a value in the memory dictionary.
        // If the key already exists, the previous value is replaced.
        // <param name="key">Short category label (e.g. "interest").</param>
        // <param name="value">The value to remember (e.g. "phishing").</param>
        public void Remember(string key, string value)
        {
            // Dictionary indexer automatically adds or replaces the entry
            Memory[key] = value;
        }

        // Retrieves a value from the memory dictionary.
        // <param name="key">The key to look up.</param>
        // <returns>
        //   The stored value, or <c>null</c> if the key does not exist.
        //   Using TryGetValue avoids a KeyNotFoundException on missing keys.
        // </returns>
        public string Recall(string key)
        {
            // TryGetValue is the safe way to read a dictionary without throwing
            return Memory.TryGetValue(key, out string val) ? val : null;
        }
    }
}

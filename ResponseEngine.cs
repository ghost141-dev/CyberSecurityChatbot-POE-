// ============================================================
// ResponseEngine.cs
// ------------------------------------------------------------
// The knowledge base and response logic of the chatbot.
//
// Key language features demonstrated
// -----------------------------------
// 1. DELEGATE — ResponseSelector
//    A custom delegate type is declared at namespace level.
//    An instance (_selector) is assigned a lambda in the constructor.
//    Every call to _selector(list) returns a randomly chosen string.
//    Using a delegate makes the selection strategy swappable without
//    changing the callers — a different strategy (e.g. round-robin)
//    could be injected at construction time.
//
// 2. GENERIC COLLECTIONS
//    Dictionary<string, List<string>> _responses
//      Maps a topic keyword to a list of possible tip strings.
//      StringComparer.OrdinalIgnoreCase means "Password" == "password".
//    Dictionary<string, string> _sentimentResponses
//      Maps a sentiment word to a single empathetic response.
//    List<string> _followUpKeywords
//      Phrases like "tell me more" that trigger a follow-up.
//
// 3. RESPONSE PRIORITY (GetResponse method)
//    Priority 1 — Keyword match (checked first so "more about phishing"
//                 hits the phishing topic, not the follow-up handler).
//    Priority 2 — Follow-up (only fires when no keyword matched and a
//                 previous topic exists in the user profile).
//    Priority 3 — Sentiment (Regex word-boundary match to avoid false
//                 positives such as "angry" inside "Hungary").
//    Priority 4 — Fallback (one of three randomised polite prompts).
// ============================================================

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CybersecurityChatbotWPF
{
    // ── Delegate declaration ───────────────────────────────────────────────
    // Declared at namespace level (outside the class) so it is reusable
    // and visible to any future class that might want to use the same signature.
    //
    // Signature: takes a List<string> and returns one string.
    // Usage: _selector(list)  →  list[_random.Next(list.Count)]

    // Delegate that selects one string from a list.
    // Used by ResponseEngine to randomly pick a tip from a topic's response list.
    public delegate string ResponseSelector(List<string> responses);

    // Contains the knowledge base (topic responses, sentiment responses) and
    // the logic to match user input to the appropriate reply.
    public class ResponseEngine
    {
        // ── Private fields ─────────────────────────────────────────────

        // Shared Random instance. A single instance is more efficient than
        // creating a new Random() on every method call (avoids same-seed issues
        // when called in rapid succession).
        private readonly Random _random = new Random();


        // The delegate instance used to select one response from a list.
        // Assigned a lambda in the constructor: picks a random element.
        private readonly ResponseSelector _selector;


        /// The main knowledge base.
        // Key   — topic keyword (e.g. "phishing", "password").
        // Value — list of 2–3 response strings for that topic.
        // OrdinalIgnoreCase comparer ensures "Password" and "password" map to the same key.
        private readonly Dictionary<string, List<string>> _responses;

        // Maps emotional keywords to empathetic response strings.
        // Checked after keyword matching and follow-up detection.
        private readonly Dictionary<string, string> _sentimentResponses;

        //
        // Phrases that signal the user wants more information on the last topic.
        // Checked only when no keyword in _responses was matched first.
        private readonly List<string> _followUpKeywords = new List<string>
        {
            "tell me more", "give me another", "another tip", "next tip",
            "more info", "elaborate", "go on", "continue", "explain more"
        };

        // ── Constructor ────────────────────────────────────────────────

        // Builds the knowledge base and assigns the delegate.
        // Called once when ChatbotEngine is constructed.
        public ResponseEngine()
        {
            // ── Assign the delegate ────────────────────────────────────
            // Lambda expression: given a list, return the element at a random index.
            // This satisfies the ResponseSelector signature (List<string> → string).
            _selector = (list) => list[_random.Next(list.Count)];

            // ── Build the topic response dictionary ────────────────────
            // Each key is a lowercase keyword that will be searched for inside
            // the user's input using string.Contains().
            // Each value is a List<string> so _selector can pick randomly.
            _responses = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                // ── General / meta topics ─────────────────────────────

                // Matched when the user asks "how are you" or similar greetings
                ["how are you"] = new List<string>
                {
                    "Running at 100% — all security checks are green! What topic can I help you with?",
                    "All systems secure and ready! Ask me anything about cybersecurity.",
                    "Fully operational and threat-free! Let's keep you safe online."
                },

                // Matched when the user asks about the bot's role or purpose
                ["purpose"] = new List<string>
                {
                    "My purpose is to educate South African citizens about cybersecurity threats " +
                    "like phishing, malware, and social engineering — and help you stay safe online.",
                    "I am a Cybersecurity Awareness Bot! I help users identify threats, " +
                    "practise safe browsing, and protect personal information."
                },

                // Full topic menu — displayed when the user types "help"
                ["help"] = new List<string>
                {
                    "Here are all the topics I can help you with:\n\n" +
                    "    password      — Tips for creating strong passwords\n" +
                    "    phishing      — Recognising phishing emails and links\n" +
                    "    browsing      — Safe browsing habits\n" +
                    "    privacy       — Protecting your personal data\n" +
                    "    malware       — Understanding and avoiding malware\n" +
                    "    social        — Social engineering awareness\n" +
                    "    2fa           — Two-factor authentication\n" +
                    "    scam          — Spotting online scams\n" +
                    "    links         — Safe link checking\n" +
                    "    mobile        — Mobile device security\n\n" +
                    "Just type any keyword above, or click a topic button on the left!"
                },

                // ── Cybersecurity topics ──────────────────────────────

                // PASSWORD — 3 responses so the user gets variety on repeated asks
                ["password"] = new List<string>
                {
                    "PASSWORD SAFETY TIP\n\n" +
                    "  • Use at least 12 characters mixing UPPERCASE, lowercase, numbers & symbols.\n" +
                    "  • Never reuse the same password across multiple accounts.\n" +
                    "  • Consider a reputable password manager like Bitwarden or 1Password.",

                    "STRONG PASSWORD ADVICE\n\n" +
                    "  • Avoid personal details like your name, birthday, or ID number.\n" +
                    "  • A passphrase like 'Coffee!Sunrise#2024' is long and memorable.\n" +
                    "  • Change passwords immediately if you suspect a breach.",

                    "DID YOU KNOW?\n\n" +
                    "  • 81% of data breaches are caused by weak or stolen passwords.\n" +
                    "  • Enable multi-factor authentication (MFA) wherever possible.\n" +
                    "  • Never share your password — not even with IT support staff."
                },

                // PHISHING — includes South Africa-specific context (SARS, SABRIC)
                ["phishing"] = new List<string>
                {
                    "PHISHING ALERT\n\n" +
                    "  • Phishing emails pretend to be from banks, SARS, or trusted brands.\n" +
                    "  • Look for spelling mistakes, urgency, and suspicious sender addresses.\n" +
                    "  • Never click links in unexpected emails — go directly to the website.",

                    "SPOTTING PHISHING\n\n" +
                    "  • Hover over any link before clicking to reveal the real URL.\n" +
                    "  • Legitimate organisations will NEVER ask for your password via email.\n" +
                    "  • Report suspicious emails to your provider immediately.",

                    "PHISHING IN SOUTH AFRICA\n\n" +
                    "  • SARS, Nedbank, and FNB are commonly impersonated in SA phishing scams.\n" +
                    "  • If you receive a suspicious SMS or email, verify via the official website.\n" +
                    "  • SABRIC (SA Banking Risk Info Centre) provides resources to help you."
                },

                // BROWSING — covers HTTPS, public Wi-Fi, and browser hygiene
                ["browsing"] = new List<string>
                {
                    "SAFE BROWSING TIPS\n\n" +
                    "  • Always check for HTTPS (the padlock icon) in the address bar.\n" +
                    "  • Avoid public Wi-Fi for banking or sensitive logins.\n" +
                    "  • Keep your browser and operating system updated at all times.",

                    "BROWSING SAFETY\n\n" +
                    "  • Use a reputable VPN when on public networks.\n" +
                    "  • Install uBlock Origin in your browser to block malicious ads.\n" +
                    "  • Regularly clear your browser cookies and cache for privacy."
                },

                // PRIVACY — includes South Africa's POPIA legislation
                ["privacy"] = new List<string>
                {
                    "PRIVACY TIPS\n\n" +
                    "  • Review app permissions regularly — does a torch app need your contacts?\n" +
                    "  • Use privacy settings on social media to limit who sees your posts.\n" +
                    "  • Be cautious about what personal information you share publicly.",

                    "PROTECTING YOUR PRIVACY\n\n" +
                    "  • South Africa's POPIA law gives you rights over your personal data.\n" +
                    "  • Opt out of marketing lists and unnecessary data collection.\n" +
                    "  • Use private/incognito mode for sensitive searches."
                },

                // MALWARE — covers ransomware, USB threats, and antivirus best practice
                ["malware"] = new List<string>
                {
                    "MALWARE AWARENESS\n\n" +
                    "  • Malware includes viruses, ransomware, spyware, and trojans.\n" +
                    "  • Only download software from official and trusted sources.\n" +
                    "  • Keep your antivirus software updated and run regular scans.",

                    "STAYING MALWARE-FREE\n\n" +
                    "  • Be wary of USB drives from unknown sources — they can carry malware.\n" +
                    "  • Ransomware locks your files and demands payment — back up your data!\n" +
                    "  • Never disable Windows Defender or your antivirus software."
                },

                // SOCIAL ENGINEERING — psychological manipulation tactics
                ["social"] = new List<string>
                {
                    "SOCIAL ENGINEERING\n\n" +
                    "  • Scammers psychologically manipulate people to reveal info or grant access.\n" +
                    "  • Common tactics: impersonating IT support, creating urgency, offering prizes.\n" +
                    "  • Always verify the identity of anyone asking for sensitive information.",

                    "PROTECTING YOURSELF\n\n" +
                    "  • No legitimate company will ever ask for your password over the phone.\n" +
                    "  • If something feels wrong, trust your instincts and verify independently.\n" +
                    "  • Educate family members — elderly people are often targeted by scammers."
                },

                // 2FA — authenticator apps vs SMS, backup codes
                ["2fa"] = new List<string>
                {
                    "TWO-FACTOR AUTHENTICATION (2FA)\n\n" +
                    "  • 2FA adds a second security layer beyond just your password.\n" +
                    "  • Even if your password is stolen, 2FA blocks unauthorised access.\n" +
                    "  • Enable it on your email, banking apps, and social media right now!",

                    "SETTING UP 2FA\n\n" +
                    "  • Use an authenticator app like Google Authenticator or Microsoft Authenticator.\n" +
                    "  • SMS-based 2FA is better than nothing, but app-based is more secure.\n" +
                    "  • Store backup codes in a safe place in case you lose your device."
                },

                // SCAM — SA-specific scam types (SAPS, SAFPS reporting)
                ["scam"] = new List<string>
                {
                    "SCAM AWARENESS\n\n" +
                    "  • If it sounds too good to be true, it almost certainly is.\n" +
                    "  • Romance scams, prize scams, and advance-fee fraud are common in SA.\n" +
                    "  • Never send money to someone you have only met online.",

                    "ONLINE SCAM TIPS\n\n" +
                    "  • Verify any prize winnings or job offers through official channels.\n" +
                    "  • Scammers often create urgency — take your time and think it through.\n" +
                    "  • Report scams to the SAPS or the SA Fraud Prevention Service (SAFPS)."
                },

                // LINKS — URL inspection, VirusTotal, typosquatting
                // Key is "links" to match the sidebar button Tag and UI labels exactly
                ["links"] = new List<string>
                {
                    "SAFE LINK CHECKING\n\n" +
                    "  • Hover over any link to preview the actual destination URL.\n" +
                    "  • Use tools like VirusTotal or Google Safe Browsing to check suspicious URLs.\n" +
                    "  • Shortened links (bit.ly, tinyurl) can hide malicious destinations.",

                    "LINK SAFETY TIPS\n\n" +
                    "  • Look for HTTPS and a valid domain name — not random characters.\n" +
                    "  • Typosquatting sites use near-identical URLs (e.g. g00gle.com).\n" +
                    "  • When in doubt, type the URL directly into your browser manually."
                },

                // MOBILE — PIN/biometrics, official app stores, remote wipe
                ["mobile"] = new List<string>
                {
                    "MOBILE DEVICE SECURITY\n\n" +
                    "  • Always lock your phone with a strong PIN, pattern, or biometric.\n" +
                    "  • Only install apps from official stores (Google Play, Apple App Store).\n" +
                    "  • Keep your operating system and apps updated to patch vulnerabilities.",

                    "SMARTPHONE SAFETY\n\n" +
                    "  • Avoid connecting to unknown Wi-Fi networks without a VPN.\n" +
                    "  • Review app permissions — deny access the app doesn't truly need.\n" +
                    "  • Enable remote wipe in case your device is lost or stolen."
                }
            };

            // ── Build the sentiment response dictionary ─────────────────
            // Each key is a single emotion word matched with word-boundary regex.
            // Each value is a full empathetic response string (no list — one response per emotion).
            _sentimentResponses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // "Worried" — reassure then give a concrete action (enable 2FA)
                ["worried"] =
                    "I completely understand feeling worried — cyber threats are very real.\n\n" +
                    "The good news is that awareness is the first and most powerful defence.\n\n" +
                    "  • Enable two-factor authentication on your most important accounts today.\n" +
                    "  • This single step blocks over 99% of automated account attacks.\n" +
                    "  • You are already doing the right thing by learning about cybersecurity! 💪",

                // "Scared" — normalise the feeling, give three quick wins
                ["scared"] =
                    "It is okay to feel scared — these threats can seem overwhelming at first.\n\n" +
                    "  • Use strong, unique passwords for each account.\n" +
                    "  • Enable 2FA wherever you can.\n" +
                    "  • Think before you click — most attacks rely on you acting impulsively.",

                // "Frustrated" — simplify cybersecurity to three core habits
                ["frustrated"] =
                    "I hear you — cybersecurity can feel like a lot to keep up with.\n\n" +
                    "Let's break it down to just three simple habits:\n\n" +
                    "  1. Strong passwords (use a password manager).\n" +
                    "  2. Keep software updated.\n" +
                    "  3. Think before clicking any link or attachment.",

                // "Curious" — reward curiosity with interesting SA-specific statistics
                ["curious"] =
                    "Curiosity is exactly the right attitude for learning about cybersecurity!\n\n" +
                    "Did you know?\n\n" +
                    "  • South Africa is among the top targeted countries for cybercrime in Africa.\n" +
                    "  • A cyberattack occurs globally roughly every 39 seconds.\n" +
                    "  • Most successful attacks exploit human behaviour, not technical weaknesses.",

                // "Confused" — give three concrete starting points
                ["confused"] =
                    "No problem at all — cybersecurity jargon can be confusing!\n\n" +
                    "Popular starting points:\n" +
                    "  • Type 'phishing' to learn about email scams.\n" +
                    "  • Type 'password' for tips on keeping accounts secure.\n" +
                    "  • Type 'help' to see everything I can assist with.",

                // "Happy" — channel positive energy into a quick actionable win
                ["happy"] =
                    "Love the positive energy! Here is a quick cybersecurity win you can do right now:\n\n" +
                    "  • Check if your email has been in a known data breach at haveibeenpwned.com\n" +
                    "  • It is free and takes under 30 seconds — knowledge is power!",

                // "Angry" — validate frustration, provide reporting channels
                ["angry"] =
                    "I understand — being targeted by scammers or hackers is genuinely infuriating.\n\n" +
                    "  • If you suspect a breach, change your passwords immediately.\n" +
                    "  • Report cybercrime to the SAPS or the Cybercrime Hub at cybercrime.org.za\n" +
                    "  • You are not alone — take a breath and let's work through this together."
            };
        }

        // ── Core response method ───────────────────────────────────────

        // Determines the best response for the given user input.
        // Checks four handlers in priority order and returns the first match.
        // </summary>
        // <param name="userInput">Raw input from the chat box (already trimmed by the caller).</param>
        // <param name="user">The active UserProfile — read and updated during processing.</param>
        // <returns>An EngineResponse containing the reply text and metadata.</returns>
        public EngineResponse GetResponse(string userInput, UserProfile user)
        {
            // Guard: should not happen because MainWindow trims before calling,
            // but defensive programming catches edge cases.
            if (string.IsNullOrWhiteSpace(userInput))
                return new EngineResponse("I didn't quite understand that. Could you rephrase?");

            // Normalise to lowercase for case-insensitive Contains() checks
            string input = userInput.Trim().ToLower();

            // ── Priority 1: Keyword matching ──────────────────────────
            // Iterate the knowledge base dictionary.
            // If the input contains any topic keyword (e.g. "phishing"), return a tip.
            // Checking keywords FIRST ensures "tell me more about phishing" correctly
            // triggers the phishing response rather than the follow-up handler.
            foreach (var entry in _responses)
            {
                if (input.Contains(entry.Key))
                {
                    // Update session memory: track the matched topic for follow-ups
                    user.LastTopic = entry.Key;

                    // Check for interest/context phrases and store them in user memory
                    ExtractAndStoreMemory(input, user);

                    // Use the delegate to randomly select one response from the list
                    string response    = _selector(entry.Value);

                    // Personalise the response if the user has a stored interest
                    string personalised = PersonaliseResponse(response, user);

                    return new EngineResponse(personalised, topicLabel: entry.Key);
                }
            }

            // ── Priority 2: Follow-up detection ───────────────────────
            // Only reached when NO keyword matched above.
            // Checks if the input is a follow-up phrase AND a previous topic exists.
            if (IsFollowUp(input) && !string.IsNullOrEmpty(user.LastTopic))
            {
                // TryGetValue is the safe way to read a dictionary without throwing
                if (_responses.TryGetValue(user.LastTopic, out List<string> followUpList))
                {
                    // Pick a random response from the same topic list
                    string followUp = _selector(followUpList);
                    return new EngineResponse(
                        $"Sure! Here is another tip on {user.LastTopic}:\n\n{followUp}",
                        topicLabel: user.LastTopic);
                }
            }

            // ── Priority 3: Sentiment detection ───────────────────────
            // Uses Regex with word boundaries (\b) to avoid false positives.
            // Example: "angry" inside "Hungary" would NOT match because \b requires
            // a word boundary before "a" and after "y".
            foreach (var sentiment in _sentimentResponses)
            {
                if (Regex.IsMatch(input, $@"\b{Regex.Escape(sentiment.Key)}\b",
                                  RegexOptions.IgnoreCase))
                {
                    // Record the mood for the memory panel
                    user.LastSentiment = sentiment.Key;

                    return new EngineResponse(
                        sentiment.Value,
                        isSentiment: true,
                        topicLabel: $"Sentiment: {sentiment.Key}");
                }
            }

            // ── Priority 4: Fallback ───────────────────────────────────
            // None of the above matched — return a polite, personalised prompt.
            // GetFallback randomly selects one of three rephrasing suggestions.
            return new EngineResponse(GetFallback(user.Name));
        }

        // ── Private helper methods ─────────────────────────────────────

        /// <summary>
        /// Returns true if the input contains any phrase from the follow-up keyword list.
        /// </summary>
        private bool IsFollowUp(string input)
        {
            foreach (string kw in _followUpKeywords)
                if (input.Contains(kw)) return true;
            return false;
        }

       
        // Scans the input for interest-declaration phrases and context sentences,
        // then stores findings in the user's Memory dictionary.
        // </summary>
        // <param name="input">Lowercased user input.</param>
        // <param name="user">The active UserProfile to update.</param>
        private void ExtractAndStoreMemory(string input, UserProfile user)
        {
            // ── Interest detection ─────────────────────────────────────
            // Phrases that indicate the user is declaring an interest in a topic.
            // Example: "I'm interested in privacy" → stores "privacy" under key "interest".
            string[] interestTriggers = { "interested in", "care about", "worried about", "want to know about" };

            foreach (string trigger in interestTriggers)
            {
                int idx = input.IndexOf(trigger, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    // Extract the text after the trigger phrase and strip trailing punctuation
                    string topic = input.Substring(idx + trigger.Length).Trim().TrimEnd('.', '!', '?');
                    if (!string.IsNullOrEmpty(topic))
                    {
                        // Store in both the dedicated property and the general-purpose dictionary
                        user.FavouriteTopic = topic;
                        user.Remember("interest", topic);
                    }
                }
            }

            // ── Context detection ──────────────────────────────────────
            // Captures sentences like "I work as a nurse" or "I am a student"
            // as general context that might be useful for personalisation later.
            if (input.Contains("i work") || input.Contains("i am a") || input.Contains("i'm a"))
            {
                // Truncate long inputs to avoid storing entire paragraphs
                user.Remember("context", input.Length > 60 ? input.Substring(0, 60) + "…" : input);
            }
        }


        // Appends a personalised reminder to the response if the matched topic
        // matches the user's stated interest.
        // </summary>
        // <param name="response">The raw response string from the knowledge base.</param>
        // <param name="user">The active UserProfile.</param>
        // <returns>The original response, possibly with an appended interest reminder.</returns>
        private string PersonaliseResponse(string response, UserProfile user)
        {
            // Recall the stored interest (returns null if not set)
            string interest = user.Recall("interest");

            // Only append when an interest is stored AND it matches the current topic
            if (!string.IsNullOrEmpty(interest) &&
                !string.IsNullOrEmpty(user.LastTopic) &&
                user.LastTopic.Equals(interest, StringComparison.OrdinalIgnoreCase))
            {
                response += $"\n\nReminder: As someone interested in {interest}, " +
                            "remember to regularly review and update your security settings.";
            }
            return response;
        }

        // Returns a randomly selected fallback message including the user's name.
        // Called when no keyword, follow-up, or sentiment matched.
        // </summary>
        // <param name="userName">The user's display name from UserProfile.</param>
        // <returns>A friendly rephrasing prompt.</returns>
        private string GetFallback(string userName)
        {
            // If somehow the name is empty, use "there" as a neutral address
            string name = string.IsNullOrEmpty(userName) ? "there" : userName;

            string[] fallbacks =
            {
                $"I didn't quite understand that, {name}. Could you rephrase?\n\nTry asking about: password, phishing, browsing, privacy, or type 'help'.",
                $"Hmm, that is outside my current knowledge base, {name}.\n\nType 'help' to see all the topics I can assist with.",
                $"I am not sure I follow, {name}. Let's try again!\n\nYou can ask me about: password safety, phishing, malware, 2FA, scams, and more."
            };

            // Random selection using the shared _random instance
            return fallbacks[_random.Next(fallbacks.Length)];
        }

        // ── Public utility ─────────────────────────────────────────────

        // Exposes the set of all topic keys in the knowledge base.
        // Could be used by the UI to dynamically generate sidebar buttons.
        // Returns IEnumerable to avoid exposing the internal Dictionary directly.
        public IEnumerable<string> GetTopicKeys() => _responses.Keys;
    }
}

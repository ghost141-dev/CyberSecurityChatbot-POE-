# Cybersecurity Awareness Chatbot — Part 3 / POE (WPF GUI + MySQL)

**Module:** PROG6221 — Programming 2A
**Student Number:** ST10485108
**Part:** 3 of 3 (POE)

---

## Overview

Part 3 is the final POE submission. It extends the Part 2 WPF chatbot with four
new features, while keeping every Part 1 and Part 2 feature fully intact:

1. **Task Assistant with Reminders** — add, view, complete, and delete
   cybersecurity tasks, persisted in a **MySQL** database.
2. **Cybersecurity Mini-Game (Quiz)** — 12 questions (multiple-choice and
   true/false), one at a time, with immediate feedback and a final score.
3. **NLP Simulation** — recognises varied phrasings of chat commands
   ("add a task to...", "remind me to...", "start the quiz", etc.) using
   keyword/phrase detection (`string.Contains()`-based, as required by the
   brief — this is a simulation, not a real ML/NLP pipeline).
4. **Activity Log** — records every significant action (tasks, reminders,
   quiz attempts, NLP-recognised commands) and displays the most recent
   entries with a "Show more" option.

All four features are reachable both from **dedicated GUI panels** (sidebar
navigation: Chat / Tasks / Quiz / Activity Log) **and**, where it makes sense,
directly from the chat box via the NLP layer — for example, typing
*"remind me to update my password tomorrow"* in the chat works exactly like
using the Tasks panel.

---

## New Classes Added in Part 3

| Class | Responsibility |
|---|---|
| `DatabaseHelper` | All MySQL access (connect, create schema, CRUD on tasks). The only class that opens a `MySqlConnection`. |
| `TaskItem` | Model for a single cybersecurity task (title, description, reminder date, completed flag). |
| `TaskAssistantHandler` | Orchestrates task actions between the GUI/NLP layer and `DatabaseHelper`; builds chat-style confirmation text. |
| `QuizQuestion` | Model for one quiz question (text, options, correct answer, explanation, topic). |
| `QuizEngine` | Owns the 12-question bank, shuffles each attempt, tracks score and progress. |
| `NlpHelper` | Detects user intent from free-text chat input via keyword/phrase matching; extracts task titles and timeframes. |
| `NlpIntent` / `NlpResult` | Enum + DTO describing a detected intent and any extracted detail text. |
| `ActivityLogEntry` | Model for one logged action (timestamp, description, category). |
| `ActivityLogger` | Stores all logged actions for the session and formats them for chat/GUI display. |

All Part 2 classes (`ChatbotEngine`, `ResponseEngine`, `UserProfile`,
`EngineResponse`, `AudioHelper`) are unchanged — Part 3 is purely additive.

---

## MySQL Setup

The Task Assistant requires a local MySQL server. The app **automatically
creates the database and table on first run** — no manual SQL is required.

### Prerequisites
- MySQL Server installed and running locally (e.g. via MySQL Installer or XAMPP).
- Default connection assumed: `localhost:3306`, user `root`, **no password**.

> If your local MySQL uses a different user, password, or port, update the
> four constants at the top of `DatabaseHelper.cs`:
> ```csharp
> private const string Server   = "localhost";
> private const string Port     = "3306";
> private const string UserId   = "root";
> private const string Password = "YOUR_OWN_MYSQL_PASSWORD";
> private const string Database = "cybersecurity_chatbot";
> ```

### What happens on startup
1. The app connects to MySQL **without** selecting a database and runs
   `CREATE DATABASE IF NOT EXISTS cybersecurity_chatbot;`
2. It then connects to that database and runs
   `CREATE TABLE IF NOT EXISTS tasks (...)` if the table doesn't already exist.
3. If either step fails (e.g. MySQL isn't running), the app **does not crash**.
   The Chat, Quiz, and Activity Log panels keep working normally; the Tasks
   panel shows a clear "Could not connect to MySQL" message instead.

### `tasks` table schema
| Column | Type | Notes |
|---|---|---|
| `Id` | `INT AUTO_INCREMENT PRIMARY KEY` | |
| `Title` | `VARCHAR(255) NOT NULL` | |
| `Description` | `TEXT` | |
| `ReminderDate` | `DATETIME NULL` | `NULL` if no reminder was set |
| `IsCompleted` | `BOOLEAN NOT NULL DEFAULT 0` | |
| `CreatedAt` | `DATETIME NOT NULL` | |

### NuGet dependency
`MySqlConnector` (added via `PackageReference` in the `.csproj`) — a modern,
fully-async ADO.NET driver for MySQL. Visual Studio restores it automatically
on first build.

---

## Feature Walkthrough

### 1. Task Assistant (Tasks panel)
- Type a title, optionally pick a reminder offset (1/3/7 days), click **Add Task**.
- Tasks are listed with title, description, and reminder date (if set).
- **Complete** marks a task done (shown with a strikethrough); **Delete** removes it permanently.
- Every action writes to and reads from MySQL via `DatabaseHelper`.

### 2. Quiz panel
- Click **Start Quiz** to begin a randomly-ordered run through the 12-question bank.
- Each question shows 4 options (multiple-choice) or 2 (true/false).
- Selecting an answer immediately colours it green/red and shows an explanation.
- After the last question, a tiered final message is shown based on score
  (≥80% / ≥50% / below), matching the brief's example feedback.

### 3. NLP simulation (via the Chat box)
The chatbot recognises these chat-typed intents without needing the GUI panels:

| You type | What happens |
|---|---|
| `Add a task to enable 2FA` | Creates a task titled "Enable 2FA" |
| `Remind me to update my password tomorrow` | Creates a task with a reminder set for tomorrow |
| `Show my tasks` | Lists all current tasks in the chat |
| `Mark task password as complete` | Marks the closest-matching task as done |
| `Start the quiz` | Switches to the Quiz panel and begins a new attempt |
| `Show activity log` / `What have you done for me?` | Prints the recent activity log in chat |
| `Show more` | Prints the full activity history |

Detection is implemented with `string.Contains()`-based phrase matching
against lists of trigger phrases (see `NlpHelper.cs`), per the brief's
explicit instruction to simulate NLP with basic string manipulation rather
than a real NLP library.

### 4. Activity Log panel
- Shows the 8 most recent actions by default.
- **Show Full History** reveals every action logged this session.
- Every Task, Reminder, Quiz, and NLP-driven action is logged automatically —
  no manual logging step is needed from the user.

---

## How to Run

### Prerequisites
- Windows 10 or 11
- [.NET Framework 4.8](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48)
- Visual Studio 2022 (Community edition is free) **or** the .NET SDK
- A local MySQL server (see *MySQL Setup* above)

### Option A — Visual Studio (recommended)
1. Open `CybersecurityChatbotWPF.csproj` in Visual Studio 2022.
2. Press **F5**. NuGet restores `MySqlConnector` automatically.
3. The app opens; the Tasks panel will show a connection warning if MySQL
   isn't reachable, but everything else works regardless.

### Option B — Command line (.NET CLI)
```bash
cd CybersecurityChatbotWPF
dotnet build --configuration Release
dotnet run
```
> WPF is Windows-only — this will not build on macOS/Linux, which is expected.

### Option C — Run the compiled EXE directly
```
bin\Debug\net48\CybersecurityChatbotWPF.exe
```

---

## Project Structure (Part 3 additions in bold)

```
CybersecurityChatbotWPF/
├── .github/workflows/ci.yml
├── App.xaml                    <- Application resources: button styles (incl. new Nav/Action styles)
├── App.xaml.cs
├── MainWindow.xaml              <- Adds Tasks / Quiz / Activity Log panels + sidebar nav
├── MainWindow.xaml.cs           <- Adds nav switching, NLP routing, task/quiz/log UI logic
├── ChatbotEngine.cs
├── ResponseEngine.cs
├── UserProfile.cs
├── EngineResponse.cs
├── AudioHelper.cs
├── DatabaseHelper.cs        <- NEW: MySQL connection + task CRUD
├── TaskItem.cs              <- NEW: Task model
├── TaskAssistantHandler.cs  <- NEW: Task orchestration layer
├── QuizQuestion.cs          <- NEW: Quiz question model
├── QuizEngine.cs            <- NEW: Quiz question bank, scoring, state
├── NlpHelper.cs             <- NEW: Intent detection via keyword/phrase matching
├── NlpIntent.cs             <- NEW: Intent enum + result DTO
├── ActivityLogEntry.cs      <- NEW: Log entry model
├── ActivityLogger.cs        <- NEW: Log storage + formatted summaries
├── greet.wav
├── logo.png
├── CybersecurityChatbotWPF.csproj  <- Now references MySqlConnector via NuGet
└── README.md
```

---

## Combining Parts 1, 2, and 3

All three parts run inside the **same window** and the **same session**:
- The voice greeting, ASCII/logo display, name entry, topic buttons, keyword
  recognition, memory, and sentiment detection from Parts 1–2 are unchanged
  and fully functional in the Chat panel.
- The sidebar's new navigation buttons (Chat / Tasks / Quiz / Activity Log)
  sit directly above the existing Topics list, in the same visual style.
- The NLP layer intercepts chat input *before* falling back to the Part 2
  `ResponseEngine`, so cybersecurity topic questions ("tell me about
  phishing") behave exactly as in Part 2, while task/quiz/log commands are
  handled by the new Part 3 logic — both paths share the same chat window
  and message bubbles.

---

## Commit History Guidelines (POE)

1. `Initial Part 3 commit: scaffold TaskItem, QuizQuestion, ActivityLogEntry models`
2. `Added DatabaseHelper with MySQL schema creation and full task CRUD`
3. `Implemented TaskAssistantHandler and wired Tasks panel to the database`
4. `Added QuizEngine and Quiz panel with scoring and tiered feedback`
5. `Implemented NlpHelper intent detection and wired it into the chat send flow`
6. `Added ActivityLogger and Activity Log panel with show-more support`
7. `Combined Parts 1–3 into a single navigable window; polished styling`
.

---

## References

- Pieterse, H. 2021. *The Cyber Threat Landscape in South Africa: A 10-Year Review*. The African Journal of Information and Communication, 28(28). doi: https://doi.org/10.23962/10539/32213
- Microsoft. 2024. *WPF Overview*. Available at: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/
- MySqlConnector Authors. 2024. *MySqlConnector Documentation*. Available at: https://mysqlconnector.net/
- Microsoft. 2024. *Async Programming with async and await*. Available at: https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/

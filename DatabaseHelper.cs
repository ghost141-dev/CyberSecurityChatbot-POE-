// ============================================================
// DatabaseHelper.cs
// ------------------------------------------------------------
// All MySQL access for the Task Assistant lives here — the rest
// of the app (TaskAssistantHandler, MainWindow) never opens a
// connection or writes SQL directly. This keeps persistence
// concerns isolated, in line with the Single Responsibility
// pattern already used throughout the project.
//
// Library: MySqlConnector (NuGet) — a modern, actively maintained,
// fully-async ADO.NET driver for MySQL. Added via PackageReference
// in the .csproj.
//
// Connection details
// ------------------
// Defaults to: server=localhost; port=3306; user=root; no password.
// This matches a typical local MySQL install (e.g. via MySQL
// Installer or XAMPP with default settings). The connection string
// is a single constant below — change it here if your local setup
// differs (e.g. you set a root password).
//
// Robustness
// ----------
// - InitialiseDatabaseAsync() creates the database and table if they
//   do not already exist, so first run "just works" with no manual
//   SQL needed.
// - Every public method wraps its MySqlException in a try/catch and
//   surfaces a clear DatabaseException to the caller, rather than
//   letting raw ADO.NET exceptions bubble up into the UI layer.
// - All methods are async (Task-returning) so the WPF UI thread is
//   never blocked by a slow query — consistent with AudioHelper's
//   background-thread approach elsewhere in the project.
// ============================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySqlConnector;

namespace CybersecurityChatbotWPF
{

    // Thrown when a database operation fails, wrapping the underlying
    // MySqlException with a clearer, user-facing message.

    public class DatabaseException : Exception
    {
        public DatabaseException(string message, Exception inner) : base(message, inner) { }
    }


    // Provides all MySQL CRUD operations for cybersecurity tasks.
    // One instance is created and reused for the lifetime of the app.
    
    public class DatabaseHelper
    {
        // ── Connection configuration ───────────────────────────────────
        // Adjust here if your local MySQL setup differs (password, port, etc.)
        private const string Server   = "localhost";
        private const string Port     = "3306";
        private const string UserId   = "root";
        private const string Password = "YourActualPasswordHere";
        private const string Database = "cybersecurity_chatbot";

        private string ConnectionString =>
            $"Server={Server};Port={Port};Uid={UserId};Pwd={Password};";

        private string ConnectionStringWithDb =>
            $"Server={Server};Port={Port};Uid={UserId};Pwd={Password};Database={Database};";

        // True once InitialiseDatabaseAsync() has completed successfully.
        // MainWindow checks this before allowing Task Assistant actions,
        // so a connection failure is surfaced once, clearly, rather than
        // on every individual task action.
        public bool IsAvailable { get; private set; }

        // Creates the database and `tasks` table if they do not already
        // exist. Safe to call every time the app starts (CREATE ... IF NOT
        // EXISTS is idempotent). Sets IsAvailable based on success.

        public async Task<bool> InitialiseDatabaseAsync()
        {
            try
            {
                // Step 1: connect WITHOUT specifying a database, so we can create it.
                using (var connection = new MySqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    using (var createDbCmd = connection.CreateCommand())
                    {
                        createDbCmd.CommandText =
                            $"CREATE DATABASE IF NOT EXISTS {Database};";
                        await createDbCmd.ExecuteNonQueryAsync();
                    }
                }

                // Step 2: connect to the (now guaranteed to exist) database
                // and create the tasks table if needed.
                using (var connection = new MySqlConnection(ConnectionStringWithDb))
                {
                    await connection.OpenAsync();

                    using (var createTableCmd = connection.CreateCommand())
                    {
                        createTableCmd.CommandText = @"
                            CREATE TABLE IF NOT EXISTS tasks (
                                Id INT AUTO_INCREMENT PRIMARY KEY,
                                Title VARCHAR(255) NOT NULL,
                                Description TEXT,
                                ReminderDate DATETIME NULL,
                                IsCompleted BOOLEAN NOT NULL DEFAULT 0,
                                CreatedAt DATETIME NOT NULL
                            );";
                        await createTableCmd.ExecuteNonQueryAsync();
                    }
                }

                IsAvailable = true;
                return true;
            }
            catch (MySqlException)
            {
                // Database unreachable (server not running, wrong credentials, etc.)
                // We deliberately do not throw here — the app should still start
                // and the chat/quiz features should keep working; only the Task
                // Assistant panel needs to know the DB is unavailable.
                IsAvailable = false;
                return false;
            }
        }

        // Inserts a new task and returns it with its generated Id populated.
        // </summary>
        public async Task<TaskItem> AddTaskAsync(TaskItem task)
        {
            try
            {
                using (var connection = new MySqlConnection(ConnectionStringWithDb))
                {
                    await connection.OpenAsync();

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = @"
                            INSERT INTO tasks (Title, Description, ReminderDate, IsCompleted, CreatedAt)
                            VALUES (@title, @description, @reminderDate, @isCompleted, @createdAt);
                            SELECT LAST_INSERT_ID();";

                        cmd.Parameters.AddWithValue("@title", task.Title);
                        cmd.Parameters.AddWithValue("@description", task.Description ?? string.Empty);
                        cmd.Parameters.AddWithValue("@reminderDate", task.ReminderDate.HasValue ? (object)task.ReminderDate.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@isCompleted", task.IsCompleted);
                        cmd.Parameters.AddWithValue("@createdAt", task.CreatedAt);

                        object result = await cmd.ExecuteScalarAsync();
                        task.Id = Convert.ToInt32(result);
                    }
                }
                return task;
            }
            catch (MySqlException ex)
            {
                throw new DatabaseException("Could not save the task to the database.", ex);
            }
        }

        // Returns every task, most recently created first.
        public async Task<List<TaskItem>> GetAllTasksAsync()
        {
            var tasks = new List<TaskItem>();
            try
            {
                using (var connection = new MySqlConnection(ConnectionStringWithDb))
                {
                    await connection.OpenAsync();

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT Id, Title, Description, ReminderDate, IsCompleted, CreatedAt FROM tasks ORDER BY CreatedAt DESC;";

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                tasks.Add(new TaskItem
                                {
                                    Id           = reader.GetInt32("Id"),
                                    Title        = reader.GetString("Title"),
                                    Description  = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString("Description"),
                                    ReminderDate = reader.IsDBNull(reader.GetOrdinal("ReminderDate")) ? (DateTime?)null : reader.GetDateTime("ReminderDate"),
                                    IsCompleted  = reader.GetBoolean("IsCompleted"),
                                    CreatedAt    = reader.GetDateTime("CreatedAt")
                                });
                            }
                        }
                    }
                }
                return tasks;
            }
            catch (MySqlException ex)
            {
                throw new DatabaseException("Could not load tasks from the database.", ex);
            }
        }

        // Marks a task as completed (or not) by Id.
       
        public async Task MarkCompletedAsync(int taskId, bool isCompleted = true)
        {
            try
            {
                using (var connection = new MySqlConnection(ConnectionStringWithDb))
                {
                    await connection.OpenAsync();

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "UPDATE tasks SET IsCompleted = @isCompleted WHERE Id = @id;";
                        cmd.Parameters.AddWithValue("@isCompleted", isCompleted);
                        cmd.Parameters.AddWithValue("@id", taskId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw new DatabaseException("Could not update the task's completed status.", ex);
            }
        }

        // Permanently deletes a task by Id.
        public async Task DeleteTaskAsync(int taskId)
        {
            try
            {
                using (var connection = new MySqlConnection(ConnectionStringWithDb))
                {
                    await connection.OpenAsync();

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM tasks WHERE Id = @id;";
                        cmd.Parameters.AddWithValue("@id", taskId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw new DatabaseException("Could not delete the task.", ex);
            }
        }

        // Updates the reminder date on an existing task (e.g. when the
        // user adds a reminder to a task after creating it without one).
        public async Task SetReminderAsync(int taskId, DateTime reminderDate)
        {
            try
            {
                using (var connection = new MySqlConnection(ConnectionStringWithDb))
                {
                    await connection.OpenAsync();

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "UPDATE tasks SET ReminderDate = @reminderDate WHERE Id = @id;";
                        cmd.Parameters.AddWithValue("@reminderDate", reminderDate);
                        cmd.Parameters.AddWithValue("@id", taskId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw new DatabaseException("Could not set the reminder.", ex);
            }
        }
    }
}

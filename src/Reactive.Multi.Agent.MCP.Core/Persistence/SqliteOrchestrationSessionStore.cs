namespace Reactive.Multi.Agent.MCP.Core.Persistence;

/// <summary>
/// Provides a persistent store for orchestration sessions using a local SQLite database. Supports loading, saving, and
/// searching orchestration session data and history.
/// </summary>
/// <remarks>This implementation is thread-safe and intended for use in environments where orchestration session
/// state must be reliably persisted and queried. The database schema is initialized on first use to avoid exceptions
/// during dependency injection construction. Dispose the instance to release internal resources when no longer
/// needed.</remarks>
public sealed class SqliteOrchestrationSessionStore : IOrchestrationSessionStore, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _connectionString;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _schemaInitialized;

    public SqliteOrchestrationSessionStore(ReactiveMultiAgentOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Directory.CreateDirectory(options.StateRootPath);
        _connectionString = $"Data Source={options.SessionDatabasePath}";
        // InitializeSchema is deferred to the first database operation so that
        // a SQLite failure (e.g. missing native library in a dotnet-tool context)
        // cannot throw inside the DI constructor.  Throwing there would prevent
        // the singleton from ever being cached, causing every MCP tool call to
        // fail with an exception that escapes McpSafeExecutor.
    }

    public OrchestrationSession? Load(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        _gate.Wait();
        try
        {
            EnsureSchema();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT payload_json FROM orchestration_sessions WHERE session_id = $sessionId;";
            command.Parameters.AddWithValue("$sessionId", sessionId);
            var payload = command.ExecuteScalar() as string;
            return string.IsNullOrWhiteSpace(payload)
                ? null
                : JsonSerializer.Deserialize<OrchestrationSession>(payload, SerializerOptions);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Save(OrchestrationSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var summary = BuildHistoryEntry(session);
        _gate.Wait();
        try
        {
            EnsureSchema();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var tx = connection.BeginTransaction();

            using (var command = connection.CreateCommand())
            {
                command.Transaction = tx;
                command.CommandText = """
                    INSERT INTO orchestration_sessions(session_id, created_at_utc, updated_at_utc, payload_json)
                    VALUES ($sessionId, $createdAtUtc, $updatedAtUtc, $payloadJson)
                    ON CONFLICT(session_id) DO UPDATE SET
                        updated_at_utc = excluded.updated_at_utc,
                        payload_json = excluded.payload_json;
                    """;
                command.Parameters.AddWithValue("$sessionId", session.SessionId);
                command.Parameters.AddWithValue("$createdAtUtc", session.CreatedAtUtc.ToString("O"));
                command.Parameters.AddWithValue("$updatedAtUtc", session.UpdatedAtUtc.ToString("O"));
                command.Parameters.AddWithValue("$payloadJson", JsonSerializer.Serialize(session, SerializerOptions));
                _ = command.ExecuteNonQuery();
            }

            using (var historyCommand = connection.CreateCommand())
            {
                historyCommand.Transaction = tx;
                historyCommand.CommandText = """
                    INSERT INTO orchestration_session_history
                    (session_id, created_at_utc, updated_at_utc, user_request, status, total_tasks, completed_tasks, resume_required_tasks, auto_checkpoint_tasks, auto_retry_tasks)
                    VALUES ($sessionId, $createdAtUtc, $updatedAtUtc, $userRequest, $status, $totalTasks, $completedTasks, $resumeRequiredTasks, $autoCheckpointTasks, $autoRetryTasks)
                    ON CONFLICT(session_id) DO UPDATE SET
                        updated_at_utc = excluded.updated_at_utc,
                        user_request = excluded.user_request,
                        status = excluded.status,
                        total_tasks = excluded.total_tasks,
                        completed_tasks = excluded.completed_tasks,
                        resume_required_tasks = excluded.resume_required_tasks,
                        auto_checkpoint_tasks = excluded.auto_checkpoint_tasks,
                        auto_retry_tasks = excluded.auto_retry_tasks;
                    """;
                historyCommand.Parameters.AddWithValue("$sessionId", summary.SessionId);
                historyCommand.Parameters.AddWithValue("$createdAtUtc", summary.CreatedAtUtc.ToString("O"));
                historyCommand.Parameters.AddWithValue("$updatedAtUtc", summary.UpdatedAtUtc.ToString("O"));
                historyCommand.Parameters.AddWithValue("$userRequest", summary.UserRequest);
                historyCommand.Parameters.AddWithValue("$status", summary.Status);
                historyCommand.Parameters.AddWithValue("$totalTasks", summary.TotalTasks);
                historyCommand.Parameters.AddWithValue("$completedTasks", summary.CompletedTasks);
                historyCommand.Parameters.AddWithValue("$resumeRequiredTasks", summary.ResumeRequiredTasks);
                historyCommand.Parameters.AddWithValue("$autoCheckpointTasks", summary.AutoCheckpointTasks);
                historyCommand.Parameters.AddWithValue("$autoRetryTasks", summary.AutoRetryTasks);
                _ = historyCommand.ExecuteNonQuery();
            }

            tx.Commit();
        }
        finally
        {
            _gate.Release();
        }
    }

    public IReadOnlyList<SessionHistoryEntry> Search(string? query = null, int limit = 20)
    {
        _gate.Wait();
        try
        {
            EnsureSchema();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            var effectiveLimit = Math.Max(1, limit);

            command.Parameters.AddWithValue("$limit", effectiveLimit);
            if (string.IsNullOrWhiteSpace(query))
            {
                command.CommandText = """
                    SELECT session_id, created_at_utc, updated_at_utc, user_request, status, total_tasks, completed_tasks, resume_required_tasks, auto_checkpoint_tasks, auto_retry_tasks
                    FROM orchestration_session_history
                    ORDER BY updated_at_utc DESC
                    LIMIT $limit;
                    """;
            }
            else
            {
                command.CommandText = """
                    SELECT session_id, created_at_utc, updated_at_utc, user_request, status, total_tasks, completed_tasks, resume_required_tasks, auto_checkpoint_tasks, auto_retry_tasks
                    FROM orchestration_session_history
                    WHERE user_request LIKE $query OR status LIKE $query OR session_id LIKE $query
                    ORDER BY updated_at_utc DESC
                    LIMIT $limit;
                    """;
                command.Parameters.AddWithValue("$query", $"%{query.Trim()}%");
            }

            using var reader = command.ExecuteReader();
            var results = new List<SessionHistoryEntry>();
            while (reader.Read())
            {
                results.Add(new SessionHistoryEntry
                {
                    SessionId = reader.GetString(0),
                    CreatedAtUtc = DateTimeOffset.Parse(reader.GetString(1)),
                    UpdatedAtUtc = DateTimeOffset.Parse(reader.GetString(2)),
                    UserRequest = reader.GetString(3),
                    Status = reader.GetString(4),
                    TotalTasks = reader.GetInt32(5),
                    CompletedTasks = reader.GetInt32(6),
                    ResumeRequiredTasks = reader.GetInt32(7),
                    AutoCheckpointTasks = reader.GetInt32(8),
                    AutoRetryTasks = reader.GetInt32(9),
                });
            }

            return results;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();

    // Called inside the gate block — no additional synchronisation needed.
    // If InitializeSchema() throws the flag stays false so the next caller
    // retries, making transient failures recoverable.
    private void EnsureSchema()
    {
        if (_schemaInitialized)
        {
            return;
        }

        InitializeSchema();
        _schemaInitialized = true;
    }

    private void InitializeSchema()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS orchestration_sessions
            (
                session_id TEXT NOT NULL PRIMARY KEY,
                created_at_utc TEXT NOT NULL,
                updated_at_utc TEXT NOT NULL,
                payload_json TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS orchestration_session_history
            (
                session_id TEXT NOT NULL PRIMARY KEY,
                created_at_utc TEXT NOT NULL,
                updated_at_utc TEXT NOT NULL,
                user_request TEXT NOT NULL,
                status TEXT NOT NULL,
                total_tasks INTEGER NOT NULL,
                completed_tasks INTEGER NOT NULL,
                resume_required_tasks INTEGER NOT NULL,
                auto_checkpoint_tasks INTEGER NOT NULL,
                auto_retry_tasks INTEGER NOT NULL
            );
            """;
        _ = command.ExecuteNonQuery();
    }

    private static SessionHistoryEntry BuildHistoryEntry(OrchestrationSession session)
    {
        var tasks = session.Plan.Tasks;
        var completed = tasks.Count(task => task.Status == AgentTaskStatus.Completed);
        var resumeRequired = tasks.Count(task => task.RecoveryState.NeedsResume);
        var autoCheckpoint = tasks.Count(task => task.RecoveryState.PolicyState.AutoCheckpointRecommended);
        var autoRetry = tasks.Count(task => task.RecoveryState.PolicyState.AutoRetryRecommended);

        return new SessionHistoryEntry
        {
            SessionId = session.SessionId,
            CreatedAtUtc = session.CreatedAtUtc,
            UpdatedAtUtc = session.UpdatedAtUtc,
            UserRequest = session.Request.UserRequest,
            Status = completed == tasks.Count ? "Completed" : "InProgress",
            TotalTasks = tasks.Count,
            CompletedTasks = completed,
            ResumeRequiredTasks = resumeRequired,
            AutoCheckpointTasks = autoCheckpoint,
            AutoRetryTasks = autoRetry,
        };
    }
}

using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace GerenciadorAulas.Core;

public sealed class JournalStore(string directory)
{
    public const int SchemaVersion = 1;
    public string DirectoryPath { get; } = Path.GetFullPath(directory);
    public string DatabasePath => Path.Combine(DirectoryPath, "journal.db");
    public string AttachmentsPath => Path.Combine(DirectoryPath, "attachments");
    internal SemaphoreSlim Gate { get; } = new(1, 1);
    public event EventHandler? Changed;
    private bool initialized;
    internal static readonly Dictionary<string, Type> Types = new[] { typeof(Student), typeof(SchoolYear), typeof(Subject), typeof(Lesson), typeof(PlannedLesson), typeof(StudySession), typeof(Difficulty), typeof(Activity), typeof(Occurrence), typeof(Evidence), typeof(SubjectCatalog) }.ToDictionary(t => t.Name);

    internal SqliteConnection Open()
    {
        Directory.CreateDirectory(DirectoryPath);
        var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = DatabasePath, ForeignKeys = true, Pooling = false }.ToString());
        db.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "PRAGMA busy_timeout=10000;";
        cmd.ExecuteNonQuery();
        return db;
    }
    public async Task InitializeAsync()
    {
        await Gate.WaitAsync();
        try
        {
            using var db = Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = "PRAGMA user_version";
            var version = Convert.ToInt32(cmd.ExecuteScalar());
            if (version > SchemaVersion) throw new InvalidDataException("Banco criado por uma versão mais recente.");
            if (version == 0)
            {
                using var tx = db.BeginTransaction();
                cmd.Transaction = tx;
                cmd.CommandText = """
                    CREATE TABLE Records(Id TEXT PRIMARY KEY, Kind TEXT NOT NULL, ParentId TEXT REFERENCES Records(Id) ON DELETE RESTRICT, UniqueKey TEXT UNIQUE, Data TEXT NOT NULL);
                    CREATE INDEX IX_Records_KindParent ON Records(Kind, ParentId);
                    CREATE TABLE Links(SourceId TEXT NOT NULL REFERENCES Records(Id) ON DELETE CASCADE, TargetId TEXT NOT NULL REFERENCES Records(Id) ON DELETE RESTRICT, PRIMARY KEY(SourceId,TargetId));
                    CREATE TABLE Metadata(Key TEXT PRIMARY KEY, Value TEXT NOT NULL);
                    INSERT INTO Metadata VALUES('revision','0');
                    INSERT INTO Metadata VALUES('last-change-utc','1970-01-01T00:00:00+00:00');
                    PRAGMA user_version=1;
                    """;
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            Directory.CreateDirectory(AttachmentsPath);
            initialized = true;
        }
        finally { Gate.Release(); }
    }
    internal static List<T> Read<T>(SqliteConnection db, SqliteTransaction? tx = null) where T : Entry
    {
        using var cmd = db.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT Data FROM Records WHERE Kind=$kind ORDER BY rowid";
        cmd.Parameters.AddWithValue("$kind", typeof(T).Name);
        using var reader = cmd.ExecuteReader();
        var result = new List<T>();
        while (reader.Read()) result.Add(JsonSerializer.Deserialize<T>(reader.GetString(0)) ?? throw new InvalidDataException("Registro inválido."));
        return result;
    }
    internal static Entry? Find(SqliteConnection db, SqliteTransaction tx, string id)
    {
        using var cmd = db.CreateCommand(); cmd.Transaction = tx;
        cmd.CommandText = "SELECT Kind,Data FROM Records WHERE Id=$id"; cmd.Parameters.AddWithValue("$id", id);
        using var r = cmd.ExecuteReader();
        return r.Read() ? (Entry?)JsonSerializer.Deserialize(r.GetString(1), Types[r.GetString(0)]) : null;
    }
    internal static void Put(SqliteConnection db, SqliteTransaction tx, Entry entry)
    {
        entry.UpdatedUtc = DateTimeOffset.UtcNow;
        var existing = Find(db, tx, entry.Id);
        if (existing is not null && (existing.GetType() != entry.GetType() || existing.ParentId != entry.ParentId)) throw new InvalidOperationException("Não é permitido transferir vínculos de um registro existente.");
        using var cmd = db.CreateCommand(); cmd.Transaction = tx;
        cmd.CommandText = "INSERT INTO Records(Id,Kind,ParentId,UniqueKey,Data) VALUES($id,$kind,$parent,$unique,$data) ON CONFLICT(Id) DO UPDATE SET Data=excluded.Data,UniqueKey=excluded.UniqueKey";
        cmd.Parameters.AddWithValue("$id", entry.Id); cmd.Parameters.AddWithValue("$kind", entry.GetType().Name);
        cmd.Parameters.AddWithValue("$parent", (object?)entry.ParentId ?? DBNull.Value);
        string? unique = entry switch { Lesson l => $"lesson:{l.SubjectId}:{l.Number}", PlannedLesson p => $"plan:{p.LessonId}", _ => null };
        cmd.Parameters.AddWithValue("$unique", (object?)unique ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$data", JsonSerializer.Serialize(entry, entry.GetType())); cmd.ExecuteNonQuery();
    }
    internal static void Link(SqliteConnection db, SqliteTransaction tx, string source, IEnumerable<string> targets)
    {
        using var cmd = db.CreateCommand(); cmd.Transaction = tx;
        cmd.CommandText = "DELETE FROM Links WHERE SourceId=$id"; cmd.Parameters.AddWithValue("$id", source); cmd.ExecuteNonQuery();
        foreach (var target in targets.Distinct())
        {
            cmd.Parameters.Clear(); cmd.CommandText = "INSERT INTO Links VALUES($source,$target)";
            cmd.Parameters.AddWithValue("$source", source); cmd.Parameters.AddWithValue("$target", target); cmd.ExecuteNonQuery();
        }
    }
    internal async Task<T> ReadAsync<T>(Func<SqliteConnection, T> action)
    {
        if (!initialized) await InitializeAsync();
        await Gate.WaitAsync();
        try { using var db = Open(); return action(db); }
        finally { Gate.Release(); }
    }
    internal async Task MutateAsync(Action<SqliteConnection, SqliteTransaction> action)
    {
        if (!initialized) await InitializeAsync();
        await Gate.WaitAsync();
        try
        {
            await Task.Run(() =>
            {
                using var db = Open(); using var tx = db.BeginTransaction(); action(db, tx);
                using var cmd = db.CreateCommand(); cmd.Transaction = tx;
                cmd.CommandText = "UPDATE Metadata SET Value=CAST(Value AS INTEGER)+1 WHERE Key='revision'; INSERT INTO Metadata(Key,Value) VALUES('last-change-utc',$utc) ON CONFLICT(Key) DO UPDATE SET Value=excluded.Value;";
                cmd.Parameters.AddWithValue("$utc",DateTimeOffset.UtcNow.ToString("O"));cmd.ExecuteNonQuery(); tx.Commit();
            });
        }
        finally { Gate.Release(); }
        Changed?.Invoke(this, EventArgs.Empty);
    }
    public Task<List<T>> AllAsync<T>() where T : Entry => ReadAsync(db => Read<T>(db));
    public Task<long> RevisionAsync() => ReadAsync(db => Revision(db));
    internal static long Revision(SqliteConnection db)
    {
        using var cmd = db.CreateCommand(); cmd.CommandText = "SELECT Value FROM Metadata WHERE Key='revision'"; return Convert.ToInt64(cmd.ExecuteScalar());
    }
    public Task DeleteAsync(string id) => MutateAsync((db, tx) =>
    {
        using var cmd = db.CreateCommand(); cmd.Transaction = tx;
        cmd.Parameters.AddWithValue("$id", id);
        try
        {
            if (Find(db,tx,id) is Subject)
            {
                // Generated lessons can be removed only when no planning or history references them.
                cmd.CommandText="DELETE FROM Records WHERE ParentId=$id AND Kind='Lesson'";cmd.ExecuteNonQuery();
            }
            cmd.CommandText = "DELETE FROM Records WHERE Id=$id";cmd.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19) { throw new InvalidOperationException("Há registros vinculados. Remova primeiro os registros dependentes.", ex); }
    });
}

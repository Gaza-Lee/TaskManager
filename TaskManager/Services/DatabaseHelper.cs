using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using TaskManager.Data;

namespace TaskManager.Services;

public static class DatabaseHelper
{
    public static void Initialize(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
        
        // Ensure database exists
        db.Database.EnsureCreated();
        
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) connection.Open();

        using var command = connection.CreateCommand();

        // Enable WAL mode for concurrent Blazor circuit safety
        command.CommandText = "PRAGMA journal_mode=WAL;";
        command.ExecuteNonQuery();
        command.CommandText = "PRAGMA synchronous=NORMAL;";
        command.ExecuteNonQuery();
        
        // 1. Repair/Expand Tasks Table
        command.CommandText = "PRAGMA table_info(Tasks);";
        var columns = new List<string>();
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read()) columns.Add(reader["name"].ToString()!);
        }

        AddColumnIfMissing(command, columns, "NeedsHelp", "INTEGER DEFAULT 0");
        AddColumnIfMissing(command, columns, "HelpDetails", "TEXT DEFAULT ''");
        AddColumnIfMissing(command, columns, "NeedsModification", "INTEGER DEFAULT 0");
        AddColumnIfMissing(command, columns, "AuditRemark", "TEXT DEFAULT ''");
        AddColumnIfMissing(command, columns, "ModificationHistory", "TEXT DEFAULT '[]'");

        // 2. Create Supplemental Tables
        CreateTableIfNotExists(command, "Remarks", 
            "Id TEXT PRIMARY KEY, TaskId TEXT, Author TEXT, Content TEXT, CreatedAt TEXT, " +
            "CONSTRAINT FK_Remarks_Tasks_TaskId FOREIGN KEY (TaskId) REFERENCES Tasks (Id) ON DELETE CASCADE");

        CreateTableIfNotExists(command, "Notifications", 
            "Id TEXT PRIMARY KEY, Recipient TEXT, Message TEXT, TaskId TEXT, IsRead INTEGER, CreatedAt TEXT, Type TEXT");
    }

    private static void AddColumnIfMissing(IDbCommand command, List<string> existingColumns, string columnName, string definition)
    {
        if (!existingColumns.Contains(columnName, StringComparer.OrdinalIgnoreCase))
        {
            command.CommandText = $"ALTER TABLE Tasks ADD COLUMN {columnName} {definition};";
            command.ExecuteNonQuery();
        }
    }

    private static void CreateTableIfNotExists(IDbCommand command, string tableName, string schema)
    {
        command.CommandText = $"CREATE TABLE IF NOT EXISTS {tableName} ({schema});";
        command.ExecuteNonQuery();
    }
}

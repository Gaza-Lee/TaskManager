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

        
        // 1. Repair/Expand Tasks Table
        command.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tasks';";
        var columns = new List<string>();
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read()) columns.Add(reader["COLUMN_NAME"].ToString()!);
        }

        AddColumnIfMissing(command, columns, "NeedsHelp", "BIT DEFAULT 0");
        AddColumnIfMissing(command, columns, "HelpDetails", "NVARCHAR(MAX) DEFAULT ''");
        AddColumnIfMissing(command, columns, "NeedsModification", "BIT DEFAULT 0");
        AddColumnIfMissing(command, columns, "AuditRemark", "NVARCHAR(MAX) DEFAULT ''");
        AddColumnIfMissing(command, columns, "ModificationHistory", "NVARCHAR(MAX) DEFAULT '[]'");

        // 2. Create Supplemental Tables
        CreateTableIfNotExists(command, "Remarks", 
            "Id UNIQUEIDENTIFIER PRIMARY KEY, TaskId UNIQUEIDENTIFIER, Author NVARCHAR(100), Content NVARCHAR(1000), CreatedAt DATETIME2, " +
            "CONSTRAINT FK_Remarks_Tasks_TaskId FOREIGN KEY (TaskId) REFERENCES Tasks (Id) ON DELETE CASCADE");

        CreateTableIfNotExists(command, "Notifications", 
            "Id UNIQUEIDENTIFIER PRIMARY KEY, Recipient NVARCHAR(100), Message NVARCHAR(500), TaskId UNIQUEIDENTIFIER, IsRead BIT, CreatedAt DATETIME2, Type NVARCHAR(50)");
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
        command.CommandText = $@"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{tableName}')
            BEGIN
                CREATE TABLE {tableName} ({schema});
            END";
        command.ExecuteNonQuery();
    }
}

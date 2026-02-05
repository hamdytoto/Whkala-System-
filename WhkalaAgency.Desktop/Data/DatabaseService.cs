using System;
using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;

namespace WhkalaAgency.Desktop.Data;

public static class DatabaseService
{
    private static readonly string BaseDirectory =
        AppDomain.CurrentDomain.BaseDirectory;

    private static readonly string DbDirectory =
        Path.Combine(BaseDirectory, "Data");

    private static readonly string DbPath =
        Path.Combine(DbDirectory, "agency.db");

    // ملف السكربت الخاص بإنشاء الجداول
    // يتم نسخه إلى نفس مجلد التشغيل بجانب الـ exe
    private static readonly string SchemaPath =
        Path.Combine(BaseDirectory, "Schema.sql");

    public static readonly string ConnectionString =
        $"Data Source={DbPath}";

    public static void Initialize()
    {
        if (!Directory.Exists(DbDirectory))
        {
            Directory.CreateDirectory(DbDirectory);
        }

        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();

            // تطبيق سكربت إنشاء الجداول دائماً (الأوامر معرفّة بـ IF NOT EXISTS و OR IGNORE)
            if (File.Exists(SchemaPath))
            {
                var schemaSql = File.ReadAllText(SchemaPath);
                using var cmd = connection.CreateCommand();
                cmd.CommandText = schemaSql;
                cmd.ExecuteNonQuery();
            }
        }
    }

    public static SqliteConnection GetConnection()
    {
        var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        return connection;
    }

    public static object? ExecuteScalar(string sql, params SqliteParameter[] parameters)
    {
        using var connection = GetConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        if (parameters is { Length: > 0 })
        {
            cmd.Parameters.AddRange(parameters);
        }

        return cmd.ExecuteScalar();
    }

    public static int ExecuteNonQuery(string sql, params SqliteParameter[] parameters)
    {
        using var connection = GetConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        if (parameters is { Length: > 0 })
        {
            cmd.Parameters.AddRange(parameters);
        }

        return cmd.ExecuteNonQuery();
    }

    public static DataTable ExecuteDataTable(string sql, params SqliteParameter[] parameters)
    {
        using var connection = GetConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        if (parameters is { Length: > 0 })
        {
            cmd.Parameters.AddRange(parameters);
        }

        using var reader = cmd.ExecuteReader();
        var table = new DataTable();
        table.Load(reader);
        return table;
    }
}

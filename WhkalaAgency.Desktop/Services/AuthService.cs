using System;
using Microsoft.Data.Sqlite;
using WhkalaAgency.Desktop.Data;
using WhkalaAgency.Desktop.Models;

namespace WhkalaAgency.Desktop.Services;

public static class AuthService
{
    public static User? Login(string username, string password)
    {
        const string sql = @"
SELECT Id, Username, PasswordHash, FullName, Role, IsActive
FROM Users
WHERE Username = $u AND PasswordHash = $p AND IsActive = 1
LIMIT 1;
";

        using var connection = DatabaseService.GetConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqliteParameter("$u", username));
        cmd.Parameters.Add(new SqliteParameter("$p", password)); // يمكن لاحقاً استبداله بتجزئة كلمة المرور

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return new User
        {
            Id = reader.GetInt32(0),
            Username = reader.GetString(1),
            PasswordHash = reader.GetString(2),
            FullName = reader.GetString(3),
            Role = reader.GetString(4),
            IsActive = reader.GetBoolean(5)
        };
    }
}


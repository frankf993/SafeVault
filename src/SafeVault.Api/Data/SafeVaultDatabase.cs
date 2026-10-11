using Microsoft.Data.SqlClient;

namespace SafeVault.Api.Data;

public static class SafeVaultDatabase
{
    public static async Task InitializeAsync(string connectionString)
    {
        const string sql = """
            IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Users (
                    UserId INT IDENTITY(1,1) PRIMARY KEY,
                    Username NVARCHAR(100) NOT NULL UNIQUE,
                    Email NVARCHAR(100) NOT NULL,
                    PasswordHash NVARCHAR(500) NOT NULL,
                    Role NVARCHAR(20) NOT NULL DEFAULT N'User'
                );

                CREATE INDEX IX_Users_Username ON dbo.Users(Username);
            END
            """;

        await using var connection = new SqlConnection(connectionString);
        await using var command = new SqlCommand(sql, connection);

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }
}

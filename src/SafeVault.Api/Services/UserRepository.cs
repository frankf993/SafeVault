using System.Data;
using Microsoft.Data.SqlClient;
using SafeVault.Api.Models;

namespace SafeVault.Api.Services;

public class UserRepository
{
    private readonly string _connectionString;

    public UserRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SafeVault")
            ?? throw new InvalidOperationException("Connection string 'SafeVault' non trovata.");
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        const string sql = """
            SELECT UserId, Username, Email, PasswordHash, Role
            FROM dbo.Users
            WHERE Username = @Username;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@Username", SqlDbType.NVarChar, 100).Value = username;

        await connection.OpenAsync();

        await using var reader = await command.ExecuteReaderAsync();

        return await reader.ReadAsync() ? MapUser(reader) : null;
    }

    public async Task<bool> ExistsAsync(string username)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.Users
            WHERE Username = @Username;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@Username", SqlDbType.NVarChar, 100).Value = username;

        await connection.OpenAsync();

        return (int)await command.ExecuteScalarAsync() > 0;
    }

    public async Task CreateAsync(User user)
    {
        const string sql = """
            INSERT INTO dbo.Users (Username, Email, PasswordHash, Role)
            VALUES (@Username, @Email, @PasswordHash, @Role);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@Username", SqlDbType.NVarChar, 100).Value = user.Username;
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 100).Value = user.Email;
        command.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 500).Value = user.PasswordHash;
        command.Parameters.Add("@Role", SqlDbType.NVarChar, 20).Value = user.Role;

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }

    private static User MapUser(SqlDataReader reader) => new()
    {
        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
        Username = reader.GetString(reader.GetOrdinal("Username")),
        Email = reader.GetString(reader.GetOrdinal("Email")),
        PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
        Role = reader.GetString(reader.GetOrdinal("Role"))
    };
}

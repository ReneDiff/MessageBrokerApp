using MessageShared;
using System;
using Npgsql;
using System.Threading.Tasks; 

namespace MessageConsumer;

// Gemmer beskeder i en PostgreSQL-database.
public class PostgresService : IDatabaseService
{
    private readonly string _connectionString;

    public PostgresService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task SaveMessageAsync(Message message)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        
        await conn.OpenAsync();

        using var cmd = new NpgsqlCommand("INSERT INTO messages (timestamp, counter) VALUES (@timestamp, @counter)", conn);

        cmd.Parameters.AddWithValue("timestamp", message.Timestamp);
        cmd.Parameters.AddWithValue("counter",message.Counter);

        await cmd.ExecuteNonQueryAsync();

        Console.WriteLine($"[DB] Gemte besked: Counter={message.Counter}, Timestamp={message.Timestamp:HH:mm:ss}");
    }
}
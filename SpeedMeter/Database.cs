using MySqlConnector;

namespace cs2_speedmeter
{
    public class Database
    {
        private readonly string _connectionString;

        public Database(string host, int port, string username, string password, string database)
        {
            _connectionString = $"Server={host};Port={port};User ID={username};Password={password};Database={database};";
        }

        private async Task<MySqlConnection> OpenConnectionAsync()
        {
            var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        public async Task InitializeDatabaseAsync()
        {
            using var connection = await OpenConnectionAsync();

            using var cmd = new MySqlCommand(@"
CREATE TABLE IF NOT EXISTS speedmeter_records (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    SteamId BIGINT UNSIGNED NOT NULL UNIQUE,
    PlayerName VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    Speed FLOAT NOT NULL
);

CREATE TABLE IF NOT EXISTS speedmeter_settings (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    SteamId BIGINT UNSIGNED NOT NULL UNIQUE,
    X FLOAT NOT NULL,
    Y FLOAT NOT NULL,
    Size TINYINT NOT NULL,
    Color VARCHAR(32) NOT NULL
);
            ", connection);

            await cmd.ExecuteNonQueryAsync();
        }

        #region Settings

        public async Task SaveSettingAsync(ulong steamId, float x, float y, byte size, string color)
        {
            using var connection = await OpenConnectionAsync();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                INSERT INTO speedmeter_settings (SteamId, X, Y, Size, Color) 
                VALUES (@SteamId, @X, @Y, @Size, @Color)
                ON DUPLICATE KEY UPDATE X = @X, Y = @Y, Size = @Size, Color = @Color;
            ";

            command.Parameters.AddWithValue("@SteamId", steamId);
            command.Parameters.AddWithValue("@X", x);
            command.Parameters.AddWithValue("@Y", y);
            command.Parameters.AddWithValue("@Size", size);
            command.Parameters.AddWithValue("@Color", color);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<SpeedSettingsDatabase?> GetSettingAsync(ulong steamId)
        {
            using var connection = await OpenConnectionAsync();
            using var command = connection.CreateCommand();

            command.CommandText = "SELECT X, Y, Size, Color FROM speedmeter_settings WHERE SteamId = @SteamId;";
            command.Parameters.AddWithValue("@SteamId", steamId);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new SpeedSettingsDatabase
                {
                    X = reader.GetFloat("X"),
                    Y = reader.GetFloat("Y"),
                    Size = reader.GetByte("Size"),
                    Color = reader.GetString("Color")
                };
            }

            return null;
        }

        public async Task<Dictionary<ulong, SpeedSettingsDatabase>> GetAllSettingsAsync()
        {
            using var connection = await OpenConnectionAsync();
            using var command = connection.CreateCommand();

            command.CommandText = "SELECT SteamId, X, Y, Size FROM speedmeter_settings;";

            var settings = new Dictionary<ulong, SpeedSettingsDatabase>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var steamId = (ulong)reader.GetInt64("SteamId");
                settings[steamId] = new SpeedSettingsDatabase
                {
                    X = reader.GetFloat("X"),
                    Y = reader.GetFloat("Y"),
                    Size = reader.GetByte("Size")
                };
            }

            return settings;
        }

        #endregion

        #region Speed Records

        public async Task SaveSpeedRecordAsync(ulong steamId, string playerName, float speed)
        {
            using var connection = await OpenConnectionAsync();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                INSERT INTO speedmeter_records (SteamId, PlayerName, Speed)
                VALUES (@SteamId, @PlayerName, @Speed)
                ON DUPLICATE KEY UPDATE Speed = @Speed;
            ";

            command.Parameters.AddWithValue("@SteamId", steamId);
            command.Parameters.AddWithValue("@PlayerName", playerName);
            command.Parameters.AddWithValue("@Speed", speed);

            await command.ExecuteNonQueryAsync();
        }

        public async Task ResetSpeedRecordDatasAsync()
        {
            using var connection = await OpenConnectionAsync();
            using var command = connection.CreateCommand();

            command.CommandText = "TRUNCATE TABLE speedmeter_records;";

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<SpeedRecord>> GetTopSpeedRecordsAsync(int limit = 10)
        {
            using var connection = await OpenConnectionAsync();
            using var command = connection.CreateCommand();

            command.CommandText = limit == -1
                ? @"
                    SELECT SteamId, PlayerName, Speed
                    FROM speedmeter_records
                    ORDER BY Speed DESC;"
                : @"
                    SELECT SteamId, PlayerName, Speed 
                    FROM speedmeter_records
                    ORDER BY Speed DESC
                    LIMIT @limit;";

            if (limit != -1)
            {
                command.Parameters.AddWithValue("@limit", limit);
            }

            var records = new List<SpeedRecord>();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(new SpeedRecord
                {
                    SteamId = (ulong)reader.GetInt64("SteamId"),
                    PlayerName = reader.GetString("PlayerName"),
                    Speed = reader.GetFloat("Speed")
                });
            }

            return records;
        }

        public async Task<SpeedRecord?> GetPlayerBestSpeedAsync(ulong steamId)
        {
            using var connection = await OpenConnectionAsync();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT SteamId, PlayerName, Speed
                FROM speedmeter_records
                WHERE SteamId = @SteamId
                ORDER BY Speed DESC
                LIMIT 1;
            ";

            command.Parameters.AddWithValue("@SteamId", steamId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new SpeedRecord
                {
                    SteamId = (ulong)reader.GetInt64("SteamId"),
                    PlayerName = reader.GetString("PlayerName"),
                    Speed = reader.GetFloat("Speed")
                };
            }

            return null;
        }

        #endregion
    }

    public class SpeedRecord
    {
        public ulong SteamId { get; set; }
        public string PlayerName { get; set; } = "";
        public float Speed { get; set; }
    }

    public class SpeedSettingsDatabase
    {
        public float X { get; set; } = 0.57f;
        public float Y { get; set; } = 4.367f;
        public byte Size { get; set; } = 50;
        public string Color { get; set; } = "White";
    }
}

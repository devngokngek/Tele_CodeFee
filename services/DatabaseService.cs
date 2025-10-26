using Microsoft.Data.Sqlite;
using System;
using System.Threading.Tasks;

namespace TeleBot.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string dbPath = "data.db")
        {
            _connectionString = $"Data Source={dbPath}";
            InitializeDatabase();
        }

        //get list of users
        public async Task<List<long>> GetAllUserIdsAsync()
        {
            var userIds = new List<long>();

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT ChatId FROM Users";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                userIds.Add(reader.GetInt64(0));
            }
            foreach (var id in userIds)
            {
                Console.WriteLine($"👤 User ChatId: {id}");
            }
            Console.WriteLine($"📊 Đã lấy danh sách {userIds.Count} user từ database.");
            return userIds;
        }
        public async Task InitializeAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
        }
        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ChatId INTEGER NOT NULL,
                Username TEXT,
                FirstSeen DATETIME DEFAULT CURRENT_TIMESTAMP
            );";
            cmd.ExecuteNonQuery();
        }
        public async Task AddUserAsync(long chatId, string username)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            // Kiểm tra tồn tại
            var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM Users WHERE ChatId = $chatId";
            checkCmd.Parameters.AddWithValue("$chatId", chatId);

            var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;

            if (exists)
            {
                Console.WriteLine($"👀 User {chatId} đã tồn tại, bỏ qua.");
                return;
            }

            // Thêm mới
            var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = "INSERT INTO Users (ChatId, Username) VALUES ($chatId, $username)";
            insertCmd.Parameters.AddWithValue("$chatId", chatId);
            insertCmd.Parameters.AddWithValue("$username", username ?? "unknown");

            await insertCmd.ExecuteNonQueryAsync();
            Console.WriteLine($"✅ Đã thêm user mới: {chatId}");
        }


        public async Task<int> CountUsersAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Users";
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
    }
}

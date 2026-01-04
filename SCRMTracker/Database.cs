using SCRMTracker.Items;
using System.Collections.ObjectModel;
using Microsoft.Data.Sqlite;
using System.IO;

namespace SCRMTracker
{
    public class Database
    {
        private readonly string DatabaseConnStr;

        /// <summary>
        /// Constructor for Database singleton class
        /// </summary>
        /// <param name="connStr">SQLite connection string</param>
        private Database(string connStr)
        {
            DatabaseConnStr = connStr;
            InitializeTableAsync().GetAwaiter().GetResult();
        }

        private static Database? instance;

        /// <summary>
        /// Get Database class singleton object
        /// </summary>
        /// <returns>Database class singleton object</returns>
        public static Database GetInstance()
        {
            if (instance == null)
            {
                string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Players.db");
                string connStr = $"Data Source={dbPath};Mode=ReadWriteCreate;Cache=Shared;Foreign Keys=True;";
                instance = new Database(connStr);
            }

            return instance;
        }

        /// <summary>
        /// Enable WAL mode for atomic commits
        /// </summary>
        /// <param name="conn">SqliteConnection object</param>
        private async Task EnableWALModeAsync(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA journal_mode=WAL;";
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Initialize database by db file, create table and index (if possible)
        /// </summary>
        public async Task InitializeTableAsync()
        {
            using (var conn = new SqliteConnection(DatabaseConnStr))
            {
                await conn.OpenAsync();

                // Enable WAL for atomic commit
                await EnableWALModeAsync(conn);

                // Create table and index if not exists
                // IP, username would be automatically removed by FOREIGN KEY DELETE CASCADE
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS PLAYERBATTLETAGS(BATTLETAG TEXT PRIMARY KEY, MEETDATE TEXT NOT NULL, ISBLACKLIST INTEGER DEFAULT 0, COMMENT TEXT);

                        CREATE TABLE IF NOT EXISTS PLAYERUSERNAMES(ID INTEGER PRIMARY KEY AUTOINCREMENT, BATTLETAG TEXT NOT NULL, USERNAME TEXT NOT NULL,
                        UNIQUE(BATTLETAG, USERNAME), FOREIGN KEY (BATTLETAG) REFERENCES PLAYERBATTLETAGS(BATTLETAG) ON DELETE CASCADE);

                        CREATE TABLE IF NOT EXISTS PLAYERIPS(ID INTEGER PRIMARY KEY AUTOINCREMENT, BATTLETAG TEXT NOT NULL, IPHASH BLOB NOT NULL,
                        UNIQUE(BATTLETAG, IPHASH), FOREIGN KEY (BATTLETAG) REFERENCES PLAYERBATTLETAGS(BATTLETAG) ON DELETE CASCADE);

                        CREATE INDEX IF NOT EXISTS idx_battletags ON PLAYERBATTLETAGS(BATTLETAG);
                        CREATE INDEX IF NOT EXISTS idx_username ON PLAYERUSERNAMES(USERNAME);
                        CREATE INDEX IF NOT EXISTS idx_ip ON PLAYERIPS(IPHASH);
                    ";
                await cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Load player list from database
        /// </summary>
        /// <returns>ObservableCollection<DatabasePlayer> for item binding</returns>
        public async Task<ObservableCollection<DatabasePlayer>> LoadPlayersAsync()
        {
            var playerList = new ObservableCollection<DatabasePlayer>();
            var playerUsernameDict = new Dictionary<string, List<string>>();
            var playerIPDict = new Dictionary<string, List<byte[]>>();

            using (var conn = new SqliteConnection(DatabaseConnStr))
            {
                await conn.OpenAsync();

                // Load usernames for this player
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT BATTLETAG, USERNAME FROM PLAYERUSERNAMES";
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string battleTag = reader.GetString(0);
                            string username = reader.GetString(1);
                            if (!playerUsernameDict.ContainsKey(battleTag))
                            {
                                playerUsernameDict[battleTag] = new List<string>();
                            }

                            playerUsernameDict[battleTag].Add(username);
                        }
                    }
                }

                // Load IP hashes for this player
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT BATTLETAG, IPHASH FROM PLAYERIPS";
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string battleTag = reader.GetString(0);
                            byte[] ipHash = (byte[])reader.GetValue(1);
                            if (!playerIPDict.ContainsKey(battleTag))
                            {
                                playerIPDict[battleTag] = new List<byte[]>();
                            }

                            playerIPDict[battleTag].Add(ipHash);
                        }
                    }
                }

                // Load player's information
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT BATTLETAG, MEETDATE, ISBLACKLIST, COMMENT FROM PLAYERBATTLETAGS";
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string battleTag = reader.GetString(0);
                            string meetDate = reader.GetString(1);
                            bool isBlacklist = reader.GetBoolean(2);
                            string comment = reader.GetString(3);
                            List<string> userNameList = playerUsernameDict.ContainsKey(battleTag) ? playerUsernameDict[battleTag] : new List<string>();
                            List<byte[]> ipHashList = playerIPDict.ContainsKey(battleTag) ? playerIPDict[battleTag] : new List<byte[]>();

                            playerList.Add(new DatabasePlayer(battleTag, meetDate, isBlacklist, comment, userNameList, ipHashList));
                        }
                    }
                }
            }

            return playerList;
        }

        /// <summary>
        /// Add new player to database
        /// </summary>
        /// <param name="battleTag">Player's battletag</param>
        /// <param name="userName">Player's username</param>
        /// <param name="ipAddressHash">SHA256 hash of player, can be null</param>
        public async Task AddNewPlayerAsync(string battleTag, string userName, byte[]? ipAddressHash)
        {
            using (var conn = new SqliteConnection(DatabaseConnStr))
            {
                await conn.OpenAsync();

                // Add parent table (PLAYERBATTLETAGS)
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        INSERT INTO PLAYERBATTLETAGS (BATTLETAG, MEETDATE, ISBLACKLIST, COMMENT) VALUES (@tag, @time, 0, '')
                    ";
                    cmd.Parameters.AddWithValue("@tag", battleTag);
                    cmd.Parameters.AddWithValue("@time", DateTime.Now.ToString("yyyy-MM-dd"));
                    await cmd.ExecuteNonQueryAsync();
                }

                // Add username to child table
                if (!string.IsNullOrEmpty(userName))
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            INSERT OR IGNORE INTO PLAYERUSERNAMES (BATTLETAG, USERNAME)
                            VALUES (@tag, @username)
                        ";
                        cmd.Parameters.AddWithValue("@tag", battleTag);
                        cmd.Parameters.AddWithValue("@username", userName);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                // Add ip address info to child table
                if (ipAddressHash != null && ipAddressHash.Length == 32)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            INSERT OR IGNORE INTO PLAYERIPS (BATTLETAG, IPHASH)
                            VALUES (@tag, @iphash)
                        ";
                        cmd.Parameters.AddWithValue("@tag", battleTag);
                        cmd.Parameters.AddWithValue("@iphash", ipAddressHash);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Remove players from database, subtable rows are automatically removed by DELETE CASCADE
        /// </summary>
        /// <param name="selectedPlayers">List of selected DatabasePlayer</param>
        /// <returns>Removed rows count</returns>
        public async Task<int> RemovePlayerListAsync(List<DatabasePlayer> selectedPlayers)
        {
            int result = 0;
            using (var conn = new SqliteConnection(DatabaseConnStr))
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM PLAYERBATTLETAGS WHERE BATTLETAG = @tag";
                    foreach (var player in selectedPlayers)
                    {
                        cmd.Parameters.AddWithValue("@tag", player.BattleTag);
                        result += await cmd.ExecuteNonQueryAsync();
                        cmd.Parameters.Clear();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Remove player from database, subtable rows are automatically removed by DELETE CASCADE
        /// </summary>
        /// <param name="battleTag">Target battletag to remove</param>
        /// <returns>Removed rows count</returns>
        public async Task<int> RemovePlayerAsync(string battleTag)
        {
            using (var conn = new SqliteConnection(DatabaseConnStr))
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM PLAYERBATTLETAGS WHERE BATTLETAG = @tag";
                    cmd.Parameters.AddWithValue("@tag", battleTag);
                    return await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Modify player's blacklist boolean
        /// </summary>
        /// <param name="battleTag">Target battletag to modify</param>
        /// <param name="isBlackList">Blacklist boolean</param>
        /// <returns>Modified rows count</returns>
        public async Task<int> ModifyPlayerBlackListAsync(string battleTag, bool isBlackList)
        {
            using (var conn = new SqliteConnection(DatabaseConnStr))
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE PLAYERBATTLETAGS SET ISBLACKLIST = @blacklist WHERE BATTLETAG = @tag";
                    cmd.Parameters.AddWithValue("@blacklist", isBlackList);
                    cmd.Parameters.AddWithValue("@tag", battleTag);
                    return await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Modify player's comment
        /// </summary>
        /// <param name="battleTag">Target battletag to modify</param>
        /// <param name="comment">Comment text with UTF-8</param>
        /// <returns>Modified rows count</returns>
        public async Task<int> ModifyPlayerCommentAsync(string battleTag, string comment)
        {
            using (var conn = new SqliteConnection(DatabaseConnStr))
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE PLAYERBATTLETAGS SET COMMENT = @comment WHERE BATTLETAG = @tag";
                    cmd.Parameters.AddWithValue("@comment", comment);
                    cmd.Parameters.AddWithValue("@tag", battleTag);
                    return await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Add new username for battletag
        /// </summary>
        /// <param name="battleTag">Target battle tag to add username</param>
        /// <param name="userName">Username</param>
        /// <returns>New rows count</returns>
        public async Task<int> AddPlayerUsernameAsync(string battleTag, string userName)
        {
            using (var conn = new SqliteConnection(DatabaseConnStr))
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                            INSERT OR IGNORE INTO PLAYERUSERNAMES (BATTLETAG, USERNAME)
                            VALUES (@tag, @username)
                        ";
                    cmd.Parameters.AddWithValue("@tag", battleTag);
                    cmd.Parameters.AddWithValue("@username", userName);
                    return await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Add new IP address SHA-256 hash for battletag
        /// </summary>
        /// <param name="battleTag">Target battle tag to add username</param>
        /// <param name="ipAddressHash">IP address SHA-256 hash</param>
        /// <returns>New rows count</returns>
        public async Task<int> AddPlayerIPHashAsync(string battleTag, byte[] ipAddressHash)
        {
            using (var conn = new SqliteConnection(DatabaseConnStr))
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                            INSERT OR IGNORE INTO PLAYERIPS (BATTLETAG, IPHASH)
                            VALUES (@tag, @iphash)
                        ";
                    cmd.Parameters.AddWithValue("@tag", battleTag);
                    cmd.Parameters.AddWithValue("@iphash", ipAddressHash);
                    return await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}

using SCRMTracker.Items;
using System.Collections.ObjectModel;

namespace SCRMTracker
{
    /// <summary>
    /// Child class of IEqualityCompare to compare byte[] at Dictionary
    /// </summary>
    public class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[]? x, byte[]? y)
        {
            if (x == null || y == null)
                return x == y;

            if (x.Length != y.Length)
                return false;

            return x.SequenceEqual(y);
        }

        public int GetHashCode(byte[] obj)
        {
            if (obj == null || obj.Length == 0)
                return 0;

            if (obj.Length >= 4)
                return BitConverter.ToInt32(obj, 0);

            unchecked
            {
                int hash = 17;
                foreach (byte b in obj)
                {
                    hash = hash * 31 + b;
                }
                return hash;
            }
        }

        public static bool AreEqual(byte[] a, byte[] b)
        {
            if (a == null || b == null)
                return a == b;

            return a.SequenceEqual(b);
        }
    }

    /// <summary>
    /// DatabasePlayerRepo class, manage Dictionary with ObservableCollection, Dictionary
    /// </summary>
    public class DatabasePlayerRepo
    {
        public ObservableCollection<DatabasePlayer> DatabasePlayersCollection { get; private set; }

        private Dictionary<string, DatabasePlayer> DatabasePlayerByBattleTagDict;

        private Dictionary<byte[], List<DatabasePlayer>> DatabasePlayerByIPHashDict;

        public DatabasePlayerRepo()
        {
            DatabasePlayersCollection = new ObservableCollection<DatabasePlayer>();
            DatabasePlayerByBattleTagDict = new Dictionary<string, DatabasePlayer>();
            DatabasePlayerByIPHashDict = new Dictionary<byte[], List<DatabasePlayer>>(new ByteArrayComparer());

            // CollectionChanged event to automatically add IP hash, username Dict to find proper information with O(1) Dictionary
            DatabasePlayersCollection.CollectionChanged += DatabasePlayersCollection_CollectionChanged;
        }

        /// <summary>
        /// Load player database from .\Players.db
        /// </summary>
        /// <returns></returns>
        public async Task LoadFromDatabaseAsync()
        {
            var players = await Database.GetInstance().LoadPlayersAsync();

            DatabasePlayersCollection.Clear();
            DatabasePlayerByBattleTagDict.Clear();
            DatabasePlayerByIPHashDict.Clear();

            foreach (var player in players)
            {
                // Use Add() to trigger event, = await ....LoadPlayersAsync() function does not trigger CollectionChanged event
                DatabasePlayersCollection.Add(player);
            }
        }

        /// <summary>
        /// CollectionChanged event to automatically add IP hash, username Dict to find proper information with O(1) Dictionary
        /// </summary>
        private void DatabasePlayersCollection_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // New items
            if (e.NewItems != null)
            {
                foreach (DatabasePlayer player in e.NewItems)
                {
                    DatabasePlayerByBattleTagDict[player.BattleTag] = player;

                    // IP Hash could be same (Shared PC like PC room, etc...) so use List<DatabasePlayer>
                    foreach (var ipHash in player.IPHashList)
                    {
                        if (!DatabasePlayerByIPHashDict.ContainsKey(ipHash))
                            DatabasePlayerByIPHashDict[ipHash] = new List<DatabasePlayer>();

                        DatabasePlayerByIPHashDict[ipHash].Add(player);
                    }
                }
            }

            // Removed items
            if (e.OldItems != null)
            {
                foreach (DatabasePlayer player in e.OldItems)
                {
                    DatabasePlayerByBattleTagDict.Remove(player.BattleTag);

                    foreach (var ipHash in player.IPHashList)
                    {
                        if (DatabasePlayerByIPHashDict.TryGetValue(ipHash, out var list))
                        {
                            list.Remove(player);
                            if (list.Count == 0)
                                DatabasePlayerByIPHashDict.Remove(ipHash);
                        }
                    }
                }

            }
        }

        /// <summary>
        /// Find player by battletag, search for Dictionary object O(1)
        /// </summary>
        /// <param name="battleTag">Player's battletag</param>
        /// <returns>DatabasePlayer if exists</returns>
        public DatabasePlayer? FindPlayerByBattleTag(string battleTag)
        {
            return DatabasePlayerByBattleTagDict.TryGetValue(battleTag, out var player) ? player : null;
        }

        /// <summary>
        /// Find player by IP address hash, serch for Dictionary object O(1)
        /// </summary>
        /// <param name="ipHash">Player's IP address SHA-256 hash</param>
        /// <returns>DatabasePlayer if exists</returns>
        public List<DatabasePlayer>? FindPlayersByIPHash(byte[]? ipHash)
        {
            if (ipHash == null)
                return null;
            else
                return DatabasePlayerByIPHashDict.TryGetValue(ipHash, out var players) ? players : null;
        }
    }
}

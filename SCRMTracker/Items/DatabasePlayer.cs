using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SCRMTracker.Items
{
    public class DatabasePlayer : INotifyPropertyChanged
    {
        public string BattleTag { get; private set; }

        public string MeetDate { get; private set; }

        public bool IsBlackList { get; private set; }

        public string Comment { get; private set; }

        public List<string> UsernameList = new List<string>();

        public List<byte[]> IPHashList = new List<byte[]>();
        
        // Convert username list with multiline username string
        public string UsernameListStr => string.Join("\n", UsernameList);

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// DatabasePlayer Constructor for database read data
        /// </summary>
        /// <param name="_battleTag">Player's battletag</param>
        /// <param name="_meetDate">Player first seen date</param>
        /// <param name="_isBlackList">Blacklist boolean</param>
        /// <param name="_comment">User defined player comment</param>
        /// <param name="_userNameList">Player's username list</param>
        /// <param name="_ipHashList">Player's IP address SHA-256 hash list</param>
        public DatabasePlayer(string _battleTag, string _meetDate, bool _isBlackList, string _comment, List<string> _userNameList, List<byte[]> _ipHashList)
        {
            BattleTag = _battleTag;
            MeetDate = _meetDate;
            IsBlackList = _isBlackList;
            Comment = _comment;
            UsernameList = _userNameList;
            IPHashList = _ipHashList;
        }

        /// <summary>
        /// Constructor for new player data
        /// </summary>
        /// <param name="_battleTag">Player's battletag</param>
        /// <param name="_userNameList">Player's username</param>
        /// <param name="_hashedIPAddress">Player's IP address SHA-256 hash list, can be null due to proxy server</param>
        public DatabasePlayer(string _battleTag, string _userName, byte[]? _hashedIPAddress)
        {
            Database.GetInstance().AddNewPlayerAsync(_battleTag, _userName, _hashedIPAddress).GetAwaiter().GetResult();
            BattleTag = _battleTag;
            MeetDate = DateTime.Now.ToString("yyyy-MM-dd");
            IsBlackList = false;
            Comment = "";
            UsernameList.Add(_userName);
            if (_hashedIPAddress != null) IPHashList.Add(_hashedIPAddress);
        }

        /// <summary>
        /// Update DatabaseUser object's member, trigger PropertyChanged event for ObservableCollection
        /// </summary>
        /// <param name="_isBlackList">Blacklist boolean</param>
        /// <param name="_userName">Player's username</param>
        /// <param name="_hashedIPAddress">Player's IP address SHA-256 hash list, can be null due to proxy server</param>
        /// <param name="_comment">User defined player comment</param>
        public void Update(bool? _isBlackList = null, string? _userName = null, byte[]? _hashedIPAddress = null, string? _comment = null)
        {
            if (_isBlackList.HasValue)
            {
                if (Database.GetInstance().ModifyPlayerBlackListAsync(BattleTag, _isBlackList.Value).GetAwaiter().GetResult() > 0)
                {
                    IsBlackList = _isBlackList.Value;
                    OnPropertyChanged(nameof(IsBlackList));
                }
            }

            if (_userName != null)
            {
                if (!UsernameList.Contains(_userName))
                {
                    if (Database.GetInstance().AddPlayerUsernameAsync(BattleTag, _userName).GetAwaiter().GetResult() > 0)
                    {
                        UsernameList.Add(_userName);
                        OnPropertyChanged(nameof(UsernameListStr));
                    }
                }
            }

            if (_hashedIPAddress != null)
            {
                if (!IPHashList.Contains(_hashedIPAddress))
                {
                    if (Database.GetInstance().AddPlayerIPHashAsync(BattleTag, _hashedIPAddress).GetAwaiter().GetResult() > 0)
                    {
                        IPHashList.Add(_hashedIPAddress);
                    }
                }
            }

            if (_comment != null)
            {
                if (Database.GetInstance().ModifyPlayerCommentAsync(BattleTag, _comment).GetAwaiter().GetResult() > 0)
                {
                    Comment = _comment;
                    OnPropertyChanged(nameof(Comment));
                }
            }
        }
    }
}

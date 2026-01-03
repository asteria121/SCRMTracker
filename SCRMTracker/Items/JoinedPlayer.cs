namespace SCRMTracker.Items
{
    public class JoinedPlayer
    {
        public string JoinTime { get; private set; }

        public byte PID { get; private set; }

        public string Username { get; private set; }

        public string BattleTag { get; private set; }

        public string IPAddress { get; private set; }

        public bool IsBlackList { get; private set; }

        public string Comment { get; private set; }

        /// <summary>
        /// Constructor for JoinedPlayer class, contains information of joined player
        /// </summary>
        /// <param name="_pid"></param>
        /// <param name="_username"></param>
        /// <param name="_battleTag"></param>
        /// <param name="_maskedIpAddress"></param>
        /// <param name="_isBlackList"></param>
        /// <param name="_comment"></param>
        public JoinedPlayer(byte _pid, string _username, string _battleTag, string _maskedIpAddress, bool? _isBlackList, string? _comment = null)
        {
            // Due to log message, does not abstract database related action.
            PID = _pid;
            Username = _username;
            BattleTag = _battleTag;
            IPAddress = _maskedIpAddress;
            IsBlackList = _isBlackList == null ? false : _isBlackList.Value;
            if (_comment != null) Comment = _comment;
            else Comment = "";
            JoinTime = DateTime.Now.ToString("HH:mm:ss");
        }
    }
}

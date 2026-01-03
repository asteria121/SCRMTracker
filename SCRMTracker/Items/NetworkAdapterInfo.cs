namespace SCRMTracker.Items
{
    public class NetworkAdapterInfo
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Description;
        public readonly string FriendlyName;

        /// <summary>
        /// Constructor of NetworkAdapterInfo, contains network adapter's information
        /// </summary>
        /// <param name="_id"></param>
        /// <param name="_name"></param>
        /// <param name="_description"></param>
        /// <param name="_friendlyName"></param>
        public NetworkAdapterInfo(string _id, string _name, string _description, string _friendlyName)
        {
            Id = _id;
            Name = _name;
            Description = _description;
            FriendlyName = _friendlyName;
        }

        public override string ToString()
        {
            return FriendlyName ?? Description;
        }
    }
}
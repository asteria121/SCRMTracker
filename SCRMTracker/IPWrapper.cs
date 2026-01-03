using System.Security.Cryptography;
using System.Net;

namespace SCRMTracker
{
    /// <summary>
    /// IPWrapper class to mask IP address, hash with SHA-256
    /// </summary>
    public class IPWrapper
    {
        public readonly IPAddress IP;
        public readonly bool IsBlizzardProxy;

        /// <summary>
        /// Constructor of IPWrapper
        /// </summary>
        /// <param name="_ipAddress">32bit IPv4 address</param>
        public IPWrapper(uint _ipAddress)
        {
            byte[] bytes = BitConverter.GetBytes(_ipAddress);
            IP = new IPAddress(bytes);

            // Check for blizzard proxy server IP 59.153.xxx.xxx, 158.115.xxx.xxx
            if ((bytes[0] == 59 && bytes[1] == 153) || (bytes[0] == 158 && bytes[1] == 115))
                IsBlizzardProxy = true;
            else
                IsBlizzardProxy = false;
        }

        /// <summary>
        /// Get masked IP address string
        /// </summary>
        /// <returns>Masked IP address string (ex. 192.168.xxx.xxx)</returns>
        public string ToMaskedIP()
        {
            string[] parts = IP.ToString().Split('.');
            string maskedIpAddress = $"{parts[0]}.{parts[1]}.xxx.xxx";
            if (IsBlizzardProxy) maskedIpAddress += " (블리자드 프록시 서버)";

            return maskedIpAddress;
        }

        /// <summary>
        /// Get SHA-256 hash of IP address
        /// </summary>
        /// <returns>32 Bytes SHA-256 hash</returns>
        public byte[]? ToHashBytes()
        {
            if (!IsBlizzardProxy)
            {
                byte[] ipBytes = IP.GetAddressBytes();
                byte[] hash = SHA256.HashData(ipBytes);
                return hash;
            }
            return null;
        }
    }
}

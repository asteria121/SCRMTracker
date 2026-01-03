using System.Runtime.InteropServices;

namespace SCRMTracker
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PlayerInfo
    {
        public byte Pid;
        public IntPtr BattleTag;
        public IntPtr UserName;
        public uint IPAddress;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void PlayerJoinCallback(IntPtr info);

    public class NativeMethods
    {
        private const string DllName = "TrackerCore.dll";

        /// <summary>
        /// Initialize capture service with npcap
        /// </summary>
        /// <param name="strAdapterId">ID of network adapter</param>
        /// <param name="callbackFunction">Callback function which is triggered by player join</param>
        /// <returns></returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool InitializeCaptureService(string strAdapterId, PlayerJoinCallback callbackFunction);

        /// <summary>
        /// Release capture service
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ReleaseCaptureService();

        /// <summary>
        /// Get exception string from TrackerCore.dll
        /// </summary>
        /// <returns>Recent exception string ptr</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetLastException();

        /// <summary>
        /// Helper method for NativeMethods.GetLastException()
        /// </summary>
        /// <returns>Recent exception string</returns>
        public static string GetLastExceptionString()
        {
            IntPtr ptr = GetLastException();
            if (ptr == IntPtr.Zero)
                return string.Empty;

            try
            {
                return Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
            }
            finally
            {
                // Free CoTaskMemAlloc
                Marshal.FreeCoTaskMem(ptr);
            }
        }
    }
}

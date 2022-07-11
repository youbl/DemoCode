using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace DemoCodeConsole.WindowsDemo
{
    internal class WindowsStateCheck
    {
        public static void OutputWindowsLockedStatus()
        {
            while (true)
            {
                Console.WriteLine(GetLockStatus());
                if (IsScreensaverRunning())
                {
                    Console.WriteLine("Screensaver is runing");
                }

                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// 获取系统是否处于锁屏状态
        /// </summary>
        /// <returns>状态值</returns>
        public static LockStatus GetLockStatus()
        {
            uint dwSessionID = WTSGetActiveConsoleSessionId();
            uint dwBytesReturned = 0;
            int dwFlags = 0;
            IntPtr pInfo = IntPtr.Zero;
            WTSQuerySessionInformationW(IntPtr.Zero, dwSessionID, WTS_INFO_CLASS.WTSSessionInfoEx, ref pInfo,
                ref dwBytesReturned);
            var shit = Marshal.PtrToStructure<WTSINFOEXW>(pInfo);

            if (shit.Level == 1)
            {
                dwFlags = shit.Data.WTSInfoExLevel1.SessionFlags;
            }

            switch (dwFlags)
            {
                case 0:
                    return LockStatus.LOCKED;
                case 1:
                    return LockStatus.UNLOCK;
                default:
                    return LockStatus.UNKNOWN;
            }
        }

        public enum LockStatus
        {
            LOCKED,
            UNLOCK,
            UNKNOWN,
        }

        /// <summary>
        /// 屏保程序是否运行中
        /// </summary>
        /// <returns>true false</returns>
        public static bool IsScreensaverRunning()
        {
            const int SPI_GETSCREENSAVERRUNNING = 114;
            bool isRunning = false;

            if (!SystemParametersInfo(SPI_GETSCREENSAVERRUNNING, 0, ref isRunning, 0))
            {
                // Could not detect screen saver status...
                return false;
            }

            if (isRunning)
            {
                // Screen saver is ON.
                return true;
            }

            // Screen saver is OFF.
            return false;
        }

        // Used to check if the screen saver is running
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfo(uint uAction,
            uint uParam, ref bool lpvParam, int fWinIni);

        [DllImport("Wtsapi32.dll", CharSet = CharSet.Unicode)]
        private static extern bool WTSQuerySessionInformationW(IntPtr hServer, uint SessionId,
            WTS_INFO_CLASS WTSInfoClass, ref IntPtr ppBuffer, ref uint pBytesReturned);

        [DllImport("Wtsapi32.dll", CharSet = CharSet.Unicode)]
        private static extern void WTSFreeMemory(IntPtr pMemory);

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern uint WTSGetActiveConsoleSessionId();

        public enum WTS_INFO_CLASS
        {
            WTSInitialProgram,
            WTSApplicationName,
            WTSWorkingDirectory,
            WTSOEMId,
            WTSSessionId,
            WTSUserName,
            WTSWinStationName,
            WTSDomainName,
            WTSConnectState,
            WTSClientBuildNumber,
            WTSClientName,
            WTSClientDirectory,
            WTSClientProductId,
            WTSClientHardwareId,
            WTSClientAddress,
            WTSClientDisplay,
            WTSClientProtocolType,
            WTSIdleTime,
            WTSLogonTime,
            WTSIncomingBytes,
            WTSOutgoingBytes,
            WTSIncomingFrames,
            WTSOutgoingFrames,
            WTSClientInfo,
            WTSSessionInfo,
            WTSSessionInfoEx,
            WTSConfigInfo,
            WTSValidationInfo, // Info Class value used to fetch Validation Information through the WTSQuerySessionInformation
            WTSSessionAddressV4,
            WTSIsRemoteSession
        }

        public enum WTS_CONNECTSTATE_CLASS
        {
            WTSActive, // User logged on to WinStation
            WTSConnected, // WinStation connected to client
            WTSConnectQuery, // In the process of connecting to client
            WTSShadow, // Shadowing another WinStation
            WTSDisconnected, // WinStation logged on without client
            WTSIdle, // Waiting for client to connect
            WTSListen, // WinStation is listening for connection
            WTSReset, // WinStation is being reset
            WTSDown, // WinStation is down due to error
            WTSInit, // WinStation in initialization
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WTSINFOEXW
        {
            public int Level;
            public WTSINFOEX_LEVEL_W Data;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WTSINFOEX_LEVEL_W
        {
            public WTSINFOEX_LEVEL1_W WTSInfoExLevel1;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WTSINFOEX_LEVEL1_W
        {
            public int SessionId;
            public WTS_CONNECTSTATE_CLASS SessionState;
            public int SessionFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
            public string WinStationName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 21)]
            public string UserName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 18)]
            public string DomainName;

            public LARGE_INTEGER LogonTime;
            public LARGE_INTEGER ConnectTime;
            public LARGE_INTEGER DisconnectTime;
            public LARGE_INTEGER LastInputTime;
            public LARGE_INTEGER CurrentTime;
            public uint IncomingBytes;
            public uint OutgoingBytes;
            public uint IncomingFrames;
            public uint OutgoingFrames;
            public uint IncomingCompressedBytes;
            public uint OutgoingCompressedBytes;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct LARGE_INTEGER //此结构体在C++中使用的为union结构，在C#中需要使用FieldOffset设置相关的内存起始地址
        {
            [FieldOffset(0)] uint LowPart;
            [FieldOffset(4)] int HighPart;
            [FieldOffset(0)] long QuadPart;
        }
    }
}
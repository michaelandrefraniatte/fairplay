using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.Security.Principal;
namespace FairPlay
{
    class Program
    {
        [DllImport("dsm.dll", EntryPoint = "DeleteSharedMemory")]
        public static extern void DeleteSharedMemory();
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        public static extern uint TimeBeginPeriod(uint ms);
        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        public static extern uint TimeEndPeriod(uint ms);
        [DllImport("ntdll.dll", EntryPoint = "NtSetTimerResolution")]
        public static extern void NtSetTimerResolution(uint DesiredResolution, bool SetResolution, ref uint CurrentResolution);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);
        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern IntPtr GetConsoleWindow();
        [DllImport("User32.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow([In] IntPtr hWnd, [In] Int32 nCmdShow);
        const Int32 SW_MINIMIZE = 6;
        static ConsoleEventDelegate handler;
        private delegate bool ConsoleEventDelegate(int eventType);
        public static ThreadStart threadstart;
        public static Thread thread;
        public static uint CurrentResolution = 0;
        private static bool closed = false;
        static void Main()
        {
            MinimizeConsoleWindow();
            handler = new ConsoleEventDelegate(ConsoleEventCallback);
            SetConsoleCtrlHandler(handler, true);
            bool runelevated = false;
            bool oneinstanceonly = true;
            try
            {
                TimeBeginPeriod(1);
                NtSetTimerResolution(1, true, ref CurrentResolution);
                if (oneinstanceonly)
                {
                    if (AlreadyRunning())
                    {
                        return;
                    }
                }
                if (runelevated)
                {
                    if (!hasAdminRights())
                    {
                        RunElevated();
                        return;
                    }
                }
            }
            catch
            {
                return;
            }
            Task.Run(() => Start());
            Console.ReadLine();
        }
        public static bool hasAdminRights()
        {
            WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        public static void RunElevated()
        {
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo();
                processInfo.Verb = "runas";
                processInfo.FileName = Application.ExecutablePath;
                Process.Start(processInfo);
            }
            catch { }
        }
        private static bool AlreadyRunning()
        {
            String thisprocessname = Process.GetCurrentProcess().ProcessName;
            Process[] processes = Process.GetProcessesByName(thisprocessname);
            if (processes.Length > 1)
                return true;
            else
                return false;
        }
        public static void Start()
        {
            while (!closed)
            {
                try
                {
                    DeleteSharedMemory();
                }
                catch { }
                Thread.Sleep(60000);
            }
        }
        private static void MinimizeConsoleWindow()
        {
            IntPtr hWndConsole = GetConsoleWindow();
            ShowWindow(hWndConsole, SW_MINIMIZE);
        }
        static bool ConsoleEventCallback(int eventType)
        {
            if (eventType == 2)
            {
                threadstart = new ThreadStart(FormClose);
                thread = new Thread(threadstart);
                thread.Start();
            }
            return false;
        }
        private static void FormClose()
        {
            closed = true;
            TimeEndPeriod(1);
        }
    }
}
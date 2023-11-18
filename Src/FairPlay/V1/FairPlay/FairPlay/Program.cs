using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Security.Principal;
using System.Collections;
namespace FairPlay
{
    class Program
    {
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
        [DllImport("kernel32.dll")]
        private unsafe static extern IntPtr OpenProcess(UInt32 dwDesiredAccess, Int32 bInheritHandle, UInt32 dwProcessId);
        [DllImport("kernel32.dll")]
        private unsafe static extern Int32 ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [In, Out] byte[] buffer, UInt64 size, out IntPtr lpNumberOfBytesRead);
        [DllImport("kernel32.dll")]
        private unsafe static extern Int32 WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [In, Out] byte[] buffer, UInt64 size, out IntPtr lpNumberOfBytesRead);
        [DllImport("kernel32.dll")]
        private unsafe static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr size, uint flAllocationType, uint lpflOldProtect);
        [DllImport("kernel32.dll")]
        private unsafe static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, IntPtr size, uint flNewProtect, out uint lpflOldProtect);
        [DllImport("kernel32.dll")]
        private unsafe static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out uint basicInformation, IntPtr dwSize);
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private unsafe static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, uint dwFreeType);
        [DllImport("kernel32.dll")]
        private unsafe static extern bool VirtualLock(IntPtr lpAddress, IntPtr dwSize);
        [DllImport("kernel32.dll")]
        private unsafe static extern bool VirtualUnlock(IntPtr lpAddress, IntPtr dwSize);
        [DllImport("kernel32.dll")]
        public static extern bool SetProcessWorkingSetSizeEx(IntPtr hProcess, int dwMinimumWorkingSetSize, int dwMaximumWorkingSetSize, int Flags);
        const Int32 SW_MINIMIZE = 6;
        static ConsoleEventDelegate handler;
        private delegate bool ConsoleEventDelegate(int eventType);
        public static ThreadStart threadstart;
        public static Thread thread;
        public static uint CurrentResolution = 0;
        public static int processid = 0;
        private static List<string> procnames = new List<string>();
        private static string datasetit, datauseit, dataputit;
        private static bool closed = false;
        private static Action action;
        private static Task task;
        private static PerformanceCounter counter;
        private static uint dwdesiredaccess = PROCESS_ALL_ACCESS;
        private static IntPtr pP;
        public static uint oP, lpOldProtect;
        public static Int64 bA;
        public static Int64 lA;
        public static IntPtr A, Ai, sizei;
        public static UInt64 sizeu;
        private static int effic; // (8587934592 - 140000000) / 2147483647 = 4;
        public static int size;
        public static byte[] fbytes, fbytesshift;
        private static IntPtr lpNumberOfBytesRead;
        private static Random rnd = new Random();
        const uint DELETE = 0x00010000;
        const uint READ_CONTROL = 0x00020000;
        const uint WRITE_DAC = 0x00040000;
        const uint WRITE_OWNER = 0x00080000;
        const uint SYNCHRONIZE = 0x00100000;
        const uint END = 0xFFF;
        const uint PROCESS_ALL_ACCESS = DELETE | READ_CONTROL | WRITE_DAC | WRITE_OWNER | SYNCHRONIZE | END | PROCESS_VM_OPERATION | PROCESS_VM_READ | PROCESS_VM_WRITE;
        const uint PROCESS_VM_OPERATION = 0x0008;
        const uint PROCESS_VM_READ = 0x0010;
        const uint PROCESS_VM_WRITE = 0x0020;
        const uint PROCESS_CREATE_THREAD = 0x0002;
        const uint PROCESS_QUERY_INFORMATION = 0x0400;
        const uint PAGE_READWRITE = 0x04;
        const uint MEM_COMMIT = 0x00001000;
        const uint PAGE_EXECUTE_READWRITE = 0x40;
        const uint MEM_DECOMMIT = 0x00004000;
        const uint MEM_RESERVE = 0x00002000;
        const uint PAGE_READONLY = 0x02;
        const uint PAGE_GUARD = 0x100;
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
            Task.Run(() => StartFlood());
            System.Threading.Thread.Sleep(60000);
            counter = new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName, true);
            Task.Run(() => StartTick());
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
        public static void StartFlood()
        {
            Put();
            try
            {
                using (System.IO.StreamReader file = new System.IO.StreamReader("fp.txt"))
                {
                    while (true)
                    {
                        string procName = file.ReadLine();
                        if (procName == "")
                        {
                            file.Close();
                            break;
                        }
                        else
                            procnames.Add(procName);
                    }
                }
                while (processid == 0)
                {
                    foreach (Process p in Process.GetProcesses())
                    {
                        string match = procnames.FirstOrDefault(stringToCheck => stringToCheck == p.ProcessName);
                        if (match != null)
                        {
                            processid = p.Id;
                            break;
                        }
                        Thread.Sleep(1);
                    }
                    Thread.Sleep(1);
                }
                datasetit = setIt(processid);
                if (datasetit != "")
                {
                    Console.WriteLine(datasetit);
                }
                Console.WriteLine("executed");
                task = Task.Run(action = () => Flood());
            }
            catch (Exception e) 
            {
                Console.WriteLine(e.ToString());
            }
        }
        private static void Flood()
        {
            while (!closed)
            {
                try
                {
                    datauseit = useIt();
                    if (datauseit != "")
                    {
                        Console.WriteLine(datauseit);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                Thread.Sleep(1);
            }
        }
        private static void Put()
        {
            try
            {
                dataputit = putIt();
                if (dataputit != "")
                {
                    Console.WriteLine(dataputit);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        private static void StartTick()
        {
            while (true)
            {
                try
                {
                    if (counter.NextValue() / Environment.ProcessorCount <= 0.50000)
                    {
                        closed = true;
                        System.Threading.Thread.Sleep(1000);
                    }
                    if (task.IsCompleted)
                    {
                        closed = false;
                        Task.Run(() => StartFlood());
                    }
                }
                catch { }
                System.Threading.Thread.Sleep(60000);
            }
        }
        public unsafe static string setIt(int processid)
        {
            try
            {
                TimeBeginPeriod(1);
                NtSetTimerResolution(1, true, ref CurrentResolution);
                pP = OpenProcess(dwdesiredaccess, 1, (uint)processid);
                bA = 140000000;
                lA = 8587934592;
            }
            catch (Exception e)
            {
                return e.ToString();
            }
            return "";
        }
        public unsafe static string useIt()
        {
            try
            {
                Array.Copy(fbytes, 1, fbytesshift, 0, size - 1);
                fbytesshift[size - 1] = (byte)rnd.Next(180, 256);
                fbytes = fbytesshift;
                for (int i = (int)bA; i < (int)lA; i = i + size)
                {
                    Ai = (IntPtr)i;
                    A = VirtualAllocEx(pP, Ai, sizei, MEM_RESERVE | MEM_COMMIT, PAGE_READONLY | PAGE_GUARD | PAGE_EXECUTE_READWRITE);
                    VirtualQueryEx(pP, A, out oP, sizei);
                    VirtualProtectEx(pP, A, sizei, PAGE_GUARD | oP, out lpOldProtect);
                    WriteProcessMemory(pP, A, fbytes, sizeu, out lpNumberOfBytesRead);
                    VirtualQueryEx(pP, Ai, out oP, sizei);
                    VirtualProtectEx(pP, Ai, sizei, PAGE_GUARD | oP, out lpOldProtect);
                    WriteProcessMemory(pP, Ai, fbytes, sizeu, out lpNumberOfBytesRead);
                }
            }
            catch (Exception e)
            {
                return e.ToString();
            }
            return "";
        }
        public unsafe static string putIt()
        {
            try
            {
                effic = rnd.Next(10, 20);
                size = 2147483647 / effic;
                fbytes = new byte[size];
                fbytesshift = new byte[size];
                sizei = (IntPtr)size;
                sizeu = (UInt64)size;
                for (int j = 0; j < size; j++)
                {
                    fbytes[j] = (byte)rnd.Next(180, 256);
                }
                return "filled";
            }
            catch (Exception e)
            {
                return e.ToString();
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
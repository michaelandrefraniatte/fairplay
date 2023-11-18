#include "pch.h"
#include <stdlib.h>
#include <malloc.h>
#include <winioctl.h>
#include <iostream>
#include <WtsApi32.h>
#include <sddl.h>
#include <wtypes.h>
#include <oleauto.h>
#include <process.h>
#include <winbase.h>
#include <string.h>
#include <tlhelp32.h>
#include <locale.h>
#include <wchar.h>
#include <stdio.h>
#include <unordered_map>
#include <psapi.h>
#include <stdio.h>
#include <stdlib.h>
#include <errno.h>
#include <string.h>
#include <sys/stat.h>
#include <sys/types.h>
#include <windows.h>
#include <stdio.h>
#include <conio.h>
#include <tchar.h>
#pragma comment(lib, "user32.lib")
#pragma comment(lib, "Msvcrt.lib")
#pragma comment(lib, "Wtsapi32.lib")
#pragma comment(lib, "winmm.lib")
#pragma comment(lib, "ntdll.lib")
extern "C"
{
    __declspec(dllexport) void DeleteSharedMemory()
    {
        WTS_PROCESS_INFO* pWPIs = NULL;
        DWORD dwProcCount = 0;
        if (WTSEnumerateProcesses(WTS_CURRENT_SERVER_HANDLE, NULL, 1, &pWPIs, &dwProcCount))
        {
            for (DWORD i = 0; i < dwProcCount; i++)
            {
                HANDLE hProcess = OpenProcess(PROCESS_VM_OPERATION, 0, pWPIs[i].ProcessId);
                if (hProcess)
                {
                    SYSTEM_INFO info;
                    DWORD dwPageSize;
                    DWORD dwMemSize;
                    LPVOID lpvMem;
                    GetSystemInfo(&info);
                    dwPageSize = info.dwPageSize;
                    dwMemSize = 16 * dwPageSize;
                    lpvMem = VirtualAllocEx(hProcess, (LPVOID)0x00F00000, dwMemSize, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
                    if (lpvMem != 0)
                    {
                        VirtualFreeEx(hProcess, lpvMem, 0, MEM_RELEASE);
                        VirtualFreeEx(hProcess, lpvMem, dwMemSize, MEM_COMMIT);
                    }
                    fflush(stdout);
                    CloseHandle(hProcess);
                }
            }
        }
        if (pWPIs)
        {
            WTSFreeMemory(pWPIs);
            pWPIs = NULL;
        }
    }
}
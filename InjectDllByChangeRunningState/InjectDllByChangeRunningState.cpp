#include <iostream>
#include <Windows.h>
#include <Psapi.h>
#include <tchar.h>

BOOL WINAPI InjectDLLToProcess_CreateRemoteThread(DWORD dwTargetPid, LPCSTR DLLPath) {
    HANDLE hProc = NULL;
    hProc = OpenProcess(PROCESS_ALL_ACCESS, FALSE, dwTargetPid);

    if (hProc == NULL) {
        printf("[-] OpenProcess Failed!\n");
        return FALSE;
    }

    LPSTR psLibFileRemote = NULL;

    psLibFileRemote = (LPSTR)VirtualAllocEx(hProc, NULL, lstrlenA(DLLPath) + 1, MEM_COMMIT, PAGE_READWRITE);

    if (psLibFileRemote == NULL) {
        printf("[-] VirtualAlloc Failed.\n");
        return FALSE;
    }

    if (WriteProcessMemory(hProc, psLibFileRemote, (void*)DLLPath, lstrlenA(DLLPath)+1, NULL) == 0) {
        printf("[-] WriteProcessMemory Failed.\n");
        return FALSE;
    }
    /*
    PTHREAD_START_ROUTINE pfnStartAddr = (PTHREAD_START_ROUTINE)GetProcAddress(GetModuleHandle(L"Kernel32"), "LoadLibraryA");

    if (pfnStartAddr == NULL) {
        printf("[-] GetProcAddress Failed.\n");
        return FALSE;
    }*/
    HMODULE hMods[1024];
    DWORD cbNeeded;
    int i;
    HMODULE Kernel32_HMod = NULL;
    if (EnumProcessModules(hProc, hMods, sizeof(hMods), &cbNeeded)!=0)
    {
        for (i = 0; i < (cbNeeded / sizeof(HMODULE)); i++)
        {
            TCHAR szModName[MAX_PATH];

            // Get the full path to the module's file.

            if (GetModuleFileNameEx(hProc, hMods[i], szModName,
                sizeof(szModName) / sizeof(TCHAR)))
            {
                if (lstrcmp(szModName, L"C:\\WINDOWS\\System32\\KERNEL32.DLL") == 0) {
                    Kernel32_HMod = hMods[i];
                    break;
                }
            }
        }
    }
    LPVOID pfnStartAddr = (LPVOID)GetProcAddress(Kernel32_HMod, "LoadLibraryA");
    //LPVOID pfnStartAddr = (LPVOID)GetProcAddress(LoadLibraryA("C:\\Windows\\System32\\Kernel32.dll"), "LoadLibraryA");
    HANDLE hThread = CreateRemoteThread(hProc, NULL, 0, (LPTHREAD_START_ROUTINE)pfnStartAddr, psLibFileRemote, 0, NULL);
    //HANDLE hThread = CreateThread(NULL, 0, (LPTHREAD_START_ROUTINE)LoadLibraryA, (LPVOID)"D:\\windows\\LearnWindowsHowItWorksByBadThings\\InjectDllByChangeRunningState\\x64\\Debug\\TestDll.dll", 0, NULL);
    if (hThread == NULL) {
        printf("CreateRemoteThread failed\n");
        return FALSE;
    }

    printf("Inject Success\n");
    WaitForSingleObject(hThread,INFINITE);
    CloseHandle(hProc);
    return TRUE;
}
void threadFunc(char* str) {
    LoadLibraryA(str);
    getchar();
}
void test() {
    HANDLE hThread = CreateThread(NULL, 0, (LPTHREAD_START_ROUTINE)LoadLibraryA, (LPVOID)"D:\\windows\\LearnWindowsHowItWorksByBadThings\\InjectDllByChangeRunningState\\x64\\Debug\\TestDll.dll", 0, NULL);
    if (hThread == NULL) {
        printf("Create Failed!\n");
    }
    WaitForSingleObject(hThread, INFINITE);
}
int main()
{
    InjectDLLToProcess_CreateRemoteThread(21056, "D:\\windows\\LearnWindowsHowItWorksByBadThings\\InjectDllByChangeRunningState\\x64\\Debug\\TestDll.dll");
    //LoadLibraryW(L"D:\\windows\\LearnWindowsHowItWorksByBadThings\\InjectDllByChangeRunningState\\x64\\Debug\\TestDll.dll");
    //PrintModules(13632);
    //test();
    return 0;
}


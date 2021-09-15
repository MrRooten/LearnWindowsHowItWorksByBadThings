// dllmain.cpp : 定义 DLL 应用程序的入口点。
#include "pch.h"

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        WaitForSingleObject(CreateThread(NULL, 0, ThreadShow, NULL, 0, NULL),INFINITE);
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

DWORD WINAPI ThreadShow(LPVOID lpParameter) {
    wchar_t szPath[1024];
    wchar_t szBuf[1024] = { 0 };
    GetModuleFileName(NULL, szPath, 1024);
    wsprintf(szBuf, L"Dll inject to Process %s [Pid = %d]\n", szPath, GetCurrentProcessId());

    MessageBox(NULL, szBuf, L"DLL Inject", MB_OK);
    _tprintf(L"%s", szBuf);
    return 0;
}
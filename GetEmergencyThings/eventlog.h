#pragma once
#include "headers.h"
#define ARRAY_SIZE 10
#define TIMEOUT 1000
#pragma comment(lib,"wevtapi.lib")
DWORD PrintResults(EVT_HANDLE hResults);
LPWSTR GetEvent(EVT_HANDLE hEvent);
DWORD GetEventLog(LPCWSTR pwsQuery);

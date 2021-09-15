#pragma once
#include "headers.h"
#include <vector>
struct AppCompatCacheEntry {
public:
	INT32 position;
	INT32 size;
	BYTE* data;
	INT32 dataSize;
	LPWSTR path;
	INT32 pathSize;
	LPSTR sigture;
	INT64 lastModifyTime;
};

class AppCompatCache_Win10 {
public:
	AppCompatCache_Win10(BYTE* bytes,size_t length);
	INT32 getEntryNumber();
	LPWSTR getSignature();
	LPWSTR getPath();
	SYSTEMTIME getModifyTime();
	BYTE* getData();
private:
	DWORD length;
	std::vector<AppCompatCacheEntry*> entries;
	INT32 entryNumber = -1;
	BYTE* bytes = NULL;
	LPWSTR path = NULL;
	SYSTEMTIME modifyTime = { 0 };
};
DWORD GetRegistryValue(LPCWSTR registryPath,LPCWSTR key, LPDWORD pdwType,DWORD* psize,PVOID* pdata);
#include "IAT_hook.h"

BOOL InstallModuleIATHook(
	const char* szModuleName,
	const char* szFuncName,
	PVOID DetourFunc,
	PULONG *pThunkPointer,
	ULONG *pOrignalFuncAddr
) {
	PIMAGE_IMPORT_DESCRIPTOR pImportDescriptor;
	PIMAGE_THUNK_DATA pThunkData;
	HMODULE hModule;
	ULONG targetFunc;
	ULONG ulSize;

	HMODULE hModToHook = GetModuleHandleA(NULL);
	hModule = LoadLibraryA(szModuleName);
	targetFunc = (ULONG)GetProcAddress(hModule, szFuncName);

	pImportDescriptor = (PIMAGE_IMPORT_DESCRIPTOR)ImageDirectoryEntryToData(
		hModToHook,
		TRUE,
		IMAGE_DIRECTORY_ENTRY_IMPORT,
		&ulSize
	);

	char* szModName;
	ULONG* lpAddr;
	BOOL bRetn;
	BOOL result = FALSE;
	while (pImportDescriptor->FirstThunk) {
		szModName = (char*)((PBYTE)hModToHook + pImportDescriptor->Name);
		printf("Current Module Name:%s\n",szModName);
		if (_stricmp(szModName, szModuleName) != 0) {
			printf("Module Name doesn't match,search next...\n");
			pImportDescriptor++;
			continue;
		}

		pThunkData = (PIMAGE_THUNK_DATA)((BYTE*)hModToHook + pImportDescriptor->FirstThunk);
		while (pThunkData->u1.Function) {
			lpAddr = (ULONG*)pThunkData;
			
			if ((*lpAddr) == targetFunc) {
				printf("[+]Find the address\n");
				DWORD dwOldProtect;
				MEMORY_BASIC_INFORMATION mbi;
				VirtualQuery(lpAddr, &mbi, sizeof(mbi));
				bRetn = VirtualProtect(mbi.BaseAddress, mbi.RegionSize, PAGE_EXECUTE_READWRITE, &dwOldProtect);
				if (bRetn) {
					if (pThunkPointer != NULL) {
						*pThunkPointer = lpAddr;
					}

					if (pOrignalFuncAddr != NULL) {
						*pOrignalFuncAddr = *lpAddr;
					}

					*lpAddr = (ULONG)DetourFunc;
					result = TRUE;
					VirtualProtect(mbi.BaseAddress, mbi.RegionSize, dwOldProtect, 0);
					printf("[+]Hook done!\n");
				}
				break;
			}
			pThunkData++;
		}
		pImportDescriptor++;
	}

	FreeLibrary(hModule);
	return result;
}
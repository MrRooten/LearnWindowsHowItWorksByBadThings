#include "registrylog.h"
#include <tchar.h>
DWORD GetRegistryValue(LPCWSTR registryPath,LPCWSTR key, LPDWORD pdwType,DWORD* psize,PVOID* pdata) {
	HKEY hKey;
	DWORD size;
	DWORD status = ERROR_SUCCESS;
	if ((status = RegOpenKeyEx(HKEY_LOCAL_MACHINE, registryPath, 0, KEY_QUERY_VALUE, &hKey)) != ERROR_SUCCESS) {
		*pdata = NULL;
		return status;
	}

	if ((status= RegQueryValueEx(hKey,key,NULL,pdwType,NULL,&size)) != ERROR_SUCCESS) {
		*pdata = NULL;
		RegCloseKey(hKey);
		return status;
	}

	*psize = size;
	void* value_pointer = ZMalloc(size);

	if (value_pointer == NULL) {
		*pdata = NULL;
		return status;
	}
	
	if ((status = RegQueryValueEx(hKey, key, NULL, pdwType, (LPBYTE)value_pointer, &size)) != ERROR_SUCCESS) {
		RegCloseKey(hKey);
		*pdata = NULL;
		return status;
	}
	*pdata = value_pointer;
	RegCloseKey(hKey);
	return status;
}
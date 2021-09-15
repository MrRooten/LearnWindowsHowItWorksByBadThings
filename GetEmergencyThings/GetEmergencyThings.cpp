#include "eventlog.h"
#include "registrylog.h"

int main(void)
{
    HANDLE hFile = CreateFile(L"abc.bin", GENERIC_READ, 0, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
    DWORD fileSize = GetFileSize(hFile, NULL);
    BYTE* data = (BYTE*)ZMalloc(fileSize);
    ReadFile(hFile, data, fileSize, NULL, NULL);
    AppCompatCache_Win10 aw10(data,fileSize);
    CloseHandle(hFile);
    ZCleanUp();
}


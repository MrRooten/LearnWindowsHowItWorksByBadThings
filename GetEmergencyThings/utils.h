#pragma once
#include <Windows.h>
#include <set>
#include <iterator>
#include <vector>
#include <cstring>
#include <string>
static std::set<void*> _pointers;
void* ZMalloc(size_t size);
void ZCleanUp();
void ZFree(void* pointer);
UINT16 readUINT16(BYTE* data, size_t offset);
INT16 readINT16(BYTE* data, size_t offset);
UINT32 readUINT32(BYTE* data, size_t offset);
INT32 readINT32(BYTE* data, size_t offset);
INT64 readINT64(BYTE* data, size_t offset);
BYTE* readBYTES(BYTE* data, size_t offset, size_t length);
LPWSTR readString(BYTE* data, size_t offset, size_t length);
LPWSTR readStringUTF8(BYTE* bytes, size_t offset, size_t length);
std::wstring readStringWString(BYTE* bytes, size_t offset, size_t length);
LPSTR readStringAscii(BYTE* data, size_t offset, size_t length);
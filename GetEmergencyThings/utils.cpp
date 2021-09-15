#include "utils.h"

void* ZMalloc(size_t size) {
	void* res = GlobalAlloc(GMEM_ZEROINIT, size);
	if (res == NULL) {
		return res;
	}
	_pointers.insert(res);
	return res;
}

void ZCleanUp() {
	std::vector<void*> _tmp;
	for (auto p : _pointers) {
		_tmp.push_back(p);
	}
	_pointers.clear();
	for (auto p : _tmp) {
		GlobalFree(p);
	}
}

void ZFree(void* pointer) {
	std::set<void*>::const_iterator it = _pointers.find(pointer);
	GlobalFree(*it);
	_pointers.erase(it);
}

UINT16 readUINT16(BYTE* bytes, size_t offset) {
	UINT16 res = 0;
	std::memcpy(&res, &bytes[offset], sizeof(UINT16));
	return res;
}

INT16 readINT16(BYTE* bytes, size_t offset) {
	INT16 res = 0;
	std::memcpy(&res, &bytes[offset], sizeof(INT16));
	return res;
}

INT32 readINT32(BYTE* bytes, size_t offset) {
	INT32 res = 0;
	std::memcpy(&res, &bytes[offset], sizeof(INT32));
	return res;
}

LPSTR readStringAscii(BYTE* bytes, size_t offset, size_t length) {
	LPSTR s = (LPSTR)ZMalloc(length + 1);
	if (s == NULL) {
		return NULL;
	}
	
	std::memcpy((void*)s, &bytes[offset], length);
	s[length] = '\0';
	return s;
}
LPWSTR readString(BYTE* bytes, size_t offset, size_t length) {
	LPWSTR s = (LPWSTR)ZMalloc(length + 2);
	if (s == NULL) {
		return NULL;
	}

	size_t i;
	
	for (i = 0; i < length; i++) {
		s[i] = bytes[offset + 2 * i];
	}

	return s;
}

INT64 readINT64(BYTE* bytes, size_t offset) {
	INT64 res = 0;
	std::memcpy(&res, &bytes[offset], sizeof(INT64));
	return res;
}

BYTE* readBYTES(BYTE* bytes, size_t offset, size_t length) {
	BYTE* res = (BYTE*)ZMalloc(length);
	std::memcpy(res, &bytes[offset], length);
	return res;
}

std::wstring readStringWString(BYTE* bytes, size_t offset, size_t length) {
	auto* wstrBytes = new wchar_t[length+1];
	memcpy_s(wstrBytes, length, bytes, length);
	std::wstring unicodeStr(wstrBytes, length);
	delete[] wstrBytes;
	return unicodeStr;
}

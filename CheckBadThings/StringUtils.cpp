#include "StringUtils.h"
MPEString::MPEString() {
	this->bufferString = NULL;
	this->bufferSize = 0;
	this->length = 0;
}

MPEString::MPEString(const LPTSTR string) {
	size_t size = wcslen(string);
	size_t maxSize = ceil(size * 1.5);
	this->bufferString = (LPTSTR)GlobalAlloc(GPTR, sizeof(TCHAR) * maxSize);

	if (this->bufferString == nullptr) {
		//error when alloc buffer string
	}

	wcscpy(this->bufferString, string);
	this->bufferSize = maxSize;
	this->length = size;
}

MPEString::MPEString(const LPSTR string) {
	size_t size = strlen(string);
	size_t maxSize = ceil(size * 1.5);
	this->bufferString = (LPTSTR)GlobalAlloc(GPTR, sizeof(TCHAR) * maxSize);

	if (this->bufferString == nullptr) {
		//error when alloc buffer string
	}

	mbstowcs(this->bufferString, string, size + 1);
	this->bufferSize = maxSize;
	this->length = size;

}


MPEString::~MPEString() {
	if (this->bufferString != nullptr)
		GlobalFree(this->bufferString);

	if (this->bufferCString != nullptr)
		GlobalFree(this->bufferCString);
}

MPEString MPEString::operator+(MPEString& string) const {
	return *this;
}

MPEString MPEString::operator+=(MPEString& string) {
	size_t size = this->length + string.length;
	size_t maxSize;
	HGLOBAL res;
	if (this->length > size + 1) {
		maxSize = this->length;
		res = this->bufferString;
	}
	else {
		maxSize = ceil(size * 1.5);
		res = GlobalReAlloc(this->bufferString, maxSize, GMEM_MOVEABLE);
		if (res == NULL) {
			//error when realloc heap
			return *this;
		}
	}

	this->bufferString = (LPTSTR)res;
	wcscat(this->bufferString, string.toTString());

	this->length = size;
	this->bufferSize = maxSize;
	return *this;
}

LPTSTR MPEString::toTString() {
	return this->bufferString;
}

LPWSTR MPEString::toWString() {
	return this->bufferString;
}

LPSTR MPEString::toCString() {
	if (this->compare(this->bufferCString, this->bufferString)) {
		return this->bufferCString;
	}
	BYTE* bytes = (BYTE*)this->bufferString;
	size_t bytesLength = this->length * 2;
	char* resString = nullptr;
	if (this->bufferCString == nullptr) {
		resString = (char*)GlobalAlloc(GPTR, sizeof(char) * (this->length + 1));
	}
	else {
		resString = (char*)GlobalReAlloc(this->bufferCString, this->bufferSize * sizeof(char), GMEM_MOVEABLE);
	}

	if (resString == nullptr) {
		//error when resString allocing memory
		return nullptr;
	}
	this->bufferCString = resString;
	size_t index = 0;
	for (size_t i = 0; i < bytesLength; i += 2) {
		resString[index] = bytes[i];
		index += 1;
	}
	return resString;
}

void MPEString::operator=(const char* string) {
	size_t size = strlen(string);
	size_t maxSize = ceil(size * 1.5);
	this->bufferString = (LPTSTR)GlobalAlloc(GPTR, sizeof(TCHAR) * maxSize);

	if (this->bufferString == nullptr) {
		//error when alloc buffer string
	}

	mbstowcs(this->bufferString, string, size + 1);
	this->bufferSize = maxSize;
	this->length = size;
}

void MPEString::operator=(int nullString) {
	if (nullString == NULL) {
		this->length = 0;
		this->bufferSize = 0;
		this->bufferString = NULL;
	}
}
void MPEString::operator=(const LPTSTR string) {
	size_t size = wcslen(string);
	size_t maxSize = ceil(size * 1.5);
	this->bufferString = (LPTSTR)GlobalAlloc(GPTR, sizeof(TCHAR) * maxSize);

	if (this->bufferString == nullptr) {
		//error when alloc buffer string
	}

	wcscpy(this->bufferString, string);
	this->bufferSize = maxSize;
	this->length = size;
}

BOOL MPEString::compare(LPSTR cString, LPTSTR tString) {
	size_t cLength = strlen(cString);
	size_t tLength = wcslen(tString);

	if (cLength != tLength) {
		return FALSE;
	}

	for (size_t i = 0; i < tLength; i++) {
		BYTE* bytes = (BYTE*)&tString[i];
		if (bytes[0] != cString[i]) {
			return FALSE;
		}
	}

	return TRUE;
}

BOOL MPEString::operator==(MPEString mpeString) {
	return wcscmp(mpeString.toTString(), this->toTString()) == 0 ? TRUE : FALSE;
}

BOOL MPEString::operator==(LPSTR cString) {
	return strcmp(cString, this->toCString()) == 0 ? TRUE : FALSE;
}

BOOL MPEString::operator==(LPTSTR tString) {
	return wcscmp(tString, this->toTString()) == 0 ? TRUE : FALSE;
}

BOOL MPEString::operator==(int nullString) {
	if (nullString == NULL) {
		return NULL == this->bufferString;
	}
	else {
		//if nullString is a pointer,then estimate the bufferString's address is equivalence the nullString
		return (LPTSTR)nullString == this->bufferString;
	}
}
#pragma once
#ifndef UTILS_H
#define UTILS_H
#include "header.h"
#include <math.h>

typedef size_t position_t;
class MPEString {
public:
	MPEString();
	~MPEString();
	MPEString(const LPTSTR string);
	MPEString(const LPSTR string);
	SIZE_T length;
	MPEString operator+(MPEString& rMPEString) const;
	void operator=(const char* cString);
	void operator=(const LPTSTR tString);
	void operator=(int nullString);
	BOOL operator==(MPEString string);
	BOOL operator==(int nullString);
	BOOL operator==(LPTSTR tString);
	BOOL operator==(LPSTR cString);
	MPEString operator+=(MPEString& mpeString);
	LPSTR toCString();
	LPWSTR toWString();
	LPTSTR toTString();
	LPBYTE toBytes();
	LONG64 toNumber(LONG64 base = 10);
	void replace(position_t start, position_t end, MPEString string);
	position_t findString(MPEString mpeString);
	position_t* findAllStrings(MPEString mpeString);
	static MPEString fromNumber(LONG64 number, int base = 10);
private:
	LPTSTR bufferString = nullptr;
	LPSTR bufferCString = nullptr;
	SIZE_T bufferSize;

	BOOL compare(LPSTR cString, LPTSTR tString);
};

#endif
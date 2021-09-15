#pragma once
#include <Windows.h>
#include <DbgHelp.h>
#include <stdio.h>
BOOL InstallModuleIATHook(
	const char* szModuleName,
	const char* szFuncName,
	PVOID DetourFunc,
	PULONG* pThunkPointer,
	ULONG* pOrignalFuncAddr
);

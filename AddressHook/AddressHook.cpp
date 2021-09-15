#include <iostream>
#include <Windows.h>
#include "IAT_hook.h"
#include "testdll.h"
typedef void(_cdecl* TESTDLL)();


void DetourFunc() {
    printf("Your function have been hacked\n");
}
int main()
{
    
    PULONG pointer;
    ULONG orignalFunction;
    InstallModuleIATHook("TestDll.dll", "TestFunction", DetourFunc, &pointer, &orignalFunction);
    
    TestFunction();
}



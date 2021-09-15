// binary_read.cpp
// compile with: /clr
#using<system.dll>
#include <Windows.h>
using namespace System;
using namespace System::IO;

int main() {
   String ^ s = "1.2.3.4";
   auto version = gcnew Version(s);
   Console::WriteLine("full type name '{0}' ", version->GetType());

   
}
#define _WIN32_DCOM
using namespace std;
#include <comdef.h>
#include <tchar.h>

#pragma comment(lib, "stdole2.tlb")

int main(int argc, char** argv)
{
    HRESULT hres;

    // Step 1: ------------------------------------------------
    // 初始化COM组件. ------------------------------------------

    hres = CoInitializeEx(0, COINIT_MULTITHREADED);

    // Step 2: ------------------------------------------------
    // 初始化COM安全属性 ---------------------------------------

    hres = CoInitializeSecurity(
        NULL,
        -1,                          // COM negotiates service
        NULL,                        // Authentication services
        NULL,                        // Reserved
        RPC_C_AUTHN_LEVEL_DEFAULT,   // Default authentication 
        RPC_C_IMP_LEVEL_IMPERSONATE, // Default Impersonation
        NULL,                        // Authentication info
        EOAC_NONE,                   // Additional capabilities 
        NULL                         // Reserved
    );
    // Step 3: ---------------------------------------
    // 获取COM组件的接口和方法 -------------------------
    LPDISPATCH lpDisp;
    CLSID clsidshell;
    hres = CLSIDFromProgID(L"WScript.Shell", &clsidshell);
    if (FAILED(hres))
        return FALSE;
    hres = CoCreateInstance(clsidshell, NULL, CLSCTX_INPROC_SERVER, IID_IDispatch, (LPVOID*)&lpDisp);
    if (FAILED(hres))
        return FALSE;
    wchar_t name[4];
    lstrcpyW(name, L"Run");
    LPOLESTR pFuncName = name;
    DISPID Run;
    hres = lpDisp->GetIDsOfNames(IID_NULL, &pFuncName, 1, LOCALE_SYSTEM_DEFAULT, &Run);
    if (FAILED(hres))
        return FALSE;
    // Step 4: ---------------------------------------
    // 填写COM组件参数并执行方法 -----------------------
    VARIANTARG V[1];
    V[0].vt = VT_BSTR;
    V[0].bstrVal = _bstr_t(L"cmd /c calc.exe");
    DISPPARAMS disParams = { V, NULL, 1, 0 };
    hres = lpDisp->Invoke(Run, IID_NULL, LOCALE_SYSTEM_DEFAULT, DISPATCH_METHOD, &disParams, NULL, NULL, NULL);
    if (FAILED(hres))
        return FALSE;
    // Clean up
    //--------------------------
    lpDisp->Release();
    CoUninitialize();
    return 1;
}
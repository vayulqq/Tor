#include <windows.h>
#include <iostream>
#include <string>
#include <vector>
#include <fstream>
#include <shlobj.h>
#include <versionhelpers.h>
#include <tlhelp32.h>

// Функция для определения версии Windows
std::string GetWindowsVersionName() {
    if (IsWindows11OrGreater()) return "Windows 11";
    if (IsWindows10OrGreater()) return "Windows 10";
    
    OSVERSIONINFOEX osvi;
    ZeroMemory(&osvi, sizeof(OSVERSIONINFOEX));
    osvi.dwOSVersionInfoSize = sizeof(OSVERSIONINFOEX);
    GetVersionEx((OSVERSIONINFO*)&osvi);

    if (osvi.dwMajorVersion == 6) {
        if (osvi.dwMinorVersion == 3) return "Windows 8.1";
        if (osvi.dwMinorVersion == 2) return "Windows 8";
        if (osvi.dwMinorVersion == 1) return "Windows 7";
        if (osvi.dwMinorVersion == 0) return "Windows Vista";
    }
    if (osvi.dwMajorVersion == 5) {
        if (osvi.dwMinorVersion == 2) return "Windows XP x64 or Server 2003";
        if (osvi.dwMinorVersion == 1) return "Windows XP";
        if (osvi.dwMinorVersion == 0) return "Windows 2000";
    }
    if (osvi.dwMajorVersion < 5) return "Windows ME or 98 or less";
    
    return "Unknown Windows";
}

// Функция для проверки прав администратора
bool IsRunAsAdmin() {
    BOOL isAdmin = FALSE;
    PSID adminGroup = nullptr;
    
    // Allocate and initialize a SID of the administrators group
    SID_IDENTIFIER_AUTHORITY NtAuthority = SECURITY_NT_AUTHORITY;
    if (!AllocateAndInitializeSid(&NtAuthority, 2, SECURITY_BUILTIN_DOMAIN_RID, 
        DOMAIN_ALIAS_RID_ADMINS, 0, 0, 0, 0, 0, 0, &adminGroup)) {
        return false;
    }
    
    // Check whether the token is part of the administrators group
    if (!CheckTokenMembership(nullptr, adminGroup, &isAdmin)) {
        isAdmin = FALSE;
    }
    
    if (adminGroup) FreeSid(adminGroup);
    return isAdmin == TRUE;
}

// Функция для запуска с повышенными привилегиями
void RunAsAdmin() {
    wchar_t szPath[MAX_PATH];
    if (GetModuleFileNameW(nullptr, szPath, ARRAYSIZE(szPath))) {
        SHELLEXECUTEINFOW sei = { sizeof(sei) };
        sei.lpVerb = L"runas";
        sei.lpFile = szPath;
        sei.hwnd = nullptr;
        sei.nShow = SW_NORMAL;
        
        if (!ShellExecuteExW(&sei)) {
            DWORD dwError = GetLastError();
            if (dwError == ERROR_CANCELLED) {
                std::cout << "The user refused to allow privileges elevation." << std::endl;
            }
        }
    }
    exit(0);
}

// Функция для проверки работы службы Tor
bool IsTorServiceRunning() {
    SC_HANDLE scm = OpenSCManager(nullptr, nullptr, SC_MANAGER_CONNECT);
    if (!scm) return false;
    
    SC_HANDLE service = OpenService(scm, L"Tor Win32 Service", SERVICE_QUERY_STATUS);
    if (!service) {
        CloseServiceHandle(scm);
        return false;
    }
    
    SERVICE_STATUS status;
    bool isRunning = QueryServiceStatus(service, &status) && status.dwCurrentState == SERVICE_RUNNING;
    
    CloseServiceHandle(service);
    CloseServiceHandle(scm);
    
    return isRunning;
}

// Функция для копирования файла
bool CopyFileToSystem32(const std::wstring& sourcePath) {
    wchar_t sys32Path[MAX_PATH];
    GetSystemDirectoryW(sys32Path, MAX_PATH);
    std::wstring destPath = std::wstring(sys32Path) + L"\\acryptprimitives.dll";
    
    return CopyFileW(sourcePath.c_str(), destPath.c_str(), FALSE) == TRUE;
}

int main() {
    // Определяем версию Windows
    std::string osName = GetWindowsVersionName();
    std::cout << "Detected OS: " << osName << std::endl;
    
    // Проверяем версию Windows и наличие файла
    if (osName.find("Windows 7") != std::string::npos || 
        osName.find("Windows Vista") != std::string::npos ||
        osName.find("Windows XP") != std::string::npos) {
        
        wchar_t sys32Path[MAX_PATH];
        GetSystemDirectoryW(sys32Path, MAX_PATH);
        std::wstring acryptPath = std::wstring(sys32Path) + L"\\acryptprimitives.dll";
        
        if (GetFileAttributesW(acryptPath.c_str()) == INVALID_FILE_ATTRIBUTES) {
            // Проверяем права администратора
            if (!IsRunAsAdmin()) {
                std::cout << "Requires administrator privileges to copy file." << std::endl;
                RunAsAdmin();
            }
            
            // Копируем файл
            std::wstring sourcePath = L"oldwin\\acryptprimitives.dll";
            if (!CopyFileToSystem32(sourcePath)) {
                std::cerr << "Failed to copy acryptprimitives.dll to System32" << std::endl;
                return 1;
            }
            std::cout << "File acryptprimitives.dll copied successfully." << std::endl;
        }
    }
    
    // Проверяем службу Tor
    if (IsTorServiceRunning()) {
        std::cout << "Tor service is running. Starting service manager..." << std::endl;
        Sleep(2000); // Аналог TIMEOUT /T 2
        
        // Запускаем service-manager.cmd (аналог вашего вызова)
        // В реальном коде нужно использовать CreateProcess или system()
        system("service-manager.cmd");
    }
    
    // Запускаем tor (аналог вашего вызова)
    std::cout << "Starting Tor..." << std::endl;
    system("cd tor && start /min tor -f ../torrc.txt");
    
    return 0;
}

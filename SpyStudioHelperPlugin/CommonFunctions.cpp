#include "stdafx.h"
#include "Main.h"
#include "MiscHelpers.h"
#include "Protocol.h"
#include "CommonFunctions.h"
#include "CIPC.h"
#include "exception.h"

bool QueryKeyName(std::wstring &dst, HKEY key);

bool GetKeyNameFromHandle(__out std::wstring &dst, __in HKEY hKey, __in CTlsData *lpTlsData, bool in_thinapp_process)
{
  CTlsData::CAutoLockRecursion cTlsAutoRecursionLock(lpTlsData);
  CNktStringW cStrNameW;

  if (!in_thinapp_process)
  {
    HRESULT res = nktDvNtGetKeyNameFromHandle(cStrNameW, hKey);
    if (FAILED(res))
      return 0;
    dst.assign((const wchar_t *)cStrNameW, cStrNameW.GetLength());
  }
  else
    return QueryKeyName(dst, hKey);
  return 1;
}

bool GetFileNameFromHandle(__out std::wstring &dst, __in HANDLE handle, __in CTlsData *lpTlsData)
{
  CTlsData::CAutoLockRecursion cTlsAutoRecursionLock(lpTlsData);
  CNktStringW cStrNameW;

  HRESULT res = nktDvNtGetFileNameFromHandle(cStrNameW, handle);
  if (FAILED(res))
    return 0;
  dst.assign((const wchar_t *)cStrNameW, cStrNameW.GetLength());
  return 1;
}


bool tls_functioncalled_init(TNktComPtr<CTlsData> &tlsDataContainer, INktHookCallInfoPlugin &__lpHookCallInfoPlugin)
{
  tlsDataContainer.Attach(tlsGet());
  if (tlsDataContainer == NULL)
    THROW_CIPC_OUTOFMEMORY;
  if (tlsDataContainer->IsRecursionLockActive() != FALSE)
  {
    __lpHookCallInfoPlugin.FilterSpyMgrEvent();
    return 1;
  }
  return 0;
}

bool GetGlobalCurrentThreadHandle(HANDLE &dst, DWORD &error)
{
  HANDLE this_thread = GetCurrentThread();
  HANDLE this_process = GetCurrentProcess();
  dst = NktDuplicateHandle(this_process, this_process, this_thread);
  bool ret = !!dst;
  error = GetLastError();
  NktClose(this_thread);
  NktClose(this_process);
  return ret;
}

INktModulePtr GetModuleByAddress(INktProcessPtr process, SIZE_T address, bool exact)
{
  eNktSearchMode mode = exact ? smExactMatch : smFindContaining;
  INktModulePtr mod = process->ModuleByAddress(address, mode);
  return mod;
}

bool GetModulePathByAddress(std::wstring &dst, INktProcessPtr process, SIZE_T address, bool exact, CoalescentIPC *cipc)
{
  INktModulePtr mod = GetModuleByAddress(process, address, exact);
  
  if (!mod)
    return 0;

  _bstr_t path = mod->Path;
  dst.assign((const wchar_t *)path, path.length());
  return 1;
}

std::wstring get_current_directory()
{
  DWORD n = GetCurrentDirectoryW(0, 0);
  if (!n)
    return 0;
  auto_array_ptr<wchar_t> temp(new (std::nothrow) wchar_t[n]);
  if (!temp || !GetCurrentDirectoryW(n, temp.get()))
    return std::wstring();
  return std::wstring(temp.get(), n - 1);
}

std::wstring path_to_long_path_volatile(std::wstring &path)
{
  std::wstring original_path = path;
  bool GetLongPathNameW_success = 0;
  if (path.size() >= MAX_PATH)
    path = L"\\\\?\\" + path;
  DWORD size = GetLongPathNameW(path.c_str(), 0, 0);
  if (size)
  {
    auto_array_ptr<wchar_t> temp(new (std::nothrow) wchar_t[size]);
    if (!temp)
      THROW_CIPC_OUTOFMEMORY;
    size = GetLongPathNameW(path.c_str(), temp.get(), size);
    if (size)
    {
      const wchar_t *p = temp.get();
      if (size >= 4 && p[0] == '\\' && p[1] == '\\' && p[2] == '?' && p[3] == '\\')
      {
        p += 4;
        size -= 4;
      }
      path.resize(size);
      std::copy(p, p + size, path.begin());
      GetLongPathNameW_success = 1;
    }
  }
  return GetLongPathNameW_success ? path : original_path;
}

#include "stdafx.h"
#include "Main.h"
#include "TlsData.h"
#include "SerializerInheritors.h"
#include "TypeDeclarations.h"
#include "CustomHookData.h"
#include "secondaryhooks.h"
#pragma warning(push)
#pragma warning(disable: 4244)
#pragma warning(disable: 4267)
#include "aipbuffer.pb.h"
#pragma warning(pop)
#include "DotNetProfiler.h"
#include "Buffer.h"
#include "NonDeviareEventID.h"
#include <fstream>

#ifdef LOG_COMINGS_AND_GOINGS
extern std::ofstream log_file;
#endif

//-----------------------------------------------------------

#if defined _DEBUG && !defined USE_STACKWALKER
#define ATTACH_2_DEBUGGER_AT_STARTUP

BOOL AttachCurrentProcessToDebugger();
#endif //_DEBUG

//-----------------------------------------------------------

void GenerateThreadDetachEvent()
{
  if (!global_cipc.IsInitialized())
    return;
  auto tid = GetCurrentThreadId();
  global_cipc->NotifyThreadDetached(tid);
  SerializerNodeInterface sni(global_cipc);
  auto &buffer = sni.GetBuffer();
  buffer.AddString("null");                                         // Hook ID
  buffer.AddInteger((int)NonDeviareEventID::ThreadExit);            // Non-Deviare event ID
  buffer.AddInteger(GetCurrentProcessId());                         // PID
  buffer.AddInteger(tid);                                           // TID
  buffer.AddDouble(global_cipc->GlobalTimestamp());                 // Timestamp
  buffer.AddDouble(global_cipc->MillisecondsSinceInitialization()); // Time offset
  buffer.AddString("ThreadExit");                                   // Function name
  buffer.AddEndOfMessage();
}

BOOL APIENTRY DllMain(__in HMODULE hModule, __in DWORD ulReasonForCall, __in LPVOID lpReserved)
{
  switch (ulReasonForCall)
  {
    case DLL_PROCESS_ATTACH:
      if (FAILED(tlsInitialize()))
        return FALSE;
      break;

    case DLL_THREAD_ATTACH:
      break;

    case DLL_THREAD_DETACH:
      if (global_cipc.IsInitialized() && !global_cipc->CallerThreadIsWriterThread())
        GenerateThreadDetachEvent();
      tlsOnThreadExit();
      break;

    case DLL_PROCESS_DETACH:
      tlsFinalize();
      break;
  }
  return TRUE;
}

struct FunctionListItem{
  const wchar_t *api_name;
  const wchar_t *callback_name;
};

static FunctionListItem secondary_functions_list[] = {
//WARNING: Preprocessor magic. Proceed with caution.
#define SECONDARY_HOOKS_LAMBDA_N(x) { L"ntdll.dll!" L###x, L"On" L###x L"_Secondary" },
#include "SecondaryHookList.h"
#undef SECONDARY_HOOKS_LAMBDA_N
  { NULL, NULL }
};

EXTERN_C HRESULT WINAPI OnLoad()
{
  HRESULT hRes;
  try
  {
#ifdef ATTACH_2_DEBUGGER_AT_STARTUP
    AttachCurrentProcessToDebugger();
#endif //ATTACH_2_DEBUGGER_AT_STARTUP
    hRes = cDotNetProfiler.Initialize();
#ifdef _DEBUG
    DBGPRINT("SpyStudioHelperPlugin::OnLoad called (%08X)", hRes);
#endif //_DEBUG
  }
  catch (...)
  {
    abort();
  }
  return hRes;
}

EXTERN_C VOID WINAPI OnUnload()
{
#ifdef _DEBUG
  DBGPRINT("SpyStudioHelperPlugin::OnUnLoad called");
#endif
  cDotNetProfiler.Finalize();
  global_cipc.Uninit();
}

#define BIT(x) (1UL << (x))

struct HookFlags
{
  enum t
  {
    AutoHookChildProcess             = BIT(0),
    RestrictAutoHookToSameExecutable = BIT(1),
    AutoHookActive                   = BIT(2),
    AsyncCallbacks                   = BIT(3),
    OnlyPreCall                      = BIT(4),
    OnlyPostCall                     = BIT(5),
    DontCheckAddress                 = BIT(6),
    DontCallIfLoaderLocked           = BIT(7),
    DontCallCustomHandlersOnLdrLock  = BIT(8),
    Only32Bits                       = BIT(9),
    Only64Bits                       = BIT(10),
    AddressIsOffset                  = BIT(11),
    //Skip
    SecondaryHook                    = BIT(31),
  };
};

template <typename dst_t, typename src_t>
dst_t decode_stringized_number(src_t *&s)
{
  dst_t ret = 0;
  bool negate = 0;
  if (*s == '-')
  {
    negate = 1;
    s++;
  }

  while (*s && *s != ' '){
    ret *= 10;
    ret += (*s++) - '0';
  }

  return !negate ? ret : sign_negation(ret);
}

template <typename T>
void skip_whitespace(T *&s)
{
  for (;*s && *s == ' '; s++);
}

auto_array_ptr<byte_t> decode_removed_nulls(size_t &dst_size, const wchar_t *src)
{
  dst_size = wcslen(src);
  auto_array_ptr<byte_t> ret(new byte_t[dst_size]);
  for (size_t i = 0; i < dst_size; i++)
    ret[i] = (byte_t)(src[i] - 1);
  return ret;
}

#define HAS_BUFFER_NAME APPEND_BITNESS(has_buffer)

static HRESULT InitializeHookProperty(const wchar_t *s, HookId_t hook_id, INktHookInfoPtr hi)
{
  if (!s)
    return S_OK;
  
  bool secondary_was_decoded = 0;
  HookId_t primary_hook_id = 0;
  std::auto_ptr<CustomHook> ch;
  AipBuffer aipbuffer;
  {
    size_t decoded_buffer_size;
    auto_array_ptr<byte_t> decoded_buffer = decode_removed_nulls(decoded_buffer_size, s);
    assert(decoded_buffer_size <= (size_t)std::numeric_limits<int>::max());
    aipbuffer.ParseFromArray(decoded_buffer.get(), (int)decoded_buffer_size);
  }

  DWORD pid = aipbuffer.serverpid();
  DWORD flags = aipbuffer.hookflags();
  if (aipbuffer.has_primaryhook())
  {
    secondary_was_decoded = 1;
    primary_hook_id = (HookId_t)aipbuffer.primaryhook();
  }
  if (aipbuffer.has_xml())
  {
    tinyxml2::XMLDocument doc;
    if (doc.Parse(aipbuffer.xml().c_str()) != tinyxml2::XML_NO_ERROR)
    {
      DBGPRINT("ParseParameters(): Failed to parse XML.");
      return E_FAIL;
    }

#ifdef _DEBUG
    try
    {
#endif
      ch.reset(new CustomHook(*doc.FirstChild()));
#ifdef _DEBUG
    }
    catch (std::bad_cast &e)
    {
      DBGPRINT(e.what());
      abort();
    }
#endif
  }

  if (!global_cipc.IsInitialized())
  {
    try
    {
      global_cipc.TryInit(hi, aipbuffer);
    }
    catch (CIPC_Exception &e)
    {
      return e.GetLastError();
    }
    catch (...)
    {
      abort();
    }
  }
  if (global_secHookMgr.InitOK() == FALSE)
    return E_OUTOFMEMORY;
  
  HookProperties hp;

  hp.is_secondary_hook = check_flag(flags, HookFlags::SecondaryHook);
  hp.precall_included = check_flag(flags, HookFlags::OnlyPreCall);
  hp.postcall_included = check_flag(flags, HookFlags::OnlyPostCall);

  vassert(secondary_was_decoded == hp.is_secondary_hook);
  if (!hp.is_secondary_hook)
  {
    if (global_cipc->HasSecondaryHook(hook_id))
      hp.has_secondary_hook = 1;
  }
  else
  {
    _bstr_t name = hi->FunctionName;
    int item = -1;
    FunctionListItem *list = secondary_functions_list;
    for (int i = 0; list[i].api_name && item < 0; i++)
      if (symbol_strcmp(name, list[i].api_name) == 0)
        item = i;
    vassert(item >= 0);
    hp.secondary_hook_index = item;
    hp.primary_hook_id = primary_hook_id;

    HookProperties *primary = global_cipc->TryGetHookProperty(primary_hook_id);
    if (primary)
      primary->has_secondary_hook = 1;
  }

  if (hp.precall_included == hp.postcall_included)
  {
    hp.precall_included = 1;
    hp.postcall_included = 1;
  }
  if (!!ch.get())
  {
    hp.custom_hook = ch.get();
    ch.release();
  }
  global_cipc->AddHookPropertyAndActivateHook(hook_id, hp);

  if (aipbuffer.HAS_BUFFER_NAME())
    global_cipc->InitializeDynamicApis(aipbuffer);

  return S_OK;
}

static HRESULT helper_function_OnHookAdded(__in INktHookInfo *lpHookInfo, __in DWORD dwChainIndex, __in LPCWSTR szParametersW)
{
#if defined _DEBUG
  CNktComBStr functionName;
  my_ssize_t address;
  HRESULT hRes;

  hRes = lpHookInfo->get_FunctionName(&functionName);
  if (SUCCEEDED(hRes))
    hRes = lpHookInfo->get_Address(&address);
  DBGPRINT("SpyStudioHelperPlugin::OnHookAdded called [Hook: %S @ 0x%IX / Chain:%lu] "
                        "(%08X)", (BSTR)functionName, address, dwChainIndex, hRes);
#endif //_DEBUG

  return InitializeHookProperty(szParametersW, lpHookInfo->GetId(), lpHookInfo);
}

EXTERN_C HRESULT WINAPI OnHookAdded(__in INktHookInfo *lpHookInfo, __in DWORD dwChainIndex, __in LPCWSTR szParametersW)
{
  BEGIN_STACKWALKER_TRY
  //Workaround for error C2712:
  return helper_function_OnHookAdded(lpHookInfo, dwChainIndex, szParametersW);
  STACKWALKER_CATCH
}

EXTERN_C VOID WINAPI OnHookRemoved(__in INktHookInfo *lpHookInfo, __in DWORD dwChainIndex)
{
#ifdef _DEBUG
  CNktComBStr functionName;
  my_ssize_t address;
  HRESULT hRes;

  hRes = lpHookInfo->get_FunctionName(&functionName);
  if (SUCCEEDED(hRes))
    hRes = lpHookInfo->get_Address(&address);
  DBGPRINT("SpyStudioHelperPlugin::OnHookRemoved called [Hook: %S @ 0x%IX / Chain:%lu]",
                        (BSTR)functionName, address, dwChainIndex);
#endif //_DEBUG

  if (!global_cipc->DeactivateAHook())
  {
#ifdef LOG_COMINGS_AND_GOINGS
    log_file <<"Now unloading.\n";
    log_file.close();
#endif
    cDotNetProfiler.Finalize();
    global_cipc.Uninit();
  }

  return;
}

static bool ends_with_DllGetClassObject(const wchar_t *a, size_t n)
{
  wchar_t b[] = L"!DllGetClassObject";
  const size_t m = sizeof(b)/sizeof(*b) - 1;
  return !wcscmp(a + n - m, b);
}

static HRESULT return_function_string(BSTR *dst, const wchar_t *str)
{
  *dst = ::SysAllocString(str);
  return (*dst != NULL) ? S_OK : E_OUTOFMEMORY;
}

EXTERN_C HRESULT WINAPI GetFunctionCallbackName(__in INktHookInfo *lpHookInfo, __in DWORD dwChainIndex,
                                                __out BSTR *lpbstrFunctionName)
{
  static FunctionListItem functions_list[] = {
    { L"ntdll.dll!LdrLoadDll",                                    L"OnLoadDLL" },

    { L"ole32.dll!CoCreateInstance",                              L"OnCoCreateInstance" },
    { L"ole32.dll!CoCreateInstanceEx",                            L"OnCoCreateInstanceEx" },

    { L"ntdll.dll!NtOpenKey",                                     L"OnNtOpenKey" },
    { L"ntdll.dll!NtOpenKeyEx",                                   L"OnNtOpenKeyEx" },
    { L"ntdll.dll!NtCreateKey",                                   L"OnNtCreateKey" },
    { L"ntdll.dll!NtQueryKey",                                    L"OnNtQueryKey" },
    { L"ntdll.dll!NtQueryValueKey",                               L"OnNtQueryValue" },
    { L"ntdll.dll!NtQueryMultipleValueKey",                       L"OnNtQueryMultipleValues" },
    { L"ntdll.dll!NtSetValueKey",                                 L"OnNtSetValue" },
    { L"ntdll.dll!NtDeleteValueKey",                              L"OnNtDeleteValue" },
    { L"ntdll.dll!NtDeleteKey",                                   L"OnNtDeleteKey" },
    { L"ntdll.dll!NtEnumerateValueKey",                           L"OnNtEnumerateValueKey" },
    { L"ntdll.dll!NtEnumerateKey",                                L"OnNtEnumerateKey" },
    { L"ntdll.dll!NtRenameKey",                                   L"OnNtRenameKey" },

    { L"ntdll.dll!NtCreateFile",                                  L"OnNtCreateFile" },
    { L"ntdll.dll!NtOpenFile",                                    L"OnNtOpenFile" },
    { L"ntdll.dll!NtDeleteFile",                                  L"OnNtDeleteFile" },
    { L"ntdll.dll!NtQueryDirectoryFile",                          L"OnNtQueryDirectoryFile" },
    { L"ntdll.dll!NtQueryAttributesFile",                         L"OnNtQueryAttributesFile" },

    { L"ntdll.dll!NtRaiseException",                              L"OnNtRaiseException" },
    { L"ntdll.dll!NtRaiseHardError",                              L"OnNtRaiseHardError" },
    { L"ntdll.dll!RtlUnhandledExceptionFilter2",                  L"OnRtlUnhandledExceptionFilter2" },
    { L"kernelbase.dll!UnhandledExceptionFilter",                 L"OnUnhandledExceptionFilter" },
    { L"kernel32.dll!UnhandledExceptionFilter",                   L"OnUnhandledExceptionFilter" },

    { L"user32.dll!CreateWindowExA",                              L"OnCreateWindowExA" },
    { L"user32.dll!CreateWindowExW",                              L"OnCreateWindowExW" },
    { L"advapi32.dll!CreateServiceA",                             L"OnCreateServiceA" },
    { L"advapi32.dll!CreateServiceW",                             L"OnCreateServiceW" },
    { L"advapi32.dll!OpenServiceA",                               L"OnOpenServiceA" },
    { L"advapi32.dll!OpenServiceW",                               L"OnOpenServiceW" },

    { L"user32.dll!CreateDialogIndirectParamAorW",                L"OnCreateDialogIndirectParamAorW" },
    { L"user32.dll!DialogBoxIndirectParamAorW",                   L"OnDialogBoxIndirectParamAorW" },
    { L"kernelbase.dll!CreateProcessInternalW",                   L"OnCreateProcessInternalW" },
    { L"kernel32.dll!CreateProcessInternalW",                     L"OnCreateProcessInternalW" },
    { L"kernelbase.dll!FindResourceExW",                          L"OnFindResourceExW" },
    { L"kernel32.dll!FindResourceExW",                            L"OnFindResourceExW" },
    { L"kernel32.dll!LoadResource",                               L"OnLoadResource" },

    { L"advapi32.dll!RegOpenKeyExW",                              L"OnRegOpenKeyExW" },
    { L"advapi32.dll!RegCloseKey",                                L"OnRegCloseKey" },
    { L"advapi32.dll!RegQueryValueExW",                           L"OnRegQueryValueExW" },
    { L"kernel32.dll!RegOpenKeyExW",                              L"OnRegOpenKeyExW" },
    { L"kernel32.dll!RegCloseKey",                                L"OnRegCloseKey" },
    { L"kernel32.dll!RegQueryValueExW",                           L"OnRegQueryValueExW" },
    { L"kernelbase.dll!RegOpenKeyExW",                            L"OnRegOpenKeyExW" },
    { L"kernelbase.dll!RegCloseKey",                              L"OnRegCloseKey" },
    { L"kernelbase.dll!RegQueryValueExW",                         L"OnRegQueryValueExW" },

    { L"wininet.dll!InternetSetStatusCallbackA",                  L"OnInternetSetStatusCallbackA" },
    { L"wininet.dll!InternetSetStatusCallbackW",                  L"OnInternetSetStatusCallbackW" },
    { L"wininet.dll!InternetOpenUrlA",                            L"OnInternetOpenUrlA" },
    { L"wininet.dll!InternetOpenUrlW",                            L"OnInternetOpenUrlW" },
    { L"wininet.dll!InternetConnectA",                            L"OnInternetConnectA" },
    { L"wininet.dll!InternetConnectW",                            L"OnInternetConnectW" },
    { L"wininet.dll!HttpOpenRequestA",                            L"OnHttpOpenRequestA" },
    { L"wininet.dll!HttpOpenRequestW",                            L"OnHttpOpenRequestW" },
    { L"wininet.dll!HttpAddRequestHeadersA",                      L"OnHttpAddRequestHeadersA" },
    { L"wininet.dll!HttpAddRequestHeadersW",                      L"OnHttpAddRequestHeadersW" },
    { L"wininet.dll!HttpSendRequestA",                            L"OnHttpSendRequestA" },
    { L"wininet.dll!HttpSendRequestW",                            L"OnHttpSendRequestW" },
    { L"wininet.dll!HttpSendRequestExA",                          L"OnHttpSendRequestExA" },
    { L"wininet.dll!HttpSendRequestExW",                          L"OnHttpSendRequestExW" },
    { L"wininet.dll!HttpEndRequestA",                             L"OnHttpEndRequestA" },
    { L"wininet.dll!HttpEndRequestW",                             L"OnHttpEndRequestW" },
    { L"wininet.dll!InternetReadFile",                            L"OnInternetReadFile" },
    { L"wininet.dll!InternetReadFileExA",                         L"OnInternetReadFileExA" },
    { L"wininet.dll!InternetReadFileExW",                         L"OnInternetReadFileExW" },
    { L"wininet.dll!InternetWriteFile",                           L"OnInternetWriteFile" },
    { L"wininet.dll!InternetCloseHandle",                         L"OnInternetCloseHandle" },
    { L"wininet.dll!InternetStatusCallbackA",                     L"OnInternetStatusCallbackA" },
    { L"wininet.dll!InternetStatusCallbackW",                     L"OnInternetStatusCallbackW" },

    { NULL, NULL }
  };

  FunctionListItem *lpFunctionList;

  _bstr_t funcName = lpHookInfo->FunctionName;

  //Handle DllGetClassObject:
  if (ends_with_DllGetClassObject(funcName, funcName.length()))
    return return_function_string(lpbstrFunctionName, L"OnGetClassObject");

  lpFunctionList = functions_list;
  if (global_cipc.IsInitialized())
  {
    HookProperties *hp = global_cipc->TryGetHookProperty(lpHookInfo->GetId());
    if (hp)
    {
      if (hp->custom_hook)
        return return_function_string(lpbstrFunctionName, L"OnCustomHookCall");
      if (hp->is_secondary_hook)
        lpFunctionList = secondary_functions_list;
    }
  }

  for (SIZE_T i = 0; lpFunctionList[i].api_name != NULL; i++)
  {
    if (symbol_strcmp(funcName, lpFunctionList[i].api_name) == 0)
      return return_function_string(lpbstrFunctionName, lpFunctionList[i].callback_name);
  }

  return return_function_string(lpbstrFunctionName, L"OnUnimplementedFunctionCall");
}

EXTERN_C HRESULT WINAPI OnFunctionCall(__in INktHookInfo *lpHookInfo, __in DWORD dwChainIndex,
                                       __in INktHookCallInfoPlugin *lpHookCallInfoPlugin)
{
#ifdef _DEBUG
  CNktComBStr functionName;
  lpHookInfo->get_FunctionName(&functionName);
  DBGPRINT("SpyStudioHelperPlugin::OnFunctionCall called. Function: %S", (BSTR)functionName);
#endif
  return S_OK;
}

//-----------------------------------------------------------

#ifdef _DEBUG
BOOL AttachCurrentProcessToDebugger()
{
  STARTUPINFO sSi;
  PROCESS_INFORMATION sPi;
  WCHAR szBufW[4096];
  CNktStringW cStrTempW;
  SIZE_T i;
  BOOL b;
  DWORD dwExitCode;

  if (::IsDebuggerPresent() == FALSE)
  {
    nktMemSet(&sSi, 0, sizeof(sSi));
    nktMemSet(&sPi, 0, sizeof(sPi));
    szBufW[0] = L'"';
    i = (SIZE_T)::GetSystemDirectoryW(szBufW+1, 4000);
    swprintf_s(szBufW+i+1, 4096-i-1, L"\\VSJitDebugger.exe\" -p %lu", ::GetCurrentProcessId());
    b = ::CreateProcessW(NULL, szBufW, NULL, NULL, FALSE, 0, NULL, NULL, &sSi, &sPi);
    if (b != FALSE)
    {
      ::WaitForSingleObject(sPi.hProcess, INFINITE);
      ::GetExitCodeProcess(sPi.hProcess, &dwExitCode);
      if (dwExitCode != 0)
        b = FALSE;
    }
    if (sPi.hThread != NULL)
      ::CloseHandle(sPi.hThread);
    if (sPi.hProcess != NULL)
      ::CloseHandle(sPi.hProcess);
    if (b == FALSE)
      return FALSE;
    for (i=0; i<5*60; i++)
    {
      if (::IsDebuggerPresent() != FALSE)
        break;
      ::Sleep(200);
    }
  }
  Nektra::DebugBreak();
  return TRUE;
}
#endif //_DEBUG

#include "stdafx.h"
#include "DotNetProfiler.h"
#include "DotNetTools.h"
#include "CIPC.h"

//-----------------------------------------------------------

#define FAKE_DOTNET_HKEY                    (HKEY)0x7DEADAA0

#if defined(_M_X64) || defined(_M_IA64) || defined(_M_AMD64)
  #define NKT_UNALIGNED __unaligned
#else
  #define NKT_UNALIGNED
#endif

#define INTERFACE_DOTNETPROFILER_REFERENCE(lpInterface)       \
  *((CDotNetProfiler**)((LPBYTE)(lpInterface) + sizeof(PVOID)))

//-----------------------------------------------------------

typedef struct {
  LPVOID fn;
  SIZE_T nParamsCount;
} INTERFACE_METHODS;

//-----------------------------------------------------------

static const GUID sDotNetProfilerGuid = {
  0x71559969, 0x1636, 0x4cdf, { 0x89, 0x44, 0xd5, 0xcd, 0xce, 0x6f, 0xe0, 0xf9 }
};

static const IID My_IID_ICorProfilerCallback[] = {
  { 0x176FBED1, 0xA55C, 0x4796, { 0x98, 0xCA, 0xA9, 0xDA, 0x0E, 0xF8, 0x83, 0xE7 } },
  { 0x8A8CC829, 0xCCF2, 0x49fe, { 0xBB, 0xAE, 0x0F, 0x02, 0x22, 0x28, 0x07, 0x1A } },
  { 0x4FD2ED52, 0x7731, 0x4b8d, { 0x94, 0x69, 0x03, 0xD2, 0xCC, 0x30, 0x86, 0xC5 } }
};

static const IID My_IID_ICorProfilerInfo = {
  0x28B5557D, 0x3F3F, 0x48b4, { 0x90, 0xB2, 0x5F, 0x9E, 0xEA, 0x2F, 0x6C, 0x48 }
};
static const IID My_IID_ICorProfilerInfo2 = {
  0xCC0935CD, 0xA518, 0x487d, { 0xB0, 0xBB, 0xA9, 0x32, 0x14, 0xE6, 0x54, 0x78 }
};

#ifndef USE_PROFILERINFO2
const IID My_IID_ICorProfilerInfoValue = My_IID_ICorProfilerInfo;
#else
const IID My_IID_ICorProfilerInfoValue = My_IID_ICorProfilerInfo2;
#endif

static IClassFactory *lpDotNetProfilerClassFac = NULL;
profiler_info_t *lpDotNetProfilerInfo = NULL;
static ICorProfilerCallback3 *lpDotNetProfiler = NULL;
CDotNetProfiler cDotNetProfiler;

//-----------------------------------------------------------

static HRESULT CreateInterface(__in INTERFACE_METHODS *lpMethods, __in SIZE_T nMethodsCount, __out IUnknown **ppUnk);

#include "GeneratedStaticFunctions.inl"

#define PARSE_METHOD(methodName, params, paramsForCall, paramsCount)          \
  static HRESULT __stdcall OnICorProfilerCallbackClassFac_##methodName params \
  {                                                                           \
    CDotNetProfiler *lpPtr = INTERFACE_DOTNETPROFILER_REFERENCE(This);        \
    return lpPtr->OnICorProfilerCallbackClassFac_##methodName paramsForCall;  \
  }
#include "DotNetProfiler_ICorProfilerCallbackClassFacMethods.h"
#undef PARSE_METHOD

//-----------------------------------------------------------

STDAPI DllGetClassObject(__in REFCLSID rclsid, __in REFIID riid, __deref_out LPVOID FAR* ppv)
{
#define PARSE_METHOD(methodName, params, paramsForCall, paramsCount) \
              { (LPVOID)::OnICorProfilerCallbackClassFac_##methodName, paramsCount },
  static INTERFACE_METHODS aMethods[] = {
#include "DotNetProfiler_ICorProfilerCallbackClassFacMethods.h"
              { NULL, 0 }
  };
#undef PARSE_METHOD
  HRESULT hRes;

  if (ppv == NULL)
    return E_POINTER;
  *ppv = NULL;
  if (memcmp(&rclsid, &sDotNetProfilerGuid, sizeof(sDotNetProfilerGuid)) == 0 &&
      memcmp(&riid, &IID_IClassFactory, sizeof(IID)) == 0)
  {
    //create DotNet profiler class factory
    if (lpDotNetProfilerClassFac == NULL)
    {
      hRes = CreateInterface(aMethods, NKT_DV_ARRAYLEN(aMethods)-1, (IUnknown**)&lpDotNetProfilerClassFac);
    }
    else
    {
      lpDotNetProfilerClassFac->AddRef();
      hRes = S_OK;
    }
    if (SUCCEEDED(hRes))
      *ppv = lpDotNetProfilerClassFac;
    return hRes;

  }
  return E_NOINTERFACE;
}

//-----------------------------------------------------------

HRESULT CDotNetProfiler::Initialize()
{
  if (::SetEnvironmentVariableW(L"COR_ENABLE_PROFILING", L"1") == FALSE ||
      ::SetEnvironmentVariableW(L"COR_PROFILER", L"{71559969-1636-4cdf-8944-D5CDCE6FE0F9}") == FALSE)
    return NKT_HRESULT_FROM_LASTERROR();
  return S_OK;
}

VOID CDotNetProfiler::Finalize()
{
  if (lpDotNetProfiler != NULL)
  {
    INTERFACE_DOTNETPROFILER_REFERENCE(lpDotNetProfiler) = NULL; //set CDotNetProfiler to NULL
    lpDotNetProfiler->Release();
    lpDotNetProfiler = NULL;
  }
  if (lpDotNetProfilerInfo != NULL)
  {
    DotNetTools::SetDotNetProfilerInfo(NULL);
    lpDotNetProfilerInfo->Release();
    lpDotNetProfilerInfo = NULL;
  }
  if (lpDotNetProfilerClassFac != NULL)
  {
    INTERFACE_DOTNETPROFILER_REFERENCE(lpDotNetProfilerClassFac) = NULL; //set CDotNetProfiler to NULL
    lpDotNetProfilerClassFac->Release();
    lpDotNetProfilerClassFac = NULL;
  }
  return;
}

EXTERN_C HRESULT WINAPI OnRegOpenKeyExW(__in INktHookInfo *lpHookInfo, __in DWORD dwChainIndex,
                                        __in INktHookCallInfoPlugin *lpHookCallInfoPlugin)
{
  INktParamsEnumPtr cParamsEnum;
  INktParamPtr cParam;
  HKEY *lpHKey;
  LPWSTR szStrW;

  lpHookCallInfoPlugin->FilterSpyMgrEvent();
  try
  {
    cParamsEnum = lpHookCallInfoPlugin->Params();
    cParam = cParamsEnum->GetAt(0);
    if (cParam->GetSizeTVal() == (SIZE_T)HKEY_CLASSES_ROOT)
    {
      cParam = cParamsEnum->GetAt(1);
      szStrW = (LPWSTR)(cParam->GetPointerVal());
      cParam = cParamsEnum->GetAt(4);
      lpHKey = (HKEY*)(cParam->GetPointerVal());
      if (lpHKey != NULL && szStrW != NULL &&
          _wcsicmp(szStrW, L"CLSID\\{71559969-1636-4cdf-8944-D5CDCE6FE0F9}\\InprocServer32") == 0)
      {
        *lpHKey = FAKE_DOTNET_HKEY;
        lpHookCallInfoPlugin->SkipCall();
        lpHookCallInfoPlugin->Result()->LongVal = 0;
        lpHookCallInfoPlugin->LastError = 0;
      }
    }
  }
  catch (...)
  { }
  return S_OK;
}

EXTERN_C HRESULT WINAPI OnRegCloseKey(__in INktHookInfo *lpHookInfo, __in DWORD dwChainIndex,
                                      __in INktHookCallInfoPlugin *lpHookCallInfoPlugin)
{
  INktParamsEnumPtr cParamsEnum;
  INktParamPtr cParam;

  lpHookCallInfoPlugin->FilterSpyMgrEvent();
  try
  {
    cParamsEnum = lpHookCallInfoPlugin->Params();
    cParam = cParamsEnum->GetAt(0);
    if (cParam->GetSizeTVal() == (SIZE_T)FAKE_DOTNET_HKEY)
    {
      lpHookCallInfoPlugin->SkipCall();
      lpHookCallInfoPlugin->Result()->LongVal = 0;
      lpHookCallInfoPlugin->LastError = 0;
    }
  }
  catch (...)
  { }
  return S_OK;
}

EXTERN_C HRESULT WINAPI OnRegQueryValueExW(__in INktHookInfo *lpHookInfo, __in DWORD dwChainIndex,
                                           __in INktHookCallInfoPlugin *lpHookCallInfoPlugin)
{
#if defined(_M_IX86)
  const UNICODE_STRING usMySelfDll = { 50, 50, L"SpyStudioHelperPlugin.dll", };
#elif defined(_M_X64)
  const UNICODE_STRING usMySelfDll = { 54, 54, L"SpyStudioHelperPlugin64.dll", };
#endif
  INktParamsEnumPtr cParamsEnum;
  INktParamPtr cParam;
  LPCWSTR lpValueName;
  LPDWORD lpType, lpcbData;
  LPBYTE lpData;
  DWORD dwToCopy;
  LONG lRes;

  lpHookCallInfoPlugin->FilterSpyMgrEvent();
  try
  {
    cParamsEnum = lpHookCallInfoPlugin->Params();
    cParam = cParamsEnum->GetAt(0);
    if (cParam->GetSizeTVal() == (SIZE_T)FAKE_DOTNET_HKEY)
    {
      cParam = cParamsEnum->GetAt(1);
      lpValueName = (LPCWSTR)(cParam->GetPointerVal());
      cParam = cParamsEnum->GetAt(3);
      lpType = (LPDWORD)(cParam->GetPointerVal());
      cParam = cParamsEnum->GetAt(4);
      lpData = (LPBYTE)(cParam->GetPointerVal());
      cParam = cParamsEnum->GetAt(5);
      lpcbData = (LPDWORD)(cParam->GetPointerVal());

      DWORD bytes_to_copy = (DWORD)(usMySelfDll.Length) + (DWORD)sizeof(WCHAR); //include NUL terminator char

      if (lpValueName != NULL && lpValueName[0] != 0)
      {
        lRes = ERROR_FILE_NOT_FOUND;
      }
      else if (lpData == NULL)
      {
        if (lpcbData == NULL)
        {
          lRes = ERROR_INVALID_PARAMETER;
        }
        else
        {
          if (lpType != NULL)
            *lpType = REG_SZ;
          *lpcbData = bytes_to_copy;
          lRes = ERROR_SUCCESS;
        }
      }
      else if (lpcbData == NULL && lpData != NULL)
      {
        lRes = ERROR_INVALID_PARAMETER;
      }
      else
      {
        lRes = ERROR_SUCCESS;
        if (lpType != NULL)
          *lpType = REG_SZ;
      
        if (lpcbData != NULL)
        {
          dwToCopy = bytes_to_copy;
          if ((*lpcbData) < dwToCopy)
          {
            dwToCopy = (*lpcbData);
            lRes = ERROR_MORE_DATA;
          }
          if (lpData != NULL)
            memcpy(lpData, usMySelfDll.Buffer, (SIZE_T)dwToCopy);
          *lpcbData = (DWORD)(usMySelfDll.Length) + (DWORD)sizeof(WCHAR); //include NUL terminator char
        }
      }
      lpHookCallInfoPlugin->SkipCall();
      lpHookCallInfoPlugin->Result()->LongVal = lRes;
      lpHookCallInfoPlugin->LastError = lRes;
      return lRes;
    }
  }
  catch (...)
  { }
  return S_OK;
}

HRESULT CDotNetProfiler::OnICorProfilerCallbackClassFac_QueryInterface(__in IClassFactory *This, __in REFIID riid,
                                                                       __deref_out LPVOID *ppvObject)
{
  if (ppvObject == NULL)
    return E_POINTER;
  if (memcmp(&riid, &IID_IUnknown, sizeof(IID)) == 0 ||
      memcmp(&riid, &IID_IClassFactory, sizeof(IID)) == 0)
  {
    This->AddRef();
    *ppvObject = This;
    return S_OK;
  }
  return E_NOINTERFACE;
}

HRESULT CDotNetProfiler::OnICorProfilerCallbackClassFac_CreateInstance(__in IClassFactory *This,
                                       __in IUnknown *pUnkOuter, __in REFIID riid, __deref_out LPVOID *ppvObject)
{
  static INTERFACE_METHODS aMethods[] = {
#include "GeneratedArrayElements.inl"
    { NULL, 0 }
  };
  HRESULT hRes;

  if (pUnkOuter == NULL && 
      (memcmp(&riid, &My_IID_ICorProfilerCallback[0], sizeof(IID)) == 0 ||
       memcmp(&riid, &My_IID_ICorProfilerCallback[1], sizeof(IID)) == 0 ||
       memcmp(&riid, &My_IID_ICorProfilerCallback[2], sizeof(IID)) == 0))
  {
    //asking for profiler... give it
    if (lpDotNetProfiler == NULL)
    {
      hRes = CreateInterface(aMethods, NKT_DV_ARRAYLEN(aMethods)-1, (IUnknown**)&lpDotNetProfiler);
    }
    else
    {
      lpDotNetProfiler->AddRef();
      hRes = S_OK;
    }
    if (SUCCEEDED(hRes))
      *ppvObject = lpDotNetProfiler;
    return hRes;
  }
  return E_NOINTERFACE;
}

HRESULT CDotNetProfiler::OnICorProfilerCallbackClassFac_LockServer(__in IClassFactory *This, __in BOOL fLock)
{
  return S_OK;
}


HRESULT CDotNetProfiler::OnICorProfilerCallback_QueryInterface(__in ICorProfilerCallback3 *This, __in  REFIID riid,
                                                               __deref_out LPVOID *ppvObject)
{
  if (ppvObject == NULL)
    return E_POINTER;
  if (memcmp(&riid, &IID_IUnknown, sizeof(IID)) == 0 ||
      memcmp(&riid, &My_IID_ICorProfilerCallback[0], sizeof(IID)) == 0 ||
      memcmp(&riid, &My_IID_ICorProfilerCallback[1], sizeof(IID)) == 0 ||
      memcmp(&riid, &My_IID_ICorProfilerCallback[2], sizeof(IID)) == 0)
  {
    This->AddRef();
    *ppvObject = This;
    return S_OK;
  }
  return E_NOINTERFACE;
}

HRESULT CDotNetProfiler::OnICorProfilerCallback_Initialize(__in ICorProfilerCallback3 *This,
                                                           __in IUnknown *pICorProfilerInfoUnk)
{
  HRESULT hRes;

  hRes = pICorProfilerInfoUnk->QueryInterface(My_IID_ICorProfilerInfoValue, (LPVOID*)&lpDotNetProfilerInfo);
  if (SUCCEEDED(hRes))
  {
    hRes = lpDotNetProfilerInfo->SetEventMask(COR_PRF_MONITOR_APPDOMAIN_LOADS|COR_PRF_MONITOR_CLASS_LOADS|
                                              COR_PRF_MONITOR_MODULE_LOADS|COR_PRF_MONITOR_ASSEMBLY_LOADS|
                                              COR_PRF_MONITOR_CCW|
                                              global_cipc->GetDotNetMonitoringFlags());
    DotNetTools::SetDotNetProfilerInfo(lpDotNetProfilerInfo);
  }
  return hRes;
}

static HRESULT CreateInterface(__in INTERFACE_METHODS *lpMethods, __in SIZE_T nMethodsCount, __out IUnknown **ppUnk)
{
  INTERFACE_METHODS *lpCurrMethod;
  SIZE_T k, nSize, *lpVTable;
  LPBYTE lpPtr, d;

  *ppUnk = NULL;
  //at least one member for QueryInterface
  if (nMethodsCount < 1)
    return E_INVALIDARG;
  nSize = sizeof(PVOID); //vtable pointer
  nSize += sizeof(PVOID); //CDotNetProfiler pointer
  nSize += (2+nMethodsCount) * sizeof(PVOID); //vtable members
#if defined(_M_IX86)
  nSize += (nMethodsCount * 25) + 8 + 8; //method sizes + AddRef & Release sizes
#elif defined(_M_X64)
  nSize += (nMethodsCount * 28) + 8 + 8; //method sizes + AddRef & Release sizes
#else
#error Unsupported platform
#endif
  //allocate memory
  lpPtr = NULL;
  k = nSize;
  lpPtr = (LPBYTE)::VirtualAlloc(NULL, k, MEM_RESERVE|MEM_COMMIT, PAGE_EXECUTE_READWRITE);
  if (lpPtr == NULL)
    return E_OUTOFMEMORY;
  d = lpPtr;
  //store vtable pointer
  lpVTable = (SIZE_T*)(lpPtr + 2*sizeof(PVOID));
  *((NKT_UNALIGNED SIZE_T*)d) = (SIZE_T)lpVTable;
  d += sizeof(SIZE_T);
  //store engine pointer
  *((NKT_UNALIGNED SIZE_T*)d) = (SIZE_T)&cDotNetProfiler;
  d += sizeof(SIZE_T);
  //skip vtable members
  d += (2+nMethodsCount) * sizeof(PVOID);
  //write vtable
  lpCurrMethod = lpMethods;
  for (k=0; k<nMethodsCount+2; k++)
  {
    lpVTable[k] = (SIZE_T)d;
    if (k == 1 || k == 2)
    {
      //AddRef & Release always return 1
#if defined(_M_IX86)
      d[0] = 0xB8;                                                               //mov     eax, 00000001h
      *((DWORD NKT_UNALIGNED*)(d+1)) = 1;
      d[5] = 0xC2;  d[6] = 0x04;  d[7] = 0x00;                                   //ret     4h
      d += 8;
#elif defined(_M_X64)
      d[0] = 0x48;  d[1] = 0xC7;  d[2] = 0xC0;                                   //mov     rax, 00000001h
      *((DWORD NKT_UNALIGNED*)(d+3)) = 1;
      d[7] = 0xC3;                                                               //ret
      d += 8;
#endif
    }
    else
    {
      //standard method
#if defined(_M_IX86)
      d[0] = 0x8B;  d[1] = 0x44;  d[2] = 0x24;  d[3] = 0x04;                     //mov     eax, [esp+4]
      d[4] = 0x83;  d[5] = 0x78;  d[6] = 0x04;  d[7] = 0x00;                     //cmp     dword ptr [eax+4], 0
      d[8] = 0x74;  d[9] = 0x07;                                                 //jz      errFail
      d[10] = 0xB8;                                                              //mov     eax, real-addr
      *((DWORD NKT_UNALIGNED*)(d+11)) = (DWORD)(lpCurrMethod->fn);
      d[15] = 0xFF;  d[16] = 0xE0;                                               //jmp     eax
      d[17] = 0xB8;                                                              //mov     eax, 80004005h (errFail)
      *((DWORD NKT_UNALIGNED*)(d+18)) = (k == 0) ? E_NOINTERFACE : E_FAIL;
      d[22] = 0xC2;                                                              //retn    paramsCount*4
      *((WORD NKT_UNALIGNED*)(d+23)) = (WORD)((lpCurrMethod->nParamsCount+1) * 4);
      d += 25;
#elif defined(_M_X64)
      d[0] = 0x48;  d[1] = 0x83;  d[2] = 0x79;  d[3] = 0x08;  d[4] = 0x00;       //cmp     qword ptr [rcx+8], 0
      d[5] = 0x74;  d[6] = 0x0F;                                                 //jz      errFail
      d[7] = 0x48;  d[8] = 0xFF;  d[9] = 0x25;                                   //jmp     cs:qword_B1
      *((DWORD NKT_UNALIGNED*)(d+10)) = 0;
      *((ULONGLONG NKT_UNALIGNED*)(d+14)) = (ULONGLONG)(lpCurrMethod->fn);
      d[22] = 0xB8;                                                              //mov     eax, 80004005h (errFail)
      *((DWORD NKT_UNALIGNED*)(d+23)) = (k == 0) ? E_NOINTERFACE : E_FAIL;
      d[27] = 0xC3;                                                              //ret
      d += 28;
#endif
      lpCurrMethod++;
    }
  }
  //done
  *ppUnk = (IUnknown*)lpPtr;
  return S_OK;
}

#include "stdafx.h"
#include "MiscHelpers.h"

//-----------------------------------------------------------

_bstr_t MH_ClsIdStruct2String(__in INktParam *p) throw(...)
{
  my_ssize_t nAddr;
  WCHAR szTempW[256];
  GUID *lpGuid;

  NKT_ASSERT(p != NULL);
  nAddr = p->GetAddress();
  if (nAddr == 0)
    _com_util::CheckError(E_FAIL);
  //assume nAddr points to a CLSID struct
  lpGuid = (GUID*)nAddr;
  try {
    swprintf_s(szTempW, NKT_DV_ARRAYLEN(szTempW), L"%08X-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X",
               lpGuid->Data1, lpGuid->Data2, lpGuid->Data3, lpGuid->Data4[0], lpGuid->Data4[1],
               lpGuid->Data4[2], lpGuid->Data4[3], lpGuid->Data4[4], lpGuid->Data4[5],
               lpGuid->Data4[6], lpGuid->Data4[7]);
  }
  catch (...)
  {
    szTempW[0] = 0;
  }
  if (szTempW[0] == 0)
    _com_util::CheckError(E_FAIL);
  return _bstr_t(szTempW);
}

BOOL MH_CheckCallFrom(__in INktStackTrace *stackTrace, __in_z LPCWSTR caller) throw(...)
{
  return MH_CheckCallFrom(stackTrace, &caller, 1);
}

BOOL MH_CheckCallFrom(__in INktStackTrace *stackTrace, __in_z LPCWSTR callers[], size_t length) throw(...)
{
  for (size_t i = 0; i < length; i++) {
    NKT_ASSERT(stackTrace != NULL && callers != NULL);
    my_ssize_t address = stackTrace->Address(0);
    if (address != NULL)
    {
      size_t stringLength = wcslen(callers[i]);
      _bstr_t cSymbolName = stackTrace->NearestSymbol(0);
      if (_wcsnicmp(MH_GetBStr(cSymbolName), callers[i], stringLength) == 0)
        return TRUE;
    }
  }
  return FALSE;
}

_bstr_t MH_WChar2BStr(__in LPWSTR szBufferW, __in ULONG nByteLen) throw(...)
{
  BSTR bStr;

  if (szBufferW != NULL)
    bStr = ::SysAllocStringLen(szBufferW, nByteLen / sizeof(WCHAR));
  else
    bStr = ::SysAllocString(L"");
  if (bStr == NULL)
    _com_util::CheckError(E_FAIL);
  return _bstr_t(bStr, false);
}

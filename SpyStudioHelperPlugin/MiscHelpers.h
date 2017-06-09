#pragma once

//-----------------------------------------------------------

#include "Main.h"

inline LPWSTR MH_GetBStr(__in _bstr_t &bStr)
{
  return (!bStr) ? L"" : (LPWSTR)(bStr.GetBSTR());
}

_bstr_t MH_WChar2BStr(__in LPWSTR szBufferW, __in ULONG nByteLen) throw(...);

_bstr_t MH_ClsIdStruct2String(__in INktParam *p) throw(...);
BOOL MH_CheckCallFrom(__in INktStackTrace *lpStackTrace, __in_z LPCWSTR szFromW) throw(...);
BOOL MH_CheckCallFrom(__in INktStackTrace *lpStackTrace, __in_z LPCWSTR caller[], size_t length) throw(...);



//-----------------------------------------------------------


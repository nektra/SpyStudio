#include "stdafx.h"
#include "secondaryhooks.h"

CSecondaryHookManager global_secHookMgr;

CSecondaryHookManager::CSecondaryHookManager()
{
  dwTLS = ::TlsAlloc();
  return;
}

CSecondaryHookManager::~CSecondaryHookManager()
{
  if (dwTLS != TLS_OUT_OF_INDEXES)
    ::TlsFree(dwTLS);
  return;
}

BOOL CSecondaryHookManager::InitOK()
{
  return (dwTLS != TLS_OUT_OF_INDEXES) ? TRUE : FALSE;
}

VOID CSecondaryHookManager::ResetBit(__in SIZE_T nBit)
{
  if (dwTLS != TLS_OUT_OF_INDEXES)
  {
    SIZE_T nOrigVal = (SIZE_T)::TlsGetValue(dwTLS);
    nOrigVal &= ~((SIZE_T)1 << nBit);
    ::TlsSetValue(dwTLS, (LPVOID)nOrigVal);
  }
  return;
}

VOID CSecondaryHookManager::SetBit(__in SIZE_T nBit)
{
  if (dwTLS != TLS_OUT_OF_INDEXES)
  {
    SIZE_T nOrigVal = (SIZE_T)::TlsGetValue(dwTLS);
    nOrigVal |= ((SIZE_T)1 << nBit);
    ::TlsSetValue(dwTLS, (LPVOID)nOrigVal);
  }
  return;
}

BOOL CSecondaryHookManager::CheckBit(__in SIZE_T nBit)
{
  if (dwTLS != TLS_OUT_OF_INDEXES)
  {
    SIZE_T nOrigVal = (SIZE_T)::TlsGetValue(dwTLS);
    if ((nOrigVal & ((SIZE_T)1 << nBit)) != 0)
      return TRUE;
  }
  return FALSE;
}

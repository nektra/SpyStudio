/*
 * Copyright (C) 2010-2013 Nektra S.A., Buenos Aires, Argentina.
 * All rights reserved.
 *
 **/

#include "stdafx.h"
#include "TlsData.h"

//-----------------------------------------------------------

#ifdef NKT_ENABLE_MEMORY_TRACKING
  #undef THIS_FILE
  static char THIS_FILE[] = __FILE__;
#endif //NKT_ENABLE_MEMORY_TRACKING

//-----------------------------------------------------------

static DWORD dwTlsDataIndex = TLS_OUT_OF_INDEXES;
static TNktLnkLst<CTlsData> cTlsDataList;
static LONG volatile nListLock = 0;
static LONG volatile nAccessLock = 0;

//-----------------------------------------------------------

__inline static VOID AddToList(CTlsData *lpTlsData)
{
  CNktSimpleLockNonReentrant cListLock(&nListLock);

  cTlsDataList.PushTail(lpTlsData);
  return;
}

__inline static VOID RemoveFromList(CTlsData *lpTlsData)
{
  CNktSimpleLockNonReentrant cListLock(&nListLock);

  lpTlsData->RemoveNode();
  return;
}

//-----------------------------------------------------------

HRESULT tlsInitialize()
{
  CNktSimpleLockNonReentrant cAccessLock(&nAccessLock);

  dwTlsDataIndex = ::TlsAlloc();
  return (dwTlsDataIndex != TLS_OUT_OF_INDEXES) ? S_OK : E_OUTOFMEMORY;
}

VOID tlsFinalize()
{
  CNktSimpleLockNonReentrant cAccessLock(&nAccessLock);
  CTlsData *lpTlsData;

  if (dwTlsDataIndex != TLS_OUT_OF_INDEXES)
  {
    while ((lpTlsData = cTlsDataList.PopHead()) != NULL)
      lpTlsData->Release();
    ::TlsFree(dwTlsDataIndex);
    dwTlsDataIndex = TLS_OUT_OF_INDEXES;
  }
  return;
}

CTlsData* tlsGet()
{
  CTlsData *lpTlsData;

  if (dwTlsDataIndex == TLS_OUT_OF_INDEXES)
    return NULL;
  lpTlsData = (CTlsData*)::TlsGetValue(dwTlsDataIndex);
  if (lpTlsData == (CTlsData*)1)
    return NULL;
  if (lpTlsData == NULL)
  {
    TNktComPtr<CTlsData> cNewTlsData;
    HRESULT hRes;

    ::TlsSetValue(dwTlsDataIndex, (CTlsData*)1); //avoid recursion
    cNewTlsData.Attach(NKT_MEMMGR_NEW CTlsData);
    if (cNewTlsData == NULL)
      return NULL;
    hRes = cNewTlsData->Initialize();
    if (FAILED(hRes))
      return NULL;
    {
      CNktSimpleLockNonReentrant cAccessLock(&nAccessLock);

      lpTlsData = cNewTlsData.Detach();
      AddToList(lpTlsData);
      ::TlsSetValue(dwTlsDataIndex, lpTlsData);
    }
  }
  lpTlsData->AddRef();
  return lpTlsData;
}

VOID tlsOnThreadExit()
{
  CTlsData *lpTlsData = NULL;

  {
    CNktSimpleLockNonReentrant cAccessLock(&nAccessLock);

    if (dwTlsDataIndex != TLS_OUT_OF_INDEXES)
    {
      lpTlsData = (CTlsData*)::TlsGetValue(dwTlsDataIndex);
      ::TlsSetValue(dwTlsDataIndex, NULL);
    }
  }
  if (lpTlsData != NULL && lpTlsData != (CTlsData*)1)
    lpTlsData->Release(); //final release
  return;
}

//-----------------------------------------------------------

CTlsData::CTlsData() : CNktDvObject(), TNktLnkLstNode<CTlsData>()
{
  dwTid = ::GetCurrentThreadId();
  return;
}

CTlsData::~CTlsData()
{
  RemoveFromList(this);
  return;
}

HRESULT CTlsData::Initialize()
{
  nLockRecursion = 0;
  nCoCreateInstanceCounter = 0;
  return S_OK;
}

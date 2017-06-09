/*
 * Copyright (C) 2010-2013 Nektra S.A., Buenos Aires, Argentina.
 * All rights reserved.
 *
 **/

#ifndef _TLSDATA_H
#define _TLSDATA_H

#include "Main.h"
class CTlsData;

//-----------------------------------------------------------

HRESULT tlsInitialize();
VOID tlsFinalize();

CTlsData* tlsGet();
VOID tlsOnThreadExit();

//-----------------------------------------------------------

class CTlsData : public CNktDvObject, public TNktLnkLstNode<CTlsData>
{
public:
  CTlsData();
  virtual ~CTlsData();

  HRESULT Initialize();

  DWORD GetTid()
    {
    return dwTid;
    };

  BOOL IsRecursionLockActive()
    {
    return (nLockRecursion > 0) ? TRUE : FALSE;
    };

  class CAutoLockRecursion
  {
  public:
    CAutoLockRecursion(__in CTlsData *_lpTlsData)
      {
      NKT_ASSERT(_lpTlsData != NULL); //should be already initialized for current thread
      lpTlsData = _lpTlsData;
      (lpTlsData->nLockRecursion)++;
      return;
      };

    ~CAutoLockRecursion()
      {
      (lpTlsData->nLockRecursion)--;
      return;
      };

  private:
    CTlsData *lpTlsData;
  };

public:
  LONG nLockRecursion;
  LONG nCoCreateInstanceCounter;
  //----

private:
  DWORD dwTid;
};

//-----------------------------------------------------------

#define TLS_FUNCTIONCALLED_INIT(tlsDataContainer, __lpHookCallInfoPlugin) \
  tlsDataContainer.Attach(tlsGet());                                      \
  if (tlsDataContainer == NULL)                                           \
    return E_OUTOFMEMORY;                                                 \
  if (tlsDataContainer->IsRecursionLockActive() != FALSE)                 \
  {                                                                       \
    __lpHookCallInfoPlugin->FilterSpyMgrEvent();                          \
    return S_OK;                                                          \
  }

//-----------------------------------------------------------

#endif //_TLSDATA_H

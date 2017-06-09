#include "stdafx.h"
#include "SystemModules.h"
#include "CommonFunctions.h"

//-----------------------------------------------------------

CSystemModules cSysModules;

//-----------------------------------------------------------

static int ModuleList_Compare(__in LPVOID lpContext, __in LPWSTR* lpszStr1, __in LPWSTR* lpszStr2);

//-----------------------------------------------------------

CSystemModules::CSystemModules() : CNktMemMgrObj()
{
  SIZE_T i;

  for (i=0; i<SYSTEMMODULES_TABLES_COUNT; i++)
    NktInterlockedExchange(&(sLists[i].nMutex), 0);
  return;
}

CSystemModules::~CSystemModules()
{
  return;
}

HRESULT CSystemModules::Add(__in_z LPWSTR szModuleNameW)
{
  CNktStringW cStrTempW;
  SIZE_T nTableIdx;

  if (szModuleNameW == NULL)
    return E_POINTER;
  if (szModuleNameW[0] == 0)
    return E_INVALIDARG;
  nTableIdx = szModuleNameW[0] % SYSTEMMODULES_TABLES_COUNT;
  {
    CNktSimpleLockNonReentrant cLock(&(sLists[nTableIdx].nMutex));

    if (sLists[nTableIdx].aModulesList.BinarySearchPtr(&szModuleNameW, &ModuleList_Compare, NULL) == NULL)
    {
      if (cStrTempW.Copy(szModuleNameW) == FALSE ||
          sLists[nTableIdx].aModulesList.SortedInsert((LPWSTR)cStrTempW, &ModuleList_Compare,
                                                      NULL) == FALSE)
        return E_OUTOFMEMORY;
      cStrTempW.Detach();
    }
  }
  return S_OK;
}

HRESULT CSystemModules::Contains(__in_z LPWSTR szModuleNameW)
{
  SIZE_T nTableIdx;
  HRESULT hRes;

  if (szModuleNameW == NULL)
    return E_POINTER;
  if (szModuleNameW[0] == 0)
    return E_INVALIDARG;
  nTableIdx = szModuleNameW[0] % SYSTEMMODULES_TABLES_COUNT;
  {
    CNktSimpleLockNonReentrant cLock(&(sLists[nTableIdx].nMutex));

    hRes = (sLists[nTableIdx].aModulesList.BinarySearchPtr(&szModuleNameW, &ModuleList_Compare,
                                                           NULL) != NULL) ? S_OK : S_FALSE;
  }
  return hRes;
}

//-----------------------------------------------------------

static int ModuleList_Compare(__in LPVOID lpContext, __in LPWSTR* lpszStr1, __in LPWSTR* lpszStr2)
{
  return symbol_strcmp(*lpszStr1, *lpszStr2);
}

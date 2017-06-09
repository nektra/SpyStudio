#ifndef _SYSTEMMODULES_H
#define _SYSTEMMODULES_H

//-----------------------------------------------------------

#include "Main.h"

//-----------------------------------------------------------

#define SYSTEMMODULES_TABLES_COUNT                         7

//-----------------------------------------------------------

class CSystemModules : public CNktMemMgrObj
{
public:
  CSystemModules();
  ~CSystemModules();

  HRESULT Add(__in_z LPWSTR szModuleNameW);

  HRESULT Contains(__in_z LPWSTR szModuleNameW);

private:
  struct {
    LONG volatile nMutex;
    TNktArrayListWithFree<LPWSTR> aModulesList;
  } sLists[SYSTEMMODULES_TABLES_COUNT];
};

//-----------------------------------------------------------

extern CSystemModules cSysModules;

//-----------------------------------------------------------

#endif //_SYSTEMMODULES_H

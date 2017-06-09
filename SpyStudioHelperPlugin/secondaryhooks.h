#pragma once

enum SecondaryHookBits
{
//WARNING: Preprocessor magic. Proceed with caution.
#define SECONDARY_HOOKS_LAMBDA_N(x) SECONDARYHOOK_BIT_##x,
#define SECONDARY_HOOKS_LAMBDA_0(x) SECONDARYHOOK_BIT_##x = 0,
#include "SecondaryHookList.h"
#undef SECONDARY_HOOKS_LAMBDA_N
#undef SECONDARY_HOOKS_LAMBDA_0
};

class CSecondaryHookManager
{
public:
  CSecondaryHookManager();
  ~CSecondaryHookManager();

  BOOL InitOK();

  VOID ResetBit(__in SIZE_T nBit);
  VOID SetBit(__in SIZE_T nBit);

  BOOL CheckBit(__in SIZE_T nBit);

private:
  DWORD dwTLS;
};

extern CSecondaryHookManager global_secHookMgr;

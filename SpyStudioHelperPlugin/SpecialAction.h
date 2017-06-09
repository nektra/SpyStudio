#pragma once
#include <CIPC.h>

class SpecialAction
{
public:
  virtual ~SpecialAction(){}
  virtual void Perform(CoalescentIPC &cipc, INktHookCallInfoPlugin &hcip) = 0;
};

#pragma once

#include <Windows.h>

struct NktVariant
{
  VARIANT var;
  NktVariant();
  ~NktVariant();
  void operator=(long);
  void operator=(long long);
  void operator=(const _bstr_t &);
  void operator=(nullptr_t);
};

#include "stdafx.h"
#include "CommonFunctions.h"

typedef boost::make_unsigned<HRESULT>::type unsigned_HRESULT;

struct interval{
  unsigned_HRESULT begin;
  unsigned short size, index;
};

#include "hresult_table.inl"

const char *HRESULT_to_string(HRESULT hres)
{
  auto f = [](const interval &interval, unsigned x)
    {
	  return interval.begin < x;
    };
  return code_to_string(hres, intervals, HRESULT_strings, f);
}

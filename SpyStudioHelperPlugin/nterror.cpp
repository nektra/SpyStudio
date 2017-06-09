#include "stdafx.h"
#include "CommonFunctions.h"

typedef boost::make_unsigned<NTSTATUS>::type unsigned_NTSTATUS;

struct interval{
  unsigned_NTSTATUS begin;
  unsigned short size, index;
};

#include "ntstatus_table.inl"

const char *NTSTATUS_to_string(NTSTATUS nt)
{
  auto f = [](const interval &interval, unsigned x)
    {
	  return interval.begin < x;
    };
  return code_to_string(nt, intervals, NTSTATUS_strings, f);
}

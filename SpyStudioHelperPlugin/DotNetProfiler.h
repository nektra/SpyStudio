#pragma once

//-----------------------------------------------------------

class CDotNetProfiler
{
public:
  virtual ~CDotNetProfiler(){}

  HRESULT Initialize();
  VOID Finalize();

#define PARSE_METHOD(methodName, params, paramsForCall, paramsCount) \
  HRESULT OnICorProfilerCallbackClassFac_##methodName params;
#include "DotNetProfiler_ICorProfilerCallbackClassFacMethods.h"
#undef PARSE_METHOD

#include "GeneratedMemberDeclarations.inl"
};

extern CDotNetProfiler cDotNetProfiler;

#define USE_PROFILERINFO2

#ifndef USE_PROFILERINFO2
typedef ICorProfilerInfo profiler_info_t;
#else
typedef ICorProfilerInfo2 profiler_info_t;
#endif

extern profiler_info_t *lpDotNetProfilerInfo;

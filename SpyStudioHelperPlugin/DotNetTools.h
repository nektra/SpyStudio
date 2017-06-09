#pragma once

#include <cor.h>
#include <corprof.h>

class DotNetTools
{
public:
	static void SetDotNetProfilerInfo(ICorProfilerInfo *lpDotNetProfilerInfo);

	static _bstr_t GetClassNameFromObjectId(ObjectID objId);
	static _bstr_t GetClassNameFromClassId(ClassID classId);
  static bool GetFunctionProperties(FunctionID FunctionId, _bstr_t &className, _bstr_t &procName);
  static bool GetModuleInfo(ModuleID modId, SIZE_T &modAddress);

private:
	static ICorProfilerInfo *_lpDotNetProfilerInfo;
};
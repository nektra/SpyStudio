#pragma once

#include <boost/cstdint.hpp>
#include <boost/shared_array.hpp>
#include <vector>
#include "Buffer.h"
#include "CustomHookData.h"
#include "GeneratedDotNetCallbacks.h"

class DotNetProfilerMgr{
  typedef volatile LONG64 cookie_t;
  cookie_t next_cookie;
#if defined _M_IX86
  CNktFastMutex cookie_mutex;
#endif
  typedef dictionary_t<UINT_PTR, cookie_t> issued_cookies_t;
  dictionary_t<DWORD, boost::shared_array<issued_cookies_t> > cookies_by_thread;
  CNktFastMutex mutex;
  dictionary_t<FunctionID, std::wstring> functionids;
  dictionary_t<AppDomainID, std::wstring> appdomainids;
  dictionary_t<AssemblyID, std::wstring> assemblyids;
  dictionary_t<ClassID, std::wstring> classids;
  dictionary_t<ModuleID, std::wstring> moduleids;
  bool get_function_name_internal(std::wstring &dst, FunctionID);
  bool get_appdomain_name_internal(std::wstring &dst, AppDomainID);
  bool get_assembly_name_internal(std::wstring &dst, AssemblyID);
  bool get_class_name_internal(std::wstring &dst, ClassID);
  bool get_module_name_internal(std::wstring &dst, ModuleID);
  std::wstring type_array_to_string(ClassID *array, ULONG32 count);
public:
  DotNetProfilerMgr(): next_cookie(0){}
  cookie_t get_cookie();
  void standard_prelude(
    AcquiredBuffer &buffer,
	unsigned function_id,
	double elapsed_time,
	const char *function_name,
	unsigned event_kind,
	bool have_stack);
  void add_stack(AcquiredBuffer &buffer);
  void add_result(AcquiredBuffer &buffer, HRESULT);
  void add_result(AcquiredBuffer &buffer, const COR_PRF_JIT_CACHE &);
#define DECLARE_ADD_PARAM_OVERLOAD(type) void add_param_##type(AcquiredBuffer &buffer, const char *name, const type &)
  DECLARE_ADD_PARAM_OVERLOAD(FunctionID);
  DECLARE_ADD_PARAM_OVERLOAD(AppDomainID);
  DECLARE_ADD_PARAM_OVERLOAD(AssemblyID);
  DECLARE_ADD_PARAM_OVERLOAD(ClassID);
  DECLARE_ADD_PARAM_OVERLOAD(ObjectID);
  DECLARE_ADD_PARAM_OVERLOAD(DWORD);
  DECLARE_ADD_PARAM_OVERLOAD(COR_PRF_GC_REASON);
  DECLARE_ADD_PARAM_OVERLOAD(GCHandleID){}
  DECLARE_ADD_PARAM_OVERLOAD(BOOL);
  DECLARE_ADD_PARAM_OVERLOAD(COR_PRF_TRANSITION_REASON);
  DECLARE_ADD_PARAM_OVERLOAD(ModuleID);
  DECLARE_ADD_PARAM_OVERLOAD(LPGUID);
  DECLARE_ADD_PARAM_OVERLOAD(COR_PRF_SUSPEND_REASON);
  DECLARE_ADD_PARAM_OVERLOAD(ThreadID);
  DECLARE_ADD_PARAM_OVERLOAD(ULONG);
  DECLARE_ADD_PARAM_OVERLOAD(PWCHAR);
#undef DECLARE_ADD_PARAM_OVERLOAD
  void add_param_generations(AcquiredBuffer &buffer, const char *name, int cGenerations, BOOL *generationCollected){}
  void add_param_movedObjects(AcquiredBuffer &buffer, const char *name, ULONG cMovedObjectIDRanges, ObjectID *oldObjectIDRangeStart, ObjectID *newObjectIDRangeStart, ULONG *cObjectIDRangeLength){}
  void add_param_objectRefs(AcquiredBuffer &buffer, const char *name, ULONG cObjectRefs, ObjectID * objectRefIds){}
  void add_param_objectCounts(AcquiredBuffer &buffer, const char *name, ULONG cClassCount, ClassID * classIds, ULONG * cObjects){}
  void add_param_rootReferences(AcquiredBuffer &buffer, const char *name, ULONG cRootRefs, ObjectID * rootRefIds){}
  void add_param_rootReferences(AcquiredBuffer &buffer, const char *name, ULONG cRootRefs, ObjectID * rootRefIds, COR_PRF_GC_ROOT_KIND * rootKinds, COR_PRF_GC_ROOT_FLAGS * rootFlags, UINT_PTR * rootIds){}
  void add_param_survivingObjects(AcquiredBuffer &buffer, const char *name, ULONG cSurvivingObjectIDRanges, ObjectID * objectIDRangeStart, ULONG * cObjectIDRangeLength){}

  bool get_function_name(std::wstring &dst, FunctionID);
  bool get_appdomain_name(std::wstring &dst, AppDomainID);
  bool get_assembly_name(std::wstring &dst, AssemblyID);
  bool get_class_name(std::wstring &dst, ClassID);
  bool get_class_name_of_object(std::wstring &dst, ObjectID);
  bool get_module_name(std::wstring &dst, ModuleID);

  void NotifyThreadDetached(DWORD);
};

#define JITCachedFunctionSearchStarted_INSERT *pbUseCachedFunction = 1
#define JITInlining_INSERT *pfShouldInline = 1

#include "stdafx.h"
#include "DotNetProfiler.h"
#include "DotNetProfilerMgr.h"
#include "CIPC.h"
#include "CallEventSerializer.h"
#include "NonDeviareEventID.h"

#define DEFINE_ADD_PARAM_OVERLOAD(type) void DotNetProfilerMgr::add_param_##type(AcquiredBuffer &buffer, const char *name, const type &addend)

void DotNetProfilerMgr::NotifyThreadDetached(DWORD tid)
{
  CNktAutoFastMutex am(&mutex);
  auto it = cookies_by_thread.find(tid);
  if (it != cookies_by_thread.end())
    cookies_by_thread.erase(it);
}

void DotNetProfilerMgr::standard_prelude(
    AcquiredBuffer &buffer,
	unsigned function_id,
	double elapsed_time,
	const char *function_name,
	unsigned event_kind,
	bool have_stack)
{
  auto cookie = get_cookie();

  //Hook ID
  buffer.AddString("null");
  //Non-Deviare event ID
  buffer.AddInteger((int)NonDeviareEventID::DotNetProfiling);
  //Function ID
  buffer.AddInteger(function_id);
  // PID
  buffer.AddInteger(GetCurrentProcessId());
  // TID
  buffer.AddInteger(GetCurrentThreadId());
  // Cookie
  buffer.AddInteger(cookie);
  // Timestamp
  buffer.AddDouble(global_cipc->GlobalTimestamp());
  // Time offset
  buffer.AddDouble(global_cipc->MillisecondsSinceInitialization());
  // Elapsed time
  buffer.AddDouble(elapsed_time);
  // Function name
  buffer.AddString(function_name);
  // Event kind
  buffer.AddInteger(event_kind);
  
  if (have_stack)
    add_stack(buffer);
}

DotNetProfilerMgr::cookie_t DotNetProfilerMgr::get_cookie()
{
#if defined _M_IX86
  CNktAutoFastMutex af(&cookie_mutex);
  return next_cookie++;
#elif defined _M_X64
  return InterlockedIncrement64(&next_cookie) - 1;
#endif
}
  
struct StackFrameInfo
{
  FunctionID function;
  UINT_PTR ip;
  INktDbModulePtr module;
  StackFrameInfo(const FunctionID &function, const UINT_PTR &ip): function(function), ip(ip){}
};

HRESULT __stdcall MyStackSnapshotCallback (
    FunctionID funcId,
    UINT_PTR ip,
    COR_PRF_FRAME_INFO frameInfo,
    ULONG32 contextSize,
    BYTE context[],
    void *clientData
){
  auto ips = (std::vector<StackFrameInfo> *)clientData;
  ips->push_back(StackFrameInfo(funcId, ip));
  return S_OK;
}

class DotNetProfilerMgr_add_stack_helper
{
  DotNetProfilerMgr &mgr;
  std::vector<StackFrameInfo> &trace;
  AcquiredBuffer &buffer;
  CoalescentIPC &cipc;
public:
  DotNetProfilerMgr_add_stack_helper(DotNetProfilerMgr &mgr, std::vector<StackFrameInfo> &trace, AcquiredBuffer &buffer, CoalescentIPC &cipc):
    mgr(mgr),
    trace(trace),
    buffer(buffer),
    cipc(cipc)
  {
  }
  AcquiredBuffer &GetBuffer()
  {
    return buffer;
  }
  CoalescentIPC &GetCIPC()
  {
    return cipc;
  }
  bool FrameExists(int i)
  {
    return i >= 0 && (size_t)i < trace.size();
  }
  mword_t GetAddress(int i)
  {
    return trace[i].ip;
  }
  bool GetModulePath(std::wstring &s, int i)
  {
    auto mod = cipc.FindModule(trace[i].ip);
    if (!mod)
      return 0;
    auto name = mod->GetPath();
    s.assign((const wchar_t *)name, name.length());
    return 1;
  }
  bool GetModulePathAndBaseAddress(std::wstring &m, mword_t &ba, int i)
  {
    auto mod = cipc.FindModule(trace[i].ip);
    if (!mod)
      return 0;
    auto name = mod->GetPath();
    m.assign((const wchar_t *)name, name.length());
    ba = mod->GetBaseAddress();
    return 1;
  }
  std::wstring GetNearestSymbol(int i)
  {
    std::wstring ret;
    mgr.get_function_name(ret, trace[i].function);
    return ret;
  }
  mword_t GetIP(int i)
  {
    return trace[i].ip;
  }
  mword_t GetOffset(int i)
  {
    return 0;
  }
};

void DotNetProfilerMgr::add_stack(AcquiredBuffer &buffer)
{
  std::vector<StackFrameInfo> trace;
  trace.reserve(50);
  lpDotNetProfilerInfo->DoStackSnapshot(0, MyStackSnapshotCallback, COR_PRF_SNAPSHOT_DEFAULT, &trace, nullptr, 0);
  
  DotNetProfilerMgr_add_stack_helper helper(*this, trace, buffer, global_cipc);

  std::wstring mod_name,
    stack_trace_string;
  basic_add_stack(mod_name, stack_trace_string, helper);

  if (mod_name.size())
  {
    buffer.AddString("module");
    buffer.AddString(mod_name);
  }
  if (stack_trace_string.size())
  {
    buffer.AddString("stackstring");
    buffer.AddString(stack_trace_string);
  }
}

template <typename T, typename F>
bool perform_get_name(DotNetProfilerMgr *_this, std::wstring &dst, T id, dictionary_t<T, std::wstring> &map, F f){
  auto it = map.find(id);
  if (it != map.end())
  {
    dst = it->second;
    return 1;
  }
  bool ret = (_this->*f)(dst, id);
  if (ret)
    map[id] = dst;
  return ret;
}

bool DotNetProfilerMgr::get_function_name(std::wstring &dst, FunctionID id)
{
  return perform_get_name(this, dst, id, functionids, &DotNetProfilerMgr::get_function_name_internal);
}

bool DotNetProfilerMgr::get_appdomain_name(std::wstring &dst, AppDomainID id)
{
  return perform_get_name(this, dst, id, appdomainids, &DotNetProfilerMgr::get_appdomain_name_internal);
}

bool DotNetProfilerMgr::get_assembly_name(std::wstring &dst, AssemblyID id)
{
  return perform_get_name(this, dst, id, assemblyids, &DotNetProfilerMgr::get_assembly_name_internal);
}

bool DotNetProfilerMgr::get_class_name(std::wstring &dst, ClassID id)
{
  return perform_get_name(this, dst, id, classids, &DotNetProfilerMgr::get_class_name_internal);
}

bool DotNetProfilerMgr::get_class_name_of_object(std::wstring &dst, ObjectID id)
{
  ClassID classid;
  auto result = lpDotNetProfilerInfo->GetClassFromObject(id, &classid);
  if (FAILED(result))
    return 0;
  return get_class_name(dst, classid);
}

bool DotNetProfilerMgr::get_module_name(std::wstring &dst, ModuleID id)
{
  return perform_get_name(this, dst, id, moduleids, &DotNetProfilerMgr::get_module_name_internal);
}

std::wstring DotNetProfilerMgr::type_array_to_string(ClassID *array, ULONG32 count)
{
  std::wstring ret;
  if (count)
  {
    ret += '<';
    for (ULONG32 i = 0; ;)
    {
      auto c = array[i++];
      std::wstring class_name;
      if (!get_class_name(class_name, c))
        ret += L"?";
      else
        ret += class_name;
      if (i == count)
        break;
      ret += L", ";
    }
    ret += '>';
  }
  return ret;
}

bool DotNetProfilerMgr::get_function_name_internal(std::wstring &dst, FunctionID id){
  CComPtr<IMetaDataImport> metadata_import;
  mdToken token, token2;
  auto result = lpDotNetProfilerInfo->GetTokenAndMetaDataFromFunction(
    id,
    IID_IMetaDataImport,
    (IUnknown **)&metadata_import,
    &token
  );
  if (FAILED(result))
    return 0;

  ClassID classid;
  ModuleID moduleid;
  ClassID typeargs[256];
  ULONG32 typeargcount = 256;
  result = lpDotNetProfilerInfo->GetFunctionInfo2(id, 0, &classid, &moduleid, &token2, typeargcount, &typeargcount,typeargs);
  if (FAILED(result))
    return 0;

  mdTypeDef type_definition = mdTokenNil;
  ULONG method_name_length = 0;
  ULONG class_name_length = 0;
  DWORD attributes = 0;

  ULONG signature_length = 0;
  PCCOR_SIGNATURE signature = nullptr;

  result = metadata_import->GetMethodProps(
    token,
    &type_definition,
    nullptr,
    0,
    &method_name_length,
    &attributes,
    nullptr,
    nullptr,
    nullptr,
    nullptr
  );
  if (FAILED(result))
    return 0;

  std::wstring method_name(method_name_length - 1, 0);
  result = metadata_import->GetMethodProps(
    token,
    &type_definition,
    &method_name[0],
    method_name_length,
    &method_name_length,
    &attributes,
    &signature,
    &signature_length,
    nullptr,
    nullptr
  );
  if (FAILED(result))
    return 0;

  std::wstring class_name;
  if (!this->get_class_name(class_name, classid))
    return 0;

  dst = class_name;
  dst += L".";
  dst += method_name;
  dst += type_array_to_string(typeargs, typeargcount);

  return 1;
}

bool DotNetProfilerMgr::get_appdomain_name_internal(std::wstring &dst, AppDomainID id)
{
  ULONG name_length;
  ProcessID pid;
  auto result = lpDotNetProfilerInfo->GetAppDomainInfo(id, 0, &name_length, nullptr, &pid);
  if (FAILED(result))
    return 0;
  dst.resize(name_length);
  result = lpDotNetProfilerInfo->GetAppDomainInfo(id, name_length, &name_length, &dst[0], &pid);
  return SUCCEEDED(result);
}

bool DotNetProfilerMgr::get_assembly_name_internal(std::wstring &dst, AssemblyID id)
{
  ULONG name_length;
  AppDomainID domain;
  ModuleID module;
  auto result = lpDotNetProfilerInfo->GetAssemblyInfo(id, 0, &name_length, nullptr, &domain, &module);
  if (FAILED(result))
    return 0;
  dst.resize(name_length);
  result = lpDotNetProfilerInfo->GetAssemblyInfo(id, name_length, &name_length, &dst[0], &domain, &module);
  return SUCCEEDED(result);
}

bool DotNetProfilerMgr::get_class_name_internal(std::wstring &dst, ClassID id)
{
  ModuleID moduleId;
  mdTypeDef typeDef;
  ClassID parent;
  ClassID typeparams[256];
  ULONG32 typeparamcount = 256;

  auto hr = lpDotNetProfilerInfo->GetClassIDInfo2(
    id,
    &moduleId,
    &typeDef,
    &parent,
    typeparamcount,
    &typeparamcount,
    typeparams
  );
  if (FAILED(hr))
    return 0;

  if (!typeDef) // ::GetClassIDInfo can fail, yet not set HRESULT
    return 0;

  CComPtr<IMetaDataImport> pIMetaDataImport;
  hr = lpDotNetProfilerInfo->GetModuleMetaData(moduleId, ofRead, IID_IMetaDataImport, (IUnknown **)&pIMetaDataImport);
  if (FAILED(hr))
    return 0;

  if (!pIMetaDataImport)
    return 0;

  ULONG name_length;

  hr = pIMetaDataImport->GetTypeDefProps(typeDef, nullptr, 0, &name_length, nullptr, nullptr);
  if (FAILED(hr))
    return 0;

  dst.resize(name_length);

  hr = pIMetaDataImport->GetTypeDefProps(typeDef, &dst[0], name_length, &name_length, nullptr, nullptr);

  dst.pop_back();
  auto accent = dst.find('`');
  if (accent != dst.npos)
  {
    dst.resize(accent);
  }

  dst += type_array_to_string(typeparams, typeparamcount);

  return SUCCEEDED(hr);
}

bool DotNetProfilerMgr::get_module_name_internal(std::wstring &dst, ModuleID id)
{
  ULONG name_length;
  AssemblyID assembly;
  const BYTE *base_address;
  auto result = lpDotNetProfilerInfo->GetModuleInfo(id, &base_address, 0, &name_length, nullptr, &assembly);
  if (FAILED(result))
    return 0;
  dst.resize(name_length);
  result = lpDotNetProfilerInfo->GetModuleInfo(id, &base_address, name_length, &name_length, &dst[0], &assembly);
  return SUCCEEDED(result);
}

DEFINE_ADD_PARAM_OVERLOAD(FunctionID)
{
  buffer.AddString(name);
  std::wstring wname;
  if (this->get_function_name(wname, addend))
    buffer.AddString(wname);
  else
    buffer.AddString("<?>");
}

DEFINE_ADD_PARAM_OVERLOAD(AppDomainID)
{
  buffer.AddString(name);
  std::wstring wname;
  if (this->get_appdomain_name(wname, addend))
    buffer.AddString(wname);
  else
    buffer.AddString("<?>");
}

DEFINE_ADD_PARAM_OVERLOAD(AssemblyID)
{
  buffer.AddString(name);
  std::wstring wname;
  if (this->get_assembly_name(wname, addend))
    buffer.AddString(wname);
  else
    buffer.AddString("<?>");
}

DEFINE_ADD_PARAM_OVERLOAD(ClassID)
{
  buffer.AddString(name);
  std::wstring wname;
  if (get_class_name(wname, addend))
    buffer.AddString(wname);
  else
    buffer.AddString("<?>");
}

DEFINE_ADD_PARAM_OVERLOAD(ObjectID)
{
  buffer.AddString(name);
  std::wstring wname;
  if (get_class_name_of_object(wname, addend))
    buffer.AddDualString(L"instance of ", wname);
  else
    buffer.AddString("instance of <?>");
}

DEFINE_ADD_PARAM_OVERLOAD(DWORD)
{
  buffer.AddString(name);
  buffer.AddInteger(addend);
}

#define CASE(x) case x: s = #x; break

DEFINE_ADD_PARAM_OVERLOAD(COR_PRF_GC_REASON)
{
  buffer.AddString(name);
  const char *s = nullptr;
  switch (addend)
  {
    CASE(COR_PRF_GC_OTHER);
    CASE(COR_PRF_GC_INDUCED);
  }
  buffer.AddString(!s ? "<?>" : s);
}

DEFINE_ADD_PARAM_OVERLOAD(BOOL)
{
  buffer.AddString(name);
  buffer.AddString(addend != FALSE ? "true" : "false");
}

DEFINE_ADD_PARAM_OVERLOAD(COR_PRF_TRANSITION_REASON)
{
  buffer.AddString(name);
  const char *s = nullptr;
  switch (addend)
  {
    CASE(COR_PRF_TRANSITION_CALL);
    CASE(COR_PRF_TRANSITION_RETURN);
  }
  buffer.AddString(!s ? "<?>" : s);
}

DEFINE_ADD_PARAM_OVERLOAD(ModuleID)
{
  buffer.AddString(name);
  std::wstring wname;
  if (this->get_module_name(wname, addend))
    buffer.AddString(wname);
  else
    buffer.AddString("<?>");
}

DEFINE_ADD_PARAM_OVERLOAD(LPGUID)
{
  buffer.AddString(name);
  buffer.AddGUID(*addend);
}

#define COR_PRF_SUSPEND_OTHER                  0
#define COR_PRF_SUSPEND_FOR_GC                 1
#define COR_PRF_SUSPEND_FOR_APPDOMAIN_SHUTDOWN 2
#define COR_PRF_SUSPEND_FOR_CODE_PITCHING      3
#define COR_PRF_SUSPEND_FOR_SHUTDOWN           4
#define COR_PRF_SUSPEND_FOR_INPROC_DEBUGGER    6
#define COR_PRF_SUSPEND_FOR_GC_PREP            7
#define COR_PRF_SUSPEND_FOR_REJIT              8

DEFINE_ADD_PARAM_OVERLOAD(COR_PRF_SUSPEND_REASON)
{
  buffer.AddString(name);
  const char *s = nullptr;
  switch (addend)
  {
    CASE(COR_PRF_SUSPEND_OTHER);
    CASE(COR_PRF_SUSPEND_FOR_GC);
    CASE(COR_PRF_SUSPEND_FOR_APPDOMAIN_SHUTDOWN);
    CASE(COR_PRF_SUSPEND_FOR_CODE_PITCHING);
    CASE(COR_PRF_SUSPEND_FOR_SHUTDOWN);
    CASE(COR_PRF_SUSPEND_FOR_INPROC_DEBUGGER);
    CASE(COR_PRF_SUSPEND_FOR_GC_PREP);
    CASE(COR_PRF_SUSPEND_FOR_REJIT);
  }
  buffer.AddString(!s ? "<?>" : s);
}

DEFINE_ADD_PARAM_OVERLOAD(ThreadID)
{
  buffer.AddString(name);
  DWORD tid;
  auto result = lpDotNetProfilerInfo->GetThreadInfo(addend, &tid);
  if (FAILED(result))
    buffer.AddString("<?>");
  else
    buffer.AddInteger(result);
}

DEFINE_ADD_PARAM_OVERLOAD(ULONG)
{
  buffer.AddString(name);
  buffer.AddInteger(addend);
}

DEFINE_ADD_PARAM_OVERLOAD(PWCHAR)
{
  buffer.AddString(name);
  buffer.AddString(addend);
}

void DotNetProfilerMgr::add_result(AcquiredBuffer &buffer, HRESULT hres)
{
  buffer.AddString("result");
  auto s = HRESULT_to_string(hres);
  if (!s)
    s = "<?>";
  buffer.AddString(s);
  buffer.AddIntegerForceUnsigned(hres);
  buffer.AddInteger((int)(SUCCEEDED(hres)));
}

void DotNetProfilerMgr::add_result(AcquiredBuffer &buffer, const COR_PRF_JIT_CACHE &cache)
{
  buffer.AddString("result");
  const char *s = nullptr;
  bool success = 0;
  switch (cache)
  {
    case COR_PRF_CACHED_FUNCTION_FOUND:
      s = "COR_PRF_CACHED_FUNCTION_FOUND";
      success = 1;
      break;
    case COR_PRF_CACHED_FUNCTION_NOT_FOUND:
      s = "COR_PRF_CACHED_FUNCTION_NOT_FOUND";
      break;
    default:
      s = "<?>";
  }
  buffer.AddString(s);
  buffer.AddIntegerForceUnsigned(cache);
  buffer.AddInteger((int)success);
}

#pragma once
#include "CIPC.h"
#include "CustomHookData.h"
#include "TlsData.h"
#include "SpecialAction.h"

class AcquiredBuffer;

//#define LOG_COMINGS_AND_GOINGS

enum class AddFileNameBehavior{
  TreatAsNormalPath,
  TreatAsFileId,
};

class CallEventSerializer
{
  virtual TrinaryValue OverrideStackPlacement() const
  {
    return TV_NONE;
  }
  void AddStack(INktHookCallInfoPlugin &);
  void AddModule();
  void AddStackTraceString();
  void AddResult(INktHookCallInfoPlugin &);
  virtual void AddResultInner(INktHookCallInfoPlugin &) = 0;
  virtual bool ShouldParamsBeAdded(INktHookCallInfoPlugin &hcip) const
  {
    return 1;
  }
  void AddParams(INktHookCallInfoPlugin &);
  virtual void AddParamsInner(INktHookCallInfoPlugin &) = 0;
  virtual bool IgnoreCall(INktHookCallInfoPlugin &) const
  {
    return 0;
  }
protected:
  CoalescentIPC *cipc;
  AcquiredBuffer *abuffer;
  bool is_precall,
       synchronous;
  HookProperties properties;

  class TLSdata
  {
    TNktComPtr<CTlsData> cTlsData;
    INktHookCallInfoPlugin *hcip;
    int ok;
  public:
    TLSdata(): ok(-1) {}
    void init(INktHookCallInfoPlugin &hcip)
    {
      this->hcip = &hcip;
    }
    bool good();
    TNktComPtr<CTlsData> &get()
    {
      return cTlsData;
    }
  } tlsdata;

  unsigned eventno;
  std::wstring main_string;
  bool is_device_path;
  std::wstring mod_name;
  std::wstring stack_trace_string;

  bool AddMainString(INktParamPtr param, INktHookCallInfoPlugin &hcip, bool write_generic_parameter = 1, bool really_add = 1);
  virtual bool GetStringFromHandle(std::wstring &dst, SIZE_T handle)
  {
    return L"";
  }

  bool AddFileNameFromHANDLE(INktParamPtr param, INktHookCallInfoPlugin &hcip, bool add_generic = 1, bool really_add = 1);
  bool AddFileNameFromOBJECT_ATTRIBUTES(OBJECT_ATTRIBUTES *oattributes, AddFileNameBehavior, bool add_generic = 1, bool really_add = 1);
  bool AddFileIdFromUNICODE_STRING(UNICODE_STRING *string, bool add_generic = 1, bool really_add = 1);
  OBJECT_ATTRIBUTES *GetOBJECT_ATTRIBUTESPointer(INktParamsEnumPtr &params, long index)
  {
    return (OBJECT_ATTRIBUTES *)params->GetAt(2)->GetPointerVal();
  }
  void AddOBJECT_ATTRIBUTESFallback(OBJECT_ATTRIBUTES *oattributes);
  void AddOptionalModule(INktHookCallInfoPlugin &hcip, mword_t address);
public:
  CallEventSerializer(CoalescentIPC &cipc): cipc(&cipc), is_precall(1), synchronous(0), is_device_path(0) {}
  virtual ~CallEventSerializer() {}
  void AddCallEvent(INktHookInfo &, INktHookCallInfoPlugin &);
  void MakeSynchronous()
  {
    synchronous = 1;
  }
  void MakeAsynchronous()
  {
    synchronous = 0;
  }
  virtual SpecialAction *GetSpecialAction()
  {
    return 0;
  }
  virtual bool Success() const = 0;
};

/*
class TemplateStackTraceT
{
public:
  AcquiredBuffer &GetBuffer();
  CoalescentIPC &GetCIPC();
  bool FrameExists(int);
  mword_t GetAddress(int);
  bool GetModulePath(std::wstring &, int);
  bool GetModulePathAndBaseAddress(std::wstring &, mword_t &, int);
  std::wstring GetNearestSymbol(int);
  mword_t GetIP(int);
  mword_t GetOffset(int);
};
*/

#define MIN_STACKTRACE_DEPTH 20
#define MAX_STACKTRACE_DEPTH 50

template <typename StackTraceT>
void basic_add_stack(std::wstring &result_mod_name, std::wstring &stack_trace_string, StackTraceT &st)
{
  auto &buffer = st.GetBuffer();
  auto &cipc = st.GetCIPC();

  buffer.AddString("stack");
  const int MAX_DEPTH = MAX_STACKTRACE_DEPTH;
  const int MIN_DEPTH = MIN_STACKTRACE_DEPTH;
  long count = 0;
  bool found_non_system = 0;
  std::wstring fallback;
  long caller_index = -1,
    fallback_index = -1;

  while (count < MAX_DEPTH && st.FrameExists(count))
  {
    std::wstring mod_name;
    if (st.GetModulePath(mod_name, count))
    {
      strip_to_filename(mod_name);
      if (!fallback.size()){
        fallback = mod_name;
        fallback_index = count;
      }
      if (!cipc.IsSystemModule(mod_name.c_str())){
        if (!found_non_system){
          result_mod_name = mod_name;
          caller_index = count;
        }
        found_non_system = 1;
      }
    }
    
    count++;
    if (found_non_system && count >= MIN_DEPTH)
      break;
  }

  if (!found_non_system){
    result_mod_name = fallback;
    caller_index = fallback_index;
  }

  buffer.AddInteger(count);

  for (long i = 0; i < count; i++)
  {
    {
      std::wstring module_path;
      mword_t module_base_address;
      if (st.GetModulePathAndBaseAddress(module_path, module_base_address, i))
      {
        buffer.AddString(module_path);
        buffer.AddInteger(module_base_address);
      }
      else
        buffer.AddEmptyString(2);
    }
    auto function = st.GetNearestSymbol(i);
    buffer.AddIntegerForceUnsigned(st.GetIP(i));
    buffer.AddString(function);
    auto offset = st.GetOffset(i);
    buffer.AddIntegerForceUnsigned(offset);

    if (i != caller_index)
      continue;

    stack_trace_string = function;
    if (offset){
      stack_trace_string += L" + 0x";

      char temp[100];
      size_t final_size;
      char *alloced_buffer = int_to_string<16>(offset, HEX_DIGITS, 0, temp, 100, final_size);
      char *buffer = alloced_buffer ? alloced_buffer : temp;
      stack_trace_string.reserve(stack_trace_string.size() + final_size);
      for (size_t i = 0; i < final_size; i++)
        stack_trace_string += buffer[i];
      
      if (alloced_buffer)
        delete[] alloced_buffer;
    }
  }
}

#include "stdafx.h"
#include "exception.h"
#include "CustomHookData.h"
#include "CallEventSerializer.h"
#include "MiscHelpers.h"
#include "CommonFunctions.h"
#include "SerializerInheritors.h"
#include "secondaryhooks.h"
#include "../Deviare2/Source/Common/Tools.h"
//#define TIMER

#ifdef TIMER
#include "Timer.h"
#endif // TIMER


#ifdef LOG_COMINGS_AND_GOINGS
#include <fstream>
#include <iomanip>

static LONG counter = 0;
CNktFastMutex debug_mutex;
std::ofstream log_file;
#endif

#ifdef _DEBUG
BOOL AttachCurrentProcessToDebugger();
#endif

TimeMeasurement::TimeMeasurement(){
  total = 0;
}

#include <fstream>

TimeMeasurement::~TimeMeasurement(){
  WriteReport();
}

#define MEASURE_TIMES defined _DEBUG && !defined USE_STACKWALKER && 0

void TimeMeasurement::WriteReport(){
#if MEASURE_TIMES
  std::ofstream file("c:\\Users\\Victor\\Desktop\\time_report.txt", std::ios::trunc);
  int temp = total;
  char s[100];
  sprintf_s(s, "%d", temp);
  file <<s<<std::endl;
#endif
}

void TimeMeasurement::AddTime(double t){
  CNktAutoFastMutex am(&mutex);
  int before = int(total/1000.0);
  total += t;
  int after = int(total/1000.0);
  if (after != before)
    WriteReport();
}

class SingleCallMeasurement{
  double t0;
  TimeMeasurement *tm;
public:
  SingleCallMeasurement(TimeMeasurement *tm): tm(tm){
    t0 = tm->c();
  }
  ~SingleCallMeasurement(){
    tm->AddTime(tm->c() - t0);
  }
};

#ifdef TIMER
typedef std::map<std::wstring, double> AcumulatedTimesMap;
typedef std::map<std::wstring, int> AcumulatedCountMap;
AcumulatedTimesMap AcumulatedTimes;
AcumulatedCountMap AcumulatedCount;
int EventCount = 0;
#endif // TIMER

struct AutoCounter{
  CoalescentIPC *cipc;
  Clock c;
  double start;
  AutoCounter(CoalescentIPC *cipc): cipc(cipc)
  {
    start = c();
  }
  ~AutoCounter()
  {
    cipc->AddTime(c() - start);
  }
};

void CallEventSerializer::AddCallEvent(INktHookInfo &info, INktHookCallInfoPlugin &hcip)
{
  if (!synchronous)
    hcip.FilterSpyMgrEvent();
#ifdef TIMER
  HighDefinitionTimer timer;
#endif // TIMER
#ifdef LOG_COMINGS_AND_GOINGS
  eventno = InterlockedIncrement(&counter);
  {
    CNktAutoFastMutex m(&debug_mutex);
    int threadid = hcip.GetThreadId();
    if (!log_file.is_open())
    {
      char filename[100];
      int pid = hcip.CurrentProcess()->GetId();
      sprintf(filename, "debug%d.log", pid);
      log_file.open(filename, std::ios::trunc);
    }
    log_file <<"Entering AddCallEvent("<<std::setw(8)<<std::setfill('0')<<eventno
      <<") for function "<<(const char *)info.GetFunctionName()<<" from thread "
      <<std::hex<<std::setw(8)<<std::setfill('0')<<threadid<<std::endl;
  }
#endif
  HookId_t hook_id = info.GetId();
  HookProperties &hp = properties = cipc->GetHookProperty(hook_id);
  if (*cipc && !IgnoreCall(hcip))
  {
    tlsdata.init(hcip);
    SerializerNodeInterface sni(*cipc);
    AcquiredBuffer &buffer = sni.GetBuffer();
    {
#if MEASURE_TIMES
      SingleCallMeasurement scm(&cipc->tm);
#endif
      abuffer = &buffer;
      is_precall = hcip.IsPreCall != 0;

      buffer.AddInteger(hook_id);
      {
        INktProcessPtr proc = hcip.CurrentProcess();
        buffer.AddInteger(proc->GetId());
        buffer.AddString(proc->GetPath());
      }
      buffer.AddInteger(hcip.GetThreadId());
      long cookie = hcip.GetCookie();
      buffer.AddInteger(cookie);
      buffer.AddInteger(hcip.ChainDepth);
      buffer.AddDouble(cipc->GlobalTimestamp());
      buffer.AddDouble(cipc->MillisecondsSinceInitialization());
      buffer.AddDouble(hcip.GetElapsedTimeMs() - hcip.GetChildsElapsedTimeMs());
      buffer.AddString(info.GetFunctionName());
      if (!!hp.custom_hook)
        buffer.AddString(hp.custom_hook->get_displayName());

      unsigned call_was_virtualized = hp.has_secondary_hook && global_secHookMgr.CheckBit(hp.secondary_hook_index);

      buffer.AddInteger((unsigned)is_precall | (call_was_virtualized << 1));

      if (call_was_virtualized)
        global_secHookMgr.ResetBit(hp.secondary_hook_index);

      if (!cipc->server_settings.omit_call_stack)
      {
        TrinaryValue stack_placement = OverrideStackPlacement();
        if (stack_placement == TV_NONE)
        {
          if (is_precall || !hp.precall_included)
            AddStack(hcip);
        }
        else if (stack_placement)
          AddStack(hcip);
      }

      AddModule();
      AddStackTraceString();

      if (!is_precall || !!hp.custom_hook && hp.custom_hook->get_forceReturn())
        AddResult(hcip);

      if (ShouldParamsBeAdded(hcip))
        AddParams(hcip);

      buffer.AddEndOfMessage();

      SpecialAction *action = GetSpecialAction();
      if (action != NULL)
        action->Perform(*cipc, hcip);

    }
  }

#ifdef TIMER
  std::wstring s = std::wstring((wchar_t*)info.FunctionName.GetBSTR());

  AcumulatedTimes[s] += timer.GetEllapsed();
  if (AcumulatedCount.find(s) == AcumulatedCount.end())
    AcumulatedCount[s] = 0;
  AcumulatedCount[s]++;

  if((EventCount++ % 1000) == 0)
  {
    OutputDebugString(L"XXXXXXXXXXXXXXXXXXXXXXXXXXX\n");
    for(AcumulatedTimesMap::iterator it = AcumulatedTimes.begin(); it != AcumulatedTimes.end(); it++)
    {
      WCHAR str[4096];
      swprintf(str, L"%s\t%d\t%f\n", it->first.c_str(), AcumulatedCount[it->first], it->second);
      OutputDebugString(str);
    }
  }
#endif // TIMER

#ifdef LOG_COMINGS_AND_GOINGS
  {
    CNktAutoFastMutex m(&debug_mutex);
    log_file <<"Leaving  AddCallEvent("<<std::setw(8)<<std::setfill('0')<<eventno
      <<") for function "<<(const char *)info.GetFunctionName()<<" from thread "
      <<std::hex<<std::setw(8)<<std::setfill('0')<<threadid<<std::endl;
  }
#endif
}

class CallEventSerializer_AddStack_helper{
  AcquiredBuffer &buffer;
  CoalescentIPC &cipc;
  INktStackTracePtr trace;
public:
  CallEventSerializer_AddStack_helper(AcquiredBuffer &buffer, INktHookCallInfoPlugin &hcip, CoalescentIPC &cipc):
    buffer(buffer),
    cipc(cipc)
  {
    trace = hcip.StackTrace();
  }
  operator bool()
  {
    return !!trace;
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
    return !!GetAddress(i);
  }
  mword_t GetAddress(int i)
  {
    return trace->Address(i);
  }
  bool GetModulePath(std::wstring &s, int i)
  {
    auto mod = trace->Module(i);
    if (!mod)
      return 0;
    auto name = mod->GetName();
    s.assign((const wchar_t *)name, name.length());
    return 1;
  }
  bool GetModulePathAndBaseAddress(std::wstring &m, mword_t &ba, int i)
  {
    auto mod = trace->Module(i);
    if (!mod)
      return 0;
    auto name = mod->GetName();
    m.assign((const wchar_t *)name, name.length());
    ba = mod->GetBaseAddress();
    return 1;
  }
  std::wstring GetNearestSymbol(int i)
  {
    auto function = trace->NearestSymbol(i);
    return std::wstring((const wchar_t *)function, function.length());
  }
  mword_t GetIP(int i)
  {
    return trace->Address(i);
  }
  mword_t GetOffset(int i)
  {
    return trace->Offset(i);
  }
};

void CallEventSerializer::AddStack(INktHookCallInfoPlugin &hcip)
{
  if (!*cipc)
    return;

  CallEventSerializer_AddStack_helper helper(*abuffer, hcip, *cipc);
  if (!helper)
    return;

  basic_add_stack(mod_name, stack_trace_string, helper);
}

void CallEventSerializer::AddModule()
{
  if (!mod_name.size())
    return;
  abuffer->AddString("module");
  abuffer->AddString(mod_name);
}

void CallEventSerializer::AddStackTraceString(){
  if (!stack_trace_string.size())
    return;
  abuffer->AddString("stackstring");
  abuffer->AddString(stack_trace_string);
}

void CallEventSerializer::AddResult(INktHookCallInfoPlugin &hcip)
{
  abuffer->AddString("result");
  AddResultInner(hcip);
}

void CallEventSerializer::AddParams(INktHookCallInfoPlugin &hcip)
{
  abuffer->AddString("params");
  AddParamsInner(hcip);
}

bool CallEventSerializer::TLSdata::good()
{
  if (ok < 0)
    ok = !tls_functioncalled_init(cTlsData, *hcip);
  return ok > 0;
}

bool CallEventSerializer::AddMainString(INktParamPtr param, INktHookCallInfoPlugin &hcip, bool write_generic_parameter, bool really_add)
{
  if (!tlsdata.good())
  {
    if (write_generic_parameter)
      abuffer->AddEmptyString();
    return 0;
  }
  SIZE_T key = param->GetSizeTVal();
  if (key == 0)
  {
    if (write_generic_parameter)
      abuffer->AddEmptyString();
    return 0;
  }
  std::wstring key_name;
  if (!GetStringFromHandle(key_name, key))
  {
    if (write_generic_parameter)
      abuffer->AddEmptyString();
    return 0;
  }
  main_string = key_name;
  if (really_add)
    abuffer->AddString(key_name);
  return 1;
}

HRESULT perform_writes(CallEventSerializer &ces, INktHookInfo &hi, INktHookCallInfoPlugin &hcip)
{
  HRESULT res = S_OK;
#ifdef USE_STACKWALKER
  __try
  {
    ces.AddCallEvent(hi, hcip);
  }
  __except (exception_handler(GetExceptionInformation(), GetExceptionCode()))
  {
    abort();
  }
#else
  try
  {
    ces.AddCallEvent(hi, hcip);
  }
  catch (CIPC_Exception &e)
  {
    res = e.GetLastError();
  }
#if _DEBUG
  catch (std::exception &)
  {
    abort();
  }
  catch (_com_error &)
  {
    abort();
  }
  catch (...)
  {
    abort();
  }
#endif
#endif
  return res;
}

void UnimplementedCallHook::AddResultInner(INktHookCallInfoPlugin &hcip)
{
  if (!*cipc)
    return;
  abuffer->AddString("<<NOT IMPLEMENTED>>");
}

void UnimplementedCallHook::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  if (!*cipc)
    return;
  long count = hcip.Params()->GetCount();
  for (long i = 0; i < count; i++)
    abuffer->AddString("<<NOT IMPLEMENTED>>");
}

void ResultDWORD::AddResultInner(INktHookCallInfoPlugin &hcip)
{
  if (!*cipc)
    return;
  result = hcip.Result()->GetULongVal();
  abuffer->AddIntegerForceUnsigned(result);
}

void ResultHandle::AddResultInner(INktHookCallInfoPlugin &hcip)
{
  if (!*cipc)
    return;
  result = hcip.Result()->GetSizeTVal();
  abuffer->AddIntegerForceUnsigned(result);
}

void ResultBOOL::AddResultInner(INktHookCallInfoPlugin &hcip)
{
  if (!*cipc)
    return;
  result = hcip.Result()->GetLongVal() != 0;
  abuffer->AddIntegerForceUnsigned((int)result);
}

void ResultLPVOID::AddResultInner(INktHookCallInfoPlugin &hcip)
{
  if (!*cipc)
    return;
  result = (LPVOID)(hcip.Result()->GetSizeTVal());
  abuffer->AddIntegerForceUnsigned((SIZE_T)result);
}

void LoadDLLCES::JustAddUserArgument(UNICODE_STRING *us)
{
  if (!!us)
    abuffer->AddString(us);
  else
    abuffer->AddNULL();
}

#ifdef TIMER
typedef std::map<SIZE_T, std::wstring> ModuleAddressMap;
ModuleAddressMap ModuleAddress;
#endif // TIMER
void LoadDLLCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
//#ifdef TIMER
//  HighDefinitionTimer timer;
//#endif // TIMER


  if (!*cipc)
    return;

  INktParamsEnumPtr params = hcip.Params();
  INktParamPtr param;

  param = params->GetAt(2);
  UNICODE_STRING *us = get_pointer_or_null<UNICODE_STRING>(param);
  if (is_precall)
  {
    std::wstring temp(us->Buffer, us->Length / 2);
    JustAddUserArgument(us);
    return;
  }
  if (!Success())
  {
    JustAddUserArgument(us);
    abuffer->AddInteger(0);
  }
  else
  {    
    SIZE_T addr = params->GetAt(3)->Evaluate()->CastTo("SIZE_T")->GetSizeTVal();
    SIZE_T size = CNktDvTools::GetModuleSize((HINSTANCE)addr);

    INktProcessPtr process = hcip.CurrentProcess();

    std::wstring path;
    bool path_was_found = GetModulePathByAddress(path, process, addr, 1, 0);
    if (!path_was_found)
      JustAddUserArgument(us);
    else
      abuffer->AddString(path_to_long_path(path));
    abuffer->AddInteger(addr);
  }
}

HRESULT Device2DosForm(__inout CNktStringW &cStrPathW)
{
  WCHAR szTempW[512], szNameW[1024], szDriveW[3], *s, *p;
  SIZE_T nNameLen;
  HRESULT hRes;

  hRes = S_OK;
  s = (LPWSTR)cStrPathW;
  //translate path in device form to drive letters
  szDriveW[1] = L':';
  szDriveW[2] = szTempW[0] = 0;
  if (::GetLogicalDriveStringsW(NKT_DV_ARRAYLEN(szTempW)-1, szTempW) != FALSE)
  {
    for (p=szTempW; *p!=0; p++)
    {
      szDriveW[0] = *p;
      while (*p != 0)
        p++; //advance to next item
      //----
      if (::QueryDosDeviceW(szDriveW, szNameW, NKT_DV_ARRAYLEN(szNameW)) != FALSE)
      {
        nNameLen = wcslen(szNameW);
        if (nNameLen < NKT_DV_ARRAYLEN(szNameW) &&
            _wcsnicmp(s, szNameW, nNameLen) == 0 && s[nNameLen] == L'\\')
        {
          cStrPathW.Delete(0, nNameLen);
          if (cStrPathW.Insert(szDriveW, 0) == FALSE)
            hRes = E_OUTOFMEMORY;
          break;
        }
      }
    }
  }
  return hRes;
}

bool CallEventSerializer::AddFileNameFromHANDLE(INktParamPtr param, INktHookCallInfoPlugin &hcip, bool add_generic, bool really_add)
{
  HANDLE handle = (HANDLE)param->GetSizeTVal();
  {
    //OBJECT_INFORMATION_CLASS
    size_t size = 1<<9;
    BYTE *buffer = new (std::nothrow) BYTE[size];
    auto_array_ptr<BYTE> autop(buffer);
    HRESULT res;
    while (1)
    {
      if (!buffer)
      {
        THROW_CIPC_OUTOFMEMORY;
      }
      ULONG out_buffer_size;
      res = nktDvDynApis_NtQueryObject(handle, NKT_DV_ObjectNameInformation, buffer, (ULONG)size, &out_buffer_size);
      if (res != NKT_DV_NTSTATUS_BUFFER_TOO_SMALL && res != NKT_DV_NTSTATUS_INFO_LENGTH_MISMATCH)
        break;
      size *= 2;
      buffer = new (std::nothrow) BYTE[size];
      autop.reset(buffer);
    }
    if (FAILED(res))
    {
      if (add_generic)
        abuffer->AddEmptyString();
      return 0;
    }
    NKT_DV_OBJECT_NAME_INFORMATION *oni = (NKT_DV_OBJECT_NAME_INFORMATION *)buffer;
    CNktStringW string;
    string.CopyN(oni->Name.Buffer, oni->Name.Length/sizeof(*oni->Name.Buffer));
    res = Device2DosForm(string);
    if (FAILED(res))
    {
      if (res == E_OUTOFMEMORY)
      {
        THROW_CIPC_OUTOFMEMORY;
      }
      if (add_generic)
        abuffer->AddEmptyString();
      return 0;
    }
    main_string.assign((const wchar_t *)string, string.GetLength());
    is_device_path = begins_with(main_string, L"\\Device\\");
  }
  if (really_add)
    abuffer->AddString(canonicalize_path(main_string, get_current_directory()));
  return 1;
}

bool CallEventSerializer::AddFileIdFromUNICODE_STRING(UNICODE_STRING *string, bool add_generic, bool really_add)
{
  if (!string || string->Length != 8 && string->Length != 16)
  {
    if (add_generic)
      abuffer->AddEmptyString();
    return 0;
  }
  if (string->Length == 8)
  {
    auto file_id = *(unsigned long long *)string->Buffer;
    if (really_add)
      abuffer->AddInteger(file_id);
  }
  else
  {
    const GUID *guid = (const GUID *)string->Buffer;
    if (really_add)
      abuffer->AddGUID(*guid);
  }
  return 1;
}

bool CallEventSerializer::AddFileNameFromOBJECT_ATTRIBUTES(OBJECT_ATTRIBUTES *oattributes, AddFileNameBehavior behavior, bool add_generic, bool really_add)
{
  if (!oattributes)
  {
    if (add_generic)
      abuffer->AddEmptyString();
    return 0;
  }
  UNICODE_STRING *us = oattributes->ObjectName;
  if (behavior == AddFileNameBehavior::TreatAsFileId)
    return AddFileIdFromUNICODE_STRING(us, add_generic, really_add);
  SIZE_T file_handle = (SIZE_T)oattributes->RootDirectory;
  std::wstring filepath;
  {
    std::wstring filename;
    if (us)
    {
      USHORT length = us->Length / 2;
      USHORT actual_length = 0;

      if (is_device_path)
        actual_length = length;
      else{

#define a (actual_length < length - 1)
#define b (us->Buffer[actual_length] != ':')
#define c (us->Buffer[actual_length + 1] == '\\')
#define d (actual_length < length)
        while (a && (b || c) || d && b)
          actual_length++;
#undef a
#undef b
#undef c
#undef d
      }
      filename.assign((const wchar_t *)us->Buffer, actual_length);
    }
    if (file_handle && !GetStringFromHandle(filepath, file_handle))
    {
      if (add_generic)
        abuffer->AddEmptyString();
      return 0;
    }
    if (filename.size())
    {
      if (filepath.size())
        filepath += '\\';
      filepath += filename;
    }

    if (is_device_path)
    {
      main_string = filepath;
      if (really_add)
        abuffer->AddString(filepath);
      return 1;
    }
  }

  CNktStringW string;
  string.CopyN((const wchar_t *)filepath.c_str(), filepath.size());
  HRESULT res = Device2DosForm(string);
  if (FAILED(res))
  {
    if (res == E_OUTOFMEMORY)
    {
      THROW_CIPC_OUTOFMEMORY;
    }
    if (add_generic)
      abuffer->AddEmptyString();
    return 0;
  }

  main_string = (LPCWSTR)string;

  if (really_add)
  {
    std::wstring res = canonicalize_path<wchar_t>((LPCWSTR)string, get_current_directory());
    if (!res.size())
      res.assign(us->Buffer, us->Length / sizeof(wchar_t));
    abuffer->AddString(res);
  }
  
  return 1;
}

void CallEventSerializer::AddOBJECT_ATTRIBUTESFallback(OBJECT_ATTRIBUTES *oattributes)
{
  if (!oattributes)
    abuffer->AddString("(OBJECT_ATTRIBUTES *)0");
  else
    abuffer->AddIntegerForceUnsigned((mword_t)oattributes->RootDirectory);
}

void CallEventSerializer::AddOptionalModule(INktHookCallInfoPlugin &hcip, mword_t address)
{
  INktProcessPtr proc = hcip.CurrentProcess();
  INktModulePtr mod = proc->ModuleByAddress(address, smFindContaining);
  if (mod)
    abuffer->AddString(path_to_long_path(mod->Path));
  else if (!address)
  {
    INktModulesEnumPtr modules = proc->Modules();
    INktModulePtr first = modules->First();
    if (first)
      abuffer->AddString(first->Path);
    else
      abuffer->AddInteger(address);
  }
  else
    abuffer->AddInteger(address);
}

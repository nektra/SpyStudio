#include "stdafx.h"
#include "SerializerInheritors.h"
#include "Main.h"
#include "MiscHelpers.h"
#include "CommonFunctions.h"
#include "CustomHookData.h"
#include "exception.h"
#include "Variant.h"
#include <cassert>
#include <string>

bool CustomHookCallCES::IgnoreCall(INktHookCallInfoPlugin &hcip) const
{
  if (!properties.custom_hook)
  {
    assert(0);
    return 0;
  }

  INktStackTracePtr trace = hcip.StackTrace();
  if (!trace || !trace->Address(0))
    return 0;
  INktModulePtr mod = trace->Module(0);
  _bstr_t function = trace->NearestSymbol(0);
  if (!mod)
    return 0;

  for (size_t i = 0; ; i++)
  {
    const char *s = properties.custom_hook->get_skipCall(i);
    if (!s)
      break;
    if (!strcmp(s, function))
      return 1;
  }
  return 0;
}

#define PP_NONE abuffer->AddEmptyString()
#define PP_HEXINT abuffer->AddString("HEXINT")
#define PP_FILENAME abuffer->AddString("FILENAME")
#define PP_BOOLRES abuffer->AddString("BOOLRES")
#define PP_HRESULT abuffer->AddString("HRESULT")
#define PP_NTSTATUS abuffer->AddString("NTSTATUS")
#define PP_HKEY abuffer->AddString("HKEY")
#define PP_HMODULE abuffer->AddString("HMODULE")

void CustomHookCallCES::AddResultInner(INktHookCallInfoPlugin &hcip)
{
  if (properties.custom_hook->get_forceReturn())
  {
    // Generic return value.
    abuffer->AddInteger(0);
    PP_NTSTATUS;
    // Generic return value.
    abuffer->AddInteger(0);
    // Was success.
    abuffer->AddInteger(1);
    return;
  }
  CustomHookResultHandler handler;
  if (!properties.custom_hook || !(handler = properties.custom_hook->get_result_handler()))
  {
    assert(0);
    abuffer->AddEmptyString();
    return;
  }
  (this->*handler)(hcip);
}

#define DEFINE_ADD_HANDLER(x, post, successCondition)          \
  void CustomHookCallCES::Add##x(INktHookCallInfoPlugin &hcip) \
  {                                                            \
    long r = hcip.Result()->GetLongVal();                      \
    abuffer->AddIntegerForceUnsigned(r);                       \
    post;                                                      \
    abuffer->AddIntegerForceUnsigned(r);                       \
    success = (successCondition);                              \
    abuffer->AddInteger(success);                              \
  }
#define DEFINE_UNSIGNED_ADD_HANDLER(x, post, successCondition) \
  void CustomHookCallCES::Add##x(INktHookCallInfoPlugin &hcip) \
  {                                                            \
    unsigned long r = hcip.Result()->GetULongVal();            \
    abuffer->AddIntegerForceUnsigned(r);                       \
    post;                                                      \
    abuffer->AddIntegerForceUnsigned(r);                       \
    success = (successCondition);                              \
    abuffer->AddInteger(success);                              \
  }
//DEFINE_ADD_HANDLER(BOOL, !!r)
void CustomHookCallCES::AddBOOL(INktHookCallInfoPlugin &hcip)
{
  unsigned long res = hcip.Result()->CastTo("ULONG")->GetULongVal();
  abuffer->AddInteger(res);
  PP_BOOLRES;
  abuffer->AddInteger(res);
  success = !!res;
  abuffer->AddInteger(success);
}
DEFINE_ADD_HANDLER(INT, PP_NONE, 1)
DEFINE_UNSIGNED_ADD_HANDLER(UINT, PP_HEXINT, 1)
DEFINE_UNSIGNED_ADD_HANDLER(HEX, PP_NONE, 1)
DEFINE_UNSIGNED_ADD_HANDLER(HRESULT, PP_HRESULT, SUCCEEDED(r))
DEFINE_UNSIGNED_ADD_HANDLER(NTSTATUS, PP_NTSTATUS, NT_SUCCESS(r))

void CustomHookCallCES::AddVOID(INktHookCallInfoPlugin &hcip)
{
  abuffer->AddEmptyString();
  abuffer->AddEmptyString();
  abuffer->AddEmptyString();
  success = 1;
  abuffer->AddString("1");
}

bool CustomHookCallCES::ShouldParamsBeAdded(INktHookCallInfoPlugin &hcip) const
{
  if (!properties.custom_hook)
  {
    assert(0);
    return 1;
  }
  CustomHook &ch = *properties.custom_hook;
  return ch.ShouldAddParameters(is_precall);
}

#define DBGPRINT_TEMP DBGPRINT
#undef DBGPRINT
#define DBGPRINT

void CustomHookCallCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  if (!properties.custom_hook)
  {
    assert(0);
    return;
  }
  const std::vector<CustomHookParam *> &params = properties.custom_hook->get_params();
  INktParamsEnumPtr paramsEnum = hcip.Params();
  DBGPRINT("Processing params from %s", properties.custom_hook->get_function());
  //if (!strcmp(properties.custom_hook->get_displayName(), "QueryInformationFile"))
  //  __debugbreak();
  for (size_t i = 0; i < params.size(); i++)
  {
    int index = params[i]->get_index();
    if (index >= paramsEnum->GetCount())
      continue;
    assert(index >= 0);
    DBGPRINT("paramp = params->GetAt(%d)", index);
    ProcessParam(hcip, *params[i], paramsEnum->GetAt(index), params[i]->get_context());
  }
}

void CustomHookCallCES::AddParamContext(const char *context, const char *type)
{
  if (!context)
  {
    PP_NONE;
    return;
  }
  try
  {
    size_t size = strlen(context);
    size_t size2 = 0;
    if (!!type)
      size2 = strlen(type);
    std::string buffer;
    buffer.reserve(size + size2 + 10);
    buffer.append("CONTEXT ");
    buffer.append(context);
    if (!!type)
    {
      buffer.push_back(' ');
      buffer.append(type);
    }
    abuffer->AddString(buffer.c_str());
  }
  catch (std::bad_alloc &)
  {
    THROW_CIPC_OUTOFMEMORY;
  }
}

void CustomHookCallCES::AddUintParam(UINT value, const char *context)
{
  if (!!context)
  {
    AddParamContext(context);
    abuffer->AddInteger(value);
  }
  else
  {
    PP_HEXINT;
    abuffer->AddInteger(value);
  }
}

void CustomHookCallCES::ProcessParam(INktHookCallInfoPlugin &hcip, CustomHookParam &param, INktParamPtr paramp, const char *context)
{
  const char *halpString = param.get_helpString();
  if (!halpString)
  {
    const std::vector<CustomHookParam *> &params = param.get_params();
    if (!!param.get_type())
    {
      DBGPRINT("paramp = paramp->CastTo(%s)", param.get_type());
      paramp = paramp->CastTo(param.get_type());
    }
    if (param.get_pointer() && valid_pointer(paramp))
    {
      DBGPRINT("paramp = paramp->Evaluate()");
      //long pval = paramp->GetPointerVal();
      paramp = paramp->Evaluate();
    }

    for (size_t i = 0; i < params.size(); i++)
    {
      int index = params[i]->get_index();
      INktParamsEnumPtr fields = paramp->Fields();
      if (index >= fields->GetCount())
        continue;
      INktParamPtr ptr;
      if (index < 0)
        ptr = paramp;
      else
      {
        DBGPRINT("paramp = params->GetAt(%d)", index);
        ptr = fields->GetAt(index);
      }
      ProcessParam(hcip, *params[i], ptr, param.get_context());
    }
    return;
  }
#if _MSC_VER > 1500 && defined _DEBUG
  auto param_address = paramp->Address;
#endif
  //bool continue_parsing = is_precall && param.get_before();
#if (defined _DEBUG) && 0
  if (is_precall)
    DBGPRINT("Continue parsing because of is_precall.");
  else if (param.get_result() == TV_NONE)
    DBGPRINT("Continue parsing because of param.get_result() == TV_NONE.");
  else if (success == (param.get_result() == TV_TRUE))
    DBGPRINT("Continue parsing because of success == (param.get_result() == TV_TRUE).");
  else
    DBGPRINT("Parsing will not continue because success == %d, param.get_result() == %d", (int)success, (int)param.get_result());
#endif
  if (!context)
    context = param.get_context();
  bool continue_parsing = is_precall || param.get_result() == TV_NONE;
  bool success_matches = 1;
  if (!continue_parsing)
  {
    success_matches = param.get_result() != TV_NONE && success == (param.get_result() == TV_TRUE);
    continue_parsing = !is_precall && success_matches;
    if (!continue_parsing)
      return;
  }
  abuffer->AddString(halpString);

  DBGPRINT("casted = paramp");
  INktParamPtr casted = paramp;
  if (!!param.get_type())
  {
    DBGPRINT("casted = casted->CastTo(%s)", param.get_type());
    casted = casted->CastTo(param.get_type());
  }
  if (param.get_pointer())
  {
    if (!valid_pointer(casted))
    {
      PP_NONE;
      abuffer->AddString("(null)");
      return;
    }
    DBGPRINT("casted = casted->Evaluate()", param.get_type());
    casted = casted->Evaluate();
  }

  if (!casted)
  {
    PP_NONE;
    abuffer->AddString("(null)");
    return;
  }

  NktVariant val;
  eNktDboFundamentalType type;
  bool forced_to_null_pointer = 0;
  bool string_was_stored = 0;
  int b = !casted->GetIsAnsiString() && !casted->GetIsWideString() && casted->GetIsPointer();
  DBGPRINT("!casted->GetIsAnsiString() && !casted->GetIsWideString() && casted->GetIsPointer() = %d", b);
  if (b)
  {
    if (valid_pointer(casted))
    {
      DBGPRINT("val = casted->GetPointerVal()");
      val = casted->GetPointerVal();
    }
    else
    {
      type = ftVoid;
      forced_to_null_pointer = 1;
      val = nullptr;
    }
  }
  else
  {
    b = casted->GetFieldsCount() == 0;
    DBGPRINT("casted->GetFieldsCount() == 0 = %d", b);
    if (b)
      casted->get_Value(&val.var);
    else if (!strcmp(context, "IID"))
    {
      val = MH_ClsIdStruct2String(casted);
      string_was_stored = 1;
    }
    else
      assert(0); 
  }

  CustomHookParamHandler handler = param.get_param_handler();
  if (!!handler && (this->*handler)(val.var, casted, hcip))
    return;

#define INTEGER_CASE(x, y)          \
  case x:                           \
    AddParamContext(context, #x);   \
    abuffer->AddInteger(val.var.y); \
    return

/*
  Note: This code is commented because sometimes a valid INktParam can be of
  type ftNone, while its value (returned by GetValue()) is a string. I couldn't
  think of any other scenario where the type of INktParam should differ from
  the type of GetValue(). If you have any problems with parameters coming as
  empty strings, you might want to try reenabling this code.

  type = casted->GetBasicType();
  switch (type)
  {
    INTEGER_CASE(ftSignedByte, cVal);
    INTEGER_CASE(ftUnsignedByte, bVal);
    INTEGER_CASE(ftSignedWord, iVal);
    INTEGER_CASE(ftUnsignedWord, uiVal);
    INTEGER_CASE(ftSignedDoubleWord, intVal);
    INTEGER_CASE(ftSignedQuadWord, llVal);
    INTEGER_CASE(ftUnsignedQuadWord, ullVal);
    case ftFloat:
      AddParamContext(context, "ftFloat");
      abuffer->AddDouble(val.fltVal);
      return;
    case ftDouble:
      AddParamContext(context, "ftDouble");
      abuffer->AddDouble(val.fltVal);
      return;
    case ftUnsignedDoubleWord:
      AddUintParam(val.uintVal, context);
      return;
    case ftWideChar:
    case ftAnsiChar:
      AddParamContext(context);
      abuffer->AddString(val.bstrVal);
      return;
    case ftVoid:
    case ftNone:
      AddParamContext(context);
      abuffer->AddHexInteger((mword_t)val.byref);
      return;
    default:
      break;
  }
*/
  if (val.var.vt&VT_BYREF)
  {
    PP_HEXINT;
    abuffer->AddInteger((size_t)val.var.byref);
    return;
  }
  switch (val.var.vt)
  {
#define REAL_CASE(x, y)            \
  case x:                          \
    AddParamContext(context, #x);  \
    abuffer->AddDouble(val.var.y); \
    return
#define STRING_CASE(x, y)          \
  case x:                          \
    AddParamContext(context, #x);  \
    abuffer->AddString(val.var.y); \
    return
    INTEGER_CASE(VT_I8, llVal);
    INTEGER_CASE(VT_I4, lVal);
    INTEGER_CASE(VT_UI1, bVal);
    INTEGER_CASE(VT_I2, iVal);
    REAL_CASE(VT_R4, fltVal);
    REAL_CASE(VT_R8, dblVal);
    INTEGER_CASE(VT_BOOL, boolVal);
    INTEGER_CASE(VT_ERROR, scode);
    INTEGER_CASE(VT_CY, cyVal.int64);
    REAL_CASE(VT_DATE, date);
    STRING_CASE(VT_BSTR, bstrVal);
    case VT_UNKNOWN:
    case VT_DISPATCH:
    case VT_ARRAY:
      PP_NONE;
      abuffer->AddEmptyString();
      return;
    INTEGER_CASE(VT_I1, cVal);
    INTEGER_CASE(VT_UI2, uiVal);
    INTEGER_CASE(VT_UI4, ulVal);
    INTEGER_CASE(VT_UI8, ullVal);
    INTEGER_CASE(VT_INT, intVal);
    INTEGER_CASE(VT_UINT, uintVal);
  }
}

#undef DBGPRINT
#define DBGPRINT DBGPRINT_TEMP

bool CustomHookCallCES::HandleHWND(VARIANT &var, INktParamPtr param, INktHookCallInfoPlugin &hcip)
{
  HWND value = (HWND)var.uintVal;
  size_t buffer_size = 0;
  auto_array_ptr<wchar_t> buffer = BetterGetClassNameW(buffer_size, value);
  PP_NONE;
  abuffer->AddString(buffer.get(), buffer_size);
  return 1;
}

int find_backwards(wchar_t *string, size_t size, wchar_t what)
{
  for (int i = (int)size;;)
  {
    if (!i)
      break;
    i--;
    if (string[i] == what)
      return i;
  }
  return -1;
}

bool CustomHookCallCES::HandleHMODULE(VARIANT &var, INktParamPtr param, INktHookCallInfoPlugin &hcip)
{
  try
  {
    mword_t value = (mword_t)var.byref;
    INktProcessPtr process = hcip.CurrentProcess();
    std::wstring path;
    bool found = GetModulePathByAddress(path, process, value, 0, cipc);

    if (!found)
    {
      PP_HMODULE;
      abuffer->AddInteger(value);
    }
    else
    {
      //String manipulations follow. Edit with care!
      size_t dot = path.rfind('.');
      size_t backslash = path.rfind('\\');
      if (dot == path.npos || dot < backslash)
        path.append(L".dll");
      PP_FILENAME;
      abuffer->AddString(path);
    }
    return 1;
  }
  catch (std::bad_alloc &)
  {
    THROW_CIPC_OUTOFMEMORY;
  }
}

bool CustomHookCallCES::HandleHKEY(VARIANT &var, INktParamPtr param, INktHookCallInfoPlugin &hcip)
{
  if (!AddMainString(param, hcip, 0, 0))
    return 0;
  PP_HKEY;
  abuffer->AddString(main_string);
  return 1;
}

bool CustomHookCallCES::HandleHFILE(VARIANT &var, INktParamPtr param, INktHookCallInfoPlugin &hcip)
{
  if (!AddFileNameFromHANDLE(param, hcip, 0, 0))
    return 0;
  PP_FILENAME;
  abuffer->AddString(main_string);
  return 1;
}

bool CustomHookCallCES::HandleIID(VARIANT &var, INktParamPtr param, INktHookCallInfoPlugin &hcip)
{
  SET_CASE(abuffer, UpperCase);
  PP_NONE;
  abuffer->AddIID(var.bstrVal);
  return 1;
}

bool CustomHookCallCES::HandleCLASSNAME(VARIANT &var, INktParamPtr param, INktHookCallInfoPlugin &hcip)
{
  long valInt = param->GetIntResourceString();
  PP_NONE;
  if (valInt > 0)
    abuffer->AddHexInteger(valInt);
  else
    abuffer->AddString(param->ReadString());
  return 1;
}

bool CustomHookCallCES::HandleADDRESS(VARIANT &var, INktParamPtr param, INktHookCallInfoPlugin &hcip)
{
  mword_t value = (mword_t)var.byref;
  INktModulePtr mod = GetModuleByAddress(hcip.CurrentProcess(), value);
  if (!mod)
    return 0;
  INktExportedFunctionPtr func = mod->FunctionByAddress(value, 1);
  _bstr_t string = mod->GetName();
  mword_t address;
  if (!func)
    address = value - mod->GetBaseAddress();
  else
  {
    string += "!";
    string += func->GetName();
    address = value - func->GetAddr();
  }
  string += " + 0x";
  string += int_to_hex_bstr(address);
  //if (!strcmp((char *)string, "chrome.exe!SetCrashKeyValueImpl + 0x330E5"))
  //  __debugbreak();
  PP_NONE;
  abuffer->AddString(string);
  return 1;
}

bool CustomHookCallCES::HandleHTHREAD(VARIANT &var, INktParamPtr param, INktHookCallInfoPlugin &hcip)
{
  HANDLE thread = (HANDLE)
#if defined _M_IX86
    var.ulVal;
#elif defined _M_X64
    var.ullVal;
#endif
  if (!thread)
  {
    PP_NONE;
    abuffer->AddInteger(GetCurrentThreadId());
    return 1;
  }
  NKT_DV_THREAD_BASIC_INFORMATION tbi;
  NTSTATUS st = nktDvDynApis_NtQueryInformationThread(thread, (THREADINFOCLASS)0, &tbi, sizeof(tbi), 0);
  if (!NT_SUCCESS(st))
    return 0;
  PP_NONE;
  abuffer->AddInteger(tbi.ClientId.UniqueThread);
  return 1;
}

bool CustomHookCallCES::HandleHPROCESS(VARIANT &var, INktParamPtr param, INktHookCallInfoPlugin &hcip)
{
  HANDLE process = (HANDLE)
#if defined _M_IX86
    var.ulVal;
#elif defined _M_X64
    var.ullVal;
#endif
  if (!process)
  {
    PP_NONE;
    abuffer->AddInteger(GetCurrentProcessId());
    return 1;
  }
  APPEND_BITNESS(NKT_DV_PROCESS_BASIC_INFORMATION) pbi;
  NTSTATUS st = nktDvDynApis_NtQueryInformationProcess(process, 0, &pbi, sizeof(pbi), 0);
  if (!NT_SUCCESS(st))
    return 0;
  PP_NONE;
  abuffer->AddInteger(pbi.UniqueProcessId);
  return 1;
}

void get_internet_buffer_string(std::wstring &str, INktParamPtr p)
{
  INktParamPtr p1 = p->Field(2);
  if (valid_pointer(p1))
  {
    if (str.size())
      str.push_back('\n');
    str.append((const wchar_t *)p1->ReadString());
  }
  p1 = p->Field(5);
  if (valid_pointer(p1))
  {
    if (str.size())
      str.push_back('\n');
    p1 = p1->CastTo("LPSTR");
    str.append((const wchar_t *)p1->ReadString());
  }
}

bool CustomHookCallCES::HandleLPINTERNET_BUFFERS(VARIANT &var, INktParamPtr param, INktHookCallInfoPlugin &hcip)
{
  std::wstring str;
  do
  {
    param = param->Evaluate();
    get_internet_buffer_string(str, param);
    param = param->Field(1);
  }
  while (valid_pointer(param));
  PP_NONE;
  abuffer->AddString(str.c_str(), str.size());
  return 1;
}

bool CustomHookCallCES::HandleFILE_POBJECT_ATTRIBUTES(VARIANT &var, INktParamPtr param, INktHookCallInfoPlugin &hcip)
{
  if (!AddFileNameFromOBJECT_ATTRIBUTES((OBJECT_ATTRIBUTES *)param->GetPointerVal(), AddFileNameBehavior::TreatAsNormalPath, 0, 0))
    return 0;
  PP_FILENAME;
  abuffer->AddString(main_string);
  return 1;
}

bool CustomHookCallCES::HandlePUNICODE_STRING(VARIANT &var, INktParamPtr param, INktHookCallInfoPlugin &hcip)
{
  PP_NONE;
  abuffer->AddString(get_pointer_or_null<UNICODE_STRING>(param));
  return 1;
}

bool CustomHookCallCES::HandlePANSI_STRING(VARIANT &var, INktParamPtr param, INktHookCallInfoPlugin &hcip)
{
  PP_NONE;
  abuffer->AddString(get_pointer_or_null<ANSI_STRING>(param));
  return 1;
}

bool CustomHookCallCES::HandleNTSTATUS(VARIANT &var, INktParamPtr param, INktHookCallInfoPlugin &hcip)
{
  PP_NONE;
  abuffer->AddString(NTSTATUS_to_string(var.uintVal));
  return 1;
}

bool CustomHookCallCES::GetStringFromHandle(std::wstring &dst, SIZE_T handle){
  if (!tlsdata.good())
    return 0;
  return GetKeyNameFromHandle(dst, (HKEY)handle, tlsdata.get(), cipc->InThinAppProcess());
}


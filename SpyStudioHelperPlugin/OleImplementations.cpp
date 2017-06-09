#include "stdafx.h"
#include "SerializerInheritors.h"
#include "Main.h"
#include "MiscHelpers.h"
#include "CommonFunctions.h"
#include "exception.h"
#include "StringList.h"

void CoCreateInstanceCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  bool good = tlsdata.good();
  if (!good)
    throw CIPC_InializationException(0, "");
  INktStackTracePtr trace = hcip.StackTrace();
  long thread_id = hcip.GetThreadId();
  unsigned ccic = cipc->GetCoCreateInstanceCounter(thread_id);
  if (is_precall)
  {
    if (!ex)
      cipc->IncrementCoCreateInstanceCounter(thread_id);
    else
    {
      if (ccic > 0 && MH_CheckCallFrom(trace, L"ole32.dll!CoCreateInstance"))
      {
        abuffer->Discard();
        return;
      }
    }
  }
  else
  {
    if (ccic > 0)
    {
      if (!ex)
        cipc->DecrementCoCreateInstanceCounter(thread_id);
      else
        if (MH_CheckCallFrom(trace, L"ole32.dll!CoCreateInstance") != FALSE)
        {
          abuffer->Discard();
          return;
        }
    }
  }

  INktParamsEnumPtr params = hcip.Params();
  INktParamPtr param = params->GetAt(0);
  if (valid_pointer(param))
  {
    _bstr_t clsid = MH_ClsIdStruct2String(param->Evaluate());
    action.SetHash((BSTR)clsid, clsid.length());
    abuffer->AddString(clsid);
  }
  else
    abuffer->AddEmptyString();
}

static bool read_default_value(std::vector<wchar_t> &buffer, HKEY key)
{
  buffer.resize(buffer.capacity());
  LONG n = (LONG)buffer.size();
  while (RegQueryValue(key, 0, &buffer[0], &n) != ERROR_SUCCESS)
  {
    if (GetLastError() != ERROR_MORE_DATA)
      return 0;
    buffer.resize(n);
  }
  buffer.resize(n);
  return 1;
}

void CoCreateInstanceCES::CoCreateInstanceSpecialAction::SetHash(BSTR string, size_t length)
{
  if (!length)
    return;
  try
  {
    std::wstring guid;
    if (string[0] == '{')
    {
      string++;
      length--;
    }
    if (string[length - 1] == '}')
      length--;
  
    std::vector<wchar_t> buffer;
    buffer.resize(256);
    {
      std::wstring path;
      static const wchar_t path_portion1[] = L"CLSID\\{";
      static const wchar_t path_portion2[] = L"}\\InProcServer32";
      path.reserve(sizeof(path_portion1) + sizeof(path_portion2) + length);
      path = path_portion1;
      path.append(string, length);
      path.append(path_portion2);
    
      HKEY key;
      if (RegOpenKeyW(HKEY_CLASSES_ROOT, path.c_str(), &key) != ERROR_SUCCESS)
        return;

      struct AutoCloser
      {
        HKEY key;
        AutoCloser(HKEY key): key(key){}
        ~AutoCloser(){ RegCloseKey(key); }
      } ac(key);

      if (!read_default_value(buffer, key))
        return;
    }
    AllowHashSpecialAction::SetHash(&buffer[0], buffer.size());
  }
  catch (std::bad_alloc &)
  {
    THROW_CIPC_OUTOFMEMORY;
  }
}

void GetClassObjectCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  INktParamPtr param = hcip.Params()->GetAt(0);
  if (!valid_pointer(param))
    abuffer->AddEmptyString();
  else
  {
    param = param->Evaluate();
    abuffer->AddIID(MH_ClsIdStruct2String(param));
  }
}

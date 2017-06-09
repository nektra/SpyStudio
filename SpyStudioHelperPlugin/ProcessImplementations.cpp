#include "stdafx.h"
#include "SerializerInheritors.h"
#include "Main.h"
#include "MiscHelpers.h"
#include "CommonFunctions.h"
#include <cassert>
#include <limits>
#include <memory>
#include <string>

void CreateProcessInternalCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  INktParamsEnumPtr params = hcip.Params();
  INktParamPtr param;

  param = params->GetAt(1);
  if (valid_pointer(param))
    abuffer->AddString(param->ReadString());
  else
    abuffer->AddEmptyString();

  param = params->GetAt(2);
  if (valid_pointer(param))
    abuffer->AddString(param->ReadString());
  else
    abuffer->AddEmptyString();

  if (is_precall)
  {
    abuffer->AddEmptyString();
    return;
  }

  param = params->GetAt(10);
  if (!valid_pointer(param))
  {
    abuffer->AddInteger(0);
    return;
  }

  param = param->Evaluate();
  param = param->Fields()->GetAt(2);
  unsigned long pid = param->GetULongVal();
  if (Success() && pid > 0)
    cipc->SendThinAppCreateProcessMessage(pid);
  abuffer->AddInteger(pid);
}

void AllowHashSpecialAction::SetHash(BSTR string, size_t length)
{
  hash = (uint32_t)wcshash()(string, length);
  actually_perform = 1;
}

void AllowHashSpecialAction::SetHash(const wchar_t *string)
{
  hash = (uint32_t)wcshash()(string);
  actually_perform = 1;
}

void AllowHashSpecialAction::SetHash(const char *string)
{
  hash = (uint32_t)wcshash()(string);
  actually_perform = 1;
}

void AllowHashSpecialAction::Perform(CoalescentIPC &cipc, INktHookCallInfoPlugin &hcip)
{
  if (!actually_perform || !cipc.GetInstallerHookBit())
    return;
  if (!hcip.HookInfo->SendCustomMessage(CUSTOM_MESSAGE_ALLOW_COMMAND_LINE_HASH, hash, VARIANT_TRUE))
    cipc.ResetInstallerHookBit();
}

void OpenServiceSpecialAction::SetString(const wchar_t *string)
{
  ss.length = (DWORD)wcslen(string);
  ss.string = new wchar_t[ss.length];
  memcpy(ss.string, string, ss.length * sizeof(wchar_t));
  actually_perform = 1;
}

void OpenServiceSpecialAction::SetString(const char *string)
{
  struct zero_extend
  {
    wchar_t operator()(char c) const
    {
      return (wchar_t)(unsigned char)c;
    }
  };
  ss.length = (DWORD)strlen(string);
  ss.string = new wchar_t[ss.length];
  std::transform(string, string + ss.length, ss.string, zero_extend());
  actually_perform = 1;
}

void OpenServiceSpecialAction::Perform(CoalescentIPC &cipc, INktHookCallInfoPlugin &hcip)
{
  if (!actually_perform || !cipc.GetInstallerHookBit())
    return;
  if (!hcip.HookInfo->SendCustomMessage(CUSTOM_MESSAGE_OPEN_SERVICE_WAS_CALLED, (my_size_t)&ss, VARIANT_TRUE))
    cipc.ResetInstallerHookBit();
}

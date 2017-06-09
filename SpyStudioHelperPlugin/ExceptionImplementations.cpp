#include "stdafx.h"
#include "SerializerInheritors.h"
#include "Main.h"
#include "MiscHelpers.h"
#include "CommonFunctions.h"
#include "exception.h"
#include "StringList.h"
#include <cassert>
#include <limits>
#include <memory>
#include <sstream>

void ExceptionCES::AddAddressRepresentation(INktHookCallInfoPlugin &hcip, mword_t address)
{
  INktModulePtr mod = hcip.CurrentProcess()->Modules()->GetByAddress(address, smFindContaining);
  if (!mod)
    abuffer->AddHexInteger(address);
  else
  {
    INktExportedFunctionPtr func = mod->FunctionByAddress(address, VARIANT_TRUE);
    _bstr_t str = mod->GetName();
    if (!func)
    {
      str += " + 0x";
      char buffer[100];
      size_t size;
      char *allocd = int_to_string<16>(address, HEX_DIGITS, sizeof(address)*2, buffer, 100, size);
      if (!allocd)
        str += buffer;
      else
      {
        str += allocd;
        delete[] allocd;
      }
    }
    else
    {
      str += "!";
      str += func->GetName();
      str += " + 0x";
      char buffer[100];
      size_t size;
      mword_t maddress = address - func->GetAddr();
      char *allocd = int_to_string<16>(maddress, HEX_DIGITS, sizeof(maddress)*2, buffer, 100, size);
      if (!allocd)
        str += buffer;
      else
      {
        str += allocd;
        delete[] allocd;
      }
    }
    abuffer->AddString((const wchar_t *)str);
  }
}

void ExceptionCES::CommonHandler(INktHookCallInfoPlugin &hcip, INktParamPtr exception_record, INktParamPtr context)
{
  mword_t faulting_address = 0;
  if (!valid_pointer(exception_record))
  {
    abuffer->AddEmptyString(3);
    return;
  }

  exception_record = exception_record->Evaluate();
  INktParamsEnumPtr exception_record_fields = exception_record->Fields();
  unsigned long code = exception_record_fields->GetAt(0)->GetULongVal();
  if (code == 0x40010006) //DBG_PRINTEXCEPTION_C
  {
    //ignore OutputDebugString generated exceptions
    abuffer->Discard();
    return;
  }
  const char *code_string = NTSTATUS_to_string(code);
  if (!code_string)
    abuffer->AddHexInteger(code);
  else
    abuffer->AddString(code_string);

  unsigned long flags = exception_record_fields->GetAt(1)->GetULongVal();
  if ((flags & EXCEPTION_NONCONTINUABLE) == EXCEPTION_NONCONTINUABLE)
    abuffer->AddString("NON_CONTINUABLE");
  else
    abuffer->AddEmptyString();
      
  INktParamPtr param = exception_record_fields->GetAt(3);
  faulting_address = valid_pointer(param) ? param->GetPointerVal() : 0;
      
  AddAddressRepresentation(hcip, faulting_address);
}

void RaiseExceptionCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  if (!is_precall)
    return;

  INktParamsEnumPtr params = hcip.Params();
  CommonHandler(hcip, params->GetAt(0), params->GetAt(1));
}

void RaiseHardErrorCES::ParseDefaultException(
                             unsigned paramCount,
                             INktParamPtr p,
                             StringList &processedParams,
                             unsigned unicodeMask
                            )
{
  for (unsigned i = 0; i < paramCount; i++)
  {
    if ((unicodeMask & 1<<i) != 0)
    {
      UNICODE_STRING *us = get_pointer_or_null<UNICODE_STRING>(p->IndexedEvaluate(i));
      if (!!us)
        processedParams.add(StringList::AllocateStringNode(us));
      else
        processedParams.add(StringList::AllocateStringNode((size_t)0));
    }
    else
    {
      unsigned long value = p->IndexedEvaluate(i)->GetULongVal();
      processedParams.add(StringList::AllocateIntegerNode(value));
    }
  }
}

void RaiseHardErrorCES::ParseExceptionParams(
                          unsigned code,
                          INktParamPtr countParam,
                          INktParamPtr unicodeMaskParam,
                          INktParamPtr paramArrayParam,
                          StringList &processedParams
                         )
{
  unsigned count = countParam->GetULongVal();
  if (count)
  {
    unsigned unicode_mask = !unicodeMaskParam ? std::numeric_limits<unsigned>::max() : unicodeMaskParam->GetULongVal();
    ParseDefaultException(count, paramArrayParam, processedParams, unicode_mask);
  }
}

void RaiseHardErrorCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  if (!is_precall)
    return;
  INktParamsEnumPtr params = hcip.Params();
  INktParamPtr param = params->GetAt(0);
  unsigned long code = param->GetULongVal();
  const char *code_string = NTSTATUS_to_string(code);
  if (!!code_string)
    abuffer->AddString(code_string);
  else
    abuffer->AddEmptyString();
  abuffer->AddInteger(code);

  StringList list;
  ParseExceptionParams(code, params->GetAt(1), params->GetAt(2), params->GetAt(3), list);

  abuffer->AddInteger(list.size);
  for (StringList::Node *traversor = list.head; traversor; traversor = traversor->next)
  {
    switch (traversor->type)
    {
      case StringList::TYPE_STRING:
        {
          StringList::NodeString *casted = (StringList::NodeString *)traversor;
          abuffer->AddString(casted->str, casted->size);
        }
        break;
      case StringList::TYPE_BSTR:
        {
          StringList::NodeBStr *casted = (StringList::NodeBStr *)traversor;
          abuffer->AddString(casted->str);
        }
    }
  }
}

void UnhandledExceptionCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  if (!is_precall)
    return;

  INktParamsEnumPtr params = hcip.Params();
  INktParamPtr excepPtr = params->GetAt(0);
  if (!valid_pointer(excepPtr))
  {
    abuffer->AddEmptyString(3);
    return;
  }
  excepPtr = excepPtr->Evaluate();
  CommonHandler(hcip, excepPtr->Field(0), excepPtr->Field(1));
}

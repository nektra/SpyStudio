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
#include <string>

void InternetCES::AddWideOrAnsi(INktParamPtr param, AcquiredBuffer *abuffer)
{
  if (wide)
    abuffer->AddString(get_pointer_or_null<const wchar_t>(param));
  else
    abuffer->AddAnsiString(get_pointer_or_null<const char>(param));
}

void InternetCES::AddFlag(AcquiredBuffer *abuffer, DWORD flags, const char *unknown, flag_pair *pairs, size_t pairs_size)
{
  std::string s;
  for (size_t i = 0; i < pairs_size; i++)
  {
    auto &flag = pairs[i];
    if ((flags & flag.second) != flag.second)
      continue;
    flags &= ~flag.second;
    if (!s.size())
      s += "|";
    s += flag.first;
  }
  if (flags)
  {
    if (!s.size())
      s += "|";
    s += unknown;
    s += "_UNKNOWN";
    {
      char temp[10];
      size_t size;
      auto allocated = int_to_string<16>(flags, HEX_DIGITS, 8, temp, 10, size);
      s += "(0x";
      s.append(allocated ? allocated : temp, size);
      s += ")";
      if (allocated)
        delete[] allocated;
    }
  }
  abuffer->AddString(s.c_str());
}

void InternetCES::AddRequestHeaders(AcquiredBuffer *abuffer, INktParamPtr length_param, INktParamPtr str_param)
{
  auto headers_length = length_param->GetULongVal();
  
  bool zero_delimited = headers_length == max_value(headers_length);

  if (wide)
  {
    auto s = get_pointer_or_null<const wchar_t>(str_param);
    if (!zero_delimited)
      abuffer->AddString(s, headers_length);
    else
      abuffer->AddString(s);
  }
  else
  {
    auto s = get_pointer_or_null<const char>(str_param);
    if (!zero_delimited)
      abuffer->AddString(s, headers_length);
    else
      abuffer->AddString(s);
  }
}

void InternetSetStatusCallbackCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  abuffer->Discard();
  if (is_precall || !Success())
    return;

  INktParamsEnumPtr params = hcip.Params();
  INktParamPtr param = params->GetAt(1);

  auto old_callback = (mword_t)Result();
  auto message = CUSTOM_MESSAGE_REQUEST_INTERNET_STATUS_CALLBACK_UNHOOK;
  if (wide)
    message = (CustomMessageCodes)((int)message + 1);
  hcip.HookInfo->SendCustomMessage(message, old_callback, VARIANT_TRUE);
  auto new_callback = cast_to_unsigned(param->GetPointerVal());
  message = CUSTOM_MESSAGE_REQUEST_INTERNET_STATUS_CALLBACK_HOOK;
  if (wide)
    message = (CustomMessageCodes)((int)message + 1);
  hcip.HookInfo->SendCustomMessage(message, new_callback, VARIANT_TRUE);
}

static const char *internet_services[] =
{
  "INTERNET_SERVICE_UNKNOWN",
  "INTERNET_SERVICE_FTP",
  "INTERNET_SERVICE_GOPHER",
  "INTERNET_SERVICE_HTTP",
};

void InternetOpenUrlCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  abuffer->Discard();

  if (is_precall)
    return;

  auto params = hcip.Params();
}

void InternetConnectCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  abuffer->Discard();

  if (is_precall)
    return;

  auto params = hcip.Params();

  //server
  AddWideOrAnsi(params->GetAt(1), abuffer);

  //port
  abuffer->AddInteger(params->GetAt(2)->GetUShortVal());

  //user name
  AddWideOrAnsi(params->GetAt(3), abuffer);

  //password
  AddWideOrAnsi(params->GetAt(4), abuffer);

  //service
  {
    auto service = params->GetAt(5)->GetULongVal();
    if (service < 1 || service > 3)
      service = 0;
    abuffer->AddString(internet_services[service]);
  }

  //flags
  {
    flag_pair pair[] =
    {
      {"INTERNET_FLAG_PASSIVE", 0x08000000},
    };
    auto flags = params->GetAt(6)->GetULongVal();
    AddFlag(abuffer, flags, "INTERNET_FLAG", pair);
  }
}

static flag_pair HttpOpenRequest_flags[] =
{
  {"INTERNET_FLAG_NEED_FILE",                0x00000010},
  {"INTERNET_FLAG_PRAGMA_NOCACHE",           0x00000100},
  {"INTERNET_FLAG_NO_UI",                    0x00000200},
  {"INTERNET_FLAG_HYPERLINK",                0x00000400},
  {"INTERNET_FLAG_RESYNCHRONIZE",            0x00000800},
  {"INTERNET_FLAG_IGNORE_CERT_CN_INVALID",   0x00001000},
  {"INTERNET_FLAG_IGNORE_CERT_DATE_INVALID", 0x00002000},
  {"INTERNET_FLAG_IGNORE_REDIRECT_TO_HTTPS", 0x00004000},
  {"INTERNET_FLAG_IGNORE_REDIRECT_TO_HTTP",  0x00008000},
  {"INTERNET_FLAG_CACHE_IF_NET_FAIL",        0x00010000},
  {"INTERNET_FLAG_NO_AUTH",                  0x00040000},
  {"INTERNET_FLAG_NO_COOKIES",               0x00080000},
  {"INTERNET_FLAG_NO_AUTO_REDIRECT",         0x00200000},
  {"INTERNET_FLAG_KEEP_CONNECTION",          0x00400000},
  {"INTERNET_FLAG_SECURE",                   0x00800000},
  {"INTERNET_FLAG_NO_CACHE_WRITE",           0x04000000},
  {"INTERNET_FLAG_RELOAD",                   0x80000000},
};

void HttpOpenRequestCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  abuffer->Discard();

  if (is_precall)
    return;

  auto params = hcip.Params();

  //connection
  abuffer->AddInteger(params->GetAt(0)->GetSizeTVal());

  //flags
  AddFlag(abuffer, params->GetAt(6)->GetULongVal(), "INTERNET_FLAG", HttpOpenRequest_flags);

  //verb, object name, referrer
  for (int i = 1; i <= 4; i++)
    AddWideOrAnsi(params->GetAt(i), abuffer);

  //accept_types[0], accept_types[1], ...
  {
    auto accept_types = params->GetAt(5);
    if (wide)
    {
      for (auto array = get_pointer_or_null<wchar_t const * const>(accept_types); *array; array++)
        abuffer->AddString(*array);
    }
    else
    {
      for (auto array = get_pointer_or_null<char const * const>(accept_types); *array; array++)
        abuffer->AddAnsiString(*array);
    }
  }
}

static flag_pair HttpAddRequestHeaders_modifiers[] =
{
  {"INTERNET_REQFLAG_FROM_CACHE",           0x00000001},
  {"INTERNET_REQFLAG_ASYNC",                0x00000002},
  {"INTERNET_REQFLAG_VIA_PROXY",            0x00000004},
  {"INTERNET_REQFLAG_NO_HEADERS",           0x00000008},
  {"INTERNET_REQFLAG_PASSIVE",              0x00000010},
  {"INTERNET_REQFLAG_CACHE_WRITE_DISABLED", 0x00000040},
  {"INTERNET_REQFLAG_NET_TIMEOUT",          0x00000080},
};

void HttpAddRequestHeadersCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  abuffer->Discard();

  if (is_precall)
    return;

  auto params = hcip.Params();

  //request
  abuffer->AddInteger(params->GetAt(0)->GetSizeTVal());

  AddRequestHeaders(abuffer, params->GetAt(2), params->GetAt(1));

  //modifiers
  AddFlag(abuffer, params->GetAt(3), "INTERNET_REQFLAG", HttpAddRequestHeaders_modifiers);
}

void HttpSendRequestCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  abuffer->Discard();
  auto params = hcip.Params();

  //request
  abuffer->AddInteger(params->GetAt(0)->GetSizeTVal());

  AddRequestHeaders(abuffer, params->GetAt(2), params->GetAt(1));

  //optional data
  auto data = get_pointer_or_null<const void>(params->GetAt(3));
  auto data_length = params->GetAt(4)->GetULongVal();
  abuffer->AddBufferAsBase64String(data, data_length);
}

void HttpSendRequestExCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  abuffer->Discard();
}

void HttpEndRequestCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  abuffer->Discard();
}

void InternetReadFileCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  abuffer->Discard();
}

void InternetReadFileExCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  abuffer->Discard();
}

void InternetWriteFileCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  abuffer->Discard();
}

void InternetCloseHandleCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  abuffer->Discard();
}

void InternetStatusCallbackCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  abuffer->Discard();
}

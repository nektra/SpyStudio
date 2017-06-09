#include "stdafx.h"
#include "SerializerInheritors.h"
#include "Main.h"
#include "MiscHelpers.h"
#include "CommonFunctions.h"
#include <cassert>
#include <limits>
#include <memory>
#include <sstream>

void RegistryCES::AddKeyName(INktParamPtr param, INktHookCallInfoPlugin &hcip)
{
  AddMainString(param, hcip);
}

void RegistryCES::AddValueName(INktParamPtr param)
{
  abuffer->AddString(get_pointer_or_null<UNICODE_STRING>(param));
}

extern CNktFastMutex debug_mutex;

void OpenKeyCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  INktParamsEnumPtr params = hcip.Params();
  if (is_precall)
    abuffer->AddEmptyString();
  else
  {
    SIZE_T key;
    if (Success())
      AddKeyName(params->GetAt(0)->Evaluate(), hcip);
    else
      abuffer->AddEmptyString();
    {
      std::wstring key_name;
      INktParamPtr param = params->GetAt(2);
      OBJECT_ATTRIBUTES *oa = get_pointer_or_null<OBJECT_ATTRIBUTES>(param);
      if (!!oa)
      {
        key = SIZE_T(oa->RootDirectory);
        if (key)
          GetStringFromHandle(key_name, key);
        //get subkey
        if (oa->ObjectName)
        {
		  if(key_name.length() > 0)
            key_name.push_back('\\');
          key_name.append(oa->ObjectName->Buffer, oa->ObjectName->Length / sizeof(wchar_t));
        }
      }
      abuffer->AddString(key_name);
    }
  }
  //abuffer->AddInteger(params->GetAt(1)->GetULongVal());
}

#define KEYINFOCLASS_KeyBasicInformation                   0
#define KEYINFOCLASS_KeyNodeInformation                    1
#define KEYINFOCLASS_KeyFullInformation                    2
#define KEYINFOCLASS_KeyNameInformation                    3
#define KEYINFOCLASS_KeyCachedInformation                  4
#define KEYINFOCLASS_KeyFlagsInformation                   5
#define KEYINFOCLASS_KeyVirtualizationInformation          6
#define KEYINFOCLASS_KeyHandleTagsInformation              7

#define KEYINFOCLASS_KeyValueBasicInformation              0
#define KEYINFOCLASS_KeyValueFullInformation               1
#define KEYINFOCLASS_KeyValuePartialInformation            2
#define KEYINFOCLASS_KeyValueFullInformationAlign64        3
#define KEYINFOCLASS_KeyValuePartialInformationAlign64     4

typedef struct
{
  LARGE_INTEGER LastWriteTime;
  ULONG TitleIndex;
  ULONG NameLength;
  WCHAR Name[1];
} MY_KEY_BASIC_INFORMATION, *PMY_KEY_BASIC_INFORMATION;

typedef struct
{
  LARGE_INTEGER LastWriteTime;
  ULONG TitleIndex;
  ULONG ClassOffset;
  ULONG ClassLength;
  ULONG NameLength;
  WCHAR Name[1];
} MY_KEY_NODE_INFORMATION, *PMY_KEY_NODE_INFORMATION;

typedef struct
{
  LARGE_INTEGER LastWriteTime;
  ULONG TitleIndex;
  ULONG ClassOffset;
  ULONG ClassLength;
  ULONG SubKeys;
  ULONG MaxNameLen;
  ULONG MaxClassLen;
  ULONG Values;
  ULONG MaxValueNameLen;
  ULONG MaxValueDataLen;
  WCHAR Class[1];
} MY_KEY_FULL_INFORMATION, *PMY_KEY_FULL_INFORMATION;

typedef struct
{
  ULONG NameLength;
  WCHAR Name[1];
} MY_KEY_NAME_INFORMATION, *PMY_KEY_NAME_INFORMATION;

typedef struct
{
  LARGE_INTEGER LastWriteTime;
  ULONG TitleIndex;
  ULONG SubKeys;
  ULONG MaxNameLen;
  ULONG Values;
  ULONG MaxValueNameLen;
  ULONG MaxValueDataLen;
  ULONG NameLength;
} MY_KEY_CACHED_INFORMATION, *PMY_KEY_CACHED_INFORMATION;

typedef struct
{
  ULONG VirtualizationCandidate: 1;
  ULONG VirtualizationEnabled: 1;
  ULONG VirtualTarget: 1;
  ULONG VirtualStore: 1;
  ULONG VirtualSource: 1;
  ULONG Reserved: 27;
} MY_KEY_VIRTUALIZATION_INFORMATION, *PMY_KEY_VIRTUALIZATION_INFORMATION;

bool QueryKeyName(std::wstring &dst, HKEY key)
{
  if (key == NULL || key == INVALID_HANDLE_VALUE)
    return 0;
  ULONG size = 1 << 8;
  ULONG bytes_read;
  auto_array_ptr<char> buffer(new (std::nothrow) char[size]);
  while (1)
  {
    if (!buffer)
      return 0;

    NTSTATUS res = NktQueryKey(key, KEYINFOCLASS_KeyNameInformation, &buffer[0], size, &bytes_read);
    if (res != NtCodes::NT_STATUS_BUFFER_TOO_SMALL && res != NtCodes::NT_STATUS_INFO_LENGTH_MISMATCH && res != NtCodes::NT_STATUS_BUFFER_OVERFLOW)
      break;
    size <<= 1;
    if (size < bytes_read)
      size = bytes_read;
    buffer.reset(new (std::nothrow) char[size]);
  }
  PMY_KEY_NAME_INFORMATION pmkbi = (PMY_KEY_NAME_INFORMATION)buffer.get();
  dst.assign(pmkbi->Name, pmkbi->NameLength / 2);
  return 1;
}


//Adds 6 parameters.
void RegistryCES::AddKeyInfoData(LONG nInfoClass, LPBYTE lpData, ULONG nDataLen, INktHookCallInfoPlugin *lpHookCallInfoPlugin)
{
  bool null_data = lpData == NULL;
  //process data
  switch (nInfoClass)
  {
    case KEYINFOCLASS_KeyBasicInformation:
      if (nDataLen >= FIELD_OFFSET(MY_KEY_BASIC_INFORMATION, Name) && !null_data)
      {
        PMY_KEY_BASIC_INFORMATION lpInfo;
        CNktStringW cStrKeyNameW;

        lpInfo = (PMY_KEY_BASIC_INFORMATION)lpData;
        //get name
        if (lpInfo->Name != NULL && lpInfo->NameLength >= sizeof(WCHAR))
          abuffer->AddString(lpInfo->Name, lpInfo->NameLength / sizeof(WCHAR));
        else
          abuffer->AddEmptyString();
      }
      else
        abuffer->AddEmptyString();
      break;

    case KEYINFOCLASS_KeyNodeInformation:
      if (nDataLen >= FIELD_OFFSET(MY_KEY_NODE_INFORMATION, Name) && !null_data)
      {
        PMY_KEY_NODE_INFORMATION lpInfo;
        CNktStringW cStrKeyNameW, cStrClassNameW;

        lpInfo = (PMY_KEY_NODE_INFORMATION)lpData;
        //get class
        if (lpInfo->Name != NULL &&
            lpInfo->ClassOffset != ULONG_MAX &&
            lpInfo->ClassLength >= sizeof(WCHAR))
        {
          abuffer->AddString(
            lpInfo->Name + lpInfo->ClassOffset / sizeof(WCHAR),
            lpInfo->ClassLength / sizeof(WCHAR)
          );
        }
        else
          abuffer->AddEmptyString();
        //get name
        if (lpInfo->Name != NULL && lpInfo->NameLength >= sizeof(WCHAR))
          abuffer->AddString(lpInfo->Name, lpInfo->NameLength / sizeof(WCHAR));
        else
          abuffer->AddEmptyString();
      }
      else
        abuffer->AddEmptyString(2);
      break;

    case KEYINFOCLASS_KeyFullInformation:
      if (nDataLen >= FIELD_OFFSET(MY_KEY_FULL_INFORMATION, Class) && !null_data)
      {
        PMY_KEY_FULL_INFORMATION lpInfo;
        CNktStringW cStrKeyNameW, cStrClassNameW;

        lpInfo = (PMY_KEY_FULL_INFORMATION)lpData;
        //get class
        if (lpInfo->Class != NULL &&
            lpInfo->ClassOffset != ULONG_MAX &&
            lpInfo->ClassLength >= sizeof(WCHAR))
        {
          abuffer->AddString(
            lpInfo->Class + lpInfo->ClassOffset / sizeof(WCHAR),
            lpInfo->ClassLength / sizeof(WCHAR)
          );
        }
        else
          abuffer->AddEmptyString();
        abuffer->AddInteger(lpInfo->SubKeys);
        abuffer->AddInteger(lpInfo->Values);
      }
      else
        abuffer->AddEmptyString(3);
      break;

    case KEYINFOCLASS_KeyNameInformation:
      if (nDataLen >= FIELD_OFFSET(MY_KEY_NAME_INFORMATION, Name) && !null_data)
      {
        PMY_KEY_NAME_INFORMATION lpInfo;
        CNktStringW cStrKeyNameW;

        lpInfo = (PMY_KEY_NAME_INFORMATION)lpData;
        //get name
        if (lpInfo->Name != NULL && lpInfo->NameLength >= sizeof(WCHAR))
        {
          size_t length = lpInfo->NameLength / sizeof(WCHAR);
          const wchar_t *string = lpInfo->Name,
            *temp = string + length - 1;
          for (; temp != string; temp--)
          {
            if (*temp == '\\')
            {
              temp++;
              break;
            }
          }
          abuffer->AddString(temp, length - (temp - string));
        }
        else
          abuffer->AddEmptyString();
      }
      else
        abuffer->AddEmptyString();
      break;

    case KEYINFOCLASS_KeyCachedInformation:
      if (nDataLen >= sizeof(MY_KEY_CACHED_INFORMATION) && !null_data)
      {
        PMY_KEY_CACHED_INFORMATION lpInfo;

        lpInfo = (PMY_KEY_CACHED_INFORMATION)lpData;
        //subkeys count
        abuffer->AddInteger(lpInfo->SubKeys);
        //values count
        abuffer->AddInteger(lpInfo->Values);
        //name length
        abuffer->AddInteger(lpInfo->NameLength);
      }
      else
        abuffer->AddEmptyString(3);
      break;

    case KEYINFOCLASS_KeyFlagsInformation:
      //DBGPRINT("KEYINFOCLASS_KeyCachedInformation not supported");
      break;

    case KEYINFOCLASS_KeyVirtualizationInformation:
      if (nDataLen >= sizeof(MY_KEY_VIRTUALIZATION_INFORMATION) && !null_data)
      {
        MY_KEY_VIRTUALIZATION_INFORMATION *lpInfo = (MY_KEY_VIRTUALIZATION_INFORMATION *)lpData;

        abuffer->AddInteger(lpInfo->VirtualizationCandidate);
        abuffer->AddInteger(lpInfo->VirtualizationEnabled);
        abuffer->AddInteger(lpInfo->VirtualTarget);
        abuffer->AddInteger(lpInfo->VirtualStore);
        abuffer->AddInteger(lpInfo->VirtualSource);
      }
      else
        abuffer->AddEmptyString(5);
      break;

    case KEYINFOCLASS_KeyHandleTagsInformation:
      //DBGPRINT("KEYINFOCLASS_KeyHandleTagsInformation not supported");
      break;

    default:
      DBGPRINT("Unknown KEY_INFORMATION_CLASS");
      break;
  }
  return;
}

void QueryKeyCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  INktParamsEnumPtr params = hcip.Params();
  
  AddKeyName(params->GetAt(0), hcip);

  long info_class = params->GetAt(1)->GetLongVal();
  abuffer->AddInteger(info_class);

  BYTE *data = get_pointer_or_null<BYTE>(params->GetAt(2));
  ULONG *size = get_pointer_or_null<ULONG>(params->GetAt(4));
  if (!Success())
  {
    AddKeyInfoData(info_class, 0, 0, &hcip);
    return;
  }
  AddKeyInfoData(info_class, data, *size, &hcip);
}

void RegistryCES::AddValueInfoValues(LONG type, const BYTE *data, ULONG size, ULONG offset)
{
  const wchar_t *type_string;
  bool invalid_data = 0;
  if (!!data)
  {
    switch (type)
    {
      case REG_NONE:
        abuffer->AddEmptyString();
        break;
      case REG_BINARY:
        {
          type_string = L"REG_BINARY";
          data += offset;
          if (!size)
            abuffer->AddEmptyString();
          else
          {
            char *buffer = new (std::nothrow) char[size * 3 + 1];
            if (!buffer)
            {
              THROW_CIPC_OUTOFMEMORY;
            }
            // We don't want to leak any memory on account of an exception.
            std::auto_ptr<char> autoptr(buffer);
            for (unsigned i = 0; i < size; i++)
            {
              int byte = data[i];
              sprintf_s(buffer, 4, "%02X ", byte);
              buffer += 3;
            }
            buffer[-1] = 0;
            abuffer->AddString(autoptr.get(), size * 3 - 1);
          }
        }
        break;
      case REG_MULTI_SZ:
        type_string = L"REG_MULTI_SZ";
        {
          if (size % sizeof(wchar_t))
          {
            invalid_data = 1;
            break;
          }
          data += offset;
          wchar_t *wstr = (wchar_t *)data;
          size_t true_size = 0;
          size /= sizeof(wchar_t);
          while ((true_size < size && wstr[true_size]) || (true_size + 1 < size && wstr[true_size + 1]))
            true_size++;
          abuffer->AddString(wstr, size);
        }
        break;
      case REG_SZ:
      case REG_EXPAND_SZ:
        type_string = (type == REG_SZ) ? L"REG_SZ" : L"REG_EXPAND_SZ";
        {
          if (size % sizeof(wchar_t))
          {
            invalid_data = 1;
            break;
          }
          data += offset;
          abuffer->AddString((wchar_t *)data, size/sizeof(wchar_t));
        }
        break;
      case REG_DWORD:
        type_string = L"REG_DWORD";
        if (size < sizeof(DWORD))
        {
          invalid_data = 1;
          break;
        }
        data += offset;
        {
          char buffer[100];
          DWORD integer = *(DWORD *)data;
          sprintf_s(buffer, "0x%08lX (%lu)", integer, integer);
          abuffer->AddString(buffer);
        }
        break;
      case REG_DWORD_BIG_ENDIAN:
        type_string = L"REG_DWORD_BIG_ENDIAN";
        if (size < sizeof(unsigned long))
        {
          invalid_data = 1;
          break;
        }
        data += offset;
        {
          char buffer[100];
          unsigned long integer = _byteswap_ulong(*(unsigned long *)data);
          sprintf_s(buffer, "0x%08lX (%lu)", integer, integer);
          abuffer->AddString(buffer);
        }
        break;
      case REG_QWORD:
        type_string = L"REG_QWORD";
        if (size < sizeof(unsigned long long))
        {
          invalid_data = 1;
          break;
        }
        data += offset;
        {
          char buffer[100];
          unsigned long long integer = *(unsigned long long *)data;
          sprintf_s(buffer, "0x%016llX (%llu)", integer, integer);
          abuffer->AddString(buffer);
        }
        break;
      default:
        abuffer->AddEmptyString();
        break;
    }
  }
  if (!data || invalid_data)
    abuffer->AddNULL();
  abuffer->AddIntegerForceUnsigned(type);
}

void RegistryCES::AddName(const wchar_t *name, size_t n)
{
  if (name != NULL && n >= sizeof(wchar_t))
  {
    //while (n && (name[n - 1] == '\\' || name[n - 1] == 0))
    //  n--;
    abuffer->AddString(name, n);
  }
  else
    abuffer->AddEmptyString();
}

//Adds 3 parameters
void RegistryCES::AddValueInfoData(LONG nInfoClass, LPBYTE lpData, ULONG nDataLen, INktHookCallInfoPlugin &hcip)
{
  typedef struct {
    ULONG TitleIndex;
    ULONG Type;
    ULONG NameLength;
    WCHAR Name[1];
  } MY_KEY_VALUE_BASIC_INFORMATION, *PMY_KEY_VALUE_BASIC_INFORMATION;

  typedef struct {
    ULONG TitleIndex;
    ULONG Type;
    ULONG DataLength;
    UCHAR Data[1];
  } MY_KEY_VALUE_PARTIAL_INFORMATION, *PMY_KEY_VALUE_PARTIAL_INFORMATION;

  typedef struct {
    ULONG TitleIndex;
    ULONG Type;
    ULONG DataOffset;
    ULONG DataLength;
    ULONG NameLength;
    WCHAR Name[1];
  } MY_KEY_VALUE_FULL_INFORMATION, *PMY_KEY_VALUE_FULL_INFORMATION;

  bool written = 0;

  //process value
  if (lpData != NULL)
  {
    switch (nInfoClass)
    {
      case KEYINFOCLASS_KeyValueBasicInformation:
        if (nDataLen >= FIELD_OFFSET(MY_KEY_VALUE_BASIC_INFORMATION, Name))
        {
          PMY_KEY_VALUE_BASIC_INFORMATION lpInfo;
          CNktStringW cStrKeyValueNameW;

          lpInfo = (PMY_KEY_VALUE_BASIC_INFORMATION)lpData;
          //get data
          AddValueInfoValues(lpInfo->Type, 0, 0, 0);
          //get name
          AddName(lpInfo->Name, lpInfo->NameLength/sizeof(wchar_t));
          written = 1;
        }
        break;

      case KEYINFOCLASS_KeyValuePartialInformation:
        if (nDataLen >= FIELD_OFFSET(MY_KEY_VALUE_PARTIAL_INFORMATION, Data))
        {
          PMY_KEY_VALUE_PARTIAL_INFORMATION lpInfo;

          lpInfo = (PMY_KEY_VALUE_PARTIAL_INFORMATION)lpData;
          //get data
          AddValueInfoValues(lpInfo->Type, lpInfo->Data, lpInfo->DataLength, 0);
          abuffer->AddEmptyString();
          written = 1;
        }
        break;

      case KEYINFOCLASS_KeyValueFullInformation:
        if (nDataLen >= FIELD_OFFSET(MY_KEY_VALUE_FULL_INFORMATION, Name))
        {
          PMY_KEY_VALUE_FULL_INFORMATION lpInfo;
          CNktStringW cStrKeyValueNameW;

          lpInfo = (PMY_KEY_VALUE_FULL_INFORMATION)lpData;
          AddValueInfoValues(lpInfo->Type, lpData, lpInfo->DataLength, lpInfo->DataOffset);
          //get name
          AddName(lpInfo->Name, lpInfo->NameLength/sizeof(wchar_t));
          written = 1;
        }
        break;
    }
  }

  if (!written)
    abuffer->AddEmptyString(3);
  return;
}

bool RegistryCES::GetStringFromHandle(std::wstring &dst, SIZE_T handle){

  if (!tlsdata.good())
    return 0;
  return GetKeyNameFromHandle(dst, (HKEY)handle, tlsdata.get(), cipc->InThinAppProcess());
}

void QueryValueCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  INktParamsEnumPtr params = hcip.Params();

  AddKeyName(params->GetAt(0), hcip);
  AddValueName(params->GetAt(1));
  long info_class = params->GetAt(2)->GetLongVal();
  if (is_precall || !Success())
  {
	abuffer->AddNULL(3);
    //abuffer->AddEmptyString(3);
  }
  else
  {
    BYTE *data = get_pointer_or_null<BYTE>(params->GetAt(3));
    ULONG data_length = params->GetAt(4)->GetULongVal();
    AddValueInfoData(info_class, data, data_length, hcip);
  }
}

void QueryMultipleValuesCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  typedef struct {
    PUNICODE_STRING ValueName;
    ULONG DataLength;
    ULONG DataOffset;
    ULONG Type;
  } MY_KEY_VALUE_ENTRY, *PMY_KEY_VALUE_ENTRY;

  if (is_precall)
  {
    abuffer->AddEmptyString();
    abuffer->AddInteger(0);
    return;
  }

  INktParamsEnumPtr params = hcip.Params();
  unsigned entry_count = params->GetAt(2)->GetULongVal();

  AddKeyName(params->GetAt(0), hcip);

  MY_KEY_VALUE_ENTRY *entries = get_pointer_or_null<MY_KEY_VALUE_ENTRY>(params->GetAt(1));
  BYTE *data = 0;
  if (Success())
    data = get_pointer_or_null<BYTE>(params->GetAt(3));
  if (!entries)
  {
    abuffer->AddInteger(0);
    return;
  }

  abuffer->AddInteger(entry_count);

  for (unsigned i = 0; i < entry_count; i++)
  {
    UNICODE_STRING *value_name = entries[i].ValueName;
    abuffer->AddString(value_name);

    AddValueInfoValues(entries[i].Type, data, entries[i].DataLength, entries[i].DataOffset);
  }
}

void SetValueCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  INktParamsEnumPtr params = hcip.Params();


  if (is_precall)
  {
    abuffer->AddEmptyString(5);
    return;
  }

  AddKeyName(params->GetAt(0), hcip);
  AddValueName(params->GetAt(1));

  unsigned long type = params->GetAt(3)->GetULongVal();
  BYTE *data = get_pointer_or_null<BYTE>(params->GetAt(4));
  unsigned long data_size = params->GetAt(5)->GetULongVal();
  AddValueInfoValues(type, data, data_size, 0);
}

void DeleteKeyCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  INktParamsEnumPtr params = hcip.Params();

  if (!is_precall)
  {
    abuffer->AddEmptyString();
    return;
  }

  AddKeyName(params->GetAt(0), hcip);
}

void DeleteValueCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  INktParamsEnumPtr params = hcip.Params();


  if (is_precall)
  {
    abuffer->AddEmptyString(2);
    return;
  }

  AddKeyName(params->GetAt(0), hcip);
  AddValueName(params->GetAt(1));
}

void EnumerateKeyCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  INktParamsEnumPtr params = hcip.Params();

  if (is_precall)
  {
    abuffer->AddEmptyString(3);
    return;
  }

  AddKeyName(params->GetAt(0), hcip);

  unsigned long index = params->GetAt(1)->GetULongVal();
  abuffer->AddInteger(index);
  long info_class = params->GetAt(2)->GetLongVal();
  abuffer->AddInteger(info_class);

  if (!Success())
    return;
  BYTE *data = get_pointer_or_null<BYTE>(params->GetAt(3));
  unsigned long size = params->GetAt(4)->GetULongVal();
  AddKeyInfoData(info_class, data, size, &hcip);
}

void EnumerateValueKeyCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  INktParamsEnumPtr params = hcip.Params();

  if (is_precall)
  {
    abuffer->AddEmptyString(6);
    return;
  }

  AddKeyName(params->GetAt(0), hcip);

  unsigned long index = params->GetAt(1)->GetULongVal();
  abuffer->AddInteger(index);

  long info_class = params->GetAt(2)->GetLongVal();
  abuffer->AddInteger(info_class);

  if (!Success())
  {
    abuffer->AddNULL(3);
    return;
  }
  BYTE *data = get_pointer_or_null<BYTE>(params->GetAt(3));
  unsigned long size = params->GetAt(4)->GetULongVal();
  AddValueInfoData(info_class, data, size, hcip);
}

void RenameKeyCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  INktParamsEnumPtr params = hcip.Params();

  AddKeyName(params->GetAt(0), hcip);

  INktParamPtr param = params->GetAt(1);
  abuffer->AddString(get_pointer_or_null<UNICODE_STRING>(param));
}

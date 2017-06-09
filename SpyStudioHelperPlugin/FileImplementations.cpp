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

bool FileCES::GetStringFromHandle(std::wstring &dst, SIZE_T handle){

  if (!tlsdata.good())
    return 0;
  return GetFileNameFromHandle(dst, (HANDLE)handle, tlsdata.get());
}

#define FILE_DIRECTORY_INFORMATION_CLASS     1
#define FILE_FULL_DIR_INFORMATION_CLASS      2
#define FILE_BOTH_DIR_INFORMATION_CLASS      3
#define FILE_NAMES_INFORMATION_CLASS         12
#define FILE_OBJECT_ID_INFORMATION_CLASS     29
#define FILE_REPARSE_POINT_INFORMATION_CLASS 33
#define FILE_ID_BOTH_DIR_INFORMATION_CLASS   37
#define FILE_ID_FULL_DIR_INFORMATION_CLASS   38

struct FILE_DIRECTORY_INFORMATION
{
  ULONG         NextEntryOffset;
  ULONG         FileIndex;
  LARGE_INTEGER CreationTime;
  LARGE_INTEGER LastAccessTime;
  LARGE_INTEGER LastWriteTime;
  LARGE_INTEGER ChangeTime;
  LARGE_INTEGER EndOfFile;
  LARGE_INTEGER AllocationSize;
  ULONG         FileAttributes;
  ULONG         FileNameLength;
  WCHAR         FileName[1];
};

struct FILE_FULL_DIR_INFORMATION
{
  ULONG         NextEntryOffset;
  ULONG         FileIndex;
  LARGE_INTEGER CreationTime;
  LARGE_INTEGER LastAccessTime;
  LARGE_INTEGER LastWriteTime;
  LARGE_INTEGER ChangeTime;
  LARGE_INTEGER EndOfFile;
  LARGE_INTEGER AllocationSize;
  ULONG         FileAttributes;
  ULONG         FileNameLength;
  ULONG         EaSize;
  WCHAR         FileName[1];
};

struct FILE_BOTH_DIR_INFORMATION
{
  ULONG         NextEntryOffset;
  ULONG         FileIndex;
  LARGE_INTEGER CreationTime;
  LARGE_INTEGER LastAccessTime;
  LARGE_INTEGER LastWriteTime;
  LARGE_INTEGER ChangeTime;
  LARGE_INTEGER EndOfFile;
  LARGE_INTEGER AllocationSize;
  ULONG         FileAttributes;
  ULONG         FileNameLength;
  ULONG         EaSize;
  CCHAR         ShortNameLength;
  WCHAR         ShortName[12];
  WCHAR         FileName[1];
};

struct FILE_NAMES_INFORMATION
{
  ULONG NextEntryOffset;
  ULONG FileIndex;
  ULONG FileNameLength;
  WCHAR FileName[1];
};

struct FILE_ID_BOTH_DIR_INFORMATION
{
  ULONG         NextEntryOffset;
  ULONG         FileIndex;
  LARGE_INTEGER CreationTime;
  LARGE_INTEGER LastAccessTime;
  LARGE_INTEGER LastWriteTime;
  LARGE_INTEGER ChangeTime;
  LARGE_INTEGER EndOfFile;
  LARGE_INTEGER AllocationSize;
  ULONG         FileAttributes;
  ULONG         FileNameLength;
  ULONG         EaSize;
  CCHAR         ShortNameLength;
  WCHAR         ShortName[12];
  LARGE_INTEGER FileId;
  WCHAR         FileName[1];
};

struct FILE_ID_FULL_DIR_INFORMATION {
  ULONG         NextEntryOffset;
  ULONG         FileIndex;
  LARGE_INTEGER CreationTime;
  LARGE_INTEGER LastAccessTime;
  LARGE_INTEGER LastWriteTime;
  LARGE_INTEGER ChangeTime;
  LARGE_INTEGER EndOfFile;
  LARGE_INTEGER AllocationSize;
  ULONG         FileAttributes;
  ULONG         FileNameLength;
  ULONG         EaSize;
  LARGE_INTEGER FileId;
  WCHAR         FileName[1];
};

void FileCES::GetFileInfo(unsigned long file_info_class, INktParamPtr param, StringList &list)
{
  if (file_info_class < FILE_DIRECTORY_INFORMATION_CLASS || file_info_class > FILE_ID_FULL_DIR_INFORMATION_CLASS)
    return;
  void *pointer = get_pointer_or_null<void>(param);
  for (;;)
  {
    if (!pointer)
      break;
    ULONG next_offset = 0;
    switch (file_info_class)
    {
#define STANDARD_CASE(type)                                                                             \
  case type##_CLASS:                                                                                    \
    {                                                                                                   \
      type *info = (type *)pointer;                                                                     \
      assert(!!info);                                                                                   \
      next_offset = info->NextEntryOffset;                                                              \
      list.add(StringList::AllocateStringNode(info->FileNameLength / sizeof(wchar_t), info->FileName)); \
      list.add(StringList::AllocateIntegerNode(info->FileAttributes));                                  \
    }                                                                                                   \
    break
      STANDARD_CASE(FILE_BOTH_DIR_INFORMATION);
      STANDARD_CASE(FILE_DIRECTORY_INFORMATION);
      STANDARD_CASE(FILE_FULL_DIR_INFORMATION);
      STANDARD_CASE(FILE_ID_BOTH_DIR_INFORMATION);
      STANDARD_CASE(FILE_ID_FULL_DIR_INFORMATION);
      case FILE_NAMES_INFORMATION_CLASS:
        {
          FILE_NAMES_INFORMATION *info = (FILE_NAMES_INFORMATION *)pointer;
          list.add(StringList::AllocateStringNode(info->FileNameLength / sizeof(wchar_t), info->FileName));
          list.add(StringList::AllocateStringNode((size_t)0));
        }
        break;
      default:
        list.add(StringList::AllocateStringNode((size_t)0));
        list.add(StringList::AllocateStringNode((size_t)0));
    }
    if (!next_offset)
      break;
    pointer = (BYTE *)pointer + next_offset;
  }
  return;
}

void FileCES::StandardAddFileNameProcedure(INktHookCallInfoPlugin &hcip, INktParamPtr param, OBJECT_ATTRIBUTES *oattr, AddFileNameBehavior behavior)
{
  if (Success())
    AddFileNameFromHANDLE(param, hcip, 1);
  else
    abuffer->AddEmptyString();
  if (!AddFileNameFromOBJECT_ATTRIBUTES(oattr, behavior, 0))
    AddOBJECT_ATTRIBUTESFallback(oattr);
}

void CreateFileCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  INktParamsEnumPtr params = hcip.Params();

  unsigned long options = params->GetAt(8)->GetULongVal();
  bool is_fileid = (options & FILE_OPEN_BY_FILE_ID) == FILE_OPEN_BY_FILE_ID;
  AddFileNameBehavior behavior = is_fileid ? AddFileNameBehavior::TreatAsFileId : AddFileNameBehavior::TreatAsNormalPath;

  abuffer->AddInteger(is_fileid);

  OBJECT_ATTRIBUTES *oattributes = GetOBJECT_ATTRIBUTESPointer(params, 2);
  if (is_precall)
  {
    if (!AddFileNameFromOBJECT_ATTRIBUTES(oattributes, behavior, 0))
      AddOBJECT_ATTRIBUTESFallback(oattributes);
    return;
  }


  StandardAddFileNameProcedure(hcip, params->GetAt(0), oattributes, behavior);

  unsigned long access_mask = params->GetAt(1)->GetULongVal();
  unsigned long attributes = params->GetAt(5)->GetULongVal();
  unsigned long share_mask = params->GetAt(6)->GetULongVal();
  unsigned long disposition = params->GetAt(7)->GetULongVal();

  abuffer->AddInteger(access_mask);
  abuffer->AddInteger(attributes);
  abuffer->AddInteger(share_mask);
  abuffer->AddInteger(options);
  abuffer->AddInteger(disposition);
}

void OpenFileCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  INktParamsEnumPtr params = hcip.Params();

  unsigned long options = params->GetAt(5)->GetULongVal();
  bool is_fileid = (options & FILE_OPEN_BY_FILE_ID) == FILE_OPEN_BY_FILE_ID;
  AddFileNameBehavior behavior = is_fileid ? AddFileNameBehavior::TreatAsFileId : AddFileNameBehavior::TreatAsNormalPath;

  OBJECT_ATTRIBUTES *oattributes = GetOBJECT_ATTRIBUTESPointer(params, 2);
  if (is_precall)
  {
    if (!AddFileNameFromOBJECT_ATTRIBUTES(oattributes, behavior, 0))
      AddOBJECT_ATTRIBUTESFallback(oattributes);
    return;
  }

  StandardAddFileNameProcedure(hcip, params->GetAt(0), oattributes, behavior);

  unsigned long access_mask = params->GetAt(1)->GetULongVal();
  unsigned long share_mask = params->GetAt(4)->GetULongVal();

  abuffer->AddInteger(access_mask);
  abuffer->AddInteger(share_mask);
  abuffer->AddInteger(options);
}

void DeleteFileCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  if (is_precall)
  {
    abuffer->AddEmptyString();
    return;
  }

  INktParamsEnumPtr params = hcip.Params();

  INktParamPtr param = params->GetAt(2);
  if (!valid_pointer(param))
    abuffer->AddEmptyString();
  INktParamsEnumPtr fields = param->Fields();
  std::wstring filename;
  param = fields->GetAt(0);
  if (GetStringFromHandle(filename, param))
    abuffer->AddString(filename);
  else
    abuffer->AddIntegerForceUnsigned(param->GetSizeTVal());
}

void QueryDirectoryFileCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  INktParamsEnumPtr params = hcip.Params();

  try
  {
    StringList list;

    AddFileNameFromHANDLE(params->GetAt(0), hcip);
    abuffer->AddString(get_pointer_or_null<UNICODE_STRING>(params->GetAt(9)));
    unsigned long file_info_class = params->GetAt(7)->GetULongVal();
    abuffer->AddInteger(file_info_class);
    abuffer->AddInteger(params->GetAt(10)->GetULongVal());

    INktParamPtr param = params->GetAt(5);
    if (Success() && valid_pointer(param))
      GetFileInfo(file_info_class, param, list);

    abuffer->AddInteger(list.size/2);

    for (StringList::Node *traversor = list.head; traversor; traversor = traversor->next)
    {

      switch (traversor->type)
      {
        case StringList::TYPE_STRING:
          {
            StringList::NodeString *node = (StringList::NodeString *)traversor;
            abuffer->AddString(node->str, node->size);
          }
          break;
        case StringList::TYPE_BSTR:
          {
            StringList::NodeBStr *node = (StringList::NodeBStr *)traversor;
            abuffer->AddString(node->str);
          }
          break;
      }

      
    }
  }
  catch (std::bad_alloc &)
  {
    THROW_CIPC_OUTOFMEMORY;
  }
}

void QueryAttributesFileCES::AddParamsInner(INktHookCallInfoPlugin &hcip)
{
  INktParamsEnumPtr params = hcip.Params();

  INktParamPtr param = params->GetAt(0);

  if (!valid_pointer(param))
  {
    abuffer->AddNULL();
    return;
  }

  params = param->Evaluate()->Fields();
  param = params->GetAt(2);
  UNICODE_STRING *us = get_pointer_or_null<UNICODE_STRING>(param);

  SIZE_T hFile = params->GetAt(1)->GetSizeTVal();
  if (!hFile || !AddFileNameFromHANDLE(param, hcip, 0))
  {
    std::wstring temp = canonicalize_path(us->Buffer, us->Length / sizeof(wchar_t), get_current_directory());
    abuffer->AddString(temp);
  }
}

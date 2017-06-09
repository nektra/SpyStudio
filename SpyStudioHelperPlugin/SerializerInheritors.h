#pragma once
#include "Protocol.h"
#include "CallEventSerializer.h"

class UnimplementedCallHook: public CallEventSerializer
{
  void AddResultInner(INktHookCallInfoPlugin &);
  void AddParamsInner(INktHookCallInfoPlugin &);
public:
  UnimplementedCallHook(CoalescentIPC &cipc): CallEventSerializer(cipc) {}
  bool Success() const
  {
    return 1;
  }
};

class ResultDWORD: public CallEventSerializer
{
private:
  DWORD result;

  void AddResultInner(INktHookCallInfoPlugin &);
public:
  ResultDWORD(CoalescentIPC &cipc): CallEventSerializer(cipc), result(0) {}
  virtual ~ResultDWORD() {}
  DWORD GetResult() const
  {
    return result;
  }
};

class ResultHandle: public CallEventSerializer
{
private:
  size_t result;

  void AddResultInner(INktHookCallInfoPlugin &);
public:
  ResultHandle(CoalescentIPC &cipc): CallEventSerializer(cipc), result(0) {}
  virtual ~ResultHandle() {}
  size_t GetResult() const
  {
    return result;
  }
  bool Success() const
  {
    return (result != 0);
  }
};

class ResultNTSTATUS: public ResultDWORD
{
public:
  ResultNTSTATUS(CoalescentIPC &cipc): ResultDWORD(cipc) {}
  virtual ~ResultNTSTATUS() {}
  bool Success() const
  {
    return NT_SUCCESS(GetResult());
  }
};

class ResultHRESULT: public ResultDWORD
{
public:
  ResultHRESULT(CoalescentIPC &cipc): ResultDWORD(cipc) {}
  virtual ~ResultHRESULT() {}
  bool Success() const
  {
    return SUCCEEDED(GetResult());
  }
};

class ResultBOOL: public CallEventSerializer
{
  bool result;

  void AddResultInner(INktHookCallInfoPlugin &);
public:
  ResultBOOL(CoalescentIPC &cipc): CallEventSerializer(cipc) {}
  virtual ~ResultBOOL() {}
  bool Success() const
  {
    return result;
  }
};

class ResultLPVOID: public CallEventSerializer
{
  LPVOID result;

  void AddResultInner(INktHookCallInfoPlugin &);

protected:
  LPVOID Result()
  {
    return result;
  }
public:
  ResultLPVOID(CoalescentIPC &cipc): CallEventSerializer(cipc), result(NULL) {}
  virtual ~ResultLPVOID() {}
  bool Success() const
  {
    return result != NULL;
  }
};

class ResultVOID: public CallEventSerializer
{
private:
  void AddResultInner(INktHookCallInfoPlugin &)
  {
    abuffer->AddEmptyString();
  }
public:
  ResultVOID(CoalescentIPC &cipc): CallEventSerializer(cipc) {}
  virtual ~ResultVOID() {}
  bool Success() const
  {
    return true;
  }
};

class LoadDLLCES: public ResultNTSTATUS
{
  void JustAddUserArgument(UNICODE_STRING *);
  void AddParamsInner(INktHookCallInfoPlugin &);
public:
  LoadDLLCES(CoalescentIPC &cipc): ResultNTSTATUS(cipc) {}
};

class CustomHookCallCES: public CallEventSerializer
{
  bool success;

  bool IgnoreCall(INktHookCallInfoPlugin &hcip) const;
  void AddResultInner(INktHookCallInfoPlugin &hcip);
  void AddParamsInner(INktHookCallInfoPlugin &hcip);
  bool ShouldParamsBeAdded(INktHookCallInfoPlugin &hcip) const;
  void ProcessParam(INktHookCallInfoPlugin &hcip, CustomHookParam &param, INktParamPtr params, const char *context = 0);
  void AddParamContext(const char *context, const char *type = 0);
  void AddUintParam(UINT value, const char *context);
  bool GetStringFromHandle(std::wstring &dst, SIZE_T handle);
  bool Success() const
  {
    return success;
  }
public:
  CustomHookCallCES(CoalescentIPC &cipc): CallEventSerializer(cipc) {}

#include "AddHandlerDeclarations.inl"
#include "HandlerDeclarations.inl"
};

////////////////////////////////////////////////////////////////////////////////
//                                                                            //
//                                  REGISTRY                                  //
//                                                                            //
////////////////////////////////////////////////////////////////////////////////

class RegistryCES: public ResultNTSTATUS
{
protected:
  void AddKeyName(INktParamPtr param, INktHookCallInfoPlugin &hcip);
  void AddValueName(INktParamPtr param);
  void AddKeyInfoData(LONG info_class, BYTE *data, ULONG size, INktHookCallInfoPlugin *);
  void AddValueInfoData(LONG info_class, BYTE *data, ULONG size, INktHookCallInfoPlugin &);
  void AddValueInfoValues(LONG type, const BYTE *data, ULONG size, ULONG offset);
  void AddName(const wchar_t *name, size_t n);
  bool GetStringFromHandle(std::wstring &dst, SIZE_T handle);
public:
  RegistryCES(CoalescentIPC &cipc): ResultNTSTATUS(cipc) {}
};

class OpenKeyCES: public RegistryCES
{
  void AddParamsInner(INktHookCallInfoPlugin &);
public:
  OpenKeyCES(CoalescentIPC &cipc): RegistryCES(cipc) {}
};

typedef OpenKeyCES CreateKeyCES;

class QueryKeyCES: public RegistryCES
{
  void AddParamsInner(INktHookCallInfoPlugin &);
public:
  QueryKeyCES(CoalescentIPC &cipc): RegistryCES(cipc) {}
};

class QueryValueCES: public RegistryCES
{
  void AddParamsInner(INktHookCallInfoPlugin &);
public:
  QueryValueCES(CoalescentIPC &cipc): RegistryCES(cipc) {}
};

class QueryMultipleValuesCES: public RegistryCES
{
  void AddParamsInner(INktHookCallInfoPlugin &);
public:
  QueryMultipleValuesCES(CoalescentIPC &cipc): RegistryCES(cipc) {}
};

class SetValueCES: public RegistryCES
{
  void AddParamsInner(INktHookCallInfoPlugin &);
public:
  SetValueCES(CoalescentIPC &cipc): RegistryCES(cipc) {}
};

class DeleteKeyCES: public RegistryCES
{
  void AddParamsInner(INktHookCallInfoPlugin &);
public:
  DeleteKeyCES(CoalescentIPC &cipc): RegistryCES(cipc) {}
};

class DeleteValueCES: public RegistryCES
{
  void AddParamsInner(INktHookCallInfoPlugin &);
public:
  DeleteValueCES(CoalescentIPC &cipc): RegistryCES(cipc) {}
};

class EnumerateKeyCES: public RegistryCES
{
  void AddParamsInner(INktHookCallInfoPlugin &);
public:
  EnumerateKeyCES(CoalescentIPC &cipc): RegistryCES(cipc) {}
};

class EnumerateValueKeyCES: public RegistryCES
{
  void AddParamsInner(INktHookCallInfoPlugin &);
public:
  EnumerateValueKeyCES(CoalescentIPC &cipc): RegistryCES(cipc) {}
};

class RenameKeyCES: public RegistryCES
{
  void AddParamsInner(INktHookCallInfoPlugin &);
public:
  RenameKeyCES(CoalescentIPC &cipc): RegistryCES(cipc) {}
};

////////////////////////////////////////////////////////////////////////////////
//                                                                            //
//                                   FILES                                    //
//                                                                            //
////////////////////////////////////////////////////////////////////////////////

struct StringList;

class FileCES: public ResultNTSTATUS
{
protected:
  bool GetStringFromHandle(std::wstring &dst, SIZE_T handle);
  void GetFileInfo(unsigned long file_info_class, INktParamPtr param, StringList &list);
  void StandardAddFileNameProcedure(INktHookCallInfoPlugin &hcip, INktParamPtr param, OBJECT_ATTRIBUTES *oattr, AddFileNameBehavior behavior);
public:
  FileCES(CoalescentIPC &cipc): ResultNTSTATUS(cipc) {}
};

class CreateFileCES: public FileCES
{
  void AddParamsInner(INktHookCallInfoPlugin &);
public:
  CreateFileCES(CoalescentIPC &cipc): FileCES(cipc) {}
};

class OpenFileCES: public FileCES
{
  void AddParamsInner(INktHookCallInfoPlugin &);
public:
  OpenFileCES(CoalescentIPC &cipc): FileCES(cipc) {}
};

class DeleteFileCES: public FileCES
{
  void AddParamsInner(INktHookCallInfoPlugin &);
public:
  DeleteFileCES(CoalescentIPC &cipc): FileCES(cipc) {}
};

class QueryDirectoryFileCES: public FileCES
{
  void AddParamsInner(INktHookCallInfoPlugin &);
public:
  QueryDirectoryFileCES(CoalescentIPC &cipc): FileCES(cipc) {}
};

class QueryAttributesFileCES: public FileCES
{
  void AddParamsInner(INktHookCallInfoPlugin &);
public:
  QueryAttributesFileCES(CoalescentIPC &cipc): FileCES(cipc) {}
};

////////////////////////////////////////////////////////////////////////////////
//                                                                            //
//                                  PROCESS                                   //
//                                                                            //
////////////////////////////////////////////////////////////////////////////////

class CreateProcessInternalCES: public ResultBOOL
{
  void AddParamsInner(INktHookCallInfoPlugin &);
public:
  CreateProcessInternalCES(CoalescentIPC &cipc): ResultBOOL(cipc){}
};

class AllowHashSpecialAction: public SpecialAction
{
  bool actually_perform;
  uint32_t hash;
public:
  AllowHashSpecialAction(): actually_perform(0), hash(0){}
  virtual ~AllowHashSpecialAction(){}
  virtual void SetHash(BSTR string, size_t length);
  virtual void SetHash(const wchar_t *string);
  virtual void SetHash(const char *string);
  virtual void Perform(CoalescentIPC &cipc, INktHookCallInfoPlugin &hcip);
};

template <typename CharType>
class CreateServiceCES: public CallEventSerializer
{
  bool wide;
  AllowHashSpecialAction action;

  void AddParamsInner(INktHookCallInfoPlugin &cipc)
  {
    if (!is_precall)
      return;

    INktParamsEnumPtr params = cipc.Params();
    INktParamPtr param;

    param = params->GetAt(1);

    const CharType *service_name = get_pointer_or_null<const CharType>(param);
    abuffer->AddStringMaybeAnsi(service_name);

    param = params->GetAt(7);

    const CharType *command_line = get_pointer_or_null<const CharType>(param);
    if (command_line)
    {
      abuffer->AddStringMaybeAnsi(command_line);
      action.SetHash(command_line);
    }
    else
      abuffer->AddNULL();
  }
  void AddResultInner(INktHookCallInfoPlugin &hcip)
  {
    long error = hcip.GetLastError();
    success = error == 0;
    abuffer->AddIntegerForceUnsigned(error);
  }
  bool success;
  bool Success() const
  {
    return success;
  }
public:
  CreateServiceCES(CoalescentIPC &cipc): CallEventSerializer(cipc) {}
  SpecialAction *GetSpecialAction()
  {
    return &action;
  }
};

class OpenServiceSpecialAction: public SpecialAction
{
  bool actually_perform;
  struct SizedString
  {
    DWORD length;
    wchar_t *string;
  } ss;
public:
  OpenServiceSpecialAction(): actually_perform(0)
  {
    ss.length = 0;
    ss.string = 0;
  }
  ~OpenServiceSpecialAction()
  {
    delete[] ss.string;
  }
  void SetString(const wchar_t *string);
  void SetString(const char *string);
  void Perform(CoalescentIPC &cipc, INktHookCallInfoPlugin &hcip);
};

template <typename CharType>
class OpenServiceCES: public CallEventSerializer
{
  OpenServiceSpecialAction action;

  void AddParamsInner(INktHookCallInfoPlugin &hcip)
  {
    if (!is_precall)
      return;

    INktParamsEnumPtr params = hcip.Params();
    INktParamPtr param;

    param = params->GetAt(1);

    const CharType *service_name = get_pointer_or_null<const CharType>(param);
    if (service_name)
    {
      abuffer->AddStringMaybeAnsi(service_name);
      action.SetString(service_name);
    }
    else
      abuffer->AddNULL();
  }
  void AddResultInner(INktHookCallInfoPlugin &hcip)
  {
    long error = hcip.GetLastError();
    success = error == 0;
    abuffer->AddIntegerForceUnsigned(error);
  }
  bool success;
  bool Success() const
  {
    return success;
  }
public:
  OpenServiceCES(CoalescentIPC &cipc): CallEventSerializer(cipc) {}
  virtual ~OpenServiceCES(){}
  SpecialAction *GetSpecialAction()
  {
    return &action;
  }
};

////////////////////////////////////////////////////////////////////////////////
//                                                                            //
//                                  WINDOWS                                   //
//                                                                            //
////////////////////////////////////////////////////////////////////////////////

class WindowsCES: public CallEventSerializer
{
  SIZE_T result;
protected:
  virtual void AddResultInner(INktHookCallInfoPlugin &);
  void AddClassNameFromClassAtom(INktParamPtr &, int = -1);
  virtual bool Success() const
  {
    return result != 0;
  }
  HWND GetHandleValue() const
  {
    return (HWND)result;
  }
  void AddParentClassName(INktParamPtr param);
public:
  WindowsCES(CoalescentIPC &cipc): CallEventSerializer(cipc) {}
};

auto_array_ptr<wchar_t> BetterGetClassNameW(size_t &size, HWND handle);

template <bool IsAnsi>
class CreateWindowExCES: public WindowsCES
{
  void AddParamsInner(INktHookCallInfoPlugin &);
  bool IgnoreCall(INktHookCallInfoPlugin &);
public:
  CreateWindowExCES(CoalescentIPC &cipc): WindowsCES(cipc) {}
};

typedef CreateWindowExCES<true> CreateWindowExACES;
typedef CreateWindowExCES<false> CreateWindowExWCES;

#pragma warning(push)
#pragma warning(disable: 4661)
template class CreateWindowExCES<true>;
template class CreateWindowExCES<false>;
#pragma warning(pop)

class CreateDialogIndirectCES: public WindowsCES
{
  void AddParamsInner(INktHookCallInfoPlugin &);
public:
  CreateDialogIndirectCES(CoalescentIPC &cipc): WindowsCES(cipc) {}
};

class DialogBoxIndirectCES: public CreateDialogIndirectCES
{
  INT_PTR DialogBoxIndirectCES_result;
  void AddResultInner(INktHookCallInfoPlugin &);
  bool Success() const
  {
    return DialogBoxIndirectCES_result >= 0;
  }
public:
  DialogBoxIndirectCES(CoalescentIPC &cipc): CreateDialogIndirectCES(cipc) {}
};

////////////////////////////////////////////////////////////////////////////////
//                                                                            //
//                                 RESOURCE                                   //
//                                                                            //
////////////////////////////////////////////////////////////////////////////////

class FindResourceCES: public ResultHandle
{
  void AddParamsInner(INktHookCallInfoPlugin &);
  bool Success() const
  {
    return GetResult() != 0;
  }
public:
  FindResourceCES(CoalescentIPC &cipc): ResultHandle(cipc) {}
};

class LoadResourceCES: public ResultHandle
{
  void AddParamsInner(INktHookCallInfoPlugin &);
  bool Success() const
  {
    return GetResult() != 0;
  }
public:
  LoadResourceCES(CoalescentIPC &cipc): ResultHandle(cipc) {}
};

////////////////////////////////////////////////////////////////////////////////
//                                                                            //
//                                EXCEPTION                                   //
//                                                                            //
////////////////////////////////////////////////////////////////////////////////

class ExceptionCES: public ResultNTSTATUS
{
protected:
  void AddAddressRepresentation(INktHookCallInfoPlugin &, mword_t address);
  void CommonHandler(INktHookCallInfoPlugin &, INktParamPtr er, INktParamPtr context);
public:
  ExceptionCES(CoalescentIPC &cipc): ResultNTSTATUS(cipc) {}
};

class RaiseExceptionCES: public ExceptionCES
{
  void AddParamsInner(INktHookCallInfoPlugin &);
public:
  RaiseExceptionCES(CoalescentIPC &cipc): ExceptionCES(cipc) {}
};

class RaiseHardErrorCES: public ExceptionCES
{
  void AddParamsInner(INktHookCallInfoPlugin &);
  void ParseExceptionParams(
                            unsigned code,
                            INktParamPtr countParam,
                            INktParamPtr unicodeMaskParam,
                            INktParamPtr paramArrayParam,
                            StringList &processedParams
                           );
  void ParseDefaultException(
                             unsigned paramCount,
                             INktParamPtr p,
                             StringList &processedParams,
                             unsigned unicodeMask
                            );
public:
  RaiseHardErrorCES(CoalescentIPC &cipc): ExceptionCES(cipc) {}
};

class UnhandledExceptionCES: public ExceptionCES
{
  void AddParamsInner(INktHookCallInfoPlugin &);
public:
  UnhandledExceptionCES(CoalescentIPC &cipc): ExceptionCES(cipc) {}
};

////////////////////////////////////////////////////////////////////////////////
//                                                                            //
//                                   OLE                                      //
//                                                                            //
////////////////////////////////////////////////////////////////////////////////

class CoCreateInstanceCES: public ResultHRESULT
{
  class CoCreateInstanceSpecialAction: public AllowHashSpecialAction
  {
  public:
    void SetHash(BSTR string, size_t length);
  };

  CoCreateInstanceSpecialAction action;

protected:
  bool ex;

  void AddParamsInner(INktHookCallInfoPlugin &);
public:
  CoCreateInstanceCES(CoalescentIPC &cipc): ResultHRESULT(cipc), ex(0) {}
  SpecialAction *GetSpecialAction()
  {
    return &action;
  }
};

class CoCreateInstanceExCES: public CoCreateInstanceCES
{
public:
  CoCreateInstanceExCES(CoalescentIPC &cipc): CoCreateInstanceCES(cipc)
  {
    ex = 1;
  }
};

////////////////////////////////////////////////////////////////////////////////
//                                                                            //
//                               GetClassObject                               //
//                                                                            //
////////////////////////////////////////////////////////////////////////////////

class GetClassObjectCES: public ResultHRESULT
{
  void AddParamsInner(INktHookCallInfoPlugin &);
public:
  GetClassObjectCES(CoalescentIPC &cipc): ResultHRESULT(cipc) {}
};

////////////////////////////////////////////////////////////////////////////////
//                                                                            //
//                                   WININET                                  //
//                                                                            //
////////////////////////////////////////////////////////////////////////////////

template <typename T1, typename T2>
struct aggregate_pair
{
  T1 first;
  T2 second;
};

typedef aggregate_pair<const char *, DWORD> flag_pair;

class InternetCES
{
protected:
  bool wide;
  void AddWideOrAnsi(INktParamPtr param, AcquiredBuffer *abuffer);
  template <size_t N>
  void AddFlag(AcquiredBuffer *abuffer, DWORD flag, const char *unknown, flag_pair (&pairs)[N])
  {
    AddFlag(abuffer, flag, unknown, pairs, N);
  }
  void AddFlag(AcquiredBuffer *abuffer, DWORD flag, const char *unknown, flag_pair *pairs, size_t pairs_size);
  void AddRequestHeaders(AcquiredBuffer *abuffer, INktParamPtr length_param, INktParamPtr str_param);
};

#define DEFINE_INTERNET_CLASS2(name, result, extra)        \
class name##CES: public Result##result, public InternetCES \
{                                                          \
  void AddParamsInner(INktHookCallInfoPlugin &);           \
  extra                                                    \
public:                                                    \
  name##CES(CoalescentIPC &cipc): Result##result(cipc){}   \
};                                                         \
                                                           \
class name##ACES: public name##CES                         \
{                                                          \
public:                                                    \
  name##ACES(CoalescentIPC &cipc): name##CES(cipc)         \
  {                                                        \
    wide = 0;                                              \
  }                                                        \
};                                                         \
                                                           \
class name##WCES: public name##CES                         \
{                                                          \
public:                                                    \
  name##WCES(CoalescentIPC &cipc): name##CES(cipc)         \
  {                                                        \
    wide = 1;                                              \
  }                                                        \
}

#define DEFINE_INTERNET_CLASS(name, result) DEFINE_INTERNET_CLASS2(name, ##result,)

DEFINE_INTERNET_CLASS(InternetSetStatusCallback, LPVOID);
DEFINE_INTERNET_CLASS(InternetOpenUrl, Handle);
DEFINE_INTERNET_CLASS(InternetConnect, Handle);
DEFINE_INTERNET_CLASS(HttpOpenRequest, Handle);
DEFINE_INTERNET_CLASS(HttpAddRequestHeaders, BOOL);
DEFINE_INTERNET_CLASS(HttpSendRequest, BOOL);
DEFINE_INTERNET_CLASS(HttpSendRequestEx, BOOL);
DEFINE_INTERNET_CLASS(HttpEndRequest, BOOL);
DEFINE_INTERNET_CLASS(InternetReadFile, BOOL);
DEFINE_INTERNET_CLASS(InternetReadFileEx, BOOL);
DEFINE_INTERNET_CLASS(InternetStatusCallback, VOID);

class InternetWriteFileCES: public ResultBOOL
{
  void AddParamsInner(INktHookCallInfoPlugin &);
public:
  InternetWriteFileCES(CoalescentIPC &cipc): ResultBOOL(cipc){}
};

class InternetCloseHandleCES: public ResultBOOL
{
  void AddParamsInner(INktHookCallInfoPlugin &);
public:
  InternetCloseHandleCES(CoalescentIPC &cipc): ResultBOOL(cipc){}
};


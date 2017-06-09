#include "stdafx.h"
#include "Main.h"
#include "MiscHelpers.h"
#include "SerializerInheritors.h"
#include "CommonFunctions.h"
#include "secondaryhooks.h"

DEFINE_STANDARD_HANDLER(OnUnimplementedFunctionCall,     UnimplementedCallHook)

DEFINE_STANDARD_HANDLER(OnLoadDLL,                       LoadDLLCES)

DEFINE_STANDARD_HANDLER(OnNtOpenKey,                     OpenKeyCES)
DEFINE_STANDARD_HANDLER(OnNtOpenKeyEx,                   OpenKeyCES)
DEFINE_STANDARD_HANDLER(OnNtCreateKey,                   CreateKeyCES)
DEFINE_STANDARD_HANDLER(OnNtQueryKey,                    QueryKeyCES)
DEFINE_STANDARD_HANDLER(OnNtQueryValue,                  QueryValueCES)
DEFINE_STANDARD_HANDLER(OnNtQueryMultipleValues,         QueryMultipleValuesCES)
DEFINE_STANDARD_HANDLER(OnNtSetValue,                    SetValueCES)
DEFINE_STANDARD_HANDLER(OnNtDeleteKey,                   DeleteKeyCES)
DEFINE_STANDARD_HANDLER(OnNtDeleteValue,                 DeleteValueCES)
DEFINE_STANDARD_HANDLER(OnNtEnumerateKey,                EnumerateKeyCES)
DEFINE_STANDARD_HANDLER(OnNtEnumerateValueKey,           EnumerateValueKeyCES)
DEFINE_STANDARD_HANDLER(OnNtRenameKey,                   RenameKeyCES)

DEFINE_STANDARD_HANDLER(OnNtCreateFile,                  CreateFileCES)
DEFINE_STANDARD_HANDLER(OnNtOpenFile,                    OpenFileCES)
DEFINE_STANDARD_HANDLER(OnNtDeleteFile,                  DeleteFileCES)
DEFINE_STANDARD_HANDLER(OnNtQueryDirectoryFile,          QueryDirectoryFileCES)
DEFINE_STANDARD_HANDLER(OnNtQueryAttributesFile,         QueryAttributesFileCES)

DEFINE_STANDARD_HANDLER(OnCreateProcessInternalW,        CreateProcessInternalCES)
DEFINE_STANDARD_HANDLER(OnCreateServiceA,                CreateServiceCES<char>)
DEFINE_STANDARD_HANDLER(OnCreateServiceW,                CreateServiceCES<wchar_t>)
DEFINE_STANDARD_HANDLER(OnOpenServiceA,                  OpenServiceCES<char>)
DEFINE_STANDARD_HANDLER(OnOpenServiceW,                  OpenServiceCES<wchar_t>)

DEFINE_STANDARD_HANDLER(OnCreateWindowExA,               CreateWindowExACES)
DEFINE_STANDARD_HANDLER(OnCreateWindowExW,               CreateWindowExWCES)
DEFINE_STANDARD_HANDLER(OnCreateDialogIndirectParamAorW, CreateDialogIndirectCES)
DEFINE_STANDARD_HANDLER(OnDialogBoxIndirectParamAorW,    DialogBoxIndirectCES)

DEFINE_STANDARD_HANDLER(OnFindResourceExW,               FindResourceCES)
DEFINE_STANDARD_HANDLER(OnLoadResource,                  LoadResourceCES)

DEFINE_STANDARD_HANDLER(OnNtRaiseException,              RaiseExceptionCES)
DEFINE_STANDARD_HANDLER(OnNtRaiseHardError,              RaiseHardErrorCES)
DEFINE_STANDARD_HANDLER(OnRtlUnhandledExceptionFilter2,  UnhandledExceptionCES)
DEFINE_STANDARD_HANDLER(OnUnhandledExceptionFilter,      UnhandledExceptionCES)

DEFINE_STANDARD_HANDLER(OnCoCreateInstance,              CoCreateInstanceCES)
DEFINE_STANDARD_HANDLER(OnCoCreateInstanceEx,            CoCreateInstanceExCES)

DEFINE_STANDARD_HANDLER(OnCustomHookCall,                CustomHookCallCES)

DEFINE_STANDARD_HANDLER(OnGetClassObject,                GetClassObjectCES)

DEFINE_STANDARD_HANDLER(OnInternetSetStatusCallbackA,    InternetSetStatusCallbackACES)
DEFINE_STANDARD_HANDLER(OnInternetSetStatusCallbackW,    InternetSetStatusCallbackWCES)
DEFINE_STANDARD_HANDLER(OnInternetOpenUrlA,              InternetOpenUrlACES)
DEFINE_STANDARD_HANDLER(OnInternetOpenUrlW,              InternetOpenUrlWCES)
DEFINE_STANDARD_HANDLER(OnInternetConnectA,              InternetConnectACES)
DEFINE_STANDARD_HANDLER(OnInternetConnectW,              InternetConnectWCES)
DEFINE_STANDARD_HANDLER(OnHttpOpenRequestA,              HttpOpenRequestACES)
DEFINE_STANDARD_HANDLER(OnHttpOpenRequestW,              HttpOpenRequestWCES)
DEFINE_DEBUG_HANDLER___(OnHttpAddRequestHeadersA,        HttpAddRequestHeadersACES)
DEFINE_DEBUG_HANDLER___(OnHttpAddRequestHeadersW,        HttpAddRequestHeadersWCES)
DEFINE_STANDARD_HANDLER(OnHttpSendRequestA,              HttpSendRequestACES)
DEFINE_STANDARD_HANDLER(OnHttpSendRequestW,              HttpSendRequestWCES)
DEFINE_STANDARD_HANDLER(OnHttpSendRequestExA,            HttpSendRequestExACES)
DEFINE_STANDARD_HANDLER(OnHttpSendRequestExW,            HttpSendRequestExWCES)
DEFINE_STANDARD_HANDLER(OnHttpEndRequestA,               HttpEndRequestACES)
DEFINE_STANDARD_HANDLER(OnHttpEndRequestW,               HttpEndRequestWCES)
DEFINE_STANDARD_HANDLER(OnInternetReadFile,              InternetReadFileCES)
DEFINE_STANDARD_HANDLER(OnInternetReadFileExA,           InternetReadFileExACES)
DEFINE_STANDARD_HANDLER(OnInternetReadFileExW,           InternetReadFileExWCES)
DEFINE_STANDARD_HANDLER(OnInternetWriteFile,             InternetWriteFileCES)
DEFINE_STANDARD_HANDLER(OnInternetCloseHandle,           InternetCloseHandleCES)
DEFINE_STANDARD_HANDLER(OnInternetStatusCallbackA,       InternetStatusCallbackACES)
DEFINE_STANDARD_HANDLER(OnInternetStatusCallbackW,       InternetStatusCallbackWCES)

//WARNING: Preprocessor magic. Proceed with caution.
#define SECONDARY_HOOKS_LAMBDA_N DEFINE_SECONDARYHOOK_HANDLER
#include "SecondaryHookList.h"
#undef SECONDARY_HOOKS_LAMBDA_N
